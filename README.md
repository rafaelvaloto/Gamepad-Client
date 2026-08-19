# G-Client-Sharp

G-Client-Sharp is a .NET console application that consumes the
[`GamepadCoreHost.dll`](https://github.com/rafaelvaloto/Gamepad-Core-Host) native API
through C# P/Invoke and unmanaged callbacks.

The client provides the Windows HID platform bridge used by Gamepad-Core Host to
discover and communicate with Sony DualSense, DualSense Edge, and DualShock 4
controllers.

## Requirements

- Windows x64
- .NET 10 SDK
- An x64 build of `GamepadCoreHost.dll`
- A compatible Sony controller

The native DLL is not included in this repository. Build it separately with the
Gamepad-Core Host project and provide its path when running the client.

## Related project

- [Gamepad-Core Host](https://github.com/rafaelvaloto/Gamepad-Core-Host) — native C-compatible DLL API consumed by this project.

## Build

```powershell
dotnet build .\G-Client-Sharp\G-Client-Sharp.csproj
```

## Run

Running without arguments prints the default DLL location and command-line help:

```powershell
dotnet run --project .\G-Client-Sharp\G-Client-Sharp.csproj
```

Start device monitoring with the default DLL path and display the native API
descriptor information:

```powershell
dotnet run --project .\G-Client-Sharp\G-Client-Sharp.csproj -- --info
```

Pass a different native DLL path with `--dll` (or `-d`):

```powershell
dotnet run --project .\G-Client-Sharp\G-Client-Sharp.csproj -- `
  --dll "C:\path\to\GamepadCoreHost.dll"
```

The DLL must match the process architecture. Use an x64 DLL with the x64 .NET
process.

The legacy positional DLL path is also supported.

## Native API lifecycle

The client follows this initialization sequence:

1. Load the native DLL and resolve its P/Invoke functions.
2. Register the native log callback.
3. Register the platform bridge callbacks for detection, handle management, reading, and writing.
4. Register the device registry callbacks for allocation, dispatch, and disconnection.
5. Discover devices and update input state continuously.
6. Call `GCH_Shutdown` during application shutdown.

Device discovery runs independently from the input update loop because HID
enumeration and handle creation can block I/O operations. The native host must
support concurrent discovery and input updates safely.

## Required native exports

The DLL must export the following C-compatible functions:

- `GCH_GetVersion`
- `GCH_SetLogCallback`
- `GCH_InitializeDeviceRegistryPolicy`
- `GCH_InitializePlatformBridge`
- `GCH_DiscoverDevices`
- `GCH_UpdateInput`
- `GCH_GetInputState`
- `GCH_Shutdown`

The callback and structure layouts in
`G-Client-Sharp/Interop/Callbacks/` must remain compatible with the native API.

## Project structure

```text
G-Client-Sharp/
├── Application/
│   └── GamepadClientApplication.cs
├── Infrastructure/
│   └── Hid/
│       ├── HidPlatformBridge.cs
│       └── SonyHidDeviceCatalog.cs
├── Interop/
│   ├── Callbacks/
│   │   ├── GamepadCoreCallbacks.cs
│   │   ├── GamepadCoreNative.cs
│   │   └── GamepadCoreTypes.cs
│   └── Windows/
│       ├── PlatformNativeMethods.cs
│       ├── WindowsHidConstants.cs
│       └── WindowsHidTypes.cs
└── Program.cs
```

## License

Copyright (c) 2026 valoto.games. All rights reserved.
