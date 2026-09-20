using System.IO;
using System.Reflection;
using System.Windows;
namespace PanelTextor;

internal static class UiLanguage
{
 public static string Current { get; private set; } = "JP";
 private static readonly Dictionary<string, string[]> translations = Read();
 private static Dictionary<string, string[]> Read()
 {
  using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PanelTextor.Data.UiTranslations.txt")!;
  using var reader = new StreamReader(stream);
  return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(line => !string.IsNullOrWhiteSpace(line))
   .Select(line => line.TrimEnd('\r').Split("|||", StringSplitOptions.None).Select(s => s.Replace("\\n", "\n")).ToArray())
   .ToDictionary(row => row[0], row => row);
 }
 public static string T(string source)
 {
  int index = Current switch { "EN" => 1, "ZH" => 2, "KO" => 3, _ => 0 };
  return translations.TryGetValue(source, out var row) ? row[index] : source;
 }
 public static string F(string source, params object[] args) => string.Format(T(source), args);
 public static void Set(string language)
 {
  Current = Config.Languages.Contains(language) ? language : "JP";
  foreach (var pair in XamlSources) Application.Current.Resources[pair.Key] = T(pair.Value);
 }
 public static IEnumerable<string[]> Entries => translations.Values;
 public static readonly Dictionary<string, string> XamlSources = new()
 {
  ["RotationTitle"] = "回転（度）",
  ["RotationHint"] = "文字の中心を軸に回転。正の値は時計回り。",
  ["ReplacementTitle"] = "取り込み時の置換ルール",
  ["LicensesTitle"] = "ライセンス",
  ["CameraFrameTitle"] = "撮影フレームを表示",
  ["CameraTimeTitle"] = "経過時間:",
  ["CameraPageHint"] = "ページごとに保存。残量1は赤、2〜5は白。",
  ["Ui0"] = "PanelTextor — 多言語テキスト配置",
  ["Ui1"] = "画像を追加",
  ["Ui2"] = "新規",
  ["Ui3"] = "開く",
  ["Ui4"] = "保存",
  ["Ui5"] = "別名で保存",
  ["Ui6"] = "プリセット設定",
  ["Ui7"] = "このページを書き出す",
  ["Ui8"] = "全ページを書き出す",
  ["Ui9"] = "JP 日本語",
  ["Ui10"] = "EN English",
  ["Ui11"] = "CN 中文",
  ["Ui12"] = "KR 한국어",
  ["Ui13"] = "この言語を表示",
  ["Ui14"] = "JP を編集中",
  ["Ui15"] = "Background:",
  ["Ui16"] = "画像セットを追加／更新",
  ["Ui17"] = "対応チェック",
  ["Ui18"] = "画像・文字の比較情報",
  ["Ui19"] = "画像を追加して作業を開始してください。",
  ["Ui20"] = "ページ",
  ["Ui21"] = "除外",
  ["Ui22"] = "全体表示",
  ["Ui23"] = "まとめて入力",
  ["Ui24"] = "空行で分割・通常の改行は維持",
  ["Ui25"] = "空行で分割して追加",
  ["Ui26"] = "追加後は下の欄で個別に編集できます。",
  ["Ui27"] = "テキストを選択",
  ["Ui28"] = "文字サイズ",
  ["Ui29"] = "小",
  ["Ui30"] = "中",
  ["Ui31"] = "大",
  ["Ui32"] = "文字色",
  ["Ui33"] = "文字方向",
  ["Ui34"] = "縦書き",
  ["Ui35"] = "横書き",
  ["Ui36"] = "選択テキストを削除 (Delete)",
  ["Ui37"] = "行間（行送り px）",
  ["Ui38"] = "既定",
  ["Ui39"] = "0＝言語の設定を使用。改行のあるブロックに反映。",
  ["Ui40"] = "配置済みテキスト",
  ["Ui41"] = "Ctrl＋クリックで複数選択。一覧ではShiftで範囲選択。Deleteで選択を削除。単独選択でドラッグ移動・編集。座標とサイズは元画像のピクセル基準です。表示OFFでも書き出しには含まれます。",
 };
}



