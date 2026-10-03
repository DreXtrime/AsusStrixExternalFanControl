# ASUS Fan Control

Asus had a cool idea of adding external fan headers to their older strix cards but require you to use their buggy gpu
tweak sofware to control them. This aims to solve that using a Lightweight system tray app,
without needing GPU Tweak II installed.

> I am not responsible for any damage you cause to your gpu

## How it works

The external fan headers are controlled by an onboard ASUS microcontroller (I2C address `0x52`, port `1`)
that sits on the GPU PCB. This app writes fan speed directly to it via the NVIDIA driver's NvAPI I2C
interface (`NvAPI_I2CWriteEx`), bypassing GPU Tweak entirely.

Reverse engineered via x32dbg + Ghidra from `Vender.dll` and `ASUSGPUFanServiceEx.exe`.

## Requirements

- ASUS GTX 1080 Strix (other Strix cards may work -- test with Ctrl+D scan)
- Windows 10/11
- NVIDIA driver (any modern version)
- Run as Administrator (required for NvAPI I2C access)
- No GPU Tweak needed

## Usage

1. Download `AsusFanControl.exe` from [Releases](../../releases)
2. Run as Administrator
3. A tray icon appears showing current fan %
4. Left-click the icon to open the fan speed slider
5. Right-click for quick presets (0%, 25%, 50%, 75%, 100%)

### Keyboard shortcuts (in the popup)

| Key    | Action                          |
|--------|---------------------------------|
| Enter  | Apply speed                     |
| Escape | Close popup                     |
| Ctrl+D | Toggle diagnostic register scan |

### Diagnostic mode

If the fans don't respond, press Ctrl+D in the popup to reveal "Scan Registers".
This reads I2C registers 0x00-0x1F from the ASUS controller and shows their values,
which helps identify the correct fan register if 0x11 is wrong for your card variant.

## Auto-start

To run on boot: create a shortcut to `AsusFanControl.exe`, right-click it > Properties >
Advanced > check "Run as administrator", then place the shortcut in:

```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

## Building from source

Requires .NET 8 SDK.

```
dotnet publish src/AsusFanControl.csproj -c Release -r win-x86 --self-contained -p:PublishSingleFile=true -o publish/
```

Or just push to GitHub -- Actions builds it automatically.

## Roadmap

- [ ] GPU temperature-based fan curve (graph editor)
- [ ] Per-fan control if hardware supports it
- [ ] Dark/light theme popup
