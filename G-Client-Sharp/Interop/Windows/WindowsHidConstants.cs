// Project: G-Client-Sharp
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

namespace G_Client_Sharp.Interop.Windows;

internal static class WindowsHidConstants
{
    internal static readonly IntPtr InvalidHandleValue = new(-1);
    internal const uint DigcfPresent = 0x00000002;
    internal const uint DigcfDeviceInterface = 0x00000010;
    internal const uint GenericRead = 0x80000000;
    internal const uint GenericWrite = 0x40000000;
    internal const uint FileShareRead = 0x00000001;
    internal const uint FileShareWrite = 0x00000002;
    internal const uint OpenExisting = 3;
}
