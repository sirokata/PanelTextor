using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
namespace PanelTextor;

public class CameraElement
{
 public bool Visible { get; set; } = true;
 [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
public class CameraBattery : CameraElement
{
 private int level = 5;
 public int Level { get => level; set => level = Math.Clamp(value, 1, 5); }
}
public class CameraTimer : CameraElement
{
 private int seconds;
 public int Seconds { get => seconds; set => seconds = Math.Clamp(value, 0, 359999); }
 [JsonIgnore] public string Display => Seconds < 3600 ? $"{Seconds / 60:00}:{Seconds % 60:00}" : $"{Seconds / 3600:00}:{Seconds / 60 % 60:00}:{Seconds % 60:00}";
 public static bool TryParse(string text, out int seconds)
 {
  seconds = 0; var parts = text.Trim().Split(':');
  if (parts.Length is not (2 or 3) || parts.Any(p => p.Length is < 1 or > 2 || !p.All(char.IsAsciiDigit))) return false;
  var values = parts.Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();
  if (values[^1] > 59 || (parts.Length == 3 && values[1] > 59)) return false;
  seconds = parts.Length == 2 ? values[0] * 60 + values[1] : values[0] * 3600 + values[1] * 60 + values[2];
  return true;
 }
}
public class CameraFrameSettings
{
 public bool Enabled { get; set; }
 public CameraElement Corners { get; set; } = new();
 public CameraElement Recording { get; set; } = new();
 public CameraTimer Timer { get; set; } = new();
 public CameraBattery Battery { get; set; } = new();
 public CameraElement Focus { get; set; } = new();
 // Additional camera elements can be added without discarding unknown project fields.
 [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
public static class CameraFrameRenderer
{
 public static DrawingGroup Draw(Page page)
 {
  var result = new DrawingGroup(); var settings = page.CameraFrame;
  if (!settings.Enabled) { result.Freeze(); return result; }
  double scale = Math.Min(page.Width, page.Height) / 1000d;
  if (scale <= 0) { result.Freeze(); return result; }
  double w = page.Width / scale, h = page.Height / scale;
  result.Transform = new ScaleTransform(scale, scale);
  void Element(bool visible, Action<DrawingContext> draw)
  {
   if (!visible) return;
   var group = new DrawingGroup(); using (var dc = group.Open()) draw(dc);
   result.Children.Add(group);
  }
  var white = new Pen(Brushes.White, 5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
  void Corners(DrawingContext dc, double x, double y, double width, double height, double length, Pen pen)
  {
   foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 })
   {
    double cx = x + (sx == 1 ? width : 0), cy = y + (sy == 1 ? height : 0);
    dc.DrawLine(pen, new Point(cx, cy), new Point(cx - sx * length, cy));
    dc.DrawLine(pen, new Point(cx, cy), new Point(cx, cy - sy * length));
   }
  }
  void Label(DrawingContext dc, string text, double x, double y, Brush color)
  {
   var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 32, color, 1);
   dc.DrawGeometry(color, null, formatted.BuildGeometry(new Point(x, y)));
  }
  Element(settings.Corners.Visible, dc => Corners(dc, 32, 32, w - 64, h - 64, 110, white));
  Element(settings.Recording.Visible, dc => { dc.DrawEllipse(Brushes.Red, null, new Point(76, 95), 15, 15); Label(dc, "REC", 104, 74, Brushes.Red); });
  Element(settings.Timer.Visible, dc => Label(dc, settings.Timer.Display, 190, 74, Brushes.White));
  Element(settings.Battery.Visible, dc =>
  {
   Brush color = settings.Battery.Level == 1 ? Brushes.Red : Brushes.White;
   double x = w - 160, y = 78;
   dc.DrawRoundedRectangle(null, new Pen(color, 4), new Rect(x, y, 90, 36), 4, 4);
   dc.DrawRectangle(color, null, new Rect(x + 93, y + 11, 6, 14));
   for (int i = 0; i < settings.Battery.Level; i++) dc.DrawRectangle(color, null, new Rect(x + 7 + i * 16, y + 7, 12, 22));
  });
  Element(settings.Focus.Visible, dc => Corners(dc, w / 2 - 65, h / 2 - 65, 130, 130, 24, new Pen(Brushes.White, 3)));
  result.Freeze(); return result;
 }
}

