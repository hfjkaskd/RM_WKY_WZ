using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TmpTextStyleReplaceTool
{
    public enum TmpTextStyleType
    {
        None = 0,
        Header1 = 1,
        Header2 = 2,
        Header3 = 3,
        Body = 4,
    }

    [Serializable]
    public class TmpTextStylePreset
    {
        public Color Color = Color.white;
        public Material MaterialPreset;
    }

    [Serializable]
    public class TmpTextStyleReplaceConfig
    {
        public DefaultAsset ScanFolder;

        public MonoScript Header1Script;
        public MonoScript Header2Script;
        public MonoScript Header3Script;
        public MonoScript BodyScript;

        public TMP_FontAsset SharedFontAsset;

        public TmpTextStylePreset Header1Style = new TmpTextStylePreset();
        public TmpTextStylePreset Header2Style = new TmpTextStylePreset();
        public TmpTextStylePreset Header3Style = new TmpTextStylePreset();
        public TmpTextStylePreset BodyStyle = new TmpTextStylePreset();

        public bool EnableDetailedLog = true;
        public bool LogSkipNoTag = false;

        public bool HasAnyTagScript()
        {
            return Header1Script != null || Header2Script != null || Header3Script != null || BodyScript != null;
        }

        public TmpTextStylePreset GetPreset(TmpTextStyleType type)
        {
            switch (type)
            {
                case TmpTextStyleType.Header1:
                    return Header1Style;
                case TmpTextStyleType.Header2:
                    return Header2Style;
                case TmpTextStyleType.Header3:
                    return Header3Style;
                case TmpTextStyleType.Body:
                    return BodyStyle;
                default:
                    return null;
            }
        }
    }

    [Serializable]
    public class TmpTextStyleReplaceResult
    {
        public int ScannedPrefabs;
        public int ScannedTmpTexts;
        public int ReplacedTmpTexts;
        public int SkippedNoTag;
        public int SkippedMultiTags;
        public int FontMismatchFixed;
        public int SavedPrefabs;

        public string BuildSummary()
        {
            return
                "===== TMP Style Replace Result =====\n" +
                $"Scanned Prefabs: {ScannedPrefabs}\n" +
                $"Scanned TMP_Text: {ScannedTmpTexts}\n" +
                $"Replaced TMP_Text: {ReplacedTmpTexts}\n" +
                $"Skipped No Tag: {SkippedNoTag}\n" +
                $"Skipped Multi Tags: {SkippedMultiTags}\n" +
                $"Font Mismatch Fixed: {FontMismatchFixed}\n" +
                $"Saved Prefabs: {SavedPrefabs}\n" +
                "====================================";
        }
    }
}
