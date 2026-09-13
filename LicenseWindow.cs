using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
namespace PanelTextor;
public class LicenseWindow : Window
{
 internal static Dictionary<string, string> Notices()
 {
  var assembly = Assembly.GetExecutingAssembly(); var result = new Dictionary<string, string>();
  foreach (var file in new[] { "LICENSE", "TERMS.md", "TERMS.en.md", "TERMS.zh-CN.md", "TERMS.ko.md", "THIRD_PARTY_NOTICES.md" }) result[file] = DistributionTerms.Text(file);
  foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("PanelTextor.licenses.")))
  {
   using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
   result[name["PanelTextor.licenses.".Length..]] = reader.ReadToEnd();
  }
  return result;
 }
 public LicenseWindow()
 {
  Title = UiLanguage.T("ライセンス"); Width = 820; Height = 650; WindowStartupLocation = WindowStartupLocation.CenterOwner;
  var panel = new DockPanel { Margin = new Thickness(12) }; Content = panel;
  var list = new ComboBox { Margin = new Thickness(0, 0, 0, 10) }; DockPanel.SetDock(list, Dock.Top); panel.Children.Add(list);
  var text = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; panel.Children.Add(text);
  var notices = Notices(); list.ItemsSource = notices.Keys.Order().ToArray();
  list.SelectionChanged += (_, _) => { if (list.SelectedItem is string key) text.Text = notices[key]; };
  list.SelectedIndex = 0;
 }
}

