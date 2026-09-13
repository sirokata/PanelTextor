using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using HB = HarfBuzzSharp;
namespace PanelTextor;

// UAX #50 data, with Japanese upright punctuation and tate-chu-yoko tailoring.
internal static class VerticalOrientation
{
 private static readonly (int Start, int End, string Value)[] ranges = Load();
 private static (int, int, string)[] Load()
 {
  using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PanelTextor.Data.VerticalOrientation.txt")!;
  using var reader = new StreamReader(stream); var result = new List<(int, int, string)>();
  while (reader.ReadLine() is { } line)
  {
   var fields = line.Split('#')[0].Split(';'); if (fields.Length < 2) continue;
   var bounds = fields[0].Trim().Split("..");
   int first = int.Parse(bounds[0], NumberStyles.HexNumber);
   result.Add((first, bounds.Length == 1 ? first : int.Parse(bounds[1], NumberStyles.HexNumber), fields[1].Trim()));
  }
  return result.OrderBy(r => r.Item1).ToArray();
 }
 public static string Of(int scalar)
 {
  int lo = 0, hi = ranges.Length - 1;
  while (lo <= hi) { int mid = (lo + hi) / 2; var r = ranges[mid]; if (scalar < r.Start) hi = mid - 1; else if (scalar > r.End) lo = mid + 1; else return r.Value; }
  return "R";
 }
}
internal record VerticalToken(string Text, bool Combined);
internal record ShapedGlyph(ushort Id, double X, double Y);
internal record VerticalShape(ShapedGlyph[] Glyphs, bool Alternate);
public record WeightChoice(double Value, string Label);
internal sealed class VerticalFont : IDisposable
{
 private readonly HB.Blob blob;
 private readonly HB.Face face;
 private readonly HB.Font font;
 private readonly bool hasVrt2;
 private readonly Dictionary<(string, bool), VerticalShape> cache = [];
 private readonly Dictionary<ushort, Geometry> outlines = [];
 private readonly bool varied;
 private readonly string language;
 public bool HasWeightAxis => face.TryFindVariationAxis(new HB.Tag('w','g','h','t'), out _);
 public double Ascender => font.TryGetHorizontalFontExtents(out var e) ? (double)e.Ascender / face.UnitsPerEm : Glyph.Baseline;
 public double LineHeight => font.TryGetHorizontalFontExtents(out var e) ? (double)(e.Ascender - e.Descender + e.LineGap) / face.UnitsPerEm : 1.2;
 public GlyphTypeface Glyph { get; }
 public VerticalFont(GlyphTypeface glyph, double weight = 0, string language = "ja")
 {
  this.language = language;
  Glyph = glyph; blob = HB.Blob.FromFile(glyph.FontUri.LocalPath);
  // Collections need the actual family/weight, not blindly face index zero.
  int bestIndex = 0, bestScore = int.MinValue;
  for (int i = 0; i < blob.FaceCount; i++)
  {
   using var candidate = new HB.Face(blob, i); int score = 0;
   using var names = candidate.ReferenceTable(new HB.Tag('n','a','m','e'));
   var table = names.AsSpan();
   if (table.Length >= 6)
   {
    int count = Read16(table, 2), strings = Read16(table, 4);
    for (int j = 0; j < count && 6 + j * 12 + 12 <= table.Length; j++)
    {
     int at = 6 + j * 12, id = Read16(table, at + 6), length = Read16(table, at + 8), offset = strings + Read16(table, at + 10);
     if (Read16(table, at) is not (0 or 3) || offset + length > table.Length) continue;
     var name = Encoding.BigEndianUnicode.GetString(table.Slice(offset, length));
     if (id is 1 or 16 && glyph.FamilyNames.Values.Contains(name)) score += 100;
     if (id is 2 or 17 && glyph.FaceNames.Values.Contains(name)) score += 30;
    }
   }
   using var os2 = candidate.ReferenceTable(new HB.Tag('O','S','/','2'));
   if (os2.Length >= 6) score -= Math.Abs(Read16(os2.AsSpan(), 4) - glyph.Weight.ToOpenTypeWeight());
   if (score > bestScore) { bestScore = score; bestIndex = i; }
  }
  face = new HB.Face(blob, bestIndex); font = new HB.Font(face);
  font.SetFunctionsOpenType(); font.SetScale(face.UnitsPerEm, face.UnitsPerEm);
  if (weight > 0 && face.TryFindVariationAxis(new HB.Tag('w','g','h','t'), out var axis))
  {
   if (!double.IsFinite(weight) || weight < axis.MinValue || weight > axis.MaxValue) throw new ArgumentOutOfRangeException(nameof(weight), "フォントの対応範囲外のウェイトです。");
   font.SetVariations([new HB.Variation { Tag = new HB.Tag('w','g','h','t'), Value = (float)weight }]); varied = true;
  }
  using var gsub = face.ReferenceTable(new HB.Tag('G','S','U','B'));
  var data = gsub.AsSpan();
  if (data.Length >= 10)
  {
   int features = Read16(data, 6);
   if (features + 2 <= data.Length)
   {
    int count = Read16(data, features);
    for (int i = 0; i < count && features + 2 + i * 6 + 6 <= data.Length; i++)
     if (data.Slice(features + 2 + i * 6, 4).SequenceEqual("vrt2"u8)) hasVrt2 = true;
   }
  }
 }
 private static int Read16(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
 public IReadOnlyList<WeightChoice> Weights()
 {
  var result = new List<WeightChoice> { new(0, "既定（従来の表示）") };
  if (!face.TryFindVariationAxis(new HB.Tag('w','g','h','t'), out var axis)) return result;
  string Name(int id)
  {
   // Do not capture Span across the local function.
   using var nameBlob = face.ReferenceTable(new HB.Tag('n','a','m','e')); var bytes = nameBlob.AsSpan();
   if (bytes.Length < 6) return "";
   int count = Read16(bytes, 2), start = Read16(bytes, 4); string found = "";
   for (int j = 0; j < count && 6 + j * 12 + 12 <= bytes.Length; j++)
   {
    int at = 6 + j * 12, len = Read16(bytes, at + 8), off = start + Read16(bytes, at + 10);
    if (Read16(bytes, at + 6) != id || Read16(bytes, at) is not (0 or 3) || off + len > bytes.Length) continue;
    found = Encoding.BigEndianUnicode.GetString(bytes.Slice(off, len));
    if (Read16(bytes, at + 4) == 0x409) return found;
   }
   return found;
  }
  for (int i = 0; i < face.NamedInstanceCount; i++)
  {
   var coordinates = face.GetNamedInstanceDesignCoords(i);
   if (coordinates.Length <= axis.AxisIndex) continue;
   double value = coordinates[axis.AxisIndex]; string label = Name((int)face.GetNamedInstanceSubfamilyNameId(i));
   if (result.Any(w => w.Value == value)) continue;
   result.Add(new(value, (string.IsNullOrEmpty(label) ? "Weight" : label) + $" ({value:0.##})"));
  }
  foreach (var (value, label) in new[] { (100, "Thin"), (200, "ExtraLight"), (300, "Light"), (400, "Regular"), (500, "Medium"), (600, "SemiBold"), (700, "Bold"), (800, "ExtraBold"), (900, "Black") })
   if (value >= axis.MinValue && value <= axis.MaxValue && !result.Any(w => w.Value == value)) result.Add(new(value, label + $" ({value})"));
  return result.OrderBy(w => w.Value).ToArray();
 }
 public Geometry Outline(ushort glyph, double size)
 {
  if (!varied) return Glyph.GetGlyphOutline(glyph, size, size);
  if (!outlines.TryGetValue(glyph, out var outline))
  {
   if (outlines.Count >= 2048) outlines.Clear();
   outlines[glyph] = outline = HarfBuzzOutline.Build(font.Handle, glyph, face.UnitsPerEm);
  }
  return new GeometryGroup { Children = { outline }, Transform = new ScaleTransform(size, size) };
 }
 public VerticalShape Shape(string text, bool vertical)
 {
  if (cache.TryGetValue((text, vertical), out var result)) return result;
  using var buffer = new HB.Buffer(); buffer.AddUtf16(text); buffer.GuessSegmentProperties();
  buffer.Direction = vertical ? HB.Direction.TopToBottom : HB.Direction.LeftToRight;
  buffer.Language = new HB.Language(language);
  // Prefer the font's comprehensive vertical feature when supplied.
  // Features are mutually exclusive; retain default vert when vrt2 is absent.
  if (vertical && hasVrt2)
   font.Shape(buffer, [new HB.Feature(new HB.Tag('v','e','r','t'), 0), new HB.Feature(new HB.Tag('v','r','t','2'), 1)]);
  else font.Shape(buffer);
  var infos = buffer.GlyphInfos; var positions = buffer.GlyphPositions; var output = new List<ShapedGlyph>();
  double penX = 0, penY = 0, em = face.UnitsPerEm;
  for (int i = 0; i < infos.Length; i++)
  {
   output.Add(new((ushort)infos[i].Codepoint, (penX + positions[i].XOffset) / em, -(penY + positions[i].YOffset) / em));
   penX += positions[i].XAdvance; penY += positions[i].YAdvance;
  }
  bool alternate = vertical && !output.Select(g => g.Id).SequenceEqual(Shape(text, false).Glyphs.Select(g => g.Id));
  result = new(output.ToArray(), alternate);
  if (cache.Count >= 1024) cache.Clear();
  cache[(text, vertical)] = result; return result;
 }
 public void Dispose() { font.Dispose(); face.Dispose(); blob.Dispose(); }
}
internal static class VerticalLayout
{
 private static readonly object gate = new();
 private static readonly Dictionary<string, VerticalFont> fonts = [];
 public static IReadOnlyList<VerticalToken> Tokens(string text)
 {
  var elements = new List<string>(); var e = StringInfo.GetTextElementEnumerator(text);
  while (e.MoveNext()) elements.Add(e.GetTextElement());
  var tokens = new List<VerticalToken>();
  for (int i = 0; i < elements.Count;)
  {
   bool bang = elements[i] is "!" or "?";
   bool digit = elements[i].Length == 1 && elements[i][0] is >= '0' and <= '9';
   if (bang || digit)
   {
    int end = i + 1;
    while (end < elements.Count && (bang ? elements[end] is "!" or "?" : elements[end].Length == 1 && elements[end][0] is >= '0' and <= '9')) end++;
    int length = end - i;
    if (bang && length >= 2 || digit && length is 2 or 3)
    { tokens.Add(new(string.Concat(elements.Skip(i).Take(length)), true)); i = end; continue; }
    // A long numeric run is deliberately not split into false 2–3 digit groups.
    for (; i < end; i++) tokens.Add(new(elements[i], false));
    continue;
   }
   tokens.Add(new(elements[i++], false));
  }
  return tokens;
 }
 private static VerticalFont Font(GlyphTypeface glyph, double weight = 0, string language = "ja")
 {
  var key = glyph.FontUri + "|" + string.Join("/", glyph.FamilyNames.Values) + "|" + string.Join("/", glyph.FaceNames.Values) + "|" + weight.ToString(CultureInfo.InvariantCulture) + "|" + language;
  if (!fonts.TryGetValue(key, out var font))
  {
   if (fonts.Count >= 8) { var old = fonts.First(); old.Value.Dispose(); fonts.Remove(old.Key); }
   fonts[key] = font = new VerticalFont(glyph, weight, language);
  }
  return font;
 }
 private static GlyphTypeface? GlyphFor(Typeface primary, string text)
 {
  int code = text.EnumerateRunes().First().Value;
  if (primary.TryGetGlyphTypeface(out var glyph) && glyph.CharacterToGlyphMap.ContainsKey(code)) return glyph;
  foreach (var family in new[] { "Yu Mincho", "Yu Gothic", "Meiryo", "Segoe UI Symbol" })
   if (new Typeface(family).TryGetGlyphTypeface(out var fallback) && fallback.CharacterToGlyphMap.ContainsKey(code)) return fallback;
  return glyph;
 }
 private static Geometry Outline(VerticalFont font, VerticalShape shape, double size, bool vertical)
 {
  var result = new GeometryGroup { FillRule = FillRule.Nonzero };
  foreach (var g in shape.Glyphs)
  {
   var outline = font.Outline(g.Id, size);
   if (outline.IsEmpty()) continue;
   var wrapped = new GeometryGroup();
   wrapped.Children.Add(outline);
   wrapped.Transform = new TranslateTransform((g.X + (vertical ? .5 : 0)) * size, g.Y * size);
   result.Children.Add(wrapped);
  }
  return result;
 }
 private static Geometry Fit(Geometry source, double size, bool combined, bool rotate, UnicodeCategory category)
 {
  if (source.IsEmpty()) return source;
  var transform = new TransformGroup();
  if (rotate) transform.Children.Add(new RotateTransform(90));
  var b = rotate ? new RotateTransform(90).TransformBounds(source.Bounds) : source.Bounds;
  double sx = combined ? Math.Min(1, .94 * size / b.Width) : 1;
  double sy = combined ? Math.Min(1, .90 * size / b.Height) : 1;
  transform.Children.Add(new ScaleTransform(sx, sy));
  double x = (size - b.Width * sx) / 2 - b.X * sx;
  double y = (size - b.Height * sy) / 2 - b.Y * sy;
  // Semantic fallbacks for fonts with no vertical alternates, expressed in em.
  if (rotate && category == UnicodeCategory.OpenPunctuation) y = .08 * size - b.Y * sy;
  if (rotate && category == UnicodeCategory.ClosePunctuation) y = .92 * size - b.Bottom * sy;
  transform.Children.Add(new TranslateTransform(x, y));
  return new GeometryGroup { Children = { source }, Transform = transform };
 }
 internal static bool HasAlternate(string fontName, string text)
 {
  lock (gate) { var glyph = GlyphFor(FontCatalog.Typeface(fontName), text); return glyph is not null && Font(glyph).Shape(text, true).Alternate; }
 }
 public static IReadOnlyList<WeightChoice> Weights(string name)
 {
  lock (gate) { return FontCatalog.Typeface(name).TryGetGlyphTypeface(out var glyph) ? Font(glyph).Weights() : [new(0, "既定（従来の表示）")]; }
 }
 public static Geometry? VariableHorizontal(TextObject obj, string lang, Config config)
 {
  var style = config.Styles[lang]; if (style.FontWeight <= 0) return null;
  lock (gate)
  {
   if (!FontCatalog.Typeface(style.Font).TryGetGlyphTypeface(out var glyph)) return null;
   var font = Font(glyph, style.FontWeight, lang switch { "ZH" => "zh-CN", "KO" => "ko", "EN" => "en", _ => "ja" });
   if (!font.HasWeightAxis) return null;
   var result = new GeometryGroup { FillRule = FillRule.Nonzero }; double size = style.Sizes[obj.Size];
   double advance = obj.LineAdvancePx > 0 ? obj.LineAdvancePx : style.LineHeightPx > 0 ? style.LineHeightPx : font.LineHeight * size;
   var lines = obj.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
   for (int row = 0; row < lines.Length; row++)
   {
    var shaped = font.Shape(lines[row], false); var path = Outline(font, shaped, size, false);
    if (!path.IsEmpty()) result.Children.Add(new GeometryGroup { Children = { path }, Transform = new TranslateTransform(0, font.Ascender * size + row * advance) });
   }
   result.Freeze(); return result;
  }
 }
 public static Geometry Build(TextObject obj, string lang, Config config)
 {
  var style = config.Styles[lang]; double size = style.Sizes[obj.Size];
  var typeface = FontCatalog.Typeface(style.Font);
  var lines = obj.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
  var result = new GeometryGroup { FillRule = FillRule.Nonzero };
  double columns = obj.LineAdvancePx > 0 ? obj.LineAdvancePx : size * style.VerticalColumnEm;
  lock (gate)
  {
   for (int column = 0; column < lines.Length; column++)
   {
    int row = 0;
    foreach (var token in Tokens(lines[column]))
    {
     double x = (lines.Length - 1 - column) * columns, y = row++ * size;
     if (string.IsNullOrWhiteSpace(token.Text)) continue;
     var glyph = GlyphFor(typeface, token.Text); if (glyph is null) continue;
     var font = Font(glyph, style.FontWeight); var shaped = font.Shape(token.Text, true);
     int scalar = token.Text.EnumerateRunes().First().Value; string orientation = VerticalOrientation.Of(scalar);
     var category = Rune.GetUnicodeCategory(new Rune(scalar));
     Geometry geometry;
     if (token.Combined)
      geometry = Fit(Outline(font, font.Shape(token.Text, false), size, false), size, true, false, category);
     else if (shaped.Alternate)
      geometry = Outline(font, shaped, size, true);
     else if (scalar is 0x21 or 0x3f || scalar is >= 0x30 and <= 0x39)
      geometry = Fit(Outline(font, font.Shape(token.Text, false), size, false), size, false, false, category);
     else if (orientation is "R" or "Tr")
      geometry = Fit(Outline(font, font.Shape(token.Text, false), size, false), size, false, true, category);
     else
     {
      geometry = Outline(font, shaped, size, true);
      if (scalar is 0x3001 or 0x3002 or 0xff0c or 0xff0e && !geometry.IsEmpty())
      {
       // UAX50 Tu without a font alternate: punctuation belongs top-right.
       var b = geometry.Bounds;
       geometry = new GeometryGroup { Children = { geometry }, Transform = new TranslateTransform(size * .9 - b.Right, size * .1 - b.Top) };
      }
     }
     if (!geometry.IsEmpty()) result.Children.Add(new GeometryGroup { Children = { geometry }, Transform = new TranslateTransform(x, y) });
    }
   }
  }
  result.Freeze(); return result;
 }
}

