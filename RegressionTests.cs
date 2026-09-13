using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class RegressionTests
{
 public static void Run(string folder, Action<bool, string> check)
 {
  folder = Path.GetFullPath(Path.Combine(folder, "regression")); Directory.CreateDirectory(folder);
  string Fixture(string dir, string name, byte tone, int width = 400, double dpi = 300, ColorContext? context = null)
  {
   Directory.CreateDirectory(dir); var path = Path.Combine(dir, name);
   var bytes = Enumerable.Repeat(tone, width * 300 * 3).ToArray();
   var bitmap = BitmapSource.Create(width, 300, dpi, dpi, PixelFormats.Rgb24, null, bytes, width * 3);
   BitmapEncoder encoder = Path.GetExtension(name).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ? new JpegBitmapEncoder { QualityLevel = 100 } : new PngBitmapEncoder();
   encoder.Frames.Add(BitmapFrame.Create(bitmap, null, null, context is null ? null : new ReadOnlyCollection<ColorContext>([context])));
   using var stream = File.Create(path); encoder.Save(stream); return path;
  }
  byte[] Pixels(string path)
  {
   var bitmap = new FormatConvertedBitmap(TextRenderer.Load(path), PixelFormats.Bgra32, null, 0);
   var bytes = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(bytes, bitmap.PixelWidth * 4, 0); return bytes;
  }
  var first = Path.Combine(folder, "Mosaic"); var second = Path.Combine(folder, "BlackMosaic"); var third = Path.Combine(folder, "Third");
  var original = Fixture(first, "001.png", 100); Fixture(first, "002.png", 100);
  Fixture(second, "001.png", 30); Fixture(second, "002.png", 30);
  Fixture(third, "001.png", 200); Fixture(third, "002.png", 200);
  var project = new Project(); check(ImageSets.AddFolder(project, first).Count == 0, "First image set creates pages");
  var page = project.Pages[0]; var text = new TextObject { Text = "HHH", X = 55, Y = 50, Color = "color2" };
  page.Layers["EN"].Objects.Add(text); page.Layers["JP"].Draft = "入力途中"; page.Layers["KO"].Visible = false;
  var originalId = text.Id;
  ImageSets.AddFolder(project, second); ImageSets.AddFolder(project, third);
  check(project.Pages.Count == 2 && project.ImageSets.Count == 3 && ReferenceEquals(page.Layers["EN"].Objects[0], text), "Three sets share a single page-language layout");
  project.ActiveSetId = project.ImageSets[0].Id; var firstPath = ImageSets.Source(project, page);
  project.ActiveSetId = project.ImageSets[1].Id;
  check(firstPath != ImageSets.Source(project, page) && page.Layers["EN"].Objects[0].X == 55, "Background switch changes only the source");
  ImageSets.AddFolder(project, second); check(project.ImageSets.Count == 3, "Readding a folder refreshes existing set");
  var config = new Config(); config.Styles["EN"].Font = "Arial"; config.Styles["EN"].Sizes["medium"] = 60; config.Styles["EN"].OutlineWidth = 0;
  check(FontCatalog.Find("Arial")?.Key == "Arial", "Exact family lookup wins over Bold aliases");
  check(FontCatalog.Typeface("Arial").Weight == FontWeights.Normal, "Arial defaults to Regular, without unintended Bold");
  var hash = SHA256.HashData(File.ReadAllBytes(original));
  var jobs = ImageSets.Check(project, project.Pages, project.ImageSets); check(jobs.Issues.Count == 0 && jobs.Jobs.Count == 6, "Complete preflight creates all six outputs");
  var exports = new List<string>();
  foreach (var job in jobs.Jobs)
  {
   var output = ImageSets.OutputPath(folder, "EN", job.Set, job.Page, false);
   TextRenderer.Export(job.Page, "EN", config, output, job.Source); exports.Add(output);
  }
  check(exports.All(File.Exists) && exports.All(p => p.Contains(Path.DirectorySeparatorChar + "EN" + Path.DirectorySeparatorChar)), "Batch writes language / set / filename hierarchy");
  var rendered = Pixels(exports[0]); var other = Pixels(exports[2]);
  bool IsRed(byte[] p, int i) => p[i] == 0x35 && p[i + 1] == 0x39 && p[i + 2] == 0xe5 && p[i + 3] == 255;
  var mask = Enumerable.Range(0, rendered.Length / 4).Where(i => IsRed(rendered, i * 4)).ToArray();
  check(mask.Length > 100 && mask.All(i => IsRed(other, i * 4)), "Shared text has identical positions and exact sRGB HEX interiors across backgrounds");
  check(rendered[0] != other[0], "Each batch output uses its own background");
  var metadata = ColorPipeline.Inspect(exports[0]);
  check(metadata.HasProfile && Math.Abs(metadata.DpiY - 300) < .1 && metadata.Width == 400, "PNG embeds ICC and retains 300 PPI without resampling");
  var jpegPath = ImageSets.OutputPath(folder, "EN", project.ImageSets[0], page, true);
  TextRenderer.Export(page, "EN", config, jpegPath, original, true);
  var jpegInfo = ColorPipeline.Inspect(jpegPath);
  check(jpegInfo.HasProfile && Math.Abs(jpegInfo.DpiY - 300) < .1 && jpegInfo.Width == 400, "JPEG embeds ICC and retains source PPI");
  check(ImageSets.OutputPath(folder, "EN", project.ImageSets[0], page, false) != exports[0], "Repeated batch output uses a new name");
  check(hash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(original))), "Set export never alters source bytes");
  var lowDpi = Fixture(folder, "72ppi.png", 100, dpi: 72);
  var lowOutput = Path.Combine(folder, "72ppi-out.png"); TextRenderer.Export(page, "EN", config, lowOutput, lowDpi);
  check(Pixels(lowOutput).SequenceEqual(rendered), "Same px text produces identical pixels at 72 and 300 PPI");
  var projectPath = Path.Combine(folder, "sets.polytext.json"); project.Language = "EN"; project.PageIndex = 1;
  ImageSets.Save(projectPath, project); var restored = ImageSets.Read(projectPath);
  check(restored.ImageSets.Count == 3 && restored.ActiveSetId == project.ActiveSetId && restored.PageIndex == 1 && restored.Pages[0].Layers["EN"].Objects[0].Id == originalId, "V2 roundtrip retains active set, page order and shared object IDs");
  check(restored.Pages[0].Layers["JP"].Draft == "入力途中" && !restored.Pages[0].Layers["KO"].Visible, "Draft and language visibility survive migration/save");
  var json = File.ReadAllText(projectPath);
  check(json.Split(originalId).Length == 2 && !json.Contains(folder.Replace("\\", "\\\\")), "V2 stores layout once and uses relative source paths");
  var legacyPath = Path.Combine(folder, "legacy.polytext.json");
  var legacy = new Project { Version = 1, Pages = project.Pages, Language = "EN" }; Storage.Write(legacyPath, legacy);
  var legacyBytes = File.ReadAllBytes(legacyPath); var migrated = ImageSets.Read(legacyPath);
  check(File.ReadAllBytes(legacyPath).SequenceEqual(legacyBytes) && migrated.Version == 2 && migrated.ImageSets.Count == 1, "Legacy open migrates in memory without touching the file");
  ImageSets.Save(legacyPath, migrated);
  check(File.ReadAllBytes(legacyPath + ".v1.bak").SequenceEqual(legacyBytes), "First legacy save preserves byte-exact V1 backup");
  check(ImageSets.Read(legacyPath).Pages[0].Layers["EN"].Objects[0].Id == originalId, "Legacy save preserves the existing layout");
  var oldJson = System.Text.Json.Nodes.JsonNode.Parse(System.Text.Encoding.UTF8.GetString(legacyBytes))!;
  oldJson.AsObject().Remove("ImageSets"); oldJson.AsObject().Remove("ActiveSetId");
  foreach (var oldPage in oldJson["Pages"]!.AsArray()) oldPage!.AsObject().Remove("Id");
  var realOldPath = Path.Combine(folder, "old-schema.polytext.json"); File.WriteAllText(realOldPath, oldJson.ToJsonString());
  var realOld = ImageSets.Read(realOldPath);
  check(realOld.Pages[0].Layers["EN"].Objects[0].X == 55 && realOld.ImageSets[0].Images.Count == 2, "Original schema without page IDs migrates without losing layout");
  var mismatch = Path.Combine(folder, "Mismatch"); Fixture(mismatch, "001.png", 50, 410); Fixture(mismatch, "003.png", 50);
  var warnings = ImageSets.AddFolder(project, mismatch);
  check(warnings.Any(w => w.Contains("不足 002")) && warnings.Any(w => w.Contains("余分な画像 003")) && warnings.Any(w => w.Contains("解像度不一致")), "Add detects missing, extra and mismatched-resolution files");
  var bad = ImageSets.Check(project, project.Pages, [project.ImageSets.Last()]);
  check(bad.Jobs.Count == 0 && bad.Issues.Count == 3, "Invalid backgrounds are excluded from export");
  Fixture(mismatch, "001.png", 50); Fixture(mismatch, "002.png", 50);
  ImageSets.AddFolder(project, mismatch); check(ImageSets.Check(project, project.Pages, [project.ImageSets.Last()]).Jobs.Count == 2, "Refresh after repairs restores exportable pages");
  // Build a valid linear-TRC variant of the standard RGB profile, for a
  // predictable non-sRGB fixture (50% linear should be about 188 in sRGB).
  using var profileStream = ColorPipeline.Srgb.OpenProfileStream();
  using var memory = new MemoryStream(); profileStream.CopyTo(memory); var profile = memory.ToArray();
  int count = (int)BinaryPrimitives.ReadUInt32BigEndian(profile.AsSpan(128, 4));
  for (int t = 0; t < count; t++)
  {
   int entry = 132 + t * 12; var tag = System.Text.Encoding.ASCII.GetString(profile, entry, 4);
   if (tag is not ("rTRC" or "gTRC" or "bTRC")) continue;
   int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(profile.AsSpan(entry + 4, 4));
   "curv"u8.CopyTo(profile.AsSpan(offset, 4)); profile.AsSpan(offset + 4, 4).Clear();
   BinaryPrimitives.WriteUInt32BigEndian(profile.AsSpan(offset + 8, 4), 1);
   BinaryPrimitives.WriteUInt16BigEndian(profile.AsSpan(offset + 12, 2), 256);
   BinaryPrimitives.WriteUInt32BigEndian(profile.AsSpan(entry + 8, 4), 14);
  }
  var iccPath = Path.Combine(folder, "linear-rgb.icc"); File.WriteAllBytes(iccPath, profile);
  var tagged = Fixture(folder, "linear-input.png", 128, context: new ColorContext(new Uri(iccPath)));
  var converted = Pixels(tagged);
  check(converted[0] >= 185 && converted[0] <= 191, "Non-sRGB ICC input is converted numerically to sRGB");
  var convertedOut = Path.Combine(folder, "linear-output.png"); TextRenderer.Export(new Page { Source = tagged, Width = 400, Height = 300 }, "EN", config, convertedOut);
  check(Math.Abs(Pixels(convertedOut)[0] - converted[0]) <= 1, "ICC export/reopen does not apply the source conversion twice");
  var smallPreview = new FormatConvertedBitmap(TextRenderer.Load(tagged, 200), PixelFormats.Bgra32, null, 0); var samplePixel = new byte[4]; smallPreview.CopyPixels(new Int32Rect(0, 0, 1, 1), samplePixel, 4, 0);
  check(Math.Abs(samplePixel[0] - converted[0]) <= 1, "Preview and full export use the same sRGB conversion");
  File.WriteAllText(Path.Combine(folder, "comparison-settings.txt"), "Arial Regular / EN / 60 px / sRGB #E53935 / outline 0 px / 300 PPI. Compare EN/Mosaic/001.png at 100% zoom. 60px = 14.4pt at 300PPI.");
 }
}


