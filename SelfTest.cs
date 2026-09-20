using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class SelfTest
{
 public static void Run(string folder)
 {
  Directory.CreateDirectory(folder); var results = new List<string>();
  Storage.SettingsRootOverride = Path.GetFullPath(Path.Combine(folder, "app-settings"));
  void Check(bool pass, string name) { if (!pass) throw new Exception(name); results.Add("PASS " + name); }
  var migrationRoot = Path.Combine(folder, "settings-migration");
  var legacySettings = Path.Combine(migrationRoot, "PolyText", "settings.json");
  var newSettings = Path.Combine(migrationRoot, "PanelTextor", "settings.json");
  Storage.Write(legacySettings, new Config { UiLanguage = "KO" });
  Check(Storage.LoadConfig(migrationRoot).UiLanguage == "KO" && File.ReadAllBytes(legacySettings).SequenceEqual(File.ReadAllBytes(newSettings)), "Settings migrate by copying legacy file without modifying it");
  Storage.Write(newSettings, new Config { UiLanguage = "EN" });
  Check(Storage.LoadConfig(migrationRoot).UiLanguage == "EN" && Storage.Read<Config>(legacySettings).UiLanguage == "KO", "New settings take precedence and legacy settings stay intact");
  Check(Storage.ProjectFileName("作品.polytext.json") == "作品.paneltextor.json" && Storage.ProjectFileName(null) == "作品.paneltextor.json", "Project filename uses PanelTextor without losing the title");
  Check(LicenseWindow.Notices().Count >= 7, "Distribution license texts are embedded and readable");
  var consentPath = Path.GetFullPath(Path.Combine(folder, "consent", "terms-acceptance.txt"));
  Check(!DistributionTerms.Accepted(consentPath), "First use has no presumed license acceptance");
  DistributionTerms.Accept(consentPath);
  Check(DistributionTerms.Accepted(consentPath), "Explicit acceptance persists for current license texts");
  File.WriteAllText(consentPath, "old-terms");
  Check(!DistributionTerms.Accepted(consentPath), "Changed terms require renewed acceptance");
  Check(DistributionTerms.Text("LICENSE").Contains("Copyright (c) 2026 sirokata") && LicenseWindow.Notices().ContainsKey("TERMS.md"), "MIT author notice and distribution terms are visible offline");
  var termsWindow = DistributionTerms.CreateWindow();
  var termsPanel = (System.Windows.Controls.DockPanel)termsWindow.Content;
  var termsBottom = (System.Windows.Controls.StackPanel)termsPanel.Children[0];
  var consentCheck = (System.Windows.Controls.CheckBox)termsBottom.Children[0];
  var consentButtons = (System.Windows.Controls.StackPanel)termsBottom.Children[1];
  var consentButton = (System.Windows.Controls.Button)consentButtons.Children[0];
  Check(consentCheck.IsChecked != true && !consentButton.IsEnabled, "Consent dialog defaults to no agreement");
  consentCheck.IsChecked = true; Check(consentButton.IsEnabled, "Explicit checkbox enables agreement button");
  consentCheck.IsChecked = false; Check(!consentButton.IsEnabled && ((System.Windows.Controls.Button)consentButtons.Children[1]).IsCancel, "Withdrawing consent disables agreement and provides exit");
  termsPanel.Measure(new Size(870, 650)); termsPanel.Arrange(new Rect(0, 0, 870, 650)); termsPanel.UpdateLayout();
  var termsImage = new RenderTargetBitmap(870, 650, 96, 96, PixelFormats.Pbgra32); termsImage.Render(termsPanel);
  var termsPng = new PngBitmapEncoder(); termsPng.Frames.Add(BitmapFrame.Create(termsImage));
  using (var termsFile = File.Create(Path.Combine(folder, "terms-dialog.png"))) termsPng.Save(termsFile);
  termsWindow.Close();
  foreach (var language in new[] { "JP", "EN", "ZH", "KO" })
  {
   UiLanguage.Set(language); var localizedTerms = DistributionTerms.CreateWindow();
   var localizedTabs = (System.Windows.Controls.TabControl)((System.Windows.Controls.DockPanel)localizedTerms.Content).Children[1];
   Check(localizedTabs.SelectedIndex == DistributionTerms.TermsIndex(language) && ((System.Windows.Controls.TextBox)((System.Windows.Controls.TabItem)localizedTabs.SelectedItem).Content).Text.Contains("Copyright (c) 2026 sirokata"), language + " terms open in the matching language with author notice");
   localizedTerms.Close();
  }
  UiLanguage.Set("JP");
  var oldPalette = System.Text.Json.JsonSerializer.Deserialize<Config>("{\"Colors\":{\"color1\":\"#123456\",\"color2\":\"#234567\",\"color3\":\"#345678\",\"color4\":\"#456789\"}}")!;
  Check(oldPalette.Colors.Count == 10 && oldPalette.Colors["color1"] == "#123456" && oldPalette.Colors["color4"] == "#456789", "Four-color settings gain six colors without overwriting customized colors");
  oldPalette.Colors["color10"] = "#ABCDEF";
  var paletteFile = Path.Combine(folder, "ten-color-settings.json"); Storage.Write(paletteFile, oldPalette);
  Check(Storage.Read<Config>(paletteFile).Colors["color10"] == "#ABCDEF", "Additional palette colors persist after settings reload");
  var replacementConfig = new Config();
  replacementConfig.Styles["JP"].ImportReplacements = [new() { Find = "猫", ReplaceWith = "ねこ" }, new() { Find = "削除", ReplaceWith = "" }, new() { Find = "!", ReplaceWith = "?", Enabled = false }];
  Check(TextReplacements.Apply(" 猫!削除\n猫 ", replacementConfig.Styles["JP"]) == " ねこ!\nねこ ", "Import replacement preserves whitespace, replaces all matches and supports deletion/disabled rules");
  Check(TextReplacements.Apply("猫", replacementConfig.Styles["EN"]) == "猫", "Replacement rules are isolated by language");
  var literalStyle = new LanguageStyle { ImportReplacements = [new() { Find = ".*", ReplaceWith = "$1" }, new() { Find = "Cat", ReplaceWith = "Dog" }, new() { Find = "Dog", ReplaceWith = "Wolf" }, new() { Find = "", ReplaceWith = "bad" }] };
  Check(TextReplacements.Apply(".* Cat cat", literalStyle) == "$1 Wolf cat", "Rules use literal case-sensitive matching in list order and ignore empty search");
  var replacementsPath = Path.Combine(folder, "replacement-settings.json"); Storage.Write(replacementsPath, replacementConfig);
  Check(TextReplacements.Apply("猫", Storage.Read<Config>(replacementsPath).Styles["JP"]) == "ねこ" && System.Text.Json.JsonSerializer.Deserialize<LanguageStyle>("{}")!.ImportReplacements.Count == 0, "Replacement settings persist and old settings default to no rules");
  VerticalTests.Run(folder, Check);
  WhitespaceTests.Run(folder, Check);
  SpacingTests.Run(folder, Check);
  RotationTests.Run(folder, Check);
  var parts = Storage.Split("今日は遅かったね\r\n \r\nごめん\r\n仕事が長引いちゃって\r\n\r\n\r\nじゃあ帰ろうか");
  Check(parts.Length == 3 && parts[1] == "ごめん\n仕事が長引いちゃって", "Blank-line split preserves intra-block line breaks");
  Check(Storage.Split(" \n \n").Length == 0, "Empty input creates no objects");
  var source = Path.GetFullPath(Path.Combine(folder, "source.png"));
  var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) { dc.DrawRectangle(Brushes.LightSlateGray, null, new Rect(0, 0, 1200, 900)); }
  var bitmap = new RenderTargetBitmap(1200, 900, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
  var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var f = File.Create(source)) encoder.Save(f);
  var originalHash = SHA256.HashData(File.ReadAllBytes(source));
  CameraFrameTests.Run(folder, Check);
  var page = new Page { Source = source, Width = 1200, Height = 900 };
  var colorPage = new Page { Source = source, Width = 1200, Height = 900 };
  foreach (var key in Config.ColorKeys)
   colorPage.Layers["EN"].Objects.Add(new TextObject { Text = key, Color = key, X = 100, Y = 35 + 75 * colorPage.Layers["EN"].Objects.Count });
  var colorProject = new Project { Pages = [colorPage] }; Storage.Validate(colorProject);
  var colorProjectFile = Path.Combine(folder, "ten-color-project.paneltextor.json"); Storage.Write(colorProjectFile, colorProject);
  var loadedColorProject = Storage.Read<Project>(colorProjectFile); Storage.Validate(loadedColorProject);
  Check(loadedColorProject.Pages[0].Layers["EN"].Objects.Select(o => o.Color).SequenceEqual(Config.ColorKeys), "All ten text colors survive project save and validation");
  var colorOutput = Path.Combine(folder, "ten-color-export.png"); TextRenderer.Export(colorPage, "EN", oldPalette, colorOutput);
  Check(File.Exists(colorOutput), "All ten colors export successfully");
  var samples = new Dictionary<string, string> { ["JP"] = "「今日は、いい天気。」\nコーヒーを飲もう！", ["EN"] = "Hello, world!\nLet's go home.", ["ZH"] = "今天辛苦了。\n我们回家吧！", ["KO"] = "오늘도 수고했어요.\n이제 집에 가요!" };
  foreach (var lang in Config.Languages) page.Layers[lang].Objects.Add(new TextObject { Text = samples[lang], X = 160, Y = 130, Direction = lang == "JP" ? "vertical" : "horizontal", Color = "color2" });
  page.Layers["JP"].Objects[0].X = 720;
  Check(page.Layers["EN"].Objects[0].X == 160, "Language layouts are independent");
  var project = new Project { Pages = [page], Language = "KO" }; var projectFile = Path.Combine(folder, "roundtrip.polytext.json"); Storage.Write(projectFile, project);
  var restored = Storage.Read<Project>(projectFile); Storage.Validate(restored);
  Check(restored.Language == "KO" && restored.Pages[0].Layers["JP"].Objects[0].X == 720 && restored.Pages[0].Layers["JP"].Objects[0].Size == "medium", "Project roundtrip retains language, coordinates and preset keys");
  var config = new Config(); var obj = page.Layers["EN"].Objects[0]; var before = TextRenderer.Geometry(obj, "EN", config).Bounds;
  config.Styles["EN"].Sizes["medium"] = 60; var after = TextRenderer.Geometry(obj, "EN", config).Bounds;
  Check(after.Width > before.Width * 1.5, "Preset edits update existing objects");
  var configFile = Path.Combine(folder, "settings.json"); Storage.Write(configFile, config); Check(Storage.Read<Config>(configFile).Styles["EN"].Sizes["medium"] == 60, "Settings roundtrip");
  foreach (var lang in Config.Languages)
  {
   var output = Path.Combine(folder, lang + "-" + Guid.NewGuid().ToString("N")[..6] + ".png"); TextRenderer.Export(page, lang, config, output);
   Check(TextRenderer.Dimensions(output) == (1200, 900), lang + " original-resolution PNG export");
   Check(!SHA256.HashData(File.ReadAllBytes(output)).SequenceEqual(originalHash), lang + " export contains text");
   bool refused = false; try { TextRenderer.Export(page, lang, config, output); } catch (IOException) { refused = true; } Check(refused, "Existing export protected: " + lang);
  }
  bool protectedSource = false; try { TextRenderer.Export(page, "JP", config, source); } catch (IOException) { protectedSource = true; }
  Check(protectedSource && SHA256.HashData(File.ReadAllBytes(source)).SequenceEqual(originalHash), "Original image protected");
  var preview = TextRenderer.Load(source, 600); Check(preview.PixelWidth == 600 && preview.IsFrozen, "Preview decoded at reduced resolution and transferable between threads");
  var window = new MainWindow(); window.Measure(new Size(1380, 900)); window.Arrange(new Rect(0, 0, 1380, 900)); window.UpdateLayout();
  Check(window.Content is not null, "Main window XAML and settings initialize");
  File.WriteAllLines(Path.Combine(folder, "fonts.txt"), FontCatalog.Installed.Select(f => f.DisplayName + " => " + f.Key));
  foreach (var font in FontCatalog.Installed.Where(f => f.Key.Contains("GenEiAntique", StringComparison.OrdinalIgnoreCase)))
  {
   Check(font.DisplayName.Any(c => c > 127), "Japanese font display name: " + font.DisplayName);
   Check(FontCatalog.Find(font.DisplayName)?.Key == font.Key, "Localized font selection: " + font.DisplayName);
   var face = new Typeface(FontCatalog.Resolve(font.Key), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
   Check(face.TryGetGlyphTypeface(out var glyph) && glyph.FontUri.ToString().Contains("GenEiAntique", StringComparison.OrdinalIgnoreCase), "Actual user font resolved without fallback: " + font.DisplayName);
   config.Styles["JP"].Font = font.Key;
   var output = Path.Combine(folder, "font-" + Path.GetFileName(new Uri(font.Key.Split('#')[0]).LocalPath) + "-" + Guid.NewGuid().ToString("N")[..6] + ".png");
   TextRenderer.Export(page, "JP", config, output);
  }
  File.WriteAllLines(Path.Combine(folder, "fonts.txt"), FontCatalog.Installed.Select(f => f.DisplayName + " => " + f.Key));
  foreach (var font in FontCatalog.Installed.Where(f => f.Key.Contains("NanumGothic", StringComparison.OrdinalIgnoreCase) || f.Key.Contains("nanum-gothic", StringComparison.OrdinalIgnoreCase) || f.Key.Contains("SourceHanSansCN", StringComparison.OrdinalIgnoreCase)))
  {
   var face = new Typeface(FontCatalog.Resolve(font.Key), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
   Check(face.TryGetGlyphTypeface(out var glyph), "CJK font face resolves: " + font.DisplayName);
   if (Uri.TryCreate(font.Key.Split('#')[0], UriKind.Absolute, out var uri) && uri.IsFile)
    Check(string.Equals(glyph.FontUri.LocalPath, uri.LocalPath, StringComparison.OrdinalIgnoreCase), "CJK exact file used: " + font.DisplayName);
   var lang = font.Key.Contains("SourceHan", StringComparison.OrdinalIgnoreCase) ? "ZH" : "KO";
   Check(samples[lang].Where(c => !char.IsWhiteSpace(c)).All(c => glyph.CharacterToGlyphMap.ContainsKey(c)), "CJK sample glyph coverage: " + font.DisplayName);
   config.Styles[lang].Font = font.Key;
   TextRenderer.Export(page, lang, config, Path.Combine(folder, "cjk-" + Guid.NewGuid().ToString("N") + ".png"));
  }
  File.WriteAllLines(Path.Combine(folder, "font-errors.txt"), FontCatalog.LoadErrors);
  WeightTests.Run(folder, Check);
  RegressionTests.Run(folder, Check);
  UiRegression.Run(folder, Check);
  File.WriteAllLines(Path.Combine(folder, "results.txt"), results);
 }
}

