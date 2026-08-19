// Project: G-Client-Sharp
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;

using G_Client_Sharp.Infrastructure.Hid;
using G_Client_Sharp.Interop.Callbacks;
namespace G_Client_Sharp.Application;

internal static class GamepadClientApplication
{
    private const string DefaultLibraryRelativePath = @"CLionProjects\Gamepad-Core-Host\cmake-build-debug\GamepadCoreHost.dll";
    private const int SonyVendorId = 0x054C;
    private static readonly Guid BluetoothGuid = new("00001124-0000-1000-8000-00805f9b34fb");
    private static readonly object DeviceIdsSync = new();
    private static readonly HashSet<int> DeviceIds = [];
    private static readonly Dictionary<int, InputDescriptor> PreviousInputStates = [];
    private static int _nextDeviceId;

    private static readonly byte[] GallopingTrigger = [
        0x23, 0x82, 0x00, 0xf7, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00
    ];
    private static readonly byte[] MachineTrigger = [
        0x27, 0x80, 0x02, 0x3a, 0x0a, 0x04, 0x00, 0x00, 0x00, 0x00
    ];
    private static readonly byte[] FeedbackTrigger = [
        0x21, 0xfe, 0x03, 0xf8, 0xff, 0xff, 0x3f, 0x00, 0x00, 0x00
    ];
    private static readonly byte[] WeaponTrigger = [
        0x25, 0x08, 0x01, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];
    private static readonly byte[] BowTrigger = [
        0x22, 0x02, 0x01, 0x3f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    ];
    private static readonly byte[] AutomaticGunTrigger = [
        0x26, 0x00, 0x03, 0x00, 0x00, 0x00, 0x3f, 0x00, 0x00, 0x0a
    ];

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
        CommandLineOptions options = ParseCommandLine(args);
        PrintApplicationBanner();

        if (options.ShowHelp)
        {
            PrintUsage();
            return;
        }

        if (!options.RunClient)
        {
            PrintLibraryPath(options.LibraryPath);
            Console.WriteLine("[INFO] Using the default DLL path. Specify another path with: --dll or -d <path>");
            Console.WriteLine("[TIP] Use '--info' to view ctypes memory structures.");
            return;
        }

        if (options.ShowInfo)
            PrintTypeInformation();

        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            stop.Cancel();
        };

        var initialized = false;
        try
        {
            PrintLibraryPath(options.LibraryPath);
            GamepadCoreNative.Load(options.LibraryPath);
            Console.WriteLine("[+] DLL loaded successfully!");
            Console.WriteLine("[+] Running the initial device discovery cycle...");
            
            GamepadCoreNative.GCH_SetLogCallback(Log);
            GamepadCoreNative.GCH_InitializePlatformBridge(Read, Write, Detect, CreateHandle, InvalidateHandle, Configure, Haptics);
            GamepadCoreNative.GCH_InitializeDeviceRegistryPolicy(0, Alloc, Dispatch, Disconnect);

            initialized = true;
            Console.WriteLine();
            Console.WriteLine("[INFO] Starting device discovery loop (interval: 0.016s).");
            Console.WriteLine("[INFO] Press Ctrl+C to stop monitoring.");
            Console.WriteLine();
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

    private static CommandLineOptions ParseCommandLine(string[] args)
    {
        string defaultLibraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            DefaultLibraryRelativePath);

        if (args.Length == 0)
            return new CommandLineOptions(defaultLibraryPath, false, false, false);

        string? libraryPath = null;
        bool showInfo = false;

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--help":
                case "-h":
                    return new CommandLineOptions(defaultLibraryPath, false, false, true);

                case "--info":
                    showInfo = true;
                    break;

                case "--dll":
                case "-d":
                    if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
                        throw new ArgumentException($"Argument '{args[index - 1]}' requires a DLL path.");

                    libraryPath = args[index];
                    break;

                default:
                    if (args[index].StartsWith('-'))
                        throw new ArgumentException($"Unknown argument: {args[index]}");

                    if (libraryPath is not null)
                        throw new ArgumentException($"Unexpected argument: {args[index]}");

                    libraryPath = args[index];
                    break;
            }
        }

        return new CommandLineOptions(
            Path.GetFullPath(libraryPath ?? defaultLibraryPath),
            showInfo,
            true,
            false);
    }

    private static void PrintApplicationBanner()
    {
        Console.WriteLine("""
            ======================================================================
                   Gamepad Core Host - CSharp Client (CLI)
            ======================================================================
            """);
    }

    private static void PrintLibraryPath(string libraryPath)
    {
        Console.WriteLine();
        Console.WriteLine("[+] Loading DLL from:");
        Console.WriteLine($"    {libraryPath}");
    }

    private static void PrintTypeInformation()
    {
        Console.WriteLine("""

            --- API Type and Descriptor Information ---
            """);
        Console.WriteLine($"VendorId:               0x{SonyVendorId:X4} ({SonyVendorId})");
        Console.WriteLine($"BluetoothGuid:          {{{BluetoothGuid:D}}}");
        Console.WriteLine("""
            DescriptorSize:         532 bytes
            InputDescriptorSize:    148 bytes

            Device Types:
              - DualSense = 0
              - DualSenseEdge = 1
              - DualShock4 = 2
              - NotFound = 3

            Connection Types:
              - Usb = 0
              - Bluetooth = 1
              - Unrecognized = 2

            DeviceDescriptor Structure:
              - Memory size: 532 bytes
                * Handle               (offset   0, type UInt64)
                * DeviceType           (offset   8, type Int32)
                * ConnectionType       (offset  12, type Int32)
                * IsConnected          (offset  16, type Int32)
                * Path                 (offset  20, type Byte[512])

            InputDescriptor Structure:
              - Memory size: 148 bytes
              - Total mapped fields: 63
            ======================================================================
            """);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""

            Usage:
              G-Client-Sharp [--dll|-d <path>] [--info]

            Without arguments, displays the default DLL location and usage hints.
            """);
    }

    private sealed record CommandLineOptions(
        string LibraryPath,
        bool ShowInfo,
        bool RunClient,
        bool ShowHelp);

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
            SetBasicOutput(deviceId, 64, 0, 255, 0, 0, "Cross -> Heavy Rumble + RED Light");

        if (IsPressed(state.Circle, previousState.Circle))
            SetBasicOutput(deviceId, 0, 64, 255, 255, 0, "Circle -> Soft Rumble + YELLOW Light");

        if (IsPressed(state.Square, previousState.Square))
            Console.WriteLine($"Device {deviceId}: Square -> Trigger Effect: GAMECUBE (R2)");

        if (IsPressed(state.Triangle, previousState.Triangle))
        {
            StopTriggers(deviceId);
            SetBasicOutput(deviceId, 0, 0, 0, 0, 255, "Triangle -> Stop All");
        }

        // [ D-PADS & SHOULDERS ]
        if (IsPressed(state.LeftShoulder, previousState.LeftShoulder))
            SetTriggerEffect(deviceId, GallopingTrigger, 0, "L1 -> Trigger Effect: Gallop (L2)");

        if (IsPressed(state.RightShoulder, previousState.RightShoulder))
            SetTriggerEffect(deviceId, MachineTrigger, 1, "R1 -> Trigger Effect: Machine (R2)");

        if (IsPressed(state.DpadUp, previousState.DpadUp))
            SetTriggerEffect(deviceId, FeedbackTrigger, 1, "UP -> Trigger Effect: Feedback (Rigid) (R2)");

        if (IsPressed(state.DpadDown, previousState.DpadDown))
            SetTriggerEffect(deviceId, BowTrigger, 1, "DOWN -> Trigger Effect: Bow (Tension) (R2)");

        if (IsPressed(state.DpadLeft, previousState.DpadLeft))
            SetTriggerEffect(deviceId, WeaponTrigger, 1, "LEFT -> Trigger Effect: Weapon (Semi) (R2)");

        if (IsPressed(state.DpadRight, previousState.DpadRight))
            SetTriggerEffect(deviceId, AutomaticGunTrigger, 1, "RIGHT -> Trigger Effect: Automatic Gun (Buzz) (R2)");

        PreviousInputStates[deviceId] = state;
    }

    private static bool IsPressed(byte current, byte previous)
        => current != 0 && previous == 0;

    private static void SetTriggerEffect(int deviceId, byte[] buffer, int hand, string description)
    {
        bool applied = GamepadCoreNative.GCH_CustomTrigger(deviceId, buffer, buffer.Length, hand);
        GamepadCoreNative.GCH_UpdateOutput(deviceId);
        Console.WriteLine($"Device {deviceId}: {description} [{(applied ? "applied" : "failed")}]");
    }

    private static void StopTriggers(int deviceId)
    {
        GamepadCoreNative.GCH_StopTrigger(deviceId, 0);
        GamepadCoreNative.GCH_StopTrigger(deviceId, 1);
        GamepadCoreNative.GCH_UpdateOutput(deviceId);
    }

    private static void SetBasicOutput(
        int deviceId,
        byte leftRumble,
        byte rightRumble,
        byte red,
        byte green,
        byte blue,
        string description)
    {
        GamepadCoreNative.GCH_SetVibration(deviceId, leftRumble, rightRumble);
        GamepadCoreNative.GCH_Lightbar(deviceId, red, green, blue);
        GamepadCoreNative.GCH_UpdateOutput(deviceId);
        Console.WriteLine($"Device {deviceId}: {description}");
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
