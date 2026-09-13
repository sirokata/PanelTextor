using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class CameraFrameTests
{
 public static void Run(string folder, Action<bool, string> check)
 {
  var page = new Page { Source = Path.GetFullPath(Path.Combine(folder, "source.png")), Width = 1200, Height = 900 };
  check(CameraFrameRenderer.Draw(page).Children.Count == 0, "Old pages have no camera overlay by default");
  page.CameraFrame.Enabled = true; page.CameraFrame.Timer.Seconds = 38;
  for (int level = 1; level <= 5; level++)
  {
   page.CameraFrame.Battery.Level = level;
   var drawing = CameraFrameRenderer.Draw(page);
   check(drawing.Children.Count == 5, "Camera elements are independent: battery " + level);
   var battery = (DrawingGroup)drawing.Children[3];
   check(battery.Children.Count == level + 2 && battery.Children.Cast<GeometryDrawing>().All(d => ((SolidColorBrush)(d.Brush ?? d.Pen.Brush)).Color == (level == 1 ? Colors.Red : Colors.White)), "Battery shell and exact number of bars have correct color: " + level);
   var output = Path.Combine(folder, "camera-" + level + ".png");
   MainWindow.RunSta(() => TextRenderer.Export(page, "JP", new Config(), output)).GetAwaiter().GetResult();
   var image = new FormatConvertedBitmap(TextRenderer.Load(output), PixelFormats.Bgra32, null, 0);
   byte[] pixel = new byte[4]; image.CopyPixels(new Int32Rect(1067, 82, 1, 1), pixel, 4, 0);
   check(pixel[2] > 240 && (level == 1 ? pixel[0] < 10 && pixel[1] < 10 : pixel[0] > 240 && pixel[1] > 240), "Export pixels show battery color: " + level);
  }
  var second = new Page { Source = page.Source, Width = 1200, Height = 900 };
  second.CameraFrame.Enabled = true; second.CameraFrame.Battery.Level = 1; second.CameraFrame.Timer.Seconds = 105;
  var project = new Project { Pages = [page, second] };
  var saved = Path.Combine(folder, "camera-project.json"); ImageSets.Save(saved, project); var loaded = ImageSets.Read(saved);
  check(loaded.Pages[0].CameraFrame.Battery.Level == 5 && loaded.Pages[1].CameraFrame.Battery.Level == 1 && loaded.Pages[0].CameraFrame.Timer.Display == "00:38" && loaded.Pages[1].CameraFrame.Timer.Display == "01:45", "Battery and elapsed time roundtrip independently per page");
  check(JsonSerializer.Deserialize<Page>("{}")!.CameraFrame.Battery.Level == 5 && !JsonSerializer.Deserialize<Page>("{}")!.CameraFrame.Enabled, "Missing camera settings migrate to disabled/full battery");
  check(CameraTimer.TryParse("01:02:03", out var seconds) && seconds == 3723 && !CameraTimer.TryParse("00:99", out _) && !CameraTimer.TryParse("bad", out _), "Elapsed time validates mm:ss and hh:mm:ss");
  page.CameraFrame.Recording.Visible = false;
  check(CameraFrameRenderer.Draw(page).Children.Count == 4, "Recording element can be hidden independently");
  TextRenderer.Export(page, "KO", new Config(), Path.Combine(folder, "camera-ko.jpg"), jpeg: true);
  check(ColorPipeline.Inspect(Path.Combine(folder, "camera-ko.jpg")).HasProfile, "Camera JPEG export preserves sRGB profile");
 }
}

