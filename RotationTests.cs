using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class RotationTests
{
 public static void Run(string folder, Action<bool, string> check)
 {
  check(JsonSerializer.Deserialize<TextObject>("{\"Text\":\"old\"}")!.RotationDegrees == 0, "Legacy text defaults to zero rotation");
  var config = new Config(); var visual = new DrawingVisual();
  using (var dc = visual.RenderOpen())
  {
   dc.DrawRectangle(Brushes.LightSlateGray, null, new Rect(0, 0, 1000, 700));
   int row = 0;
   foreach (var (lang, text, direction) in new[] { ("JP", "あ゛〜!!!", "vertical"), ("EN", "Rotate text", "horizontal"), ("ZH", "旋转文字", "horizontal"), ("KO", "텍스트 회전", "horizontal") })
   {
    var obj = new TextObject { Text = text, Direction = direction, X = 200, Y = 70 + row++ * 155 };
    var original = TextRenderer.Drawing(obj, lang, config).Bounds;
    obj.RotationDegrees = 90;
    var rotated = TextRenderer.Drawing(obj, lang, config).Bounds;
    check(Math.Abs(original.Width - rotated.Height) < .01 && Math.Abs(original.Height - rotated.Width) < .01, lang + " right-angle rotation swaps dimensions including outline");
    check(Math.Abs(original.X + original.Width / 2 - rotated.X - rotated.Width / 2) < .01 && Math.Abs(original.Y + original.Height / 2 - rotated.Y - rotated.Height / 2) < .01, lang + " rotation retains center");
    foreach (double angle in new[] { -180, -37.5, 0, 26.2, 180 })
    {
     obj.RotationDegrees = angle;
     var drawing = TextRenderer.Drawing(obj, lang, config);
     var hit = new TextVisual(obj, lang, config);
     var center = new Point(obj.X + original.X + original.Width / 2, obj.Y + original.Y + original.Height / 2);
     check(hit.Contains(center, 0) && !hit.Contains(new Point(-500, -500), 0), lang + " rotated selection at " + angle);
     check(JsonSerializer.Deserialize<TextObject>(JsonSerializer.Serialize(obj))!.RotationDegrees == angle, lang + " rotation persists at " + angle);
    }
    obj.RotationDegrees = row % 2 == 0 ? -25 : 35;
    dc.PushTransform(new TranslateTransform(obj.X, obj.Y)); dc.DrawDrawing(TextRenderer.Drawing(obj, lang, config)); dc.Pop();
   }
  }
  var bitmap = new RenderTargetBitmap(1000, 700, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
  var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
  var source = Path.GetFullPath(Path.Combine(folder, "rotation.png"));
  using (var file = File.Create(source)) encoder.Save(file);
  var page = new Page { Source = source, Width = 1000, Height = 700 };
  var exported = new TextObject { Text = "Rotation export", X = 400, Y = 150, RotationDegrees = -37.5 };
  page.Layers["EN"].Objects.Add(exported);
  var project = new Project(); project.Pages.Add(page);
  var path = Path.Combine(folder, "rotation-project.paneltextor.json"); Storage.Write(path, project);
  var restored = Storage.Read<Project>(path); Storage.Validate(restored);
  check(restored.Pages[0].Layers["EN"].Objects[0].RotationDegrees == -37.5, "Project save and validation retain rotation");
  var rotatedOutput = Path.Combine(folder, "rotation-export.png");
  TextRenderer.Export(page, "EN", config, rotatedOutput);
  exported.RotationDegrees = 0;
  var plainOutput = Path.Combine(folder, "rotation-export-zero.png");
  TextRenderer.Export(page, "EN", config, plainOutput);
  check(!File.ReadAllBytes(rotatedOutput).SequenceEqual(File.ReadAllBytes(plainOutput)), "Image export applies text rotation");
  exported.RotationDegrees = double.NaN;
  bool rejected = false; try { Storage.Validate(project); } catch (InvalidDataException) { rejected = true; }
  check(rejected, "Invalid project rotation is rejected");
 }
}
