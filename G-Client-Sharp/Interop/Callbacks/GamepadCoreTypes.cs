// Project: G-Client-Sharp
// This project is a C# client for Gamepad-Core-Host.
// Copyright (c) 2026 valoto.games. All rights reserved.

using System.Runtime.InteropServices;

namespace G_Client_Sharp.Interop.Callbacks;

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
