# Gamepad Client

Aplicação console em C# para consumir a API nativa do Gamepad Core Host através de callbacks e interoperabilidade HID no Windows.

## Requisitos

- Windows x64
- .NET 10 SDK
- DLL nativa `GamepadCoreHost.dll` compilada para x64
- Um controle Sony DualSense, DualSense Edge ou DualShock 4 compatível

## Estrutura

- `Gamepad-Client/Program.cs` — inicialização, callbacks e loop de atualização.
- `Gamepad-Client/HidPlatformBridge.cs` — enumeração e comunicação HID.
- `Gamepad-Client/PlatformNativeMethods.cs` — declarações P/Invoke do Windows.

A DLL nativa não é versionada neste repositório. Ela deve ser compilada separadamente no projeto Gamepad Core Host.

## Executar

O caminho da DLL pode ser informado como primeiro argumento:

```powershell
dotnet run --project .\Gamepad-Client\Gamepad-Client.csproj -- `
  "C:\caminho\para\GamepadCoreHost.dll"
```

Também é possível configurar esse argumento na configuração de execução do Rider.

Se nenhum argumento for informado, o programa usa o caminho padrão configurado em `Program.cs`.

## Compilar

```powershell
dotnet build .\Gamepad-Client\Gamepad-Client.csproj
```

O host nativo deve exportar as funções com o prefixo `GCH_`, incluindo:

- `GCH_SetLogCallback`
- `GCH_InitializeDeviceRegistryPolicy`
- `GCH_InitializePlatformBridge`
- `GCH_DiscoverDevices`
- `GCH_UpdateInput`
- `GCH_GetInputState`
- `GCH_Shutdown`

## Licença

Defina aqui a licença do projeto antes de publicar uma versão pública.
