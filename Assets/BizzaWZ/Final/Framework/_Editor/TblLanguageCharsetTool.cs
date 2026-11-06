#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class TblLanguageCharsetTool
{
    private const string DefaultInputAssetPath = "Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes";
    private const string DefaultOutputAssetPath = "Assets/Game/EditorGenerated/Localization/tbllanguage_charset.txt";
    private const string DefaultReportAssetPath = "Assets/Game/EditorGenerated/Localization/tbllanguage_charset_report.txt";

    private static readonly Regex RichTextTagRegex = new Regex("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex FormatPlaceholderRegex = new Regex(@"\{\d+(:[^}]*)?\}", RegexOptions.Compiled);
    private static readonly string[] FixedSupplementalCharsets =
    {
        " 0123456789",
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        "abcdefghijklmnopqrstuvwxyz",
        "ÀÁÂÃÇÉÊÍÓÔÕÚÜ",
        "àáâãçéêíóôõúü",
        "ºª",
        "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~",
        "‘’“”…–",
    };

    [MenuItem("工具/表格/生成语言字库")]
    public static void GenerateDefaultCharset()
    {
        GenerateCharsetAssets(DefaultInputAssetPath, DefaultOutputAssetPath, DefaultReportAssetPath);
    }

    [MenuItem("工具/表格/从选中表格生成字库", false, 2000)]
    public static void GenerateCharsetFromSelectedTableBytes()
    {
        var sourceAssetPath = GetSelectedTableBytesAssetPath();
        if (string.IsNullOrEmpty(sourceAssetPath))
        {
            Debug.LogError("[TblLanguageCharsetTool] Please select one table bytes asset.");
            return;
        }

        var sourceDirectory = Path.GetDirectoryName(sourceAssetPath)?.Replace('\\', '/');
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceAssetPath);
        if (string.IsNullOrEmpty(sourceDirectory) || string.IsNullOrEmpty(sourceFileName))
        {
            Debug.LogError("[TblLanguageCharsetTool] Invalid source asset path: " + sourceAssetPath);
            return;
        }

        var outputAssetPath = sourceDirectory + "/" + sourceFileName + "_charset.txt";
        var reportAssetPath = sourceDirectory + "/" + sourceFileName + "_charset_report.txt";
        GenerateCharsetAssets(sourceAssetPath, outputAssetPath, reportAssetPath);
    }

    [MenuItem("工具/表格/从选中表格生成字库", true)]
    public static bool ValidateGenerateCharsetFromSelectedTableBytes()
    {
        return !string.IsNullOrEmpty(GetSelectedTableBytesAssetPath());
    }

    private static void GenerateCharsetAssets(string sourceAssetPath, string outputAssetPath, string reportAssetPath)
    {
        try
        {
            var sourceAbsolutePath = ToAbsolutePath(sourceAssetPath);
            if (!File.Exists(sourceAbsolutePath))
            {
                Debug.LogError("[TblLanguageCharsetTool] Source table bytes not found: " + sourceAssetPath);
                return;
            }

            var tableAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(sourceAssetPath);
            if (tableAsset == null)
            {
                Debug.LogError("[TblLanguageCharsetTool] Source table bytes cannot be loaded: " + sourceAssetPath);
                return;
            }

            var table = new cfg.TblLanguage();
            table.Init(tableAsset.bytes);

            var charset = CollectCharset(table);
            var charsetText = string.Concat(charset);
            var reportText = BuildReportText(sourceAssetPath, outputAssetPath, charset, charsetText);

            WriteAssetText(outputAssetPath, charsetText);
            WriteAssetText(reportAssetPath, reportText);

            AssetDatabase.Refresh();

            var outputAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(outputAssetPath);
            if (outputAsset != null)
            {
                Selection.activeObject = outputAsset;
                EditorGUIUtility.PingObject(outputAsset);
            }

            Debug.Log(
                "[TblLanguageCharsetTool] Generated charset successfully. Count: " + charset.Count +
                ", Source: " + sourceAssetPath +
                ", Output: " + outputAssetPath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static List<string> CollectCharset(cfg.TblLanguage table)
    {
        var orderedCharset = new List<string>();
        var uniqueCharset = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in table.DataList)
        {
            if (row.Dict == null)
            {
                continue;
            }

            foreach (var languageEntry in row.Dict)
            {
                AddTextElements(NormalizeLocalizedText(languageEntry.Value), uniqueCharset, orderedCharset);
            }
        }

        AddFixedSupplementalCharset(uniqueCharset, orderedCharset);
        return orderedCharset;
    }

    private static void AddFixedSupplementalCharset(HashSet<string> uniqueCharset, List<string> orderedCharset)
    {
        for (var i = 0; i < FixedSupplementalCharsets.Length; i++)
        {
            AddTextElements(FixedSupplementalCharsets[i], uniqueCharset, orderedCharset);
        }
    }

    private static string NormalizeLocalizedText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return string.Empty;
        }

        var text = RichTextTagRegex.Replace(rawText, string.Empty);
        text = FormatPlaceholderRegex.Replace(text, string.Empty);
        return text;
    }

    private static void AddTextElements(string text, HashSet<string> uniqueCharset, List<string> orderedCharset)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var textElement = enumerator.GetTextElement();
            if (ShouldSkipTextElement(textElement) || !uniqueCharset.Add(textElement))
            {
                continue;
            }

            orderedCharset.Add(textElement);
        }
    }

    private static bool ShouldSkipTextElement(string textElement)
    {
        return textElement == "\r" ||
               textElement == "\n" ||
               textElement == "\r\n" ||
               textElement == "\t";
    }

    private static string BuildReportText(string sourceAssetPath, string outputAssetPath, List<string> charset, string charsetText)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Source: " + sourceAssetPath);
        builder.AppendLine("Output: " + outputAssetPath);
        builder.AppendLine("Character Count: " + charset.Count);
        builder.AppendLine("Rules: removed TMP rich-text tags and string format placeholders like {0}.");
        builder.AppendLine("Fixed Supplemental Charset: Arabic numerals, English letters, PT-BR accented letters, and common punctuation for en-US / pt-BR / id-ID.");
        builder.AppendLine();
        builder.AppendLine("Charset Preview:");
        builder.AppendLine(charsetText);
        builder.AppendLine();
        builder.AppendLine("Character Details:");

        for (var i = 0; i < charset.Count; i++)
        {
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(GetReadableTextElement(charset[i]));
            builder.Append("    ");
            builder.AppendLine(GetCodePointSequence(charset[i]));
        }

        return builder.ToString();
    }

    private static string GetReadableTextElement(string textElement)
    {
        if (textElement == " ")
        {
            return "<space>";
        }

        return textElement;
    }

    private static string GetCodePointSequence(string textElement)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < textElement.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(textElement, i);
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append("U+");
            builder.Append(codePoint.ToString("X4"));

            if (char.IsSurrogatePair(textElement, i))
            {
                i++;
            }
        }

        return builder.ToString();
    }

    private static string GetSelectedTableBytesAssetPath()
    {
        var selectedObject = Selection.activeObject;
        if (selectedObject == null)
        {
            return null;
        }

        var assetPath = AssetDatabase.GetAssetPath(selectedObject);
        if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return assetPath.Replace('\\', '/');
    }

    private static void WriteAssetText(string assetPath, string content)
    {
        var absolutePath = ToAbsolutePath(assetPath);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(absolutePath, content, new UTF8Encoding(false));
    }

    private static string ToAbsolutePath(string assetPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            throw new InvalidOperationException("Unable to resolve Unity project root.");
        }

        return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
    }
}
#endif
