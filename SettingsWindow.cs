using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace PanelTextor;
public class SettingsWindow : Window
{
 public Config Result { get; }
 private readonly Dictionary<string, ComboBox> fonts = [];
 private readonly Dictionary<string, ComboBox> weights = [];
 private readonly Dictionary<string, TextBox> values = [];
 public SettingsWindow(Config source)
 {
  Result = JsonSerializer.Deserialize<Config>(JsonSerializer.Serialize(source))!;
  Title = UiLanguage.T("プリセット設定 — 変更は既存テキストにも反映"); Width = 690; Height = 720; WindowStartupLocation = WindowStartupLocation.CenterOwner;
  var panel = new StackPanel { Margin = new Thickness(20) }; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
  panel.Children.Add(new TextBlock { Text = UiLanguage.T("言語別フォント・サイズ・縁取り"), FontSize = 20, FontWeight = FontWeights.Bold });
  var installed = FontCatalog.Installed;
  panel.Children.Add(new TextBlock { Text = UiLanguage.F("全言語共通のフォント一覧：{0}件（各言語名・ファイル名で表示）", installed.Count), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 4) });
  if (FontCatalog.LoadErrors.Count > 0) panel.Children.Add(new TextBox { Text = UiLanguage.T("読み込めなかったフォント：\n") + string.Join("\n", FontCatalog.LoadErrors), IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MaxHeight = 80, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
  foreach (var lang in Config.Languages)
  {
   panel.Children.Add(new TextBlock { Text = LanguageCodes.Display(lang), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 16, 0, 4) });
   var combo = new ComboBox { ItemsSource = installed, DisplayMemberPath = nameof(FontChoice.DisplayName), Text = FontCatalog.Find(Result.Styles[lang].Font)?.DisplayName ?? Result.Styles[lang].Font, IsEditable = true, Margin = new Thickness(0, 3, 0, 6) }; fonts[lang] = combo; panel.Children.Add(combo);
   panel.Children.Add(new TextBlock { Text = UiLanguage.T("ウェイト（可変フォントの太さ）") });
   var weightBox = new ComboBox { DisplayMemberPath = nameof(WeightChoice.Label), SelectedValuePath = nameof(WeightChoice.Value), Margin = new Thickness(0, 3, 0, 8) };
   weights[lang] = weightBox; panel.Children.Add(weightBox);
   void RefreshWeight(string fontName, double selected)
   {
    var choices = VerticalLayout.Weights(fontName); weightBox.ItemsSource = choices.Select(w => new WeightChoice(w.Value, w.Value == 0 ? UiLanguage.T(w.Label) : w.Label)).ToArray();
    weightBox.SelectedValue = choices.Any(c => c.Value == selected) ? selected : 0d;
    weightBox.IsEnabled = choices.Count > 1;
    weightBox.ToolTip = choices.Count > 1 ? UiLanguage.T("フォント本来のwght軸を使います。既定は以前の表示を維持します。") : UiLanguage.T("このフォントには可変ウェイト軸がありません。別の太さはフォント一覧で選んでください。");
   }
   RefreshWeight(Result.Styles[lang].Font, Result.Styles[lang].FontWeight);
   combo.SelectionChanged += (_, _) => { if (combo.SelectedItem is FontChoice choice) RefreshWeight(choice.Key, 0); };
   combo.LostKeyboardFocus += (_, _) =>
   {
    if (FontCatalog.Find(combo.Text) is { } choice) RefreshWeight(choice.Key, weightBox.SelectedValue is double value ? value : 0);
   };
   var row = new WrapPanel(); panel.Children.Add(row);
   foreach (var size in new[] { "small", "medium", "large" }) Add(row, lang + size, size switch { "small" => UiLanguage.T("小 px"), "medium" => UiLanguage.T("中 px"), _ => UiLanguage.T("大 px") }, Result.Styles[lang].Sizes[size].ToString(CultureInfo.InvariantCulture), 70);
   Add(row, lang + "outline", UiLanguage.T("縁色 HEX"), Result.Styles[lang].OutlineColor, 100); Add(row, lang + "width", UiLanguage.T("縁幅 px"), Result.Styles[lang].OutlineWidth.ToString(CultureInfo.InvariantCulture), 70);
   var spacing = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) }; panel.Children.Add(spacing);
   Add(spacing, lang + "line", UiLanguage.T("横書きの行間 px (0=自動)"), Result.Styles[lang].LineHeightPx.ToString(CultureInfo.InvariantCulture), 220);
   Add(spacing, lang + "column", UiLanguage.T("縦書きの行間 em"), Result.Styles[lang].VerticalColumnEm.ToString(CultureInfo.InvariantCulture), 150);
  }
  panel.Children.Add(new TextBlock { Text = UiLanguage.T("共通の文字色（#RRGGBB）"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 5) });
  var colors = new WrapPanel(); panel.Children.Add(colors);
  foreach (var pair in Result.Colors) Add(colors, pair.Key, pair.Key, pair.Value, 125);
  panel.Children.Add(new TextBlock { Text = UiLanguage.T("サイズは元画像のpx。文字サイズはピクセル単位で指定します。\nHEXはsRGB。縁幅は外側への太さです。設定は全プロジェクト共通です。\n字間の追加は0。縦の文字送りは1 em。描画方式によって輪郭の見え方が異なります。"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 15, 0, 10) });
  var save = new Button { Content = UiLanguage.T("設定を保存して反映"), Padding = new Thickness(12) }; save.Click += Save; panel.Children.Add(save);
 }
 private void Add(Panel parent, string key, string label, string value, double width)
 {
  var p = new StackPanel { Width = width, Margin = new Thickness(0, 0, 8, 0) }; p.Children.Add(new TextBlock { Text = label });
  var input = new TextBox { Text = value, Padding = new Thickness(5) }; values[key] = input; p.Children.Add(input); parent.Children.Add(p);
 }
 private void Save(object sender, RoutedEventArgs e)
 {
  try
  {
   double Number(string key, double min, double max) { if (!double.TryParse(values[key].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) || !double.IsFinite(n) || n < min || n > max) throw new Exception(UiLanguage.F("{0}: {1}〜{2}の数値を入力してください。", key, min, max)); return n; }
   string Hex(string key) { var text = values[key].Text.Trim(); if (!System.Text.RegularExpressions.Regex.IsMatch(text, "^#[0-9A-Fa-f]{6}$")) throw new Exception(UiLanguage.T("色は #RRGGBB 形式で入力してください。")); return text; }
   foreach (var lang in Config.Languages)
   {
    var choice = FontCatalog.Find(fonts[lang].Text) ?? throw new Exception(lang + UiLanguage.T(": 一覧からインストール済みフォントを選択してください。"));
    var style = Result.Styles[lang]; style.Font = choice.Key;
    style.FontWeight = weights[lang].SelectedValue is double selectedWeight ? selectedWeight : 0;
    if (!VerticalLayout.Weights(style.Font).Any(w => w.Value == style.FontWeight)) throw new Exception(lang + UiLanguage.T(": ウェイトを選び直してください。"));
    foreach (var size in new[] { "small", "medium", "large" }) style.Sizes[size] = Number(lang + size, 1, 1000);
    style.OutlineColor = Hex(lang + "outline"); style.OutlineWidth = Number(lang + "width", 0, 100);
    style.LineHeightPx = Number(lang + "line", 0, 3000); style.VerticalColumnEm = Number(lang + "column", .1, 10);
   }
   foreach (var key in Result.Colors.Keys.ToArray()) Result.Colors[key] = Hex(key);
   Storage.Write(Storage.ConfigPath, Result); DialogResult = true;
  }
  catch (Exception ex) { MessageBox.Show(this, ex.Message, UiLanguage.T("設定を確認してください")); }
 }
}



