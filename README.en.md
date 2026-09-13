# PanelTextor

[日本語](README.md) | [English](README.en.md) | [简体中文](README.zh-CN.md) | [한국어](README.ko.md)

![PanelTextor](Assets/PanelTextor.png)

A local multilingual text layout tool for Windows 11.

Copyright (c) 2026 **sirokata** · Original code: [MIT License](LICENSE)

## Download and start

1. Download the Windows x64 ZIP from this repository's **Releases**.
2. Extract it and run `PanelTextor.exe`. A separate .NET installation is not required.
3. On first launch, review the terms and third-party licenses. Agree to open the editor, or decline to exit.
4. Use **Add images**, then add and position your text.

See the **[User guide](USAGE.en.md)** for detailed instructions.

The executable includes the required runtime libraries and licenses. It can run alone, but the distribution ZIP also includes documentation. Runtime files are extracted to a Windows temporary directory when the app starts.

## Features

- Independent text, positions, sizes and colors for JP / EN / CN / KR
- Japanese vertical text, common punctuation, and automatic horizontal combinations of short punctuation/digit runs within vertical text
- Variable font weights such as Regular, Medium and Bold
- Split input at blank lines, drag to move, multiple selection and Delete
- Multiple background sets matched by filename, with mismatch checks
- Batch export of multiple languages and background sets, progress and completion notifications
- PNG / JPEG quality 100, sRGB conversion and ICC profiles
- Per-page camera frame, elapsed time and five battery levels
- Japanese, English, Simplified Chinese and Korean interface languages

## Files and migration

- Projects: `作品.paneltextor.json` (you may choose another name). Images are referenced by path; keep the project and image folders together.
- Old `.polytext.json` files can still be opened. Saving suggests the new extension without automatically deleting the old file.
- Settings: `%LOCALAPPDATA%\PanelTextor\settings.json`
- Old `%LOCALAPPDATA%\PolyText\settings.json` is copied automatically if no new settings exist. The old file remains.
- Consent: `%LOCALAPPDATA%\PanelTextor\terms-acceptance.txt`. Changes to terms or included license texts require renewed agreement.

## Requirements and limitations

Windows 11 x64. Fonts are not bundled; install the fonts you need. The app has no custom image-upload or analytics feature.

Pixel-identical rendering with other image editors, ruby, Japanese line-breaking rules, automatic wrapping, and Undo/Redo are not supported. JPEG quality 100 is still lossy; use PNG for lossless output. 

## Build from source

Requires Windows and the .NET 10 SDK.

```powershell
dotnet restore PanelTextor.csproj
dotnet build PanelTextor.csproj -c Release
dotnet publish PanelTextor.csproj -c Release -r win-x64 --self-contained true -o dist/PanelTextor
```

Create the distribution ZIP:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package.ps1
```

Output goes to `release/`. The ZIP includes the executable, user documentation and required licenses.

```powershell
dist/PanelTextor/PanelTextor.exe --self-test test-output
```

Tests require Japanese, Chinese and Korean fonts. Install Source Han Sans CN VF for variable-font tests. Tests use settings in a separate test folder.

## Licensing and redistribution

Original PanelTextor code is **MIT licensed**: free and commercial use, modification and redistribution are allowed. Keep the copyright and permission notices.

Bundled .NET/WPF and other components retain their own licenses; they are not relicensed under MIT. See [Terms](TERMS.en.md), [Third-party notices](THIRD_PARTY_NOTICES.md) and `licenses/`. The distribution requires agreement on first launch, including applicable third-party conditions. Preserve required notices and the agreement mechanism when modifying or redistributing it.

For bug reports, include the application version, Windows version, font and reproduction steps. Do not attach private artwork or personal information to public issues.



