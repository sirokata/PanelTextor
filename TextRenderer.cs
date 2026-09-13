using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
public static class TextRenderer
{
 public static Brush Brush(string hex) { var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); return b; }
 public static Geometry Geometry(TextObject obj, string lang, Config config)
 {
  if (obj.Direction == "vertical") return VerticalLayout.Build(obj, lang, config);
  if (VerticalLayout.VariableHorizontal(obj, lang, config) is { } variable) return variable;
  var style = config.Styles[lang]; var size = style.Sizes[obj.Size]; var typeface = FontCatalog.Typeface(style.Font);
  FormattedText Format(string text)
  {
   var formatted = new FormattedText(text, CultureInfo.GetCultureInfo(lang switch { "JP" => "ja-JP", "ZH" => "zh-CN", "KO" => "ko-KR", _ => "en-US" }), FlowDirection.LeftToRight, typeface, size, Brushes.Black, 1);
   return formatted;
  }
  var text = obj.Text.Replace("\r\n", "\n").Replace('\r', '\n');
  var lines = text.Split('\n');
  var group = new GeometryGroup();
  if (obj.Direction != "vertical")
  {
   group.FillRule = FillRule.Nonzero;
   var advance = obj.LineAdvancePx > 0 ? obj.LineAdvancePx : style.LineHeightPx;
   if (advance <= 0) return Format(text).BuildGeometry(new Point());
   // FormattedText.LineHeight also shifts the first baseline. Lay out individual
   // lines instead so changing the leading leaves the first line anchored.
   for (int row = 0; row < lines.Length; row++)
    if (lines[row].Length > 0) group.Children.Add(Format(lines[row]).BuildGeometry(new Point(0, row * advance)));
   return group;
  }
  return group;
 }
 public static DrawingGroup Drawing(TextObject obj, string lang, Config config)
 {
  var style = config.Styles[lang]; var geometry = Geometry(obj, lang, config); var result = new DrawingGroup();
  using (var dc = result.Open())
  {
   if (style.OutlineWidth > 0) dc.DrawGeometry(null, new Pen(Brush(style.OutlineColor), style.OutlineWidth * 2) { LineJoin = PenLineJoin.Round }, geometry);
   dc.DrawGeometry(Brush(config.Colors[obj.Color]), null, geometry);
  }
  result.Freeze(); return result;
 }
 public static BitmapSource Load(string path, int decode = 0)
 {
  return ColorPipeline.Load(path, decode);
 }
 public static (int Width, int Height) Dimensions(string path)
 {
  using var stream = File.OpenRead(path); var frame = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0]; return (frame.PixelWidth, frame.PixelHeight);
 }
 public static void Export(Page page, string lang, Config config, string output, string? background = null, bool jpeg = false)
 {
  background ??= page.Source;
  if (string.Equals(Path.GetFullPath(background), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase)) throw new IOException("元画像への上書きは禁止しています。");
  if (File.Exists(output)) throw new IOException("既存ファイルへの上書きは禁止しています。");
  var info = ColorPipeline.Inspect(background);
  if ((info.Width, info.Height) != (page.Width, page.Height)) throw new IOException("背景画像の解像度がページと一致しません。");
  var bitmap = Load(background); var visual = new DrawingVisual();
  using (var dc = visual.RenderOpen())
  {
   if (jpeg) dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, page.Width, page.Height));
   dc.DrawImage(bitmap, new Rect(0, 0, page.Width, page.Height));
   foreach (var obj in page.Layers[lang].Objects) { dc.PushTransform(new TranslateTransform(obj.X, obj.Y)); dc.DrawDrawing(Drawing(obj, lang, config)); dc.Pop(); }
   dc.DrawDrawing(CameraFrameRenderer.Draw(page));
  }
  var render = new RenderTargetBitmap(page.Width, page.Height, 96, 96, PixelFormats.Pbgra32); render.Render(visual);
  ColorPipeline.Encode(render, output, info, jpeg);
 }
}
public class TextVisual : FrameworkElement
{
 public TextObject Object { get; }
 private readonly DrawingGroup drawing;
 public Rect TextBounds { get; }
 public bool Selected { get; set; }
 public TextVisual(TextObject obj, string lang, Config config)
 {
  Object = obj; drawing = TextRenderer.Drawing(obj, lang, config); TextBounds = drawing.Bounds.IsEmpty ? new Rect(0, 0, 20, 30) : drawing.Bounds;
  Width = Math.Max(1, TextBounds.Right + 8); Height = Math.Max(1, TextBounds.Bottom + 8); IsHitTestVisible = false;
 }
 protected override void OnRender(DrawingContext dc)
 {
  dc.DrawDrawing(drawing); if (Selected) { var r = TextBounds; r.Inflate(5, 5); dc.DrawRectangle(null, new Pen(Brushes.DeepSkyBlue, 2), r); }
 }
}

