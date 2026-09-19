using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PanelTextor;
internal static class VerticalTests
{
 public static void Run(string folder, Action<bool, string> check)
 {
  var specimens = new[] { "あ〜っ！", "えっ!!!", "本当に!?", "10年後", "100回", "「これはテストです」", "『本当に？』", "（そんな……）", "【注意】", "あー……", "え～～～っ！？", "そうなの？！", "……え？", "、。［］〈〉《》", "ー〜～―－…‥", "!! !!! !? ?! ??", "！ ？ ？！ ！？", "あ゛あ゛あ゛っ♥", "あ゙い゙ゔえ゙お゙", "か゛がは゜ぱ", "゛あ ゛あ\nあ゛", "ア゛イ゛エ゛オ゛" };
  var families = new[] { "源暎アンチック v6", "Yu Gothic", "Yu Mincho", "MS Mincho" };
  var diagnostics = new List<string>();
  foreach (var name in families)
  {
   var config = new Config(); config.Styles["JP"].Font = FontCatalog.Find(name)?.Key ?? name;
   config.Styles["JP"].Sizes["medium"] = 42; config.Styles["JP"].OutlineWidth = 2;
   var visual = new DrawingVisual();
   using (var dc = visual.RenderOpen())
   {
    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(190, 199, 207)), null, new Rect(0, 0, 1550, 1450));
    for (int i = 0; i < specimens.Length; i++)
    {
     int column = i % 9, band = i / 9;
     var obj = new TextObject { Text = specimens[i], Direction = "vertical", X = 30 + column * 168, Y = 55 + band * 460 };
     var geometry = VerticalLayout.Build(obj, "JP", config);
     check(!geometry.IsEmpty() && double.IsFinite(geometry.Bounds.Height), name + ": " + specimens[i]);
     dc.PushTransform(new TranslateTransform(obj.X, obj.Y));
     dc.DrawRectangle(null, new Pen(Brushes.SlateGray, .5), new Rect(0, 0, 42, 420));
     dc.DrawDrawing(TextRenderer.Drawing(obj, "JP", config)); dc.Pop();
    }
   }
   var bitmap = new RenderTargetBitmap(1550, 1450, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
   var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
   using var file = File.Create(Path.Combine(folder, "vertical-" + name + ".png")); encoder.Save(file);
   foreach (var symbol in "〜～ー―－…‥、。（）「」『』【】［］〈〉《》")
    diagnostics.Add($"{name} {symbol}: alternate={VerticalLayout.HasAlternate(config.Styles["JP"].Font, symbol.ToString())}");
   foreach (var symbol in new[] { "〜", "～", "ー", "…", "‥", "―", "－" })
   {
    var b = VerticalLayout.Build(new TextObject { Text = symbol, Direction = "vertical" }, "JP", config).Bounds;
    check(Math.Abs((b.Left + b.Right) / 2 - 21) < 42 * .20, name + ": centered vertical mark " + symbol);
   }
   foreach (var symbol in new[] { "、", "。" })
   {
    var b = VerticalLayout.Build(new TextObject { Text = symbol, Direction = "vertical" }, "JP", config).Bounds;
    check((b.Left + b.Right) / 2 > 21 && (b.Top + b.Bottom) / 2 < 21, name + ": punctuation top-right " + symbol);
   }
   var tcy = VerticalLayout.Build(new TextObject { Text = "!!!", Direction = "vertical" }, "JP", config).Bounds;
   check(tcy.Width <= 42 && Math.Abs((tcy.Left + tcy.Right) / 2 - 21) < .01, name + ": tate-chu-yoko fits and centers in one cell");
   var plain = VerticalLayout.Build(new TextObject { Text = "あ", Direction = "vertical" }, "JP", config);
   var voiced = (GeometryGroup)VerticalLayout.Build(new TextObject { Text = "あ゛あ", Direction = "vertical" }, "JP", config);
   check(voiced.Children.Count == 2 && Math.Abs(voiced.Children[1].Bounds.Top - plain.Bounds.Top - 42) < .01, name + ": spacing dakuten shares the kana cell");
   var attached = (GeometryGroup)((GeometryGroup)voiced.Children[0]).Children[0];
   check(attached.Children[1].Bounds.Left >= plain.Bounds.Right && attached.Children[1].Bounds.Top < plain.Bounds.Top, name + ": expressive dakuten sits upper-right");
   var combining = VerticalLayout.Build(new TextObject { Text = "あ\u3099あ", Direction = "vertical" }, "JP", config);
   check(combining.Bounds == voiced.Bounds, name + ": combining and spacing dakuten agree");
   var sequence = (GeometryGroup)VerticalLayout.Build(new TextObject { Text = "!!!あ", Direction = "vertical" }, "JP", config);
   check(Math.Abs(sequence.Children[1].Bounds.Top - plain.Bounds.Top - 42) < .01, name + ": following text advances one cell after tate-chu-yoko");
  }
  foreach (var run in new[] { "!!", "!!!", "!?", "?!", "??", "!?!?!!", "10", "100" })
   check(VerticalLayout.Tokens(run) is { Count: 1 } tokens && tokens[0].Combined, "Automatic tate-chu-yoko: " + run);
  check(VerticalLayout.Tokens("1000").All(t => !t.Combined) && VerticalLayout.Tokens("！？").All(t => !t.Combined), "Long numbers and fullwidth punctuation remain separate cells");
  File.WriteAllLines(Path.Combine(folder, "vertical-features.txt"), diagnostics);
  check(VerticalLayout.Tokens("か゛は゜").Select(t => t.Text).SequenceEqual(new[] { "が", "ぱ" }), "Standard voiced kana use composed glyphs");
  check(VerticalLayout.Tokens("゛あ ゛").Count == 4, "Leading and space-separated dakuten remain standalone");
 }
}


