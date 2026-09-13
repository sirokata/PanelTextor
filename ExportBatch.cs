namespace PanelTextor;
internal record ExportProgress(int Completed, int Total, string Language, string SetName, string PageName);
internal record ExportResult(int Succeeded, int Total, List<string> Errors);
internal static class ExportBatch
{
 internal static async Task<ExportResult> Run(IReadOnlyList<ExportJob> jobs, IReadOnlyList<string> languages, Config config, string root, bool jpeg, Action<ExportProgress> progress)
 {
  int total = jobs.Count * languages.Count, completed = 0, succeeded = 0;
  var errors = new List<string>();
  foreach (var language in languages) foreach (var job in jobs)
  {
   var code = LanguageCodes.Display(language);
   progress(new(completed, total, code, job.Set.Name, job.Page.Name));
   try
   {
    var output = ImageSets.OutputPath(root, language, job.Set, job.Page, jpeg);
    await MainWindow.RunSta(() => TextRenderer.Export(job.Page, language, config, output, job.Source, jpeg));
    succeeded++;
   }
   catch (Exception ex) { errors.Add(code + "/" + job.Set.Name + "/" + job.Page.Name + ": " + ex.Message); }
   progress(new(++completed, total, code, job.Set.Name, job.Page.Name));
  }
  return new(succeeded, total, errors);
 }
}

