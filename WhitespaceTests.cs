using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class WhitespaceTests
{
 public static void Run(string folder, Action<bool, string> check)
 {
  var config = new Config();
  var samples = new[] { "文字 \n次の行", " 文字", "文 字", "文字\u3000\n次の行", "\u3000文字", "文\u3000字", "文\t字", "\t文字", "文字\t\n次", " \u3000\t", "", "文\u00a0字", "文\u200b字" };
  foreach (var lang in Config.Languages)
  foreach (var direction in new[] { "vertical", "horizontal" })
  foreach (var sample in samples)
  {
   var obj = new TextObject { Text = sample, Direction = direction };
   var visual = new TextVisual(obj, lang, config);
   var drawing = TextRenderer.Drawing(obj, lang, config);
   var dv = new DrawingVisual(); using (var dc = dv.RenderOpen()) dc.DrawDrawing(drawing);
   var image = new RenderTargetBitmap(300, 300, 96, 96, PixelFormats.Pbgra32); image.Render(dv);
   obj.LineAdvancePx = 70; _ = TextRenderer.Drawing(obj, lang, config);
   check(double.IsFinite(visual.Width) && double.IsFinite(visual.Height), $"Whitespace render {lang}/{direction}: {JsonSerializer.Serialize(sample)}");
  }
  var split = Storage.Split(" 文 字 \r\n次の行\u3000\r\n\u3000\t\r\n\u3000別の文 \r\n");
  check(split.SequenceEqual(new[] { " 文 字 \n次の行\u3000", "\u3000別の文 " }), "Split preserves leading/interior/trailing spaces and treats whitespace-only lines as separators");
  check(Storage.Split(" \t\u3000\r\n \u3000").Length == 0, "Whitespace-only input creates no blocks");
  var plain = TextRenderer.Geometry(new TextObject { Text = "字", Direction = "vertical" }, "JP", config).Bounds;
  var leading = TextRenderer.Geometry(new TextObject { Text = " 字", Direction = "vertical" }, "JP", config).Bounds;
  var trailing = TextRenderer.Geometry(new TextObject { Text = "字 ", Direction = "vertical" }, "JP", config).Bounds;
  check(Math.Abs(leading.Y - plain.Y - config.Styles["JP"].Sizes["medium"]) < .01 && trailing == plain, "Vertical spaces retain advance without changing visible trailing glyphs");
  var source = Path.GetFullPath(Path.Combine(folder, "whitespace-source.png"));
  var canvas = new DrawingVisual(); using (var dc = canvas.RenderOpen()) dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 300, 300));
  var bitmap = new RenderTargetBitmap(300, 300, 96, 96, PixelFormats.Pbgra32); bitmap.Render(canvas);
  var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var file = File.Create(source)) encoder.Save(file);
  var page = new Page { Source = source, Width = 300, Height = 300 };
  page.Layers["JP"].Objects = Storage.Split(" 先頭 \n文 字\u3000\n\u3000\n\u3000次の文 ").Select(t => new TextObject { Text = t, Direction = "vertical", X = 20, Y = 20 }).ToList();
  var projectPath = Path.GetFullPath(Path.Combine(folder, "whitespace.polytext.json"));
  ImageSets.Save(projectPath, new Project { Pages = [page] });
  var restored = ImageSets.Read(projectPath).Pages[0];
  check(restored.Layers["JP"].Objects[0].Text == page.Layers["JP"].Objects[0].Text, "Whitespace survives project save/reopen");
  var output = Path.GetFullPath(Path.Combine(folder, "whitespace-export.png")); TextRenderer.Export(restored, "JP", config, output);
  check(TextRenderer.Dimensions(output) == (300, 300), "Export with leading/interior/trailing spaces succeeds");
 }
}

