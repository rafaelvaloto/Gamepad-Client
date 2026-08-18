using System.Runtime.InteropServices;
using System.Text;

internal static class Native
{
    private const string Library = "GamepadCoreHost";
    internal const string LibraryPath =
        @"C:\Users\rafae\CLionProjects\Gamepad-Core-Unity\cmake-build-debug\GamepadCoreHost.dll";

    internal static void Load(string libraryPath)
    {
        if (!File.Exists(libraryPath))
            throw new FileNotFoundException("GamepadCoreHost DLL não encontrada.", libraryPath);

        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly,
            (libraryName, _, _) => libraryName == Library
                ? NativeLibrary.Load(libraryPath)
                : IntPtr.Zero);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void LogCallback(int level, IntPtr message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int AllocEngineDeviceCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DeviceIdCallback(int deviceId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int PlatformDetectCallback(IntPtr devices, int maxDevices);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal delegate bool PlatformReadCallback(IntPtr handle, IntPtr buffer, int length, IntPtr bytesRead);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal delegate bool PlatformWriteCallback(IntPtr handle, IntPtr buffer, int length, IntPtr bytesWritten);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal delegate bool PlatformCreateHandleCallback(IntPtr device);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void PlatformInvalidateHandleCallback(ulong handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void PlatformConfigureFeaturesCallback(ulong handle, IntPtr buffer, int length, IntPtr bytes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void PlatformProcessAudioHapticsCallback(ulong handle, IntPtr buffer, int length, IntPtr bytes);

    [StructLayout(LayoutKind.Sequential)]
    internal struct DeviceDescriptor
    {
        internal ulong Handle;
        internal int DeviceType;
        internal int ConnectionType;
        internal int IsConnected;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        internal byte[] Path;
    }

    [StructLayout(LayoutKind.Explicit, Size = 148)]
    internal struct InputDescriptor
    {
        [FieldOffset(0)] internal float AnalogDeadZone;
        [FieldOffset(4)] internal float LeftAnalogX;
        [FieldOffset(8)] internal float LeftAnalogY;
        [FieldOffset(12)] internal float RightAnalogX;
        [FieldOffset(16)] internal float RightAnalogY;
        [FieldOffset(20)] internal float LeftTriggerAnalog;
        [FieldOffset(24)] internal float RightTriggerAnalog;
        [FieldOffset(28)] internal float GyroscopeX;
        [FieldOffset(32)] internal float GyroscopeY;
        [FieldOffset(36)] internal float GyroscopeZ;
        [FieldOffset(40)] internal float AccelerometerX;
        [FieldOffset(44)] internal float AccelerometerY;
        [FieldOffset(48)] internal float AccelerometerZ;
        [FieldOffset(52)] internal float GravityX;
        [FieldOffset(56)] internal float GravityY;
        [FieldOffset(60)] internal float GravityZ;
        [FieldOffset(64)] internal float TiltX;
        [FieldOffset(68)] internal float TiltY;
        [FieldOffset(72)] internal float TiltZ;
        [FieldOffset(76)] internal int TouchId;
        [FieldOffset(80)] internal int TouchFingerCount;
        [FieldOffset(84)] internal byte DirectionRaw;
        [FieldOffset(85)] internal byte IsTouching;
        [FieldOffset(88)] internal float TouchRadiusX;
        [FieldOffset(92)] internal float TouchRadiusY;
        [FieldOffset(96)] internal float TouchPositionX;
        [FieldOffset(100)] internal float TouchPositionY;
        [FieldOffset(104)] internal float TouchRelativeX;
        [FieldOffset(108)] internal float TouchRelativeY;
        [FieldOffset(112)] internal byte Cross;
        [FieldOffset(113)] internal byte Square;
        [FieldOffset(114)] internal byte Triangle;
        [FieldOffset(115)] internal byte Circle;
        [FieldOffset(116)] internal byte DpadUp;
        [FieldOffset(117)] internal byte DpadDown;
        [FieldOffset(118)] internal byte DpadLeft;
        [FieldOffset(119)] internal byte DpadRight;
        [FieldOffset(120)] internal byte LeftAnalogRight;
        [FieldOffset(121)] internal byte LeftAnalogUp;
        [FieldOffset(122)] internal byte LeftAnalogDown;
        [FieldOffset(123)] internal byte LeftAnalogLeft;
        [FieldOffset(124)] internal byte RightAnalogLeft;
        [FieldOffset(125)] internal byte RightAnalogDown;
        [FieldOffset(126)] internal byte RightAnalogUp;
        [FieldOffset(127)] internal byte RightAnalogRight;
        [FieldOffset(128)] internal byte LeftTriggerThreshold;
        [FieldOffset(129)] internal byte RightTriggerThreshold;
        [FieldOffset(130)] internal byte LeftShoulder;
        [FieldOffset(131)] internal byte RightShoulder;
        [FieldOffset(132)] internal byte LeftStick;
        [FieldOffset(133)] internal byte RightStick;
        [FieldOffset(134)] internal byte PSButton;
        [FieldOffset(135)] internal byte Share;
        [FieldOffset(136)] internal byte Start;
        [FieldOffset(137)] internal byte Touch;
        [FieldOffset(138)] internal byte Mute;
        [FieldOffset(139)] internal byte HasPhoneConnected;
        [FieldOffset(140)] internal byte Fn1;
        [FieldOffset(141)] internal byte Fn2;
        [FieldOffset(142)] internal byte PaddleLeft;
        [FieldOffset(143)] internal byte PaddleRight;
        [FieldOffset(144)] internal float BatteryLevel;
    }

    internal static byte[] CreatePathBytes(string path)
    {
        var result = new byte[512];
        byte[] bytes = Encoding.ASCII.GetBytes(path);
        Array.Copy(bytes, result, Math.Min(bytes.Length, result.Length - 1));
        return result;
    }

    internal static string PathToString(byte[]? path)
    {
        if (path is null)
            return string.Empty;

        int length = Array.IndexOf(path, (byte)0);
        if (length < 0)
            length = path.Length;
        return Encoding.ASCII.GetString(path, 0, length);
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr GCH_GetVersion();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_SetLogCallback(LogCallback callback);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_InitializeDeviceRegistryPolicy(
        int typeId, AllocEngineDeviceCallback allocCallback,
        DeviceIdCallback dispatchCallback, DeviceIdCallback disconnectCallback);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_InitializePlatformBridge(
        PlatformReadCallback readCallback, PlatformWriteCallback writeCallback,
        PlatformDetectCallback detectCallback, PlatformCreateHandleCallback createHandleCallback,
        PlatformInvalidateHandleCallback invalidateHandleCallback,
        PlatformConfigureFeaturesCallback configureFeaturesCallback,
        PlatformProcessAudioHapticsCallback processAudioHapticsCallback);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_DiscoverDevices(float deltaTime);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_UpdateInput(int deviceId, float deltaTime);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool GCH_GetInputState(int deviceId, out InputDescriptor inputState);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_Shutdown();
}

internal static class Program
{
    private static readonly HashSet<int> DeviceIds = [];
    private static int _nextDeviceId;

    // Delegates.
    private static readonly Native.LogCallback Log = OnLog;
    private static readonly Native.AllocEngineDeviceCallback Alloc = () => ++_nextDeviceId;
    private static readonly Native.DeviceIdCallback Dispatch = id =>
    {
        DeviceIds.Add(id);
        Console.WriteLine($"Device dispatched: {id}");
    };
    private static readonly Native.DeviceIdCallback Disconnect = id =>
    {
        DeviceIds.Remove(id);
        Console.WriteLine($"Device disconnected: {id}");
    };
    
    // Delegates Platforms.
    private static readonly Native.PlatformDetectCallback Detect = OnDetect;
    private static readonly Native.PlatformReadCallback Read = OnRead;
    private static readonly Native.PlatformWriteCallback Write = OnWrite;
    private static readonly Native.PlatformCreateHandleCallback CreateHandle = OnCreateHandle;
    private static readonly Native.PlatformInvalidateHandleCallback InvalidateHandle = OnInvalidateHandle;
    private static readonly Native.PlatformConfigureFeaturesCallback Configure = (_, _, _, _) => { };
    private static readonly Native.PlatformProcessAudioHapticsCallback Haptics = (_, _, _, _) => { };

    private static void Main(string[] args)
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
            string libraryPath = args.Length > 0
                ? Path.GetFullPath(args[0])
                : Native.LibraryPath;

            Console.WriteLine($"Carregando: {libraryPath}");
            Native.Load(libraryPath);
            Native.GCH_SetLogCallback(Log);
            Native.GCH_InitializePlatformBridge(Read, Write, Detect, CreateHandle, InvalidateHandle, Configure, Haptics);
            Native.GCH_InitializeDeviceRegistryPolicy(0, Alloc, Dispatch, Disconnect);

            initialized = true;
            
            Console.WriteLine($"GamepadCoreHost {Marshal.PtrToStringAnsi(Native.GCH_GetVersion())}");
            
            Native.GCH_DiscoverDevices(2.0f);
            while (!stop.Wait(TimeSpan.FromMilliseconds(16.6)))
            {
                Native.GCH_DiscoverDevices(0.0166f);
                foreach (int deviceId in DeviceIds.ToArray())
                {
                    Native.GCH_UpdateInput(deviceId, 0.0166f);
                }
                
                foreach (int deviceId in DeviceIds.ToArray())
                {
                    Native.GCH_GetInputState(deviceId, out var state);
                    
                    // Console.WriteLine($"Device {deviceId}: dPads : {state.DpadLeft} | {state.DpadRight} | {state.DpadUp} | {state.DpadDown}");
                    // Console.WriteLine($"Device {deviceId}: Face Buttons: {state.Cross} | {state.Square} | {state.Triangle} | {state.Circle}");
                    Console.WriteLine($"Device {deviceId}: Left Analog X: {state.LeftAnalogX} Y: {state.LeftAnalogY}");
                    Console.WriteLine($"Device {deviceId}: Right Analog X: {state.RightAnalogX} Y: {state.RightAnalogY}");
                }
            }
        }
        finally
        {
            if (initialized)
            {
                Native.GCH_Shutdown();
                Console.WriteLine("GamepadCoreHost GCH_Shutdown.");
            }
        }
    }

    private static void OnLog(int level, IntPtr message)
    {
        Console.WriteLine($"[Native:{level}] {Marshal.PtrToStringAnsi(message)}");
    }
    
    private static int OnDetect(IntPtr devices, int maxDevices)
    {
        Console.WriteLine($"PlatformDetectCallback (maxDevices: {maxDevices})");
        return HidPlatformBridge.OnDetect(devices, maxDevices);
    }

    private static bool OnRead(IntPtr handle, IntPtr buffer, int length, IntPtr bytesRead)
        => HidPlatformBridge.Read(handle, buffer, length, bytesRead);

    private static bool OnWrite(IntPtr handle, IntPtr buffer, int length, IntPtr bytesWritten)
        => HidPlatformBridge.Write(handle, buffer, length, bytesWritten);

    private static bool OnCreateHandle(IntPtr descriptor)
        => HidPlatformBridge.CreateHandle(descriptor);

    private static void OnInvalidateHandle(ulong handle)
        => HidPlatformBridge.InvalidateHandle(handle);
}
