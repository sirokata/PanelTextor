using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
namespace PanelTextor;
internal static class DistributionTerms
{
 internal static string Text(string file)
 {
  using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("PanelTextor." + file)!);
  return reader.ReadToEnd();
 }
 internal static string Fingerprint => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Text("TERMS.md") + Text("LICENSE") + Text("THIRD_PARTY_NOTICES.md") + string.Join("\n", LicenseWindow.Notices().OrderBy(p => p.Key).Select(p => p.Key + p.Value)))));
 internal static bool Accepted(string path) => File.Exists(path) && File.ReadAllText(path) == Fingerprint;
 internal static void Accept(string path) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, Fingerprint); }
 internal static bool Prompt()
 {
  var path = Path.Combine(Path.GetDirectoryName(Storage.ConfigPath)!, "terms-acceptance.txt");
  if (Accepted(path)) return true;
  var window = CreateWindow();
  if (window.ShowDialog() != true) return false;
  Accept(path); return true;
 }
 internal static Window CreateWindow()
 {
  var window = new Window { Title = "PanelTextor — " + UiLanguage.T("利用条件"), Width = 900, Height = 720, WindowStartupLocation = WindowStartupLocation.CenterScreen };
  var panel = new DockPanel { Margin = new Thickness(16) }; window.Content = panel;
  var bottom = new StackPanel(); DockPanel.SetDock(bottom, Dock.Bottom); panel.Children.Add(bottom);
  var agree = new CheckBox { Content = UiLanguage.T("利用条件と第三者ライセンスの適用条件に同意します。"), Margin = new Thickness(0, 12, 0, 8) }; bottom.Children.Add(agree);
  var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; bottom.Children.Add(buttons);
  var accept = new Button { Content = UiLanguage.T("同意して起動"), IsEnabled = false, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(5) };
  var decline = new Button { Content = UiLanguage.T("同意せず終了"), IsCancel = true, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(5) }; buttons.Children.Add(accept); buttons.Children.Add(decline);
  agree.Checked += (_, _) => accept.IsEnabled = true; agree.Unchecked += (_, _) => accept.IsEnabled = false;
  accept.Click += (_, _) => window.DialogResult = true;
  var tabs = new TabControl(); panel.Children.Add(tabs);
  foreach (var file in new[] { "TERMS.md", "TERMS.en.md", "TERMS.zh-CN.md", "TERMS.ko.md", "LICENSE", "THIRD_PARTY_NOTICES.md" }) tabs.Items.Add(new TabItem { Header = file switch { "TERMS.md" => "日本語", "TERMS.en.md" => "English", "TERMS.zh-CN.md" => "简体中文", "TERMS.ko.md" => "한국어", _ => file }, Content = new TextBox { Text = Text(file), IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });
  tabs.SelectedIndex = TermsIndex(UiLanguage.Current);
  var originals = new TabControl();
  foreach (var pair in LicenseWindow.Notices()) originals.Items.Add(new TabItem { Header = pair.Key, Content = new TextBox { Text = pair.Value, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });
  tabs.Items.Add(new TabItem { Header = UiLanguage.T("第三者ライセンス原文"), Content = originals });
  return window;
 }
 internal static int TermsIndex(string language) => language switch { "EN" => 1, "ZH" => 2, "KO" => 3, _ => 0 };
}
