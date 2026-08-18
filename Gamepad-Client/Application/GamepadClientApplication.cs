// Project: Gamepad-Client
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;

using Gamepad_Client.Infrastructure.Hid;
using Gamepad_Client.Interop.Callbacks;
namespace Gamepad_Client.Application;

internal static class GamepadClientApplication
{
    private static readonly HashSet<int> DeviceIds = [];
    private static int _nextDeviceId;

    private static readonly LogCallback Log = OnLog;
    private static readonly AllocEngineDeviceCallback Alloc = () => ++_nextDeviceId;
    private static readonly DeviceIdCallback Dispatch = id =>
    {
        DeviceIds.Add(id);
        Console.WriteLine($"Device dispatched: {id}");
    };
    private static readonly DeviceIdCallback Disconnect = id =>
    {
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
        using var stop = new ManualResetEventSlim(false);
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stop.Set();
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

            Console.WriteLine($"Loading: {libraryPath}");
            
            GamepadCoreNative.Load(libraryPath);
            GamepadCoreNative.GCH_SetLogCallback(Log);
            GamepadCoreNative.GCH_InitializePlatformBridge(Read, Write, Detect, CreateHandle, InvalidateHandle, Configure, Haptics);
            GamepadCoreNative.GCH_InitializeDeviceRegistryPolicy(0, Alloc, Dispatch, Disconnect);

            initialized = true;
            Console.WriteLine($"GamepadCoreHost {Marshal.PtrToStringAnsi(GamepadCoreNative.GCH_GetVersion())}");

            GamepadCoreNative.GCH_DiscoverDevices(2.0f);
            while (!stop.Wait(TimeSpan.FromMilliseconds(16.6)))
            {
                GamepadCoreNative.GCH_DiscoverDevices(0.0166f);
                foreach (int deviceId in DeviceIds.ToArray())
                    GamepadCoreNative.GCH_UpdateInput(deviceId, 0.0166f);

                foreach (int deviceId in DeviceIds.ToArray())
                {
                    GamepadCoreNative.GCH_GetInputState(deviceId, out InputDescriptor state);
                    Console.WriteLine($"Device {deviceId}: Left Analog X: {state.LeftAnalogX} Y: {state.LeftAnalogY}");
                    Console.WriteLine($"Device {deviceId}: Right Analog X: {state.RightAnalogX} Y: {state.RightAnalogY}");
                }
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

    private static void OnLog(int level, IntPtr message)
        => Console.WriteLine($"[Native:{level}] {Marshal.PtrToStringAnsi(message)}");

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
