using System;
using System.Text;

public static class RemoteTextUtility
{
    public static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

    private const string GarbledUtf8Bom = "\u00EF\u00BB\u00BF";

    public static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalizedText = text.TrimStart('\uFEFF');
        return normalizedText.StartsWith(GarbledUtf8Bom, StringComparison.Ordinal)
            ? normalizedText.Substring(GarbledUtf8Bom.Length)
            : normalizedText;
    }

    public static string NormalizeJson(string json)
    {
        return NormalizeText(json);
    }
}
