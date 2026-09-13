namespace PanelTextor;
public static class LanguageCodes
{
 // Keep existing project and font-preset keys so saved layouts remain compatible.
 public static string Display(string key) => key switch { "ZH" => "CN", "KO" => "KR", _ => key };
}

