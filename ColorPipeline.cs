using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;

public record ImageInfo(int Width, int Height, double DpiX, double DpiY, bool HasProfile, ColorContext? Context);
public static class ColorPipeline
{
 // WPF maps 8-bit Bgra32 to the standard sRGB color context.
 public static ColorContext Srgb => new(PixelFormats.Bgra32);
 public static ImageInfo Inspect(string path)
 {
  using var stream = File.OpenRead(path);
  var frame = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None).Frames[0];
  var context = frame.ColorContexts?.FirstOrDefault();
  return new(frame.PixelWidth, frame.PixelHeight, ValidDpi(frame.DpiX), ValidDpi(frame.DpiY), context is not null, context);
 }
 private static double ValidDpi(double dpi) => double.IsFinite(dpi) && dpi > 0 ? dpi : 96;
 public static BitmapSource Load(string path, int decode = 0)
 {
  var info = Inspect(path);
  using var stream = File.OpenRead(path);
  var bitmap = new BitmapImage(); bitmap.BeginInit();
  bitmap.CacheOption = BitmapCacheOption.OnLoad;
  bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
  bitmap.StreamSource = stream; if (decode > 0) bitmap.DecodePixelWidth = decode;
  bitmap.EndInit(); bitmap.Freeze();
  if (info.Context is null) return bitmap; // Untagged RGB is explicitly assumed to be sRGB.
  var converted = new ColorConvertedBitmap(bitmap, info.Context, Srgb, PixelFormats.Bgra32);
  converted.Freeze(); return converted;
 }
 public static void Encode(BitmapSource source, string output, ImageInfo info, bool jpeg)
 {
  // Change only the resolution metadata, never the rendered pixel dimensions.
  int stride = checked(source.PixelWidth * 4);
  var pixels = new byte[checked(stride * source.PixelHeight)]; source.CopyPixels(pixels, stride, 0);
  BitmapSource tagged = BitmapSource.Create(source.PixelWidth, source.PixelHeight, info.DpiX, info.DpiY, PixelFormats.Pbgra32, null, pixels, stride);
  tagged = new FormatConvertedBitmap(tagged, jpeg ? PixelFormats.Bgr24 : PixelFormats.Bgra32, null, 0);
  BitmapEncoder encoder = jpeg ? new JpegBitmapEncoder { QualityLevel = 100 } : new PngBitmapEncoder();
  encoder.Frames.Add(BitmapFrame.Create(tagged, null, null, new ReadOnlyCollection<ColorContext>([Srgb])));
  var temporary = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
  try
  {
   using (var file = new FileStream(temporary, FileMode.CreateNew)) encoder.Save(file);
   File.Move(temporary, output, false);
  }
  finally { if (File.Exists(temporary)) File.Delete(temporary); }
 }
}

