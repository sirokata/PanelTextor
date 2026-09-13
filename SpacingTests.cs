using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class SpacingTests
{
 public static void Run(string folder, Action<bool, string> check)
 {
  var config = new Config();
  foreach (var lang in Config.Languages)
  {
   config.Styles[lang].Sizes["medium"] = 72;
   var obj = new TextObject { Text = "HHH\nHHH" };
   var single = TextRenderer.Geometry(new TextObject { Text = "HHH" }, lang, config).Bounds;
   config.Styles[lang].LineHeightPx = 60; var a = TextRenderer.Geometry(obj, lang, config).Bounds;
   config.Styles[lang].LineHeightPx = 120; var b = TextRenderer.Geometry(obj, lang, config).Bounds;
   check(Math.Abs(b.Height - a.Height - 60) < .01, lang + " horizontal leading changes by exactly the specified px");
   check(Math.Abs(a.Y - single.Y) < .01 && Math.Abs(b.Y - single.Y) < .01, lang + " leading no longer moves the first line");
   obj.LineAdvancePx = 95; var custom = TextRenderer.Geometry(obj, lang, config).Bounds;
   check(Math.Abs(custom.Height - single.Height - 95) < .01, lang + " per-object leading overrides global setting");
   obj.Text = "HHH\n\nHHH"; var empty = TextRenderer.Geometry(obj, lang, config).Bounds;
   check(Math.Abs(empty.Height - single.Height - 190) < .01, lang + " blank lines preserve their leading");
  }
  var vertical = new TextObject { Text = "日日\n日日", Direction = "vertical" };
  var before = TextRenderer.Geometry(vertical, "JP", config).Bounds;
  config.Styles["JP"].VerticalColumnEm = 2.2;
  var after = TextRenderer.Geometry(vertical, "JP", config).Bounds;
  check(Math.Abs(after.Width - before.Width - 72) < .01, "JP vertical default column spacing uses em");
  vertical.LineAdvancePx = 50; before = TextRenderer.Geometry(vertical, "JP", config).Bounds;
  vertical.LineAdvancePx = 100; after = TextRenderer.Geometry(vertical, "JP", config).Bounds;
  check(Math.Abs(after.Width - before.Width - 50) < .01, "JP vertical object leading uses exact px");
  var roundtrip = JsonSerializer.Deserialize<TextObject>(JsonSerializer.Serialize(vertical))!;
  check(roundtrip.LineAdvancePx == 100, "Object leading survives save/reopen");
  check(JsonSerializer.Deserialize<TextObject>("{}")!.LineAdvancePx == 0, "Old objects keep global/default spacing");
  var visual = new DrawingVisual();
  using (var dc = visual.RenderOpen())
  {
   dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 700, 400));
   var obj = new TextObject { Text = "HHH\nHHH", X = 20, Y = 20, LineAdvancePx = 80 };
   dc.PushTransform(new TranslateTransform(20, 20)); dc.DrawDrawing(TextRenderer.Drawing(obj, "EN", config)); dc.Pop();
   obj.LineAdvancePx = 150;
   dc.PushTransform(new TranslateTransform(350, 20)); dc.DrawDrawing(TextRenderer.Drawing(obj, "EN", config)); dc.Pop();
  }
  var bitmap = new RenderTargetBitmap(700, 400, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
  var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(Path.Combine(folder, "spacing-80-vs-150.png")); encoder.Save(file);
 }
}

