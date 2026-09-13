using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class WeightTests
{
 public static void Run(string folder, Action<bool, string> check)
 {
  var selected = FontCatalog.Installed.First(f => f.Key.Contains("SourceHanSansCN-VF.otf", StringComparison.OrdinalIgnoreCase));
  var choices = VerticalLayout.Weights(selected.Key);
  File.WriteAllLines(Path.Combine(folder, "weight-options.txt"), choices.Select(c => c.Label));
  check(new[] { 400d, 500d, 700d }.All(w => choices.Any(c => c.Value == w)), "Source Han VF exposes Regular/Medium/Bold");
  var config = new Config(); config.Styles["ZH"].Font = selected.Key; config.Styles["ZH"].Sizes["medium"] = 64; config.Styles["ZH"].OutlineWidth = 0;
  var counts = new List<long>(); var verticalCounts = new List<long>();
  var specimen = new DrawingVisual();
  using (var dc = specimen.RenderOpen())
  {
   dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 1100, 450));
   int row = 0;
   foreach (var weight in new[] { 400d, 500d, 700d })
   {
    config.Styles["ZH"].FontWeight = weight;
    var obj = new TextObject { Text = "思源黑体 中文测试", Direction = "horizontal" };
    var path = TextRenderer.Geometry(obj, "ZH", config);
    long Coverage(Geometry geometry)
    {
     var v = new DrawingVisual(); using (var draw = v.RenderOpen()) draw.DrawGeometry(Brushes.Black, null, geometry);
     var bitmap = new RenderTargetBitmap(1000, 500, 96, 96, PixelFormats.Pbgra32); bitmap.Render(v);
     var bytes = new byte[1000 * 500 * 4]; bitmap.CopyPixels(bytes, 1000 * 4, 0);
     return Enumerable.Range(0, 1000 * 500).Sum(i => (long)bytes[i * 4 + 3]);
    }
    counts.Add(Coverage(path));
    obj.Text = "测试！！"; obj.Direction = "vertical"; verticalCounts.Add(Coverage(TextRenderer.Geometry(obj, "ZH", config)));
    var label = new FormattedText($"wght {weight}", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 24, Brushes.Gray, 1);
    dc.DrawText(label, new Point(20, row * 140 + 30)); dc.PushTransform(new TranslateTransform(190, row * 140));
    dc.DrawGeometry(Brushes.Black, null, path); dc.Pop(); row++;
   }
  }
  check(counts[0] > 0 && counts[0] < counts[1] && counts[1] < counts[2], "Variable horizontal glyph ink increases Regular < Medium < Bold");
  check(verticalCounts[0] > 0 && verticalCounts[0] < verticalCounts[1] && verticalCounts[1] < verticalCounts[2], "Variable vertical glyph ink increases Regular < Medium < Bold");
  var output = new RenderTargetBitmap(1100, 450, 96, 96, PixelFormats.Pbgra32); output.Render(specimen);
  var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(output));
  using (var file = File.Create(Path.Combine(folder, "variable-weights.png"))) encoder.Save(file);
  var configPath = Path.Combine(folder, "variable-settings.json"); Storage.Write(configPath, config);
  check(Storage.Read<Config>(configPath).Styles["ZH"].FontWeight == 700, "Variable weight persists in application settings");
  check(JsonSerializer.Deserialize<LanguageStyle>("{}")!.FontWeight == 0, "Old settings retain default rendering");
  foreach (var weight in new[] { 400d, 500d, 700d })
  {
   config.Styles["ZH"].FontWeight = weight;
   var page = new Page { Source = Path.GetFullPath(Path.Combine(folder, "source.png")), Width = 1200, Height = 900 };
   page.Layers["ZH"].Objects.Add(new TextObject { Text = "中文测试", X = 60, Y = 60 });
   var path = Path.GetFullPath(Path.Combine(folder, $"export-weight-{weight}.png"));
   MainWindow.RunSta(() => TextRenderer.Export(page, "ZH", config, path)).GetAwaiter().GetResult();
   check(File.Exists(path), $"Variable weight {weight} exports on background STA");
  }
 }
}

