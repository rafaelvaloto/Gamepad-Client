// Project: Gamepad-Client
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;

namespace Gamepad_Client.Interop.Callbacks;

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
