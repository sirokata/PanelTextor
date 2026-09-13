using System.IO;
using System.Text.Json;
namespace PanelTextor;

public class ImageSet
{
 public string Id { get; set; } = Guid.NewGuid().ToString("N");
 public string Name { get; set; } = "";
 public string Folder { get; set; } = "";
 public bool ExportEnabled { get; set; } = true;
 public Dictionary<string, string> Images { get; set; } = [];
}
public record ExportJob(Page Page, ImageSet Set, string Source);
public record ExportCheck(List<ExportJob> Jobs, List<string> Issues);
public static class ImageSets
{
 public static bool IsImage(string path) => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff" }.Contains(Path.GetExtension(path).ToLowerInvariant());
 public static void Upgrade(Project project)
 {
  if (project.Version is not (1 or 2)) throw new InvalidDataException(UiLanguage.T("対応していないプロジェクト形式です。"));
  if (project.ImageSets.Count == 0 && project.Pages.Count > 0)
  {
   var commonFolder = Path.GetDirectoryName(project.Pages[0].Source) ?? "";
   var sameFolder = project.Pages.All(p => string.Equals(Path.GetDirectoryName(p.Source) ?? "", commonFolder, StringComparison.OrdinalIgnoreCase));
   var set = new ImageSet { Name = sameFolder && commonFolder.Length > 0 ? Path.GetFileName(commonFolder.TrimEnd(Path.DirectorySeparatorChar)) : "元画像" };
   foreach (var page in project.Pages) set.Images[page.Id] = page.Source;
   project.ImageSets.Add(set);
  }
  if (!project.ImageSets.Any(s => s.Id == project.ActiveSetId)) project.ActiveSetId = project.ImageSets.FirstOrDefault()?.Id ?? "";
  project.Version = 2;
 }
 public static string? Source(Project project, Page page) => project.ImageSets.FirstOrDefault(s => s.Id == project.ActiveSetId)?.Images.GetValueOrDefault(page.Id);
 public static List<string> AddFolder(Project project, string folder)
 {
  Upgrade(project);
  if (project.Pages.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1)) throw new InvalidDataException(UiLanguage.T("同名のページが複数あるため対応付けできません。旧ページは維持されています。重複ページを整理してください。"));
  var paths = Directory.EnumerateFiles(folder).Where(IsImage).Order(StringComparer.CurrentCultureIgnoreCase).ToArray();
  if (paths.Length == 0) throw new IOException(UiLanguage.T("フォルダー内に対応画像がありません。"));
  var existing = project.ImageSets.FirstOrDefault(s => string.Equals(s.Folder, folder, StringComparison.OrdinalIgnoreCase)
      || s.Folder.Length == 0 && s.Images.Count > 0 && s.Images.Values.All(p => string.Equals(Path.GetDirectoryName(p), folder, StringComparison.OrdinalIgnoreCase)));
  var set = new ImageSet { Id = existing?.Id ?? Guid.NewGuid().ToString("N"), Name = existing?.Name ?? new DirectoryInfo(folder).Name, Folder = folder, ExportEnabled = existing?.ExportEnabled ?? true };
  if (existing is null)
  {
   var stem = set.Name; int suffix = 2;
   while (project.ImageSets.Any(s => s.Name.Equals(set.Name, StringComparison.OrdinalIgnoreCase))) set.Name = stem + " (" + suffix++ + ")";
  }
  var issues = new List<string>(); var addedPages = new List<Page>();
  if (project.Pages.Count == 0)
  {
   foreach (var path in paths)
   {
    var d = TextRenderer.Dimensions(path);
    var page = new Page { Source = path, Width = d.Width, Height = d.Height }; addedPages.Add(page); set.Images[page.Id] = path;
   }
  }
  else
  {
   var byName = paths.ToDictionary(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase);
   foreach (var page in project.Pages)
   {
    if (!byName.TryGetValue(page.Name, out var path)) { issues.Add(set.Name + UiLanguage.T(": 不足 ") + page.Name); continue; }
    set.Images[page.Id] = path;
    try { var d = TextRenderer.Dimensions(path); if (d != (page.Width, page.Height)) issues.Add(set.Name + UiLanguage.T(": 解像度不一致 ") + page.Name + $" ({d.Width}×{d.Height} / {page.Width}×{page.Height})"); }
    catch (Exception ex) { issues.Add(set.Name + ": " + page.Name + " " + ex.Message); }
   }
   var names = project.Pages.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
   issues.AddRange(paths.Where(p => !names.Contains(Path.GetFileName(p))).Select(p => set.Name + UiLanguage.T(": 余分な画像 ") + Path.GetFileName(p)));
  }
  project.Pages.AddRange(addedPages);
  if (existing is null) project.ImageSets.Add(set); else project.ImageSets[project.ImageSets.IndexOf(existing)] = set;
  project.ActiveSetId = set.Id;
  return issues;
 }
 public static ExportCheck Check(Project project, IEnumerable<Page> pages, IEnumerable<ImageSet> sets)
 {
  var jobs = new List<ExportJob>(); var issues = new List<string>();
  foreach (var set in sets)
  {
   foreach (var page in pages)
   {
    var source = set.Images.GetValueOrDefault(page.Id);
    if (source is null || !File.Exists(source)) { issues.Add(set.Name + UiLanguage.T(": 不足 ") + page.Name); continue; }
    try { if (TextRenderer.Dimensions(source) != (page.Width, page.Height)) { issues.Add(set.Name + UiLanguage.T(": 解像度不一致 ") + page.Name); continue; } }
    catch (Exception ex) { issues.Add(set.Name + ": " + page.Name + " " + ex.Message); continue; }
    jobs.Add(new ExportJob(page, set, source));
   }
   if (!string.IsNullOrEmpty(set.Folder))
   {
    if (!Directory.Exists(set.Folder)) issues.Add(set.Name + UiLanguage.T(": フォルダーが見つかりません。"));
    else
    {
     var names = project.Pages.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
     issues.AddRange(Directory.EnumerateFiles(set.Folder).Where(IsImage).Where(p => !names.Contains(Path.GetFileName(p))).Select(p => set.Name + UiLanguage.T(": 余分な画像 ") + Path.GetFileName(p)));
    }
   }
  }
  return new(jobs, issues);
 }
 public static Project Read(string path)
 {
  var project = Storage.Read<Project>(path); Storage.Validate(project);
  var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
  foreach (var page in project.Pages) page.Source = Path.GetFullPath(page.Source, folder);
  foreach (var set in project.ImageSets)
  {
   if (set.Folder.Length > 0) set.Folder = Path.GetFullPath(set.Folder, folder);
   foreach (var key in set.Images.Keys.ToArray()) set.Images[key] = Path.GetFullPath(set.Images[key], folder);
  }
  return project;
 }
 public static void Save(string path, Project project)
 {
  Upgrade(project); Storage.Validate(project);
  if (File.Exists(path) && Storage.Read<Project>(path).Version == 1)
  {
   var backup = path + ".v1.bak"; int suffix = 2; while (File.Exists(backup)) backup = path + ".v1." + suffix++ + ".bak";
   File.Copy(path, backup, false);
  }
  var copy = JsonSerializer.Deserialize<Project>(JsonSerializer.Serialize(project))!;
  var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
  foreach (var p in copy.Pages) p.Source = Path.GetRelativePath(folder, p.Source);
  foreach (var set in copy.ImageSets)
  {
   if (set.Folder.Length > 0) set.Folder = Path.GetRelativePath(folder, set.Folder);
   foreach (var key in set.Images.Keys.ToArray()) set.Images[key] = Path.GetRelativePath(folder, set.Images[key]);
  }
  Storage.Write(path, copy);
 }
 public static string OutputPath(string root, string language, ImageSet set, Page page, bool jpeg)
 {
  string Safe(string name) { var clean = new string(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()).TrimEnd(' ', '.'); return string.IsNullOrEmpty(clean) || clean is "." or ".." ? "set" : clean; }
  var directory = Path.Combine(root, LanguageCodes.Display(language), Safe(set.Name)); Directory.CreateDirectory(directory);
  var stem = Safe(Path.GetFileNameWithoutExtension(page.Name)); var extension = jpeg ? ".jpg" : ".png";
  var output = Path.Combine(directory, stem + extension); int suffix = 2;
  while (File.Exists(output)) output = Path.Combine(directory, stem + "_" + suffix++ + extension);
  return output;
 }
}


