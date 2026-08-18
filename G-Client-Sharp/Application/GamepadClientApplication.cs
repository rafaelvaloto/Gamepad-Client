// Project: G-Client-Sharp
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;

using G_Client_Sharp.Infrastructure.Hid;
using G_Client_Sharp.Interop.Callbacks;
namespace G_Client_Sharp.Application;

internal static class GamepadClientApplication
{
    private static readonly object DeviceIdsSync = new();
    private static readonly HashSet<int> DeviceIds = [];
    private static int _nextDeviceId;

    private static readonly LogCallback Log = OnLog;
    private static readonly AllocEngineDeviceCallback Alloc = AllocateDeviceId;
    private static readonly DeviceIdCallback Dispatch = id =>
    {
        HidPlatformBridge.ConfirmDeviceConnected(id);
        lock (DeviceIdsSync)
            DeviceIds.Add(id);
        Console.WriteLine($"Device dispatched: {id}");
    };
    private static readonly DeviceIdCallback Disconnect = id =>
    {
        HidPlatformBridge.RemoveDevice(id);
        lock (DeviceIdsSync)
            DeviceIds.Remove(id);
        Console.WriteLine($"Device disconnected: {id}");
    };

    private static readonly PlatformDetectCallback Detect = OnDetect;
    private static readonly PlatformReadCallback Read = OnRead;
    private static readonly PlatformWriteCallback Write = OnWrite;
    private static readonly PlatformCreateHandleCallback CreateHandle = OnCreateHandle;
    private static readonly PlatformInvalidateHandleCallback InvalidateHandle = OnInvalidateHandle;
    private static readonly PlatformConfigureFeaturesCallback Configure = (_, _, _, _) => { };
    private static readonly PlatformProcessAudioHapticsCallback Haptics = (_, _, _, _) => { };

    internal static void Run(string[] args)
    {
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stop.Cancel();
        };

        var initialized = false;
        try
        {
            if (args.Length <= 0)
            {
                throw new ArgumentException("Library path must be provided");
            }

            // dll path
            string libraryPath = Path.GetFullPath(args[0]);
            GamepadCoreNative.Load(libraryPath);

            Console.WriteLine($"Loading: {libraryPath}");
            Console.WriteLine($"GamepadCoreHost: {GamepadCoreNative.GetVersion()}");
            
            GamepadCoreNative.GCH_SetLogCallback(Log);
            GamepadCoreNative.GCH_InitializePlatformBridge(Read, Write, Detect, CreateHandle, InvalidateHandle, Configure, Haptics);
            GamepadCoreNative.GCH_InitializeDeviceRegistryPolicy(0, Alloc, Dispatch, Disconnect);

            initialized = true;
            Task discoveryTask = Task.Run(() => RunDiscoveryLoop(stop.Token), stop.Token);

            try
            {
                RunInputLoop(stop.Token);
            }
            finally
            {
                stop.Cancel();
                discoveryTask.GetAwaiter().GetResult();
            }
        }
        finally
        {
            if (initialized)
            {
                GamepadCoreNative.GCH_Shutdown();
                Console.WriteLine("GamepadCoreHost GCH_Shutdown.");
            }
        }
    }

    private static void RunDiscoveryLoop(CancellationToken cancellationToken)
    {
        GamepadCoreNative.GCH_DiscoverDevices(2.0f); // immediately request devices
        while (!cancellationToken.IsCancellationRequested)
        {
            GamepadCoreNative.GCH_DiscoverDevices(0.0166f);
            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(16.6));
        }
    }

    private static void RunInputLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            int[] deviceIds;
            lock (DeviceIdsSync)
                deviceIds = DeviceIds.ToArray();

            foreach (int deviceId in deviceIds)
                GamepadCoreNative.GCH_UpdateInput(deviceId, 0.0166f);

            foreach (int deviceId in deviceIds)
            {
                GamepadCoreNative.GCH_GetInputState(deviceId, out InputDescriptor state);
                // Console.WriteLine($"Device {deviceId}: Left Analog X: {state.LeftAnalogX} Y: {state.LeftAnalogY}");
                // Console.WriteLine($"Device {deviceId}: Right Analog X: {state.RightAnalogX} Y: {state.RightAnalogY}");
            }

            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(16.6));
        }
    }

    private static void OnLog(int level, IntPtr message)
        => Console.WriteLine($"[Native:{level}] {Marshal.PtrToStringAnsi(message)}");

    private static int AllocateDeviceId()
    {
        int deviceId = ++_nextDeviceId;
        HidPlatformBridge.BindNextPathToDevice(deviceId);
        return deviceId;
    }

    private static int OnDetect(IntPtr devices, int maxDevices)
        => HidPlatformBridge.OnDetect(devices, maxDevices);

    private static bool OnRead(IntPtr handle, IntPtr buffer, int length, IntPtr bytesRead)
        => HidPlatformBridge.Read(handle, buffer, length, bytesRead);

    private static bool OnWrite(IntPtr handle, IntPtr buffer, int length, IntPtr bytesWritten)
        => HidPlatformBridge.Write(handle, buffer, length, bytesWritten);

    private static bool OnCreateHandle(IntPtr descriptor)
        => HidPlatformBridge.CreateHandle(descriptor);

    private static void OnInvalidateHandle(ulong handle)
        => HidPlatformBridge.InvalidateHandle(handle);
}
