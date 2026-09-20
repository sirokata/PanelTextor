using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace PanelTextor;
public partial class MainWindow
{
 internal async Task VerifyUi(Project fixture, string folder, Action<bool, string> check)
 {
  project = fixture; config = new Config(); RefreshPages(0); await ShowPage(true);
  var obj = new TextObject { Text = "配置テスト", X = 60, Y = 65 }; Layer!.Objects.Add(obj); RefreshBlocks(); RenderObjects(); Select(obj);
  var before = (obj.X, obj.Y, obj.Size, obj.Color);
  check(ColorButtons.Children.Count == 10 && ColorButtons.Columns == 5, "Palette shows ten colors in two rows");
  foreach (var key in Config.ColorKeys)
  {
   var button = ColorButtons.Children.OfType<Button>().Single(b => (string)b.Tag == key);
   button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   check(obj.Color == key && button.BorderThickness.Left == 4, "Color button selects and highlights " + key);
  }
  obj.Color = before.Color; RenderObjects(); Select(obj);
  BackgroundSets.SelectedItem = project.ImageSets[1]; await ShowPage(false);
  check(project.ActiveSetId == project.ImageSets[1].Id && (obj.X, obj.Y, obj.Size, obj.Color) == before && visuals.Any(v => v.Object == obj), "UI background selector retains text object and position");
  SelectedInput.Text = "編集した文字";
  check(Layer!.Objects[0].Text == "編集した文字", "UI text editor still updates shared object");
  SpacingInput.Text = "96";
  check(obj.LineAdvancePx == 96, "UI leading input updates the selected object immediately");
  SpacingStep_Click(new Button { Tag = "4" }, new RoutedEventArgs());
  check(obj.LineAdvancePx == 100, "UI leading plus button updates px");
  SpacingReset_Click(new Button(), new RoutedEventArgs());
  check(obj.LineAdvancePx == 0, "UI leading default button restores global setting");
  RotationInput.Text = "32.5";
  check(obj.RotationDegrees == 32.5 && RotationSlider.Value == 32.5, "Rotation angle input updates canvas and slider");
  RotationInput.Text = "NaN";
  check(obj.RotationDegrees == 32.5 && RotationInput.ToolTip is not null, "Invalid rotation preserves previous angle");
  RotationSlider.Value = -46.2;
  check(obj.RotationDegrees == -46.2 && RotationInput.Text == "-46.2", "Rotation slider updates angle immediately");
  RotationResetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  check(obj.RotationDegrees == 0 && RotationSlider.Value == 0, "Rotation reset restores original orientation");
  LargeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  check(obj.Size == "large", "UI size preset click works after background switch");
  VerticalButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  check(obj.Direction == "vertical", "UI JP direction click still updates layout");
  BackgroundSets.SelectedItem = project.ImageSets[0]; await ShowPage(false);
  check(Layer!.Objects[0].Text == "編集した文字" && obj.Direction == "vertical", "Edits remain shared when returning to first background");
  var other = Layer.Objects[0];
  Language_Click(new Button { Tag = "KO" }, new RoutedEventArgs());
  check(Layer!.Objects.Count == 0 && !ReferenceEquals(other, selected), "UI language switching keeps layouts independent");
  Language_Click(new Button { Tag = "JP" }, new RoutedEventArgs());
  // Render the real editor content offscreen, including the background selector.
  var root = (FrameworkElement)Content; root.Measure(new Size(1380, 900)); root.Arrange(new Rect(0, 0, 1380, 900)); root.UpdateLayout();
  var render = new RenderTargetBitmap(1380, 900, 96, 96, PixelFormats.Pbgra32); render.Render(root);
  var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(render));
  using var file = File.Create(Path.Combine(folder, "editor.png")); encoder.Save(file);
  check(BackgroundSets.Items.Count == project.ImageSets.Count && Surface.Children.Count > 1, "Editor contains all backgrounds and rendered text");
  foreach (var lang in Config.Languages)
  {
   Language_Click(new Button { Tag = lang }, new RoutedEventArgs());
   Layer!.Visible = true; RenderObjects();
   int count = Layer!.Objects.Count;
   BulkInput.Text = " 先頭 \n文 字\u3000\n\n\u3000全角 文 \n行末\t";
   AddBlocks_Click(new Button(), new RoutedEventArgs());
   check(Layer.Objects.Count == count + 2 && Layer.Objects[count].Text == " 先頭 \n文 字\u3000", lang + " bulk-add UI accepts and preserves spaces");
   SelectedInput.Text = " \u3000\t";
   check(selected!.Text == " \u3000\t" && visuals.Any(v => v.Object == selected), lang + " selected-text UI accepts whitespace-only content");
  }
  var keep = new TextObject { Text = "残す" }; var deleteA = new TextObject { Text = "削除A" }; var deleteB = new TextObject { Text = "削除B" };
  Layer!.Objects.AddRange([keep, deleteA, deleteB]); RefreshBlocks(); Select(deleteA);
  BlocksList.SelectedItems.Add(deleteB);
  check(selection.SetEquals([deleteA, deleteB]) && selected is null && !SelectedInput.IsEnabled && visuals.Where(v => selection.Contains(v.Object)).All(v => v.Selected), "Multiple list selection retains both objects and disables individual editing");
  await ShowPage(false);
  check(selection.SetEquals([deleteA, deleteB]), "Background refresh preserves multiple selection");
  foreach (var input in new System.Windows.IInputElement[] { SelectedInput, BulkInput, SpacingInput, PagesList })
   check(!HandleDeleteKey(System.Windows.Input.Key.Delete, System.Windows.Input.ModifierKeys.None, input) && Layer.Objects.Contains(deleteA), "Delete outside object selection does not remove text: " + ((FrameworkElement)input).Name);
  check(HandleDeleteKey(System.Windows.Input.Key.Delete, System.Windows.Input.ModifierKeys.None, BlocksList) && !Layer.Objects.Contains(deleteA) && !Layer.Objects.Contains(deleteB) && Layer.Objects.Contains(keep) && selection.Count == 0, "Delete key removes all selected objects and preserves unselected objects");
  Select(keep); DeleteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  check(!Layer.Objects.Contains(keep) && !DeleteButton.IsEnabled, "Delete button removes selected object and resets selection");
  Select(Layer.Objects.First()); Language_Click(new Button { Tag = "JP" }, new RoutedEventArgs());
  check(selection.Count == 0 && !HandleDeleteKey(System.Windows.Input.Key.Delete, System.Windows.Input.ModifierKeys.None, Surface), "Language switch clears selection and empty Delete is harmless");
  Language_Click(new Button { Tag = "ZH" }, new RoutedEventArgs());
  check(LanguageZH.FontWeight == FontWeights.Bold && LanguageJP.FontWeight == FontWeights.Normal && LanguageZH.Background.ToString() != LanguageJP.Background.ToString(), "Active language button is highlighted");
  PagesList.SelectedIndex = PagesList.SelectedIndex == 0 ? 1 : 0;
  await ShowPage(true);
  check(project.Language == "JP" && LanguageJP.FontWeight == FontWeights.Bold && LanguageZH.FontWeight == FontWeights.Normal, "Changing page selects and highlights JP");
  check(Title.StartsWith("PanelTextor") && Icon is not null, "PanelTextor branding and window icon are present");
  var interfaceConfig = Path.Combine(folder, "interface-settings.json");
  var originalProject = System.Text.Json.JsonSerializer.Serialize(project);
  foreach (var language in new[] { "EN", "ZH", "KO", "JP" })
  {
   ChangeInterfaceLanguage(language, interfaceConfig);
   check(Storage.Read<Config>(interfaceConfig).UiLanguage == language && InterfaceLanguage.SelectedValue?.ToString() == language, language + " interface choice persists");
   check(SmallButton.Content.ToString() == UiLanguage.T("小") && DeleteButton.Content.ToString() == UiLanguage.T("選択テキストを削除 (Delete)"), language + " live buttons are localized");
   check(System.Text.Json.JsonSerializer.Serialize(project) == originalProject, language + " interface switch preserves all project text and layout");
   var settings = new SettingsWindow(config); var export = new ExportOptionsWindow(project.ImageSets);
   check(settings.Title == UiLanguage.T("プリセット設定 — 変更は既存テキストにも反映") && export.Title == UiLanguage.T("書き出す画像セット"), language + " settings and export dialogs are localized");
   settings.Close(); export.Close();
   root.Measure(new Size(1380, 900)); root.Arrange(new Rect(0, 0, 1380, 900)); root.UpdateLayout();
   var localized = new RenderTargetBitmap(1380, 900, 96, 96, PixelFormats.Pbgra32); localized.Render(root);
   var localizedEncoder = new PngBitmapEncoder(); localizedEncoder.Frames.Add(BitmapFrame.Create(localized));
   using var localizedFile = File.Create(Path.Combine(folder, "interface-" + language + ".png")); localizedEncoder.Save(localizedFile);
  }
  check(System.Text.Json.JsonSerializer.Deserialize<Config>("{}")!.UiLanguage == "JP", "Old settings default to Japanese interface");
  check(UiLanguage.Entries.All(row => row.Length == 4 && row.All(s => !string.IsNullOrWhiteSpace(s))) && UiLanguage.XamlSources.Values.All(s => UiLanguage.Entries.Any(row => row[0] == s)), "All interface resources have four translations");
  CameraEnabled.IsChecked = true; CameraEnabled_Click(CameraEnabled, new RoutedEventArgs());
  Battery_Click(new Button { Tag = "1" }, new RoutedEventArgs()); CameraTime.Text = "00:38";
  var cameraPage = Current!;
  check(cameraPage.CameraFrame.Battery.Level == 1 && cameraPage.CameraFrame.Timer.Seconds == 38 && cameraVisual is not null, "Camera buttons and elapsed input update the preview immediately");
  CameraTime.Text = "invalid";
  check(cameraPage.CameraFrame.Timer.Seconds == 38, "Invalid elapsed text does not overwrite saved time");
  Language_Click(new Button { Tag = "EN" }, new RoutedEventArgs());
  await ShowPage(false);
  check(CameraTime.Text == "00:38" && Current!.CameraFrame.Battery.Level == 1 && cameraVisual is not null, "Camera settings are shared across text languages");
  PagesList.SelectedIndex = PagesList.SelectedIndex == 0 ? 1 : 0; await ShowPage(true);
  check(Current!.CameraFrame.Battery.Level == 5 && !Current.CameraFrame.Enabled, "Next page starts with independent camera settings");
  PagesList.SelectedIndex = project.Pages.IndexOf(cameraPage); await ShowPage(true);
  check(CameraEnabled.IsChecked == true && CameraTime.Text == "00:38" && Current!.CameraFrame.Battery.Level == 1, "Returning to page restores camera controls");
  var exportOptions = new ExportOptionsWindow(project.ImageSets, "ZH");
  check(exportOptions.ReadLanguages() && exportOptions.SelectedLanguages.SequenceEqual(["ZH"]), "Export initially selects the current language");
  foreach (var item in exportOptions.LanguageChecks.Values) item.IsChecked = false;
  check(!exportOptions.ReadLanguages(), "Export requires at least one language");
  foreach (var lang in new[] { "EN", "ZH", "KO" }) exportOptions.LanguageChecks[lang].IsChecked = true;
  check(exportOptions.ReadLanguages() && exportOptions.SelectedLanguages.Count == 3 && exportOptions.LanguageChecks["ZH"].Content.ToString() == "CN" && exportOptions.LanguageChecks["KO"].Content.ToString() == "KR", "Export supports multiple languages with CN/KR labels");
  exportOptions.Close();
  var batchJobs = ImageSets.Check(project, [Current!], project.ImageSets).Jobs;
  var batchRoot = Path.Combine(folder, "multi-language");
  var progressValues = new List<int>();
  var batch = await ExportBatch.Run(batchJobs, exportOptions.SelectedLanguages, config, batchRoot, false, p => { UpdateExportProgress(p); progressValues.Add(p.Completed); });
  check(batch.Total == batchJobs.Count * 3 && batch.Succeeded == batch.Total && batch.Errors.Count == 0, "All selected languages and image sets export in one batch");
  check(new[] { "EN", "CN", "KR" }.All(l => Directory.GetFiles(Path.Combine(batchRoot, l), "*.png", SearchOption.AllDirectories).Length == batchJobs.Count) && !Directory.Exists(Path.Combine(batchRoot, "JP")) && !Directory.Exists(Path.Combine(batchRoot, "ZH")) && !Directory.Exists(Path.Combine(batchRoot, "KO")), "Output uses only selected language folders EN/CN/KR");
  check(progressValues.First() == 0 && progressValues.Last() == batch.Total && progressValues.SequenceEqual(progressValues.Order()) && ExportMeter.Value == ExportMeter.Maximum && Status.Text.Contains("100%"), "Progress gauge advances monotonically to 100 percent");
  var failed = await ExportBatch.Run([new ExportJob(Current!, project.ImageSets[0], Path.Combine(folder, "missing-file.png"))], ["KO"], config, Path.Combine(folder, "failed-batch"), false, UpdateExportProgress);
  check(failed.Succeeded == 0 && failed.Errors.Count == 1 && failed.Errors[0].StartsWith("KR/") && ExportMeter.Value == ExportMeter.Maximum, "Failed exports are counted separately and progress still completes");
  foreach (var language in Config.Languages)
  {
   Language_Click(new Button { Tag = language }, new RoutedEventArgs());
   config.Styles[language].ImportReplacements = [new() { Find = "original", ReplaceWith = LanguageCodes.Display(language) }];
   var existing = Layer!.Objects.Select(o => o.Text).ToArray(); int previousCount = Layer.Objects.Count;
   BulkInput.Text = " original \n\noriginal"; AddBlocks_Click(new Button(), new RoutedEventArgs());
   check(Layer.Objects.Count == previousCount + 2 && Layer.Objects[previousCount].Text == " " + LanguageCodes.Display(language) + " " && Layer.Objects.Take(previousCount).Select(o => o.Text).SequenceEqual(existing), language + " import replaces before splitting without touching existing text");
  }
  dirty = false;
 }
}
internal static class UiRegression
{
 public static void Run(string folder, Action<bool, string> check)
 {
  var fixture = ImageSets.Read(Path.Combine(folder, "regression", "sets.polytext.json")); fixture.Language = "JP";
  var window = new MainWindow();
  var task = window.VerifyUi(fixture, folder, check);
  if (!task.IsCompleted)
  {
   var frame = new DispatcherFrame();
   task.ContinueWith(_ => window.Dispatcher.BeginInvoke(new Action(() => frame.Continue = false)));
   Dispatcher.PushFrame(frame);
  }
  task.GetAwaiter().GetResult();
  var page = fixture.Pages[0];
  var output = Path.GetFullPath(Path.Combine(folder, "background-thread.png"));
  MainWindow.RunSta(() => TextRenderer.Export(page, "JP", new Config(), output, fixture.ImageSets[0].Images[page.Id])).GetAwaiter().GetResult();
  check(ColorPipeline.Inspect(output).HasProfile, "STA background export completes with ICC after UI editing");
 }
}

