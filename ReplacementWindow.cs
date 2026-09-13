using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace PanelTextor;
public class ReplacementWindow : Window
{
 public Config Result { get; }
 private readonly Dictionary<string, (DataGrid Grid, ObservableCollection<ReplacementRule> Rules)> editors = [];
 public ReplacementWindow(Config config, string language)
 {
  Result = JsonSerializer.Deserialize<Config>(JsonSerializer.Serialize(config))!;
  Title = UiLanguage.T("取り込み時の置換ルール"); Width = 780; Height = 570; WindowStartupLocation = WindowStartupLocation.CenterOwner;
  var panel = new DockPanel { Margin = new Thickness(16) }; Content = panel;
  var hint = new TextBlock { Text = UiLanguage.T("言語ごと・全プロジェクト共通。追加時に上から順に文字列を置換します（大文字小文字を区別・部分一致・正規表現なし）。置換後の文字列も次のルールの対象です。置換後が空欄なら削除。既存テキストには適用しません。"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
  DockPanel.SetDock(hint, Dock.Top); panel.Children.Add(hint);
  var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
  DockPanel.SetDock(footer, Dock.Bottom); panel.Children.Add(footer);
  var save = new Button { Content = UiLanguage.T("設定を保存して反映"), Padding = new Thickness(12), Margin = new Thickness(4) };
  var cancel = new Button { Content = UiLanguage.T("キャンセル"), IsCancel = true, Padding = new Thickness(12), Margin = new Thickness(4) };
  footer.Children.Add(save); footer.Children.Add(cancel);
  var tabs = new TabControl(); panel.Children.Add(tabs);
  foreach (var lang in Config.Languages)
  {
   var rules = new ObservableCollection<ReplacementRule>(Result.Styles[lang].ImportReplacements ?? []);
   var grid = new DataGrid { ItemsSource = rules, AutoGenerateColumns = false, CanUserAddRows = true, CanUserDeleteRows = true, CanUserSortColumns = false, SelectionMode = DataGridSelectionMode.Extended };
   grid.Columns.Add(new DataGridCheckBoxColumn { Header = UiLanguage.T("有効"), Binding = new Binding(nameof(ReplacementRule.Enabled)), Width = 60 });
   grid.Columns.Add(new DataGridTextColumn { Header = UiLanguage.T("置換前"), Binding = new Binding(nameof(ReplacementRule.Find)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
   grid.Columns.Add(new DataGridTextColumn { Header = UiLanguage.T("置換後"), Binding = new Binding(nameof(ReplacementRule.ReplaceWith)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
   var area = new DockPanel(); var remove = new Button { Content = UiLanguage.T("選択ルールを削除"), Padding = new Thickness(8), Margin = new Thickness(0, 8, 0, 8) };
   DockPanel.SetDock(remove, Dock.Bottom); area.Children.Add(remove); area.Children.Add(grid);
   remove.Click += (_, _) => { grid.CommitEdit(DataGridEditingUnit.Cell, true); grid.CommitEdit(DataGridEditingUnit.Row, true); foreach (var rule in grid.SelectedItems.OfType<ReplacementRule>().ToArray()) rules.Remove(rule); };
   editors[lang] = (grid, rules); tabs.Items.Add(new TabItem { Header = LanguageCodes.Display(lang), Content = area });
  }
  tabs.SelectedIndex = Math.Max(0, Array.IndexOf(Config.Languages, language));
  save.Click += (_, _) =>
  {
   try
   {
    foreach (var (lang, editor) in editors)
    {
     if (!editor.Grid.CommitEdit(DataGridEditingUnit.Cell, true) || !editor.Grid.CommitEdit(DataGridEditingUnit.Row, true)) return;
     if (editor.Rules.Any(r => string.IsNullOrEmpty(r.Find) && !string.IsNullOrEmpty(r.ReplaceWith))) throw new InvalidOperationException(UiLanguage.T("置換前を入力してください。"));
     Result.Styles[lang].ImportReplacements = editor.Rules.Where(r => !string.IsNullOrEmpty(r.Find)).ToList();
    }
    Storage.Write(Storage.ConfigPath, Result); DialogResult = true;
   }
   catch (Exception ex) { MessageBox.Show(this, ex.Message, Title); }
  };
 }
}
