using System.Diagnostics;
// Project: G-Client-Sharp
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;
using System.Text;

using G_Client_Sharp.Interop.Callbacks;
using G_Client_Sharp.Interop.Windows;

namespace G_Client_Sharp.Infrastructure.Hid;

internal static class HidPlatformBridge
{
    private static readonly object DevicePathSync = new();
    private static readonly HashSet<string> KnownPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Queue<string> PendingPaths = new();
    private static readonly Dictionary<int, string> DevicePaths = [];

    internal static bool IsPathKnown(string path)
    {
        lock (DevicePathSync)
            return KnownPaths.Contains(path);
    }

    internal static void BindNextPathToDevice(int deviceId)
    {
        lock (DevicePathSync)
        {
            if (PendingPaths.Count > 0)
                DevicePaths[deviceId] = PendingPaths.Dequeue();
        }
    }

    internal static void ConfirmDeviceConnected(int deviceId)
    {
        lock (DevicePathSync)
        {
            // Some host implementations reuse an ID after reconnecting and
            // dispatch it without invoking AllocEngineDevice again.
            if (!DevicePaths.ContainsKey(deviceId) && PendingPaths.Count > 0)
                DevicePaths[deviceId] = PendingPaths.Dequeue();

            if (DevicePaths.TryGetValue(deviceId, out string? path))
                Console.WriteLine($"Device connected: {deviceId}, Path={path}");
        }
    }

    internal static void RemoveDevice(int deviceId)
    {
        lock (DevicePathSync)
        {
            if (DevicePaths.Remove(deviceId, out string? path))
                KnownPaths.Remove(path);
        }
    }

    internal static int OnDetect(IntPtr descriptorsBuffer, int maxDevices)
    {
        if (descriptorsBuffer == IntPtr.Zero || maxDevices <= 0)
            return 0;

        PlatformNativeMethods.HidD_GetHidGuid(out var hidGuid);
        IntPtr deviceInfoSet = PlatformNativeMethods.SetupDiGetClassDevs(
            ref hidGuid, null, IntPtr.Zero,
            WindowsHidConstants.DigcfPresent | WindowsHidConstants.DigcfDeviceInterface);

        if (deviceInfoSet == WindowsHidConstants.InvalidHandleValue)
            return 0;

        var interfaceData = new SpDeviceInterfaceData
        {
            CbSize = Marshal.SizeOf<SpDeviceInterfaceData>()
        };

        var foundDevices = 0;
        try
        {
            for (uint index = 0; foundDevices < maxDevices; index++)
            {
                if (!PlatformNativeMethods.SetupDiEnumDeviceInterfaces(
                        deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData))
                    break;

                PlatformNativeMethods.SetupDiGetDeviceInterfaceDetail(
                    deviceInfoSet, ref interfaceData, IntPtr.Zero, 0,
                    out uint requiredSize, IntPtr.Zero);

                if (requiredSize == 0)
                    continue;

                IntPtr detail = Marshal.AllocHGlobal((int)requiredSize);
                try
                {
                    // cbSize of SP_DEVICE_INTERFACE_DETAIL_DATA is 8 on 64-bit Windows.
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);

                    if (!PlatformNativeMethods.SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet, ref interfaceData, detail, requiredSize,
                            out _, IntPtr.Zero))
                        continue;

                    string? path = Marshal.PtrToStringUni(IntPtr.Add(detail, 4));
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    IntPtr handle = PlatformNativeMethods.CreateFile(
                        path,
                        WindowsHidConstants.GenericRead | WindowsHidConstants.GenericWrite,
                        WindowsHidConstants.FileShareRead | WindowsHidConstants.FileShareWrite,
                        IntPtr.Zero, WindowsHidConstants.OpenExisting, 0, IntPtr.Zero);

                    if (handle == WindowsHidConstants.InvalidHandleValue)
                        continue;

                    try
                    {
                        var attributes = new HidAttributes
                        {
                            Size = Marshal.SizeOf<HidAttributes>()
                        };

                        if (!PlatformNativeMethods.HidD_GetAttributes(handle, ref attributes) ||
                            attributes.VendorId != SonyHidDeviceCatalog.VendorId)
                            continue;

                        var type = attributes.ProductId switch
                        {
                            0x05C4 or 0x09CC => SonyHidDeviceCatalog.DeviceType.DualShock4,
                            0x0CE6 => SonyHidDeviceCatalog.DeviceType.DualSense,
                            0x0DF2 => SonyHidDeviceCatalog.DeviceType.DualSenseEdge,
                            _ => SonyHidDeviceCatalog.DeviceType.NotFound
                        };

                        if (type == SonyHidDeviceCatalog.DeviceType.NotFound)
                            continue;

                        var descriptor = new DeviceDescriptor
                        {
                            Handle = 0,
                            DeviceType = (int)type,
                            ConnectionType = path.Contains(SonyHidDeviceCatalog.BluetoothGuid, StringComparison.OrdinalIgnoreCase)
                                ? (int)SonyHidDeviceCatalog.ConnectionType.Bluetooth
                                : (int)SonyHidDeviceCatalog.ConnectionType.Usb,
                            IsConnected = 1,
                            Path = GamepadCoreNative.CreatePathBytes(path)
                        };

                        IntPtr target = IntPtr.Add(descriptorsBuffer, foundDevices * SonyHidDeviceCatalog.DescriptorSize);
                        Marshal.StructureToPtr(descriptor, target, false);
                        foundDevices++;
                    }
                    finally
                    {
                        PlatformNativeMethods.CloseHandle(handle);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detail);
                }
            }
        }
        finally
        {
            PlatformNativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return foundDevices;
    }

    internal static bool CreateHandle(IntPtr descriptorBuffer)
    {
        if (descriptorBuffer == IntPtr.Zero)
            return false;

        var descriptor = Marshal.PtrToStructure<DeviceDescriptor>(descriptorBuffer);

        string path = GamepadCoreNative.PathToString(descriptor.Path);
        if (string.IsNullOrWhiteSpace(path))
            return false;

        IntPtr handle = PlatformNativeMethods.CreateFile(
            path,
            WindowsHidConstants.GenericRead | WindowsHidConstants.GenericWrite,
            WindowsHidConstants.FileShareRead | WindowsHidConstants.FileShareWrite,
            IntPtr.Zero, WindowsHidConstants.OpenExisting, 0, IntPtr.Zero);

        if (handle == WindowsHidConstants.InvalidHandleValue)
        {
            Console.WriteLine($"CreateFile fail. Win32 error: {Marshal.GetLastWin32Error()}");
            return false;
        }

        lock (DevicePathSync)
        {
            if (!KnownPaths.Add(path))
            {
                PlatformNativeMethods.CloseHandle(handle);
                return false;
            }

            PendingPaths.Enqueue(path);
        }

        descriptor.Handle = unchecked((ulong)handle.ToInt64());
        descriptor.IsConnected = 1;
        Marshal.StructureToPtr(descriptor, descriptorBuffer, false);
        Console.WriteLine($"CreateHandle: Handle={descriptor.Handle}, Path={path}");
        
        return true;
    }

    internal static bool Read(IntPtr handle, IntPtr buffer, int length, IntPtr bytesRead)
    {
        if (handle == IntPtr.Zero)
        {
            Console.WriteLine("Invalid handle provided for Read operation.");
            return false;
        }

        bool success = PlatformNativeMethods.ReadFile(handle, buffer, length, out int read, IntPtr.Zero);
        if (bytesRead != IntPtr.Zero)
        {
            Marshal.WriteInt32(bytesRead, success ? length : 0);
        }

        if (!success)
        {
            Console.WriteLine($"ReadFile handle status: {success} handle={handle}, length={length}, Win32 error={Marshal.GetLastWin32Error()}");    
        }
        
        // byte reportId = buffer == IntPtr.Zero ? (byte)0 : Marshal.ReadByte(buffer);
        // Console.WriteLine($"HID input report: id=0x{reportId:X2}, length={length}");
        
        return success;
    }

    internal static bool Write(IntPtr handle, IntPtr buffer, int length, IntPtr bytesWritten)
    {
        // byte reportId = buffer == IntPtr.Zero ? (byte)0 : Marshal.ReadByte(buffer);
        // Console.WriteLine($"HID output report: id=0x{reportId:X2}, length={length}");
        
        if (handle == IntPtr.Zero)
        {
            Console.WriteLine("Invalid handle provided for Write operation.");
            return false;
        }

        bool success = PlatformNativeMethods.WriteFile(handle, buffer, length, out int read, IntPtr.Zero);
        if (bytesWritten != IntPtr.Zero)
        {
            Marshal.WriteInt32(bytesWritten, success ? length : 0);
        }
        
        if (!success)
        {
            Console.WriteLine($"HidD_SetOutputReport status: {success} handle={handle}, length={length}, Win32 error={Marshal.GetLastWin32Error()}");
        }
        
        return success;
    }

    internal static void InvalidateHandle(ulong handle)
    {
        if (TryGetHandle(handle))
            PlatformNativeMethods.CloseHandle(new IntPtr(unchecked((long)handle)));
    }

    private static bool TryGetHandle(ulong handle)
        => handle != 0 && handle != unchecked((ulong)-1);
}
