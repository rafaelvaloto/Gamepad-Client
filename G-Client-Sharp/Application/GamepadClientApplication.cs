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
    private static readonly Dictionary<int, InputDescriptor> PreviousInputStates = [];
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
        PreviousInputStates.Remove(id);
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
            PrintStartupBanner();
            
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

    private static void PrintStartupBanner()
    {
        Console.WriteLine("""
            =======================================================
                       DUALSENSE INTEGRATION TEST
            =======================================================

             [ FACE BUTTONS ]
               (X) Cross    : Heavy Rumble + RED Light
               (O) Circle   : Soft Rumble  + YELLOW Light
               [ ] Square   : Trigger Effect: GAMECUBE (R2)
               /_\ Triangle : Stop All

            -------------------------------------------------------

             [ D-PADS & SHOULDERS ]
               [L1]    : Trigger Effect: Gallop (L2)
               [R1]    : Trigger Effect: Machine (R2)
               [UP]    : Trigger Effect: Feedback (Rigid)
               [DOWN]  : Trigger Effect: Bow (Tension)
               [LEFT]  : Trigger Effect: Weapon (Semi)
               [RIGHT] : Trigger Effect: Automatic Gun (Buzz)

            =======================================================
            """);
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
                HandleInput(deviceId, state);
            }

            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(16.6));
        }
    }

    private static void HandleInput(int deviceId, InputDescriptor state)
    {
        PreviousInputStates.TryGetValue(deviceId, out InputDescriptor previousState);

        // [ FACE BUTTONS ]
        if (IsPressed(state.Cross, previousState.Cross))
            Console.WriteLine($"Device {deviceId}: Cross -> Heavy Rumble + RED Light");

        if (IsPressed(state.Circle, previousState.Circle))
            Console.WriteLine($"Device {deviceId}: Circle -> Soft Rumble + YELLOW Light");

        if (IsPressed(state.Square, previousState.Square))
            Console.WriteLine($"Device {deviceId}: Square -> Trigger Effect: GAMECUBE (R2)");

        if (IsPressed(state.Triangle, previousState.Triangle))
            Console.WriteLine($"Device {deviceId}: Triangle -> Stop All");

        // [ D-PADS & SHOULDERS ]
        if (IsPressed(state.LeftShoulder, previousState.LeftShoulder))
            Console.WriteLine($"Device {deviceId}: L1 -> Trigger Effect: Gallop (L2)");

        if (IsPressed(state.RightShoulder, previousState.RightShoulder))
            Console.WriteLine($"Device {deviceId}: R1 -> Trigger Effect: Machine (R2)");

        if (IsPressed(state.DpadUp, previousState.DpadUp))
            Console.WriteLine($"Device {deviceId}: UP -> Trigger Effect: Feedback (Rigid)");

        if (IsPressed(state.DpadDown, previousState.DpadDown))
            Console.WriteLine($"Device {deviceId}: DOWN -> Trigger Effect: Bow (Tension)");

        if (IsPressed(state.DpadLeft, previousState.DpadLeft))
            Console.WriteLine($"Device {deviceId}: LEFT -> Trigger Effect: Weapon (Semi)");

        if (IsPressed(state.DpadRight, previousState.DpadRight))
            Console.WriteLine($"Device {deviceId}: RIGHT -> Trigger Effect: Automatic Gun (Buzz)");

        PreviousInputStates[deviceId] = state;
    }

    private static bool IsPressed(byte current, byte previous)
        => current != 0 && previous == 0;

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
