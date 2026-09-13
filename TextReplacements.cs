namespace PanelTextor;
public class ReplacementRule
{
 public bool Enabled { get; set; } = true;
 public string Find { get; set; } = "";
 public string ReplaceWith { get; set; } = "";
}
public static class TextReplacements
{
 public static string Apply(string text, LanguageStyle style)
 {
  foreach (var rule in style.ImportReplacements ?? [])
   if (rule is { Enabled: true } && !string.IsNullOrEmpty(rule.Find))
    text = text.Replace(rule.Find, rule.ReplaceWith ?? "", StringComparison.Ordinal);
  return text;
 }
}
