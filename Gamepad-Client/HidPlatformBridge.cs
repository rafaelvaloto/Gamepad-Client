using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static class HidPlatformBridge
{
    private const ushort SonyVendorId = 0x054C;
    private const int DescriptorSize = 532;
    private const string BluetoothGuid = "{00001124-0000-1000-8000-00805f9b34fb}";

    private enum DeviceType
    {
        DualSense = 0,
        DualSenseEdge = 1,
        DualShock4 = 2,
        NotFound = 3
    }

    private enum ConnectionType
    {
        Usb = 0,
        Bluetooth = 1,
        Unrecognized = 2
    }

    internal static int OnDetect(IntPtr descriptorsBuffer, int maxDevices)
    {
        if (descriptorsBuffer == IntPtr.Zero || maxDevices <= 0)
            return 0;

        PlatformNativeMethods.HidD_GetHidGuid(out var hidGuid);
        IntPtr deviceInfoSet = PlatformNativeMethods.SetupDiGetClassDevs(
            ref hidGuid, null, IntPtr.Zero,
            PlatformNativeMethods.DIGCF_PRESENT | PlatformNativeMethods.DIGCF_DEVICEINTERFACE);

        if (deviceInfoSet == PlatformNativeMethods.INVALID_HANDLE_VALUE)
            return 0;

        var interfaceData = new PlatformNativeMethods.SP_DEVICE_INTERFACE_DATA
        {
            cbSize = Marshal.SizeOf<PlatformNativeMethods.SP_DEVICE_INTERFACE_DATA>()
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
                        PlatformNativeMethods.GENERIC_READ | PlatformNativeMethods.GENERIC_WRITE,
                        PlatformNativeMethods.FILE_SHARE_READ | PlatformNativeMethods.FILE_SHARE_WRITE,
                        IntPtr.Zero, PlatformNativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);

                    if (handle == PlatformNativeMethods.INVALID_HANDLE_VALUE)
                        continue;

                    try
                    {
                        var attributes = new PlatformNativeMethods.HIDD_ATTRIBUTES
                        {
                            Size = Marshal.SizeOf<PlatformNativeMethods.HIDD_ATTRIBUTES>()
                        };

                        if (!PlatformNativeMethods.HidD_GetAttributes(handle, ref attributes) ||
                            attributes.VendorID != SonyVendorId)
                            continue;

                        DeviceType type = attributes.ProductID switch
                        {
                            0x05C4 or 0x09CC => DeviceType.DualShock4,
                            0x0CE6 => DeviceType.DualSense,
                            0x0DF2 => DeviceType.DualSenseEdge,
                            _ => DeviceType.NotFound
                        };

                        if (type == DeviceType.NotFound)
                            continue;

                        var descriptor = new Native.DeviceDescriptor
                        {
                            Handle = 0,
                            DeviceType = (int)type,
                            ConnectionType = path.Contains(BluetoothGuid, StringComparison.OrdinalIgnoreCase)
                                ? (int)ConnectionType.Bluetooth
                                : (int)ConnectionType.Usb,
                            IsConnected = 1,
                            Path = Native.CreatePathBytes(path)
                        };

                        IntPtr target = IntPtr.Add(descriptorsBuffer, foundDevices * DescriptorSize);
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

        var descriptor = Marshal.PtrToStructure<Native.DeviceDescriptor>(descriptorBuffer);

        string path = Native.PathToString(descriptor.Path);
        if (string.IsNullOrWhiteSpace(path))
            return false;

        IntPtr handle = PlatformNativeMethods.CreateFile(
            path,
            PlatformNativeMethods.GENERIC_READ | PlatformNativeMethods.GENERIC_WRITE,
            PlatformNativeMethods.FILE_SHARE_READ | PlatformNativeMethods.FILE_SHARE_WRITE,
            IntPtr.Zero, PlatformNativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);

        if (handle == PlatformNativeMethods.INVALID_HANDLE_VALUE)
        {
            Console.WriteLine($"CreateFile fail. Win32 error: {Marshal.GetLastWin32Error()}");
            return false;
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
            

        var success = PlatformNativeMethods.ReadFile(handle, buffer, length, out int read, IntPtr.Zero);
        
        if (bytesRead != IntPtr.Zero)
            Marshal.WriteInt32(bytesRead, read);

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

        var success = PlatformNativeMethods.WriteFile(handle, buffer, length, out int read, IntPtr.Zero);
        
        var error = success ? 0 : Marshal.GetLastWin32Error();
        var written = success ? length : 0;
        if (bytesWritten != IntPtr.Zero)
            Marshal.WriteInt32(bytesWritten, written);
        
        if (!success)
        {
            Console.WriteLine($"HidD_SetOutputReport status: {success} handle={handle}, length={length}, Win32 error={error}");
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
