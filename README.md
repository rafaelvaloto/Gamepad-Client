# Gamepad Client

C# console application for consuming the native Gamepad Core Host API through callbacks and Windows HID interoperability.

## Requirements

- Windows x64
- .NET 10 SDK
- x64 `GamepadCoreHost.dll`
- A compatible Sony DualSense, DualSense Edge, or DualShock 4 controller

## Structure

- `Gamepad-Client/Program.cs` — minimal application entry point.
- `Gamepad-Client/Application/` — native host initialization, callbacks, and update loop.
- `Gamepad-Client/Infrastructure/Hid/` — HID device discovery and communication bridge.
- `Gamepad-Client/Infrastructure/Hid/SonyHidDeviceCatalog.cs` — Sony device IDs, connection types, and descriptor size.
- `Gamepad-Client/Interop/Callbacks/GamepadCoreCallbacks.cs` — unmanaged callback delegates.
- `Gamepad-Client/Interop/Callbacks/GamepadCoreTypes.cs` — native-compatible descriptors and input layout.
- `Gamepad-Client/Interop/Callbacks/GamepadCoreNative.cs` — Gamepad Core P/Invoke methods.
- `Gamepad-Client/Interop/Windows/` — Windows constants, native-compatible types, and P/Invoke declarations.

The native DLL is not committed to this repository. Build it separately with the Gamepad Core Host project.

## Run

Pass the native DLL path as the first argument:

```powershell
dotnet run --project .\Gamepad-Client\Gamepad-Client.csproj -- `
  "C:\path\to\GamepadCoreHost.dll"
```

You can also configure this argument in Rider's run configuration.

If no argument is provided, the default path configured in `Program.cs` is used.

## Build

```powershell
dotnet build .\Gamepad-Client\Gamepad-Client.csproj
```

The native host must export functions using the `GCH_` prefix, including:

- `GCH_SetLogCallback`
- `GCH_InitializeDeviceRegistryPolicy`
- `GCH_InitializePlatformBridge`
- `GCH_DiscoverDevices`
- `GCH_UpdateInput`
- `GCH_GetInputState`
- `GCH_Shutdown`

## License

Add the project's license here before publishing a public release.
