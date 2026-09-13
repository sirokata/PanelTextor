using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace PanelTextor;
public class TextObject
{
 public string Id { get; set; } = Guid.NewGuid().ToString("N");
 public string Kind { get; set; } = "text";
 public string Text { get; set; } = "";
 public double X { get; set; }
 public double Y { get; set; }
 public string Size { get; set; } = "medium";
 public string Color { get; set; } = "color1";
 public string Direction { get; set; } = "horizontal";
 public double LineAdvancePx { get; set; } = 0;
 [System.Text.Json.Serialization.JsonIgnore] public string Label => Text.Replace("\n", " / ");
}
public class LanguageLayer
{
 public bool Visible { get; set; } = true;
 public string Draft { get; set; } = "";
 public List<TextObject> Objects { get; set; } = [];
}
public class Page
{
 public CameraFrameSettings CameraFrame { get; set; } = new();
 public string Id { get; set; } = Guid.NewGuid().ToString("N");
 public string Source { get; set; } = "";
 public int Width { get; set; }
 public int Height { get; set; }
 public Dictionary<string, LanguageLayer> Layers { get; set; } = Config.Languages.ToDictionary(l => l, _ => new LanguageLayer());
 [System.Text.Json.Serialization.JsonIgnore] public string Name => Path.GetFileName(Source);
}
public class Project
{
 public int Version { get; set; } = 2;
 public List<ImageSet> ImageSets { get; set; } = [];
 public string ActiveSetId { get; set; } = "";
 public string Language { get; set; } = "JP";
 public int PageIndex { get; set; }
 public List<Page> Pages { get; set; } = [];
}
public class LanguageStyle
{
 public List<ReplacementRule> ImportReplacements { get; set; } = [];
 public double FontWeight { get; set; } = 0; // 0 preserves the old/default rendering.
 public double LineHeightPx { get; set; } = 0;
 public double VerticalColumnEm { get; set; } = 1.2;
 public string Font { get; set; } = "Yu Gothic";
 public Dictionary<string, double> Sizes { get; set; } = new() { ["small"] = 32, ["medium"] = 40, ["large"] = 48 };
 public string OutlineColor { get; set; } = "#FFFFFF";
 public double OutlineWidth { get; set; } = 5;
}
public class Config
{
 public string UiLanguage { get; set; } = "JP";
 public static readonly string[] Languages = ["JP", "EN", "ZH", "KO"];
 public Dictionary<string, LanguageStyle> Styles { get; set; } = new()
 {
  ["JP"] = new(), ["EN"] = new() { Font = "Arial", Sizes = new() { ["small"] = 28, ["medium"] = 36, ["large"] = 44 } },
  ["ZH"] = new() { Font = "Microsoft YaHei", Sizes = new() { ["small"] = 30, ["medium"] = 38, ["large"] = 46 } },
  ["KO"] = new() { Font = "Malgun Gothic", Sizes = new() { ["small"] = 28, ["medium"] = 36, ["large"] = 44 } }
 };
 public Dictionary<string, string> Colors { get; set; } = new() { ["color1"] = "#000000", ["color2"] = "#E53935", ["color3"] = "#1976D2", ["color4"] = "#8E24AA" };
}
public static class Storage
{
 public static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
 internal static string? SettingsRootOverride;
 private static string SettingsRoot => SettingsRootOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
 public static string ConfigPath => Path.Combine(SettingsRoot, "PanelTextor", "settings.json");
 public static Config LoadConfig(string? localData = null)
 {
  localData ??= SettingsRoot;
  var current = Path.Combine(localData, "PanelTextor", "settings.json");
  var legacy = Path.Combine(localData, "PolyText", "settings.json");
  if (File.Exists(current)) return Read<Config>(current);
  if (!File.Exists(legacy)) return new Config();
  var config = Read<Config>(legacy); // Validate JSON before copying; leave the old settings untouched.
  Directory.CreateDirectory(Path.GetDirectoryName(current)!);
  File.Copy(legacy, current, false);
  return config;
 }
 public static string ProjectFileName(string? path) => path is null ? "作品.paneltextor.json" :
  Path.GetFileName(path).EndsWith(".polytext.json", StringComparison.OrdinalIgnoreCase)
   ? Path.GetFileName(path)[..^14] + ".paneltextor.json" : Path.GetFileName(path);
 public static void Write<T>(string path, T value)
 {
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
  var temporary = path + ".tmp";
  File.WriteAllText(temporary, JsonSerializer.Serialize(value, Json)); File.Move(temporary, path, true);
 }
 public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidDataException("データが空です。");
 public static string[] Split(string text)
 {
  var blocks = new List<string>(); var lines = new List<string>();
  void Flush() { if (lines.Count > 0) { blocks.Add(string.Join("\n", lines)); lines.Clear(); } }
  foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
  {
   if (string.IsNullOrWhiteSpace(line)) Flush();
   else lines.Add(line); // Preserve leading, interior and trailing spaces verbatim.
  }
  Flush(); return blocks.ToArray();
 }
 public static void Validate(Project p)
 {
  if (p.Version is not (1 or 2) || !Config.Languages.Contains(p.Language)) throw new InvalidDataException("対応していないプロジェクト形式です。");
  ImageSets.Upgrade(p);
  if (p.Pages.Select(page => page.Id).Distinct().Count() != p.Pages.Count || p.ImageSets.Select(s => s.Id).Distinct().Count() != p.ImageSets.Count) throw new InvalidDataException("ページまたはセットIDが重複しています。");
  foreach (var page in p.Pages)
  {
   page.CameraFrame ??= new();
   page.CameraFrame.Corners ??= new(); page.CameraFrame.Recording ??= new();
   page.CameraFrame.Timer ??= new(); page.CameraFrame.Battery ??= new(); page.CameraFrame.Focus ??= new();
   if (page.Width <= 0 || page.Height <= 0 || string.IsNullOrWhiteSpace(page.Source)) throw new InvalidDataException("画像情報が不正です。");
   foreach (var lang in Config.Languages)
   {
    if (!page.Layers.ContainsKey(lang)) page.Layers[lang] = new();
    foreach (var o in page.Layers[lang].Objects)
     if (!new[] { "small", "medium", "large" }.Contains(o.Size) || !new[] { "color1", "color2", "color3", "color4" }.Contains(o.Color) || !new[] { "vertical", "horizontal" }.Contains(o.Direction) || !double.IsFinite(o.X) || !double.IsFinite(o.Y) || !double.IsFinite(o.LineAdvancePx) || o.LineAdvancePx < 0 || o.LineAdvancePx > 3000) throw new InvalidDataException("テキスト情報が不正です。");
   }
  }
 }
}

