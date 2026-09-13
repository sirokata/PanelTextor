# Third-party notices / 第三者ライセンス

PanelTextor — Copyright (c) 2026 sirokata. Independent application; not endorsed by Microsoft or other dependency authors.

The MIT license at the repository root applies to PanelTextor's original code and documentation. Third-party components retain their own licenses. The Windows self-contained executable is not licensed solely under MIT.

| Component | Version / scope | License and included notice |
|---|---|---|
| HarfBuzzSharp | 14.2.1.102 | MIT: `licenses/HARFBUZZSHARP-LICENSE.txt` |
| HarfBuzzSharp.NativeAssets.Win32 / HarfBuzz | 14.2.1.102 package | MIT / Old MIT and component notices: `licenses/HARFBUZZ-THIRD-PARTY-NOTICES.txt` |
| Unicode Vertical_Orientation data | Unicode 17.0.0 | Unicode License V3: `licenses/UNICODE-LICENSE.txt` |
| .NET Runtime | 10.0.9, win-x64 | MIT and third-party notices: `licenses/DOTNET-RUNTIME-LICENSE.txt`, `licenses/DOTNET-THIRD-PARTY-NOTICES.txt` |
| WPF | 10.0.9, win-x64 | MIT: `licenses/WPF-LICENSE.txt`; Windows binary exceptions below |
| Windows native runtime / WPF components | coreclr, single-file runtime, PresentationNative_cor3, wpfgfx_cor3, vcruntime140_cor3 | `licenses/DOTNET-LIBRARY-LICENSE.txt` |
| D3DCompiler_47_cor3.dll | Windows desktop runtime component | `licenses/WINDOWS-SDK-LICENSE.txt` |

Microsoft documents Windows-specific exceptions in its [Windows license information](https://github.com/dotnet/core/blob/main/license-information-windows.md). MIT licensing of the source repository does not replace those binary distribution terms. The included Microsoft license is retained in its original form; consult the accompanying [distribution terms](TERMS.md).

The HarfBuzzSharp package supplies a combined SkiaSharp/HarfBuzzSharp third-party notice. It is preserved in full; its list is broader than the components used by this application. Where the FreeType License applies, the FreeType Project is acknowledged: https://freetype.org/ . No bundled fonts are granted by this application.

All notices are embedded in the executable and readable offline using the **Licenses / ライセンス** button. The release ZIP also contains their original text files. Preserve these notices and the applicable distribution terms when redistributing the binary. Do not replace them with PanelTextor's MIT license.
