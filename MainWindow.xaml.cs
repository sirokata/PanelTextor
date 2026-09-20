using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace PanelTextor;
public partial class MainWindow : Window
{
 private Project project = new();
 private Config config = new();
 private string? projectPath;
 private bool dirty, syncing, fitting = true, busy;
 private double zoom = 1;
 private int loadGeneration;
 private TextObject? selected;
 private Image? cameraVisual;
 private readonly HashSet<TextObject> selection = [];
 private Point? dragStart;
 private Point originalPosition;
 private TextVisual? draggingVisual;
 private readonly Dictionary<string, BitmapSource> cache = [];
 private readonly LinkedList<string> cacheOrder = new();
 private readonly List<TextVisual> visuals = [];
 private Page? Current => PagesList.SelectedItem as Page;
 private LanguageLayer? Layer => Current?.Layers[project.Language];
 public MainWindow()
 {
  InitializeComponent();
  try { config = Storage.LoadConfig(); }
  catch (Exception ex) { MessageBox.Show(UiLanguage.T("設定を読み込めないため初期値を使用します。\n") + ex.Message); }
  UiLanguage.Set(config.UiLanguage);
  BuildColors(); RefreshPages(); Closing += (_, e) => { if (busy || !ConfirmLeave()) e.Cancel = true; };
  InterfaceLanguage.SelectedValue = UiLanguage.Current;
 }
 private void InterfaceLanguage_Changed(object sender, SelectionChangedEventArgs e)
 {
  if (InterfaceLanguage.SelectedValue is not string language || language == UiLanguage.Current) return;
  ChangeInterfaceLanguage(language);
 }
 internal void ChangeInterfaceLanguage(string language, string? settingsPath = null)
 {
  var previous = config.UiLanguage;
  try
  {
   config.UiLanguage = language; Storage.Write(settingsPath ?? Storage.ConfigPath, config);
   UiLanguage.Set(language);
   InterfaceLanguage.SelectedValue = UiLanguage.Current;
   LanguageLabel.Text = UiLanguage.F("{0} を編集中", LanguageCodes.Display(project.Language));
   Select(selected, true);
   Title = "PanelTextor — " + (projectPath is null ? UiLanguage.T("未保存のプロジェクト") : Path.GetFileName(projectPath)) + (dirty ? " *" : "");
   Status.Text = UiLanguage.T("表示言語を変更しました。本文の言語は変更されません。");
  }
  catch (Exception ex) { config.UiLanguage = previous; InterfaceLanguage.SelectedValue = UiLanguage.Current; Error(ex); }
 }
 private void Error(Exception ex) { MessageBox.Show(this, ex.Message, UiLanguage.T("処理できませんでした"), MessageBoxButton.OK, MessageBoxImage.Warning); }
 private void Licenses_Click(object sender, RoutedEventArgs e) => new LicenseWindow { Owner = this }.ShowDialog();
 private void Changed() { dirty = true; Title = "PanelTextor — " + (projectPath is null ? UiLanguage.T("未保存のプロジェクト") : Path.GetFileName(projectPath)) + " *"; }
 private bool ConfirmLeave()
 {
  if (!dirty) return true;
  var answer = MessageBox.Show(this, UiLanguage.T("変更を保存しますか？"), UiLanguage.T("未保存の変更"), MessageBoxButton.YesNoCancel);
  return answer == MessageBoxResult.No || answer == MessageBoxResult.Yes && Save(false);
 }
 private void RefreshPages(int index = 0) { RefreshSets(); PagesList.ItemsSource = null; PagesList.ItemsSource = project.Pages; PagesList.SelectedIndex = project.Pages.Count > 0 ? Math.Clamp(index, 0, project.Pages.Count - 1) : -1; if (Current is null) { Surface.Children.Clear(); LoadLayer(); } }
 private void RefreshSets()
 {
  ImageSets.Upgrade(project); syncing = true;
  BackgroundSets.ItemsSource = null; BackgroundSets.ItemsSource = project.ImageSets; BackgroundSets.SelectedItem = project.ImageSets.FirstOrDefault(s => s.Id == project.ActiveSetId); syncing = false;
 }
 private async void Background_Changed(object sender, SelectionChangedEventArgs e)
 {
  if (syncing || BackgroundSets.SelectedItem is not ImageSet set) return;
  project.ActiveSetId = set.Id; Changed(); await ShowPage(false);
 }
 private async void AddSet_Click(object sender, RoutedEventArgs e)
 {
  var dialog = new OpenFolderDialog { Title = UiLanguage.T("画像セットのフォルダー（同じフォルダーを選ぶと再読み込み）") };
  if (dialog.ShowDialog(this) != true) return;
  busy = true; IsEnabled = false;
  try
  {
   var index = Math.Max(0, PagesList.SelectedIndex);
   var issues = await Task.Run(() => ImageSets.AddFolder(project, dialog.FolderName));
   cache.Clear(); cacheOrder.Clear(); Changed(); RefreshPages(index);
   if (issues.Count > 0) ShowIssues(issues);
  }
  catch (Exception ex) { Error(ex); }
  finally { busy = false; IsEnabled = true; }
 }
 private void ShowIssues(IEnumerable<string> issues)
 {
  var window = new Window { Owner = this, Title = UiLanguage.T("画像セットの対応チェック"), Width = 680, Height = 430, WindowStartupLocation = WindowStartupLocation.CenterOwner };
  window.Content = new TextBox { Text = string.Join("\n", issues), IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(12) }; window.ShowDialog();
 }
 private async void CheckSets_Click(object sender, RoutedEventArgs e)
 {
  busy = true; IsEnabled = false;
  try { var result = await Task.Run(() => ImageSets.Check(project, project.Pages, project.ImageSets)); IsEnabled = true; ShowIssues(result.Issues.Count > 0 ? result.Issues : [UiLanguage.T("全セットのファイル名・解像度が一致しています。")]); }
  catch (Exception ex) { Error(ex); } finally { busy = false; IsEnabled = true; }
 }
 private void ImageInfo_Click(object sender, RoutedEventArgs e)
 {
  if (Current is not { } page || ImageSets.Source(project, page) is not { } path) return;
  try
  {
   var info = ColorPipeline.Inspect(path); var style = config.Styles[project.Language]; var px = style.Sizes[selected?.Size ?? "medium"];
   var face = FontCatalog.Typeface(style.Font); face.TryGetGlyphTypeface(out var glyph);
   ShowIssues([$"{page.Name}: {info.Width} × {info.Height} px", UiLanguage.F("画像PPI: X={0}, Y={1}", info.DpiX.ToString("0.##"), info.DpiY.ToString("0.##")),
     UiLanguage.F("入力ICC: {0} / 出力ICC: sRGB", UiLanguage.T(info.HasProfile ? "あり → sRGBに変換" : "なし → sRGBと仮定")),
     UiLanguage.F("文字: {0} px = {1} pt（この画像のPPI・PostScript 72pt/inの場合）", px.ToString("0.##"), (px * 72 / info.DpiY).ToString("0.###")),
     UiLanguage.F("フォント: {0}", style.Font), UiLanguage.F("実際の書体: {0} / Weight={1} / Style={2}", glyph?.FontUri?.ToString() ?? "", style.FontWeight > 0 ? "wght=" + style.FontWeight : face.Weight.ToString(), face.Style),
     UiLanguage.F("横の行送り: {0} / 縦の列送り: {1} em", style.LineHeightPx == 0 ? UiLanguage.T("フォント既定") : style.LineHeightPx + " px", style.VerticalColumnEm),
     UiLanguage.T("追加字間: 0 / 縦の文字送り: 1 em"),
     UiLanguage.F("縁取り: 外側 {0} px / {1}", style.OutlineWidth, style.OutlineColor), UiLanguage.F("文字色: sRGB {0}", config.Colors[selected?.Color ?? "color1"]),
     UiLanguage.T("横組み: WPF（可変ウェイト指定時はHarfBuzz） / 縦組み: HarfBuzz。可変ウェイトの輪郭はHarfBuzz、描画はWPF。描画方式によって輪郭の見え方が異なります。"),
     UiLanguage.T("比較: 同じ実フォント・px指定・RGB/sRGB・拡大縮小100%・擬似太字OFF・字間0・縁取り外側。")]);
  }
  catch (Exception ex) { Error(ex); }
 }
 private async void Import_Click(object sender, RoutedEventArgs e)
 {
  var dialog = new OpenFileDialog { Filter = UiLanguage.T("画像|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff"), Multiselect = true };
  if (dialog.ShowDialog(this) != true) return;
  var errors = new List<string>(); int added = 0; int index = Math.Max(0, PagesList.SelectedIndex);
  busy = true; IsEnabled = false;
  try
  {
   ImageSets.Upgrade(project);
   if (project.ImageSets.Count == 0) { var initial = new ImageSet { Name = UiLanguage.T("元画像") }; project.ImageSets.Add(initial); project.ActiveSetId = initial.Id; }
   var set = project.ImageSets.First(s => s.Id == project.ActiveSetId);
   foreach (var path in dialog.FileNames.Order(StringComparer.CurrentCultureIgnoreCase))
   {
    if (set.Images.Values.Contains(path, StringComparer.OrdinalIgnoreCase)) continue;
    try
    {
     var d = await Task.Run(() => TextRenderer.Dimensions(path));
     var page = project.Pages.FirstOrDefault(p => p.Name.Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase));
     if (page is not null && (page.Width, page.Height) != d) throw new IOException(UiLanguage.T("同名ページと解像度が一致しません。"));
     if (page is null) { page = new Page { Source = path, Width = d.Width, Height = d.Height }; project.Pages.Add(page); }
     set.Images[page.Id] = path; added++;
    }
    catch (Exception ex) { errors.Add(Path.GetFileName(path) + ": " + ex.Message); }
   }
   if (added > 0) { Changed(); RefreshPages(index); }
   Status.Text = UiLanguage.F("{0}枚を追加しました。", added);
   if (errors.Count > 0) MessageBox.Show(this, string.Join("\n", errors), UiLanguage.T("読み込めなかった画像"));
  }
  finally { busy = false; IsEnabled = true; }
 }
 private async void Page_Changed(object sender, SelectionChangedEventArgs e)
 {
  project.Language = "JP";
  await ShowPage(true);
 }
 private async Task ShowPage(bool reset)
 {
  if (Surface is null) return;
  int generation = ++loadGeneration; project.PageIndex = Math.Max(0, PagesList.SelectedIndex);
  if (reset) { selected = null; LoadLayer(); }
  Surface.Children.Clear(); visuals.Clear();
  if (Current is not { } page) return;
  try
  {
   var path = ImageSets.Source(project, page);
   if (path is null || !File.Exists(path)) { Status.Text = UiLanguage.F("このセットには {0} がありません。画像セットを追加／更新してください。", page.Name); return; }
   var info = ColorPipeline.Inspect(path);
   if ((info.Width, info.Height) != (page.Width, page.Height)) { Status.Text = UiLanguage.T("背景の解像度が一致しません。自動拡大縮小は行いません。"); return; }
   Status.Text = UiLanguage.T("プレビューを読み込み中…");
   if (!cache.TryGetValue(path, out var preview))
   {
    int width = Math.Max(1, (int)(page.Width * Math.Min(1, 1600.0 / Math.Max(page.Width, page.Height))));
    preview = await Task.Run(() => TextRenderer.Load(path, width));
    cache[path] = preview; cacheOrder.Remove(path); cacheOrder.AddLast(path);
    while (cacheOrder.Count > 6) { cache.Remove(cacheOrder.First!.Value); cacheOrder.RemoveFirst(); }
   }
   else { cacheOrder.Remove(path); cacheOrder.AddLast(path); }
   if (generation != loadGeneration) return;
   Surface.Width = page.Width; Surface.Height = page.Height;
   Surface.Children.Add(new Image { Source = preview, Width = page.Width, Height = page.Height, Stretch = Stretch.Fill, IsHitTestVisible = false });
   if (reset) fitting = true; if (fitting) Fit(); RenderObjects(); Select(selected, true);
   Status.Text = UiLanguage.F("{0}   {1} × {2} px / {3} PPI / {4}   ／   {5}ページ", page.Name, page.Width, page.Height, info.DpiY.ToString("0.##"), info.HasProfile ? "ICC→sRGB" : UiLanguage.T("sRGBと仮定"), project.Pages.Count);
  }
  catch (Exception ex) { if (generation == loadGeneration) { Status.Text = UiLanguage.T("画像を読み込めません。元画像の場所を確認してください。"); Error(ex); } }
 }
 private void LoadLayer()
 {
  syncing = true; BulkInput.Text = Layer?.Draft ?? ""; BulkInput.IsEnabled = Layer is not null; VisibleCheck.IsChecked = Layer?.Visible ?? true;
  LanguageLabel.Text = UiLanguage.F("{0} を編集中", LanguageCodes.Display(project.Language));
  foreach (var button in new[] { LanguageJP, LanguageEN, LanguageZH, LanguageKO })
  {
   bool active = (string)button.Tag == project.Language;
   button.Background = active ? new SolidColorBrush(Color.FromRgb(32, 92, 190)) : Brushes.White;
   button.Foreground = active ? Brushes.White : Brushes.Black;
   button.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
   button.BorderBrush = active ? Brushes.DarkBlue : Brushes.LightGray;
  }
  syncing = false; RefreshBlocks(); Select(null); RefreshCameraControls();
 }
 private void RefreshBlocks()
 {
  syncing = true; BlocksList.ItemsSource = null; BlocksList.ItemsSource = Layer?.Objects;
  foreach (var obj in selection.Where(o => Layer?.Objects.Contains(o) == true)) BlocksList.SelectedItems.Add(obj);
  syncing = false;
 }
 private void RenderObjects()
 {
  foreach (var v in visuals) Surface.Children.Remove(v); visuals.Clear();
  if (cameraVisual is not null) { Surface.Children.Remove(cameraVisual); cameraVisual = null; }
  if (Current is { } page && Surface.Children.Count > 0 && page.CameraFrame.Enabled)
  {
   cameraVisual = new Image { Width = page.Width, Height = page.Height, Stretch = Stretch.None, IsHitTestVisible = false };
   // A full-page transparent drawing keeps the normalized element coordinates intact.
   var drawing = new DrawingGroup();
   using (var dc = drawing.Open()) { dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, page.Width, page.Height)); dc.DrawDrawing(CameraFrameRenderer.Draw(page)); }
   drawing.Freeze(); cameraVisual.Source = new DrawingImage(drawing);
   Panel.SetZIndex(cameraVisual, 100); Surface.Children.Add(cameraVisual);
  }
  if (Layer is not { Visible: true } layer || Surface.Children.Count == 0) return;
  foreach (var obj in layer.Objects)
  {
   var v = new TextVisual(obj, project.Language, config) { Selected = selection.Contains(obj) }; Canvas.SetLeft(v, obj.X); Canvas.SetTop(v, obj.Y); Surface.Children.Add(v); visuals.Add(v);
  }
 }
 private void RefreshCameraControls()
 {
  syncing = true;
  CameraEnabled.IsEnabled = Current is not null;
  CameraEnabled.IsChecked = Current?.CameraFrame.Enabled ?? false;
  CameraOptions.IsEnabled = Current?.CameraFrame.Enabled == true;
  CameraTime.Text = Current?.CameraFrame.Timer.Display ?? "00:00";
  CameraTime.ClearValue(Control.BorderBrushProperty); CameraTime.ToolTip = null;
  foreach (Button button in BatteryButtons.Children)
  {
   bool active = Current?.CameraFrame.Battery.Level == int.Parse((string)button.Tag);
   button.Background = active ? Brushes.DodgerBlue : Brushes.White;
   button.Foreground = active ? Brushes.White : Brushes.Black;
   button.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
  }
  syncing = false;
 }
 private void CameraEnabled_Click(object sender, RoutedEventArgs e)
 {
  if (Current is null) return;
  Current.CameraFrame.Enabled = CameraEnabled.IsChecked == true;
  Changed(); RefreshCameraControls(); RenderObjects();
 }
 private void Battery_Click(object sender, RoutedEventArgs e)
 {
  if (Current is null) return;
  Current.CameraFrame.Battery.Level = int.Parse((string)((Button)sender).Tag);
  Changed(); RefreshCameraControls(); RenderObjects();
 }
 private void CameraTime_Changed(object sender, TextChangedEventArgs e)
 {
  if (syncing || Current is null) return;
  if (!CameraTimer.TryParse(CameraTime.Text, out var seconds))
  { CameraTime.BorderBrush = Brushes.Red; CameraTime.ToolTip = UiLanguage.T("経過時間は mm:ss または hh:mm:ss で入力してください。"); return; }
  CameraTime.ClearValue(Control.BorderBrushProperty); CameraTime.ToolTip = null;
  Current.CameraFrame.Timer.Seconds = seconds; Changed(); RenderObjects();
 }
 private void Select(TextObject? obj, bool preserve = false)
 {
  if (!preserve) { selection.Clear(); if (obj is not null) selection.Add(obj); }
  obj = selection.Count == 1 ? selection.First() : null;
  selected = obj; syncing = true; SelectedInput.IsEnabled = obj is not null; SelectedInput.Text = obj?.Text ?? "";
  foreach (var item in BlocksList.SelectedItems.Cast<TextObject>().Where(o => !selection.Contains(o)).ToArray()) BlocksList.SelectedItems.Remove(item);
  foreach (var item in selection) if (BlocksList.Items.Contains(item) && !BlocksList.SelectedItems.Contains(item)) BlocksList.SelectedItems.Add(item); syncing = false;
  syncing = true; SpacingInput.IsEnabled = obj is not null; SpacingInput.Text = (obj?.LineAdvancePx ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture); syncing = false;
  SpacingInput.ClearValue(Control.BorderBrushProperty); SpacingInput.ToolTip = null;
  syncing = true;
  RotationInput.IsEnabled = RotationSlider.IsEnabled = RotationResetButton.IsEnabled = obj is not null;
  RotationInput.Text = (obj?.RotationDegrees ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture);
  RotationSlider.Value = obj?.RotationDegrees ?? 0;
  RotationInput.ClearValue(Control.BorderBrushProperty); RotationInput.ToolTip = null;
  syncing = false;
  SpacingHint.Text = obj?.Direction == "vertical" ? UiLanguage.T("縦書き：列と列の間隔。0＝言語設定。1列だけでは変化しません。") : UiLanguage.T("横書き：行頭から次の行頭までの距離。0＝言語設定。1行だけでは変化しません。");
  SelectionLabel.Text = selection.Count > 1 ? UiLanguage.F("{0}件を選択中", selection.Count) : obj is null ? UiLanguage.T("テキストを選択") : UiLanguage.T("選択中のテキスト");
  DeleteButton.IsEnabled = selection.Count > 0;
  foreach (var pair in new[] { (SmallButton, "small"), (MediumButton, "medium"), (LargeButton, "large") }) { pair.Item1.IsEnabled = obj is not null; pair.Item1.FontWeight = obj?.Size == pair.Item2 ? FontWeights.Bold : FontWeights.Normal; pair.Item1.BorderBrush = obj?.Size == pair.Item2 ? Brushes.DodgerBlue : Brushes.Gray; }
  VerticalButton.IsEnabled = obj is not null && project.Language == "JP"; HorizontalButton.IsEnabled = obj is not null;
  VerticalButton.FontWeight = obj?.Direction == "vertical" ? FontWeights.Bold : FontWeights.Normal; HorizontalButton.FontWeight = obj?.Direction == "horizontal" ? FontWeights.Bold : FontWeights.Normal;
  foreach (Button b in ColorButtons.Children) { b.IsEnabled = obj is not null; b.BorderThickness = new Thickness(obj?.Color == (string)b.Tag ? 4 : 1); b.BorderBrush = obj?.Color == (string)b.Tag ? Brushes.DeepSkyBlue : Brushes.Gray; }
  foreach (var v in visuals) { v.Selected = selection.Contains(v.Object); v.InvalidateVisual(); }
 }
 private void BuildColors()
 {
  ColorButtons.Children.Clear(); foreach (var pair in Config.ColorKeys.Select(key => new KeyValuePair<string, string>(key, config.Colors[key])))
  {
   var button = new Button { Tag = pair.Key, Background = TextRenderer.Brush(pair.Value), Height = 34, ToolTip = pair.Key + " " + pair.Value };
   button.Click += (_, _) => { if (selected is null) return; selected.Color = (string)button.Tag; Changed(); RenderObjects(); Select(selected); }; ColorButtons.Children.Add(button);
  }
 }
 private void Language_Click(object sender, RoutedEventArgs e) { project.Language = (string)((Button)sender).Tag; selected = null; LoadLayer(); RenderObjects(); Changed(); }
 private void Visibility_Click(object sender, RoutedEventArgs e) { if (Layer is null) return; Layer.Visible = VisibleCheck.IsChecked == true; RenderObjects(); Changed(); }
 private void Bulk_Changed(object sender, TextChangedEventArgs e) { if (syncing || Layer is null) return; Layer.Draft = BulkInput.Text; Changed(); }
 private void AddBlocks_Click(object sender, RoutedEventArgs e)
 {
  if (Layer is null || Current is null) return;
  var parts = Storage.Split(TextReplacements.Apply(BulkInput.Text, config.Styles[project.Language])); if (parts.Length == 0) return;
  foreach (var part in parts)
  {
   int n = Layer.Objects.Count; var obj = new TextObject { Text = part, X = Current.Width * (.08 + .22 * (n % 4)), Y = Current.Height * (.08 + .13 * ((n / 4) % 6)), Direction = project.Language == "JP" ? "vertical" : "horizontal" }; Layer.Objects.Add(obj); selected = obj;
  }
  BulkInput.Clear(); Changed(); RefreshBlocks(); RenderObjects(); Select(selected);
 }
 private void Selected_Changed(object sender, TextChangedEventArgs e) { if (syncing || selected is null) return; selected.Text = SelectedInput.Text; Changed(); RefreshBlocks(); RenderObjects(); }
 private void Block_Changed(object sender, SelectionChangedEventArgs e)
 {
  if (syncing) return;
  selection.Clear(); foreach (TextObject obj in BlocksList.SelectedItems) selection.Add(obj);
  Select(null, true);
 }
 private void Size_Click(object sender, RoutedEventArgs e) { if (selected is null) return; selected.Size = (string)((Button)sender).Tag; Changed(); RenderObjects(); Select(selected); }
 private void Direction_Click(object sender, RoutedEventArgs e) { if (selected is null) return; selected.Direction = (string)((Button)sender).Tag; Changed(); RenderObjects(); Select(selected); }
 private void Spacing_Changed(object sender, TextChangedEventArgs e)
 {
  if (syncing || selected is null) return;
  if (!double.TryParse(SpacingInput.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n) || n < 0 || n > 3000)
  { SpacingInput.ToolTip = UiLanguage.T("0〜3000の数値を入力してください。"); SpacingInput.BorderBrush = Brushes.Red; return; }
  SpacingInput.ClearValue(Control.BorderBrushProperty); SpacingInput.ToolTip = null;
  selected.LineAdvancePx = n; Changed(); RenderObjects();
 }
 private void SpacingStep_Click(object sender, RoutedEventArgs e)
 {
  if (selected is null) return;
  var style = config.Styles[project.Language]; var size = style.Sizes[selected.Size];
  var current = selected.LineAdvancePx;
  if (current <= 0)
  {
   if (selected.Direction == "vertical") current = size * style.VerticalColumnEm;
   else if (style.LineHeightPx > 0) current = style.LineHeightPx;
   else current = new FormattedText("Hg", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, FontCatalog.Typeface(style.Font), size, Brushes.Black, 1).Height;
  }
  SpacingInput.Text = Math.Clamp(Math.Round(current, 2) + double.Parse((string)((Button)sender).Tag, System.Globalization.CultureInfo.InvariantCulture), 1, 3000).ToString(System.Globalization.CultureInfo.InvariantCulture);
 }
 private void SpacingReset_Click(object sender, RoutedEventArgs e) { if (selected is not null) SpacingInput.Text = "0"; }
 private void Rotation_Changed(object sender, TextChangedEventArgs e)
 {
  if (syncing || selected is null) return;
  if (!double.TryParse(RotationInput.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var angle) || !double.IsFinite(angle) || angle < -180 || angle > 180)
  { RotationInput.BorderBrush = Brushes.Red; RotationInput.ToolTip = UiLanguage.T("−180〜180の数値を入力してください。"); return; }
  RotationInput.ClearValue(Control.BorderBrushProperty); RotationInput.ToolTip = null;
  selected.RotationDegrees = angle;
  syncing = true; RotationSlider.Value = angle; syncing = false;
  Changed(); RenderObjects();
 }
 private void RotationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
 {
  if (syncing || selected is null) return;
  RotationInput.Text = Math.Round(e.NewValue, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
 }
 private void RotationReset_Click(object sender, RoutedEventArgs e) { if (selected is not null) RotationInput.Text = "0"; }
 private void Delete_Click(object sender, RoutedEventArgs e)
 {
  if (Layer is null || selection.Count == 0) return;
  int count = Layer.Objects.RemoveAll(selection.Contains);
  dragStart = null; draggingVisual = null; Surface.ReleaseMouseCapture();
  Select(null); RefreshBlocks(); RenderObjects();
  if (count > 0) { Changed(); Status.Text = UiLanguage.F("{0}件のテキストを削除しました。", count); }
 }
 private void Window_KeyDown(object sender, KeyEventArgs e)
 {
  if (HandleDeleteKey(e.Key, Keyboard.Modifiers, Keyboard.FocusedElement)) e.Handled = true;
 }
 private bool HandleDeleteKey(Key key, ModifierKeys modifiers, IInputElement? focus)
 {
  if (key != Key.Delete || modifiers != ModifierKeys.None) return false;
  // Only object-selection surfaces own this shortcut; text editors retain normal Delete behavior.
  if (focus is not DependencyObject target || !new FrameworkElement[] { Surface, BlocksList, DeleteButton }.Any(c => c == target || c.IsAncestorOf(target))) return false;
  if (selection.Count == 0) return false;
  Delete_Click(this, new RoutedEventArgs()); return true;
 }
 private void Canvas_Down(object sender, MouseButtonEventArgs e)
 {
  var point = e.GetPosition(Surface);
  var hit = visuals.LastOrDefault(v => v.Contains(point, 8 / zoom));
  Surface.Focus();
  if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && hit is not null)
  {
   if (!selection.Remove(hit.Object)) selection.Add(hit.Object);
   Select(null, true); e.Handled = true; return;
  }
  Select(hit?.Object); if (hit is null) return; draggingVisual = hit; dragStart = point; originalPosition = new Point(hit.Object.X, hit.Object.Y); Surface.CaptureMouse(); e.Handled = true;
 }
 private void Canvas_Move(object sender, MouseEventArgs e)
 {
  if (dragStart is not { } start || selected is null || draggingVisual is null || Current is null) return;
  var delta = e.GetPosition(Surface) - start; selected.X = Math.Clamp(originalPosition.X + delta.X, 0, Current.Width - 1); selected.Y = Math.Clamp(originalPosition.Y + delta.Y, 0, Current.Height - 1);
  Canvas.SetLeft(draggingVisual, selected.X); Canvas.SetTop(draggingVisual, selected.Y);
 }
 private void Canvas_Up(object sender, MouseButtonEventArgs e) { if (dragStart is not null) Changed(); dragStart = null; draggingVisual = null; Surface.ReleaseMouseCapture(); }
 private void Fit() { if (Current is null) return; zoom = Math.Max(.01, Math.Min((Viewport.ActualWidth - 22) / Current.Width, (Viewport.ActualHeight - 22) / Current.Height)); ApplyZoom(); }
 private void ApplyZoom() { Surface.LayoutTransform = new ScaleTransform(zoom, zoom); ZoomLabel.Text = $"{zoom:P0}"; }
 private void Fit_Click(object sender, RoutedEventArgs e) { fitting = true; Fit(); }
 private void ZoomOut_Click(object sender, RoutedEventArgs e) { fitting = false; zoom = Math.Max(.01, zoom / 1.25); ApplyZoom(); }
 private void ZoomIn_Click(object sender, RoutedEventArgs e) { fitting = false; zoom = Math.Min(4, zoom * 1.25); ApplyZoom(); }
 private void Viewport_Changed(object sender, SizeChangedEventArgs e) { if (fitting) Fit(); }
 private void MovePage(int delta) { int i = PagesList.SelectedIndex, j = i + delta; if (i < 0 || j < 0 || j >= project.Pages.Count) return; (project.Pages[i], project.Pages[j]) = (project.Pages[j], project.Pages[i]); Changed(); RefreshPages(j); }
 private void Up_Click(object sender, RoutedEventArgs e) => MovePage(-1);
 private void Down_Click(object sender, RoutedEventArgs e) => MovePage(1);
 private void RemovePage_Click(object sender, RoutedEventArgs e)
 {
  if (Current is not { } page) return;
  if (MessageBox.Show(this, UiLanguage.T("このページをレイアウトごとプロジェクトから除外しますか？元画像は削除しません。"), UiLanguage.T("ページ除外"), MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
  int index = PagesList.SelectedIndex; project.Pages.Remove(page); foreach (var set in project.ImageSets) set.Images.Remove(page.Id); Changed(); RefreshPages(index);
 }
 private void New_Click(object sender, RoutedEventArgs e) { if (!ConfirmLeave()) return; project = new(); projectPath = null; dirty = false; cache.Clear(); cacheOrder.Clear(); RefreshPages(); Title = UiLanguage.T("PanelTextor — 新規プロジェクト"); }
 private void Open_Click(object sender, RoutedEventArgs e)
 {
  if (!ConfirmLeave()) return; var dialog = new OpenFileDialog { Filter = UiLanguage.T("PanelTextorプロジェクト|*.paneltextor.json;*.polytext.json|JSON|*.json") }; if (dialog.ShowDialog(this) != true) return;
  try
  {
   var wasLegacy = Storage.Read<Project>(dialog.FileName).Version == 1;
   var loaded = ImageSets.Read(dialog.FileName);
   foreach (var set in loaded.ImageSets)
   {
    foreach (var p in loaded.Pages)
    {
     var source = set.Images.GetValueOrDefault(p.Id);
     // Preserve the legacy locate-file flow; new sets may intentionally be incomplete.
     if (wasLegacy && source is not null && !File.Exists(source))
     {
      var locate = new OpenFileDialog { Title = UiLanguage.T("元画像を指定: ") + p.Name, Filter = UiLanguage.T("画像|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff") };
      if (locate.ShowDialog(this) != true) return; var d = TextRenderer.Dimensions(locate.FileName);
      if (d.Width != p.Width || d.Height != p.Height) throw new IOException(UiLanguage.T("元画像と解像度が一致しません。")); p.Source = locate.FileName; set.Images[p.Id] = locate.FileName;
     }
    }
   }
   project = loaded; projectPath = dialog.FileName; dirty = false; cache.Clear(); cacheOrder.Clear(); RefreshPages(project.PageIndex); Title = "PanelTextor — " + Path.GetFileName(projectPath);
   if (wasLegacy) Changed();
   var checks = ImageSets.Check(project, project.Pages, project.ImageSets);
   if (checks.Issues.Count > 0) ShowIssues(checks.Issues);
  }
  catch (Exception ex) { Error(ex); }
 }
 private bool Save(bool saveAs)
 {
  var path = projectPath;
  if (saveAs || path is null || path.EndsWith(".polytext.json", StringComparison.OrdinalIgnoreCase)) { var dialog = new SaveFileDialog { Filter = UiLanguage.T("PanelTextorプロジェクト|*.paneltextor.json"), DefaultExt = ".paneltextor.json", AddExtension = true, FileName = Storage.ProjectFileName(path) }; if (dialog.ShowDialog(this) != true) return false; path = dialog.FileName; }
  try
  {
   ImageSets.Save(path, project); projectPath = path; dirty = false; Title = "PanelTextor — " + Path.GetFileName(path); Status.Text = UiLanguage.T("プロジェクトを保存しました。"); return true;
  }
  catch (Exception ex) { Error(ex); return false; }
 }
 private void Save_Click(object sender, RoutedEventArgs e) => Save(false);
 private void SaveAs_Click(object sender, RoutedEventArgs e) => Save(true);
 private void Settings_Click(object sender, RoutedEventArgs e) { var dialog = new SettingsWindow(config) { Owner = this }; if (dialog.ShowDialog() != true) return; config = dialog.Result; BuildColors(); RenderObjects(); Select(selected); Status.Text = UiLanguage.T("全テキストにプリセット設定を反映しました。"); }
 private void Replacement_Click(object sender, RoutedEventArgs e)
 {
  var dialog = new ReplacementWindow(config, project.Language) { Owner = this };
  if (dialog.ShowDialog() == true) config = dialog.Result;
 }
 private async void ExportOne_Click(object sender, RoutedEventArgs e) { if (Current is not null) await ExportPages([Current]); }
 private async void ExportAll_Click(object sender, RoutedEventArgs e) { if (project.Pages.Count > 0) await ExportPages(project.Pages.ToArray()); }
 private async Task ExportPages(IReadOnlyList<Page> pages)
 {
  var options = new ExportOptionsWindow(project.ImageSets, project.Language) { Owner = this };
  if (options.ShowDialog() != true) return;
  var dialog = new OpenFolderDialog { Title = UiLanguage.F("{0}版の書き出し先", string.Join(" / ", options.SelectedLanguages.Select(LanguageCodes.Display))) }; if (dialog.ShowDialog(this) != true) return;
  busy = true; IsEnabled = false; ExportMeter.Visibility = Visibility.Visible; ExportMeter.IsIndeterminate = true;
  try
  {
   var check = await Task.Run(() => ImageSets.Check(project, pages, options.SelectedSets));
   if (check.Issues.Count > 0)
   {
    IsEnabled = true; ShowIssues(check.Issues);
    if (check.Jobs.Count == 0 || MessageBox.Show(this, UiLanguage.F("不足・不一致の画像は除外されます。正常な {0} 件のみ書き出しますか？", check.Jobs.Count * options.SelectedLanguages.Count), UiLanguage.T("書き出し対象の確認"), MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
    IsEnabled = false;
   }
   foreach (var set in project.ImageSets) set.ExportEnabled = options.SelectedSets.Contains(set);
   Changed();
   ExportMeter.IsIndeterminate = false;
   var result = await ExportBatch.Run(check.Jobs, options.SelectedLanguages, config, dialog.FolderName, options.Jpeg, UpdateExportProgress);
   Status.Text = UiLanguage.F("{0}/{1}画像を書き出しました（警告 {2} 件）: {3}", result.Succeeded, result.Total, check.Issues.Count + result.Errors.Count, dialog.FolderName);
   IsEnabled = true;
   MessageBox.Show(this, Status.Text + (result.Errors.Count > 0 ? "\n\n" + string.Join("\n", result.Errors) : ""), UiLanguage.T(result.Errors.Count == 0 ? "書き出し完了" : "書き出し終了（一部失敗）"), MessageBoxButton.OK, result.Errors.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
  }
  catch (Exception ex) { Error(ex); }
  finally { busy = false; IsEnabled = true; ExportMeter.IsIndeterminate = false; ExportMeter.Visibility = Visibility.Collapsed; }
 }
 private void UpdateExportProgress(ExportProgress progress)
 {
  ExportMeter.Maximum = Math.Max(1, progress.Total); ExportMeter.Value = progress.Completed;
  Status.Text = UiLanguage.F("{0} / {1} 書き出し中… {2}/{3}", progress.Language, progress.SetName + "/" + progress.PageName, progress.Completed, progress.Total) + $" ({(progress.Total == 0 ? 0 : 100d * progress.Completed / progress.Total):0}%)";
 }
 internal static Task RunSta(Action action)
 {
  var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  var thread = new Thread(() => { try { action(); source.SetResult(); } catch (Exception ex) { source.SetException(ex); } }) { IsBackground = true };
  thread.SetApartmentState(ApartmentState.STA); thread.Start(); return source.Task;
 }
}






