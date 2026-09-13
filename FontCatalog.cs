using System.Globalization;
using System.IO;
using System.Windows.Media;
using Microsoft.Win32;
namespace PanelTextor;

public sealed record FontChoice(string Key, string DisplayName, FontFamily Family)
{
 public override string ToString() => DisplayName;
}

public static class FontCatalog
{
 public static List<string> LoadErrors { get; } = [];
 public static IReadOnlyList<FontChoice> Installed { get; } = Load();
 private static IReadOnlyList<FontChoice> Load()
 {
  var choices = new List<FontChoice>();
  void Add(FontFamily family, string key)
  {
   // Keep Chinese, Korean, Japanese and English aliases visible together.
   var names = family.FamilyNames.OrderBy(n => n.Key.IetfLanguageTag.StartsWith("ja") ? 0 : n.Key.IetfLanguageTag.StartsWith("zh") ? 1 : n.Key.IetfLanguageTag.StartsWith("ko") ? 2 : n.Key.IetfLanguageTag.StartsWith("en") ? 3 : 4)
       .Select(n => n.Value).Distinct(StringComparer.OrdinalIgnoreCase);
   var name = string.Join(" / ", names);
   if (string.IsNullOrEmpty(name)) name = family.Source;
   if (Uri.TryCreate(key.Split('#')[0], UriKind.Absolute, out var uri) && uri.IsFile) name += "  [" + Path.GetFileName(uri.LocalPath) + "]";
   choices.Add(new FontChoice(key, name, family));
  }
  // Per-user fonts are not always returned by WPF's system collection.
  var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  var systemFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
  foreach (var folder in new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts"), systemFolder })
  {
   if (Directory.Exists(folder)) foreach (var file in Directory.EnumerateFiles(folder)) files.Add(file);
  }
  // Installed font files can also live outside the standard font folders.
  foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
  {
   using var registry = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");
   if (registry is null) continue;
   foreach (var value in registry.GetValueNames())
    if (registry.GetValue(value) is string path) files.Add(Path.IsPathRooted(path) ? path : Path.Combine(systemFolder, path));
  }
  foreach (var file in files.Where(f => new[] { ".ttf", ".otf", ".ttc", ".otc" }.Contains(Path.GetExtension(f).ToLowerInvariant())))
  {
    try
    {
     if (new[] { ".ttf", ".otf" }.Contains(Path.GetExtension(file).ToLowerInvariant()))
     {
      // Read each face directly: identical family names in separate files must
      // not cause WPF directory grouping to hide a file or choose another face.
      var glyph = new GlyphTypeface(new Uri(file));
      var familyName = glyph.FamilyNames.FirstOrDefault(n => n.Key.TwoLetterISOLanguageName == "en").Value ?? glyph.FamilyNames.Values.First();
      var key = new Uri(file).AbsoluteUri + "#" + familyName;
      Add(new FontFamily(key), key);
      continue;
     }
     foreach (var family in Fonts.GetFontFamilies(new Uri(file)))
     {
      // WPF may enumerate the containing directory, even for a file URI.
      // Only associate a family with a file that actually contains its glyphs.
      if (!family.GetTypefaces().Any(face => face.TryGetGlyphTypeface(out var glyph) && string.Equals(glyph.FontUri.LocalPath, file, StringComparison.OrdinalIgnoreCase))) continue;
      var name = family.FamilyNames.FirstOrDefault(n => n.Key.IetfLanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase)).Value ?? family.FamilyNames.Values.First();
      var key = new Uri(file).AbsoluteUri + "#" + name;
      Add(new FontFamily(key), key);
     }
    }
    catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or System.Security.SecurityException) { LoadErrors.Add(Path.GetFileName(file) + ": " + ex.Message); }
  }
  foreach (var family in Fonts.SystemFontFamilies) Add(family, family.Source);
  // Different files may share the same family name (Regular/Bold/ExtraBold).
  // Do not discard these faces just because their display names match.
  return choices.DistinctBy(f => f.Key, StringComparer.OrdinalIgnoreCase).OrderBy(f => f.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToArray();
 }
 public static FontChoice? Find(string name) => Installed.FirstOrDefault(f => string.Equals(f.Key, name, StringComparison.OrdinalIgnoreCase))
     ?? Installed.FirstOrDefault(f => string.Equals(f.DisplayName, name, StringComparison.OrdinalIgnoreCase))
     ?? Installed.OrderBy(f => f.Key.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? 1 : 0).FirstOrDefault(f => f.Family.FamilyNames.Values.Any(alias => string.Equals(alias, name, StringComparison.OrdinalIgnoreCase)));
 public static FontFamily Resolve(string name) => Find(name)?.Family ?? new FontFamily(name);
 public static Typeface Typeface(string name)
 {
  var family = Resolve(name);
  // Use a real installed face. A single-file Bold selection must stay Bold;
  // a family selection prefers its actual Regular face without faux styling.
  return family.GetTypefaces().OrderBy(t => t.Style == System.Windows.FontStyles.Normal ? 0 : 1)
      .ThenBy(t => Math.Abs(t.Weight.ToOpenTypeWeight() - 400))
      .ThenBy(t => Math.Abs(t.Stretch.ToOpenTypeStretch() - 5))
      .FirstOrDefault(t => t.TryGetGlyphTypeface(out _))
      ?? new Typeface(family, System.Windows.FontStyles.Normal, System.Windows.FontWeights.Normal, System.Windows.FontStretches.Normal);
 }
}

