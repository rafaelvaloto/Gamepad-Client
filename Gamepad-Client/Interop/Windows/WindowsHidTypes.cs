// Project: Gamepad-Client
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;

namespace Gamepad_Client.Interop.Windows;

[StructLayout(LayoutKind.Sequential)]
internal struct SpDeviceInterfaceData
{
    internal int CbSize;
    internal Guid InterfaceClassGuid;
    internal int Flags;
    internal IntPtr Reserved;
}

[StructLayout(LayoutKind.Sequential)]
internal struct HidAttributes
{
    internal int Size;
    internal ushort VendorId;
    internal ushort ProductId;
    internal ushort VersionNumber;
}
