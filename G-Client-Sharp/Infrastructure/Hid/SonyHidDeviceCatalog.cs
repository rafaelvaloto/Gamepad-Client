// Project: G-Client-Sharp
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

namespace G_Client_Sharp.Infrastructure.Hid;

internal static class SonyHidDeviceCatalog
{
    internal const ushort VendorId = 0x054C;
    internal const int DescriptorSize = 532;
    internal const string BluetoothGuid = "{00001124-0000-1000-8000-00805f9b34fb}";

    internal enum DeviceType { DualSense = 0, DualSenseEdge = 1, DualShock4 = 2, NotFound = 3 }
    internal enum ConnectionType { Usb = 0, Bluetooth = 1, Unrecognized = 2 }
}
