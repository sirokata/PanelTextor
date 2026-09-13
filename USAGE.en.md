# PanelTextor user guide

[日本語](USAGE.md) | [English](USAGE.en.md) | [简体中文](USAGE.zh-CN.md) | [한국어](USAGE.ko.md)

## Start and choose the interface language

Extract the distribution ZIP and run `PanelTextor.exe`. Review and accept the terms and third-party originals on first launch. Windows 11 x64 is required; a separate .NET installation is not. Required runtime files are extracted to a temporary directory.

Use **Language** at the top to select Japanese, English, Simplified Chinese or Korean. The choice is remembered; Japanese is the default. This changes interface text, not the JP/EN/CN/KR text layers or existing layouts. Windows system buttons and OS error messages follow the Windows language.

## Add and edit text

1. **Add images**: select JPEG, PNG or other supported images.
2. Select a page and a text language, **JP / EN / CN / KR**. The active language button is blue. Changing pages resets the text language to JP.
3. Paste text into **Bulk text input** and click **Split and add**. Blank lines separate blocks; ordinary line breaks stay inside the same block.
4. Drag a block on the canvas to move it, or select it in **Placed text**. Ctrl+click selects multiple blocks on the canvas or in the list; Shift+click selects a range in the list. The delete button or Delete key removes selected blocks when focus is on the canvas/list. In text fields, Delete edits characters normally. Select a single block to edit its text or formatting.
5. Choose Small/Medium/Large, one of four colors, and (for JP) vertical/horizontal text. **Line / column advance (px)** controls line-start spacing horizontally or column spacing vertically. Default/0 uses the language preset. A single line/column is unaffected. Move separate blocks with drag rather than this spacing control.
6. **Presets** sets fonts, sizes, outlines and shared colors. Changes affect all existing text and all projects. Variable fonts have a Weight selector: Source Han Sans CN VF supports Regular (400), Medium (500), Bold (700), etc. The actual wght axis affects horizontal text, vertical text and export; Default preserves the earlier appearance.
7. **Save** a `*.paneltextor.json` project; use **Open** to resume.
8. Export the current page or all pages. Select one or more text languages, background sets, and PNG/JPEG. Output is `output/language/set/original-name.png` (or `.jpg`). Existing names receive a number suffix. Chinese/Korean folders are CN/KR; internal legacy keys are retained for compatibility.

The bottom progress bar shows completed items, total items and percentage. A completion dialog reports successful exports and warnings; click OK to close it.

## Camera frame

Enable **Show camera frame** to show corners, REC, elapsed time, battery and center focus. Click Battery 1–5 for immediate preview. At 1, the outline, terminal and remaining bar are red; at 2–5 they are white. Enter elapsed time as `mm:ss` or `hh:mm:ss`; it does not count automatically.

Enabled state, battery and time are saved per page and shared across that page's languages and backgrounds. The frame is included in PNG/JPEG exports. Old projects default to disabled. Elements have independent settings and vector drawings, not a fixed PNG. REC blinking, 4K/FPS/ISO controls and similar extensions are not implemented.

## Multiple background sets

1. **Add / update image set**: choose the first folder, such as Mosaic. Its images define the page list. Add images also remains available.
2. Add other folders such as BlackMosaic the same way. There is no hardcoded set-count limit.
3. Matching uses the same filename including extension, ignoring case. Text/layout is stored once per page and language.
4. Switch **Background** to preview a set. Text, position, size, color and direction remain shared.
5. Check all required sets when exporting. Set selections are saved in the project.

Missing/extra files and different image dimensions are reported on import and before export; **Check matching files** also checks them. Mismatched dimensions are excluded rather than resized. With mismatches, you are asked whether to export valid images only. Extra files in an additional set do not create new pages automatically.

After changing folder contents, select the same folder again to reload. Subfolders are not scanned recursively. Duplicate page names in old data prevent matching additional sets; existing layouts are preserved.

## Files, settings and image handling

- Source images are not modified. Projects store relative paths. Move projects and background folders together, preserving their relative locations. Missing legacy source images can be relocated when opening.
- Project format is Version 2. Version 1 migrates in memory on open without modifying the file immediately. If a Version 1 file is overwritten, the original is backed up as `.v1.bak` (numbered if needed). Older Version 1 applications cannot open Version 2.
- Old `.polytext.json` files can be opened. Saving suggests a `.paneltextor.json` name; the old file is not automatically removed.
- Settings are in `%LOCALAPPDATA%/PanelTextor/settings.json`. If missing, old settings from `%LOCALAPPDATA%/PolyText/settings.json` are copied automatically; the original is retained. Presets are shared across projects, so changing them also changes how earlier work renders.
- Consent is stored per Windows user in `%LOCALAPPDATA%/PanelTextor/terms-acceptance.txt`. Changed terms require renewed agreement. Originals remain available through **Licenses**.
- The language visibility switch controls the page/language preview only. Exports include all text in each selected language.
- Previews decode up to 1600 pixels on the long edge and cache the six most recent images. Export processes one image at a time at original resolution.

## Rendering and limitations

Japanese vertical text uses HarfBuzz/OpenType vertical glyphs and Unicode orientation. Consecutive halfwidth `!`/`?` and two- or three-digit numbers are automatically combined horizontally within vertical text. Ruby, Japanese line-breaking rules and automatic wrapping are unsupported. Variable-font selection supports wght, not width or optical size. EN/CN/KR are horizontal only in the UI. Install appropriate fonts. 

Background ICC profiles are converted to sRGB; exports embed sRGB ICC and retain original PPI. Unprofiled RGB is assumed sRGB, rather than reattaching a mismatched original profile. EXIF rotation, HDR, CMYK print workflows and preserving all metadata are outside scope. Pixel orientation is used. JPEG quality is 100 and transparency becomes white; JPEG remains lossy.

Sizes are image pixels. Monitor-profile compensation and antialiasing specific to other image editors are not reproduced exactly. Insert manual line breaks; text outside the image is clipped. Do not silently replace source files later. Very large images depend on available memory. Undo/Redo, speech balloons and sound-effect design are unsupported.




## Import replacement rules

Open **Import replacement rules** below **Split and add**. Select JP/EN/CN/KR, enter Find and Replace with in the bottom row, then save. Uncheck Enabled to disable a rule or use Delete selected rules to remove it. Cancel discards changes.

Rules are saved per text language in application settings and shared across projects. They run top to bottom before blank-line splitting when adding new text. Matching is literal, case-sensitive and includes substrings; regex is not used. Later rules process earlier replacement results. Empty replacement deletes matches; an empty Find cannot be registered. For example, an EN rule “colour → color” only changes newly imported EN text. Existing blocks and individual editing are unaffected.
