using System.Windows;
using System.Windows.Controls;
namespace PanelTextor;
public class ExportOptionsWindow : Window
{
 public List<ImageSet> SelectedSets { get; private set; } = [];
 public bool Jpeg { get; private set; }
 public List<string> SelectedLanguages { get; private set; } = [];
 internal readonly Dictionary<string, CheckBox> LanguageChecks = [];
 public ExportOptionsWindow(IEnumerable<ImageSet> sets, string currentLanguage = "JP")
 {
  Title = UiLanguage.T("書き出す画像セット"); Width = 480; Height = 560; WindowStartupLocation = WindowStartupLocation.CenterOwner;
  var panel = new StackPanel { Margin = new Thickness(20) };
  Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
  panel.Children.Add(new TextBlock { Text = UiLanguage.T("書き出す言語（複数選択可）"), FontWeight = FontWeights.Bold });
  var languages = new WrapPanel { Margin = new Thickness(0, 8, 0, 20) }; panel.Children.Add(languages);
  foreach (var language in Config.Languages)
  {
   var checkbox = new CheckBox { Content = LanguageCodes.Display(language), IsChecked = language == currentLanguage, Margin = new Thickness(4, 4, 18, 4) };
   LanguageChecks[language] = checkbox; languages.Children.Add(checkbox);
  }
  panel.Children.Add(new TextBlock { Text = UiLanguage.T("同じテキストを適用する背景を選択"), FontWeight = FontWeights.Bold, FontSize = 17 });
  var checks = new List<(ImageSet Set, CheckBox Check)>();
  foreach (var set in sets)
  {
   var check = new CheckBox { Content = set.Name, IsChecked = set.ExportEnabled, Margin = new Thickness(4, 10, 4, 5) }; checks.Add((set, check)); panel.Children.Add(check);
  }
  var format = new ComboBox { ItemsSource = new[] { UiLanguage.T("PNG（劣化なし・sRGB）"), UiLanguage.T("JPEG（品質100・sRGB）") }, SelectedIndex = 0, Margin = new Thickness(0, 15, 0, 10) }; panel.Children.Add(format);
  panel.Children.Add(new TextBlock { Text = UiLanguage.T("出力先 / 言語 / セット名 / 元の名前.png（または.jpg）\n同名の出力は連番にします。JPEGの透明部分は白です。"), TextWrapping = TextWrapping.Wrap });
  var button = new Button { Content = UiLanguage.T("次へ：書き出し先を選ぶ"), Padding = new Thickness(10), Margin = new Thickness(0, 20, 0, 0) }; panel.Children.Add(button);
  button.Click += (_, _) =>
  {
   SelectedSets = checks.Where(c => c.Check.IsChecked == true).Select(c => c.Set).ToList();
   if (SelectedSets.Count == 0) { MessageBox.Show(this, UiLanguage.T("1つ以上のセットを選択してください。")); return; }
   if (!ReadLanguages()) { MessageBox.Show(this, UiLanguage.T("1つ以上の言語を選択してください。")); return; }
   Jpeg = format.SelectedIndex == 1; DialogResult = true;
  };
 }
 internal bool ReadLanguages()
 {
  SelectedLanguages = Config.Languages.Where(l => LanguageChecks[l].IsChecked == true).ToList();
  return SelectedLanguages.Count > 0;
 }
}

