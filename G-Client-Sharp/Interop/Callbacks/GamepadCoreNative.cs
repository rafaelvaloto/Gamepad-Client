// Project: G-Client-Sharp
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;
using System.Text;

namespace G_Client_Sharp.Interop.Callbacks;

internal static partial class GamepadCoreNative
{
    private const string Library = "GamepadCoreHost";

    internal static void Load(string libraryPath)
    {
        if (!File.Exists(libraryPath))
            throw new FileNotFoundException("Not found .dll", libraryPath);

        NativeLibrary.SetDllImportResolver(typeof(GamepadCoreNative).Assembly, (libraryName, _, _) => libraryName == Library
            ? NativeLibrary.Load(libraryPath)
            : IntPtr.Zero);
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
        return Encoding.ASCII.GetString(path, 0, length < 0 ? path.Length : length);
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr GCH_GetVersion();

    internal static string GetVersion()
    {
        IntPtr version = GCH_GetVersion();
        return version == IntPtr.Zero
            ? "<null>"
            : Marshal.PtrToStringUTF8(version) ?? "<invalid>";
    }

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_SetLogCallback(LogCallback callback);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_InitializeDeviceRegistryPolicy( int typeId, AllocEngineDeviceCallback allocCallback, DeviceIdCallback dispatchCallback, DeviceIdCallback disconnectCallback);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GCH_InitializePlatformBridge(
        PlatformReadCallback readCallback, 
        PlatformWriteCallback writeCallback,
        PlatformDetectCallback detectCallback, 
        PlatformCreateHandleCallback createHandleCallback,
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
