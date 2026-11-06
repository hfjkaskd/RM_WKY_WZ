using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TmpTextStyleReplaceTool
{
    public class TmpTextStyleReplaceProcessor
    {
        private readonly TmpTextStyleReplaceConfig _config;

        public TmpTextStyleReplaceProcessor(TmpTextStyleReplaceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public TmpTextStyleReplaceResult Execute()
        {
            var result = new TmpTextStyleReplaceResult();

            string folderPath = AssetDatabase.GetAssetPath(_config.ScanFolder);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            result.ScannedPrefabs = guids.Length;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                    EditorUtility.DisplayProgressBar(
                        "TMP Style Replace",
                        $"Processing Prefab {i + 1}/{guids.Length}\n{prefabPath}",
                        guids.Length == 0 ? 1f : (float)(i + 1) / guids.Length);

                    ProcessPrefab(prefabPath, result);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return result;
        }

        private void ProcessPrefab(string prefabPath, TmpTextStyleReplaceResult result)
        {
            GameObject prefabRoot = null;

            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot == null)
                {
                    Debug.LogWarning($"[TMP Style Replace] 无法加载 Prefab: {prefabPath}");
                    return;
                }

                TMP_Text[] texts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
                result.ScannedTmpTexts += texts.Length;

                bool prefabModified = false;

                foreach (TMP_Text tmpText in texts)
                {
                    if (tmpText == null)
                    {
                        continue;
                    }

                    ProcessSingleText(prefabPath, tmpText, result, ref prefabModified);
                }

                if (prefabModified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    result.SavedPrefabs++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TMP Style Replace] 处理 Prefab 失败: {prefabPath}\n{ex}");
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
        }

        private void ProcessSingleText(string prefabPath, TMP_Text tmpText, TmpTextStyleReplaceResult result, ref bool prefabModified)
        {
            List<TmpTextStyleType> matchedTypes = GetMatchedTypes(tmpText.gameObject);

            if (matchedTypes.Count == 0)
            {
                result.SkippedNoTag++;

                if (_config.LogSkipNoTag)
                {
                    Debug.Log($"[Skip] Prefab: {prefabPath}, Object: {GetObjectPath(tmpText.transform)}, TMP_Text 未挂任何类型脚本，已跳过");
                }

                return;
            }

            if (matchedTypes.Count > 1)
            {
                result.SkippedMultiTags++;

                Debug.LogWarning(
                    $"[Warning] Prefab: {prefabPath}, Object: {GetObjectPath(tmpText.transform)}, TMP_Text 同时挂载多个类型脚本: {BuildTypeList(matchedTypes)}，已跳过");

                return;
            }

            TmpTextStyleType styleType = matchedTypes[0];
            TmpTextStylePreset targetStyle = GetStylePreset(styleType);
            if (targetStyle == null)
            {
                Debug.LogWarning(
                    $"[TMP Style Replace] Prefab: {prefabPath}, Object: {GetObjectPath(tmpText.transform)}, 类型 {styleType} 未配置样式，已跳过");
                return;
            }

            bool textModified = false;

            if (tmpText.font != _config.SharedFontAsset)
            {
                tmpText.font = _config.SharedFontAsset;
                textModified = true;
                result.FontMismatchFixed++;

                Debug.LogWarning(
                    $"[Warning] Prefab: {prefabPath}, Object: {GetObjectPath(tmpText.transform)}, TMP_Text 当前 FontAsset 与统一字体资源不一致，已自动替换");
            }

            if (tmpText.color != targetStyle.Color)
            {
                tmpText.color = targetStyle.Color;
                textModified = true;
            }

            if (tmpText.fontSharedMaterial != targetStyle.MaterialPreset)
            {
                tmpText.fontSharedMaterial = targetStyle.MaterialPreset;
                textModified = true;
            }

            if (!textModified)
            {
                return;
            }

            prefabModified = true;
            result.ReplacedTmpTexts++;

            if (_config.EnableDetailedLog)
            {
                Debug.Log(
                    $"[Info] Prefab: {prefabPath}, Object: {GetObjectPath(tmpText.transform)}, 已应用样式: {GetStyleDisplayName(styleType)}");
            }

            EditorUtility.SetDirty(tmpText);
        }

        private List<TmpTextStyleType> GetMatchedTypes(GameObject target)
        {
            var matchedTypes = new List<TmpTextStyleType>();

            if (target == null)
            {
                return matchedTypes;
            }

            TryAddMatchedType(target, _config.Header1Script, TmpTextStyleType.Header1, matchedTypes);
            TryAddMatchedType(target, _config.Header2Script, TmpTextStyleType.Header2, matchedTypes);
            TryAddMatchedType(target, _config.Header3Script, TmpTextStyleType.Header3, matchedTypes);
            TryAddMatchedType(target, _config.BodyScript, TmpTextStyleType.Body, matchedTypes);

            return matchedTypes;
        }

        private void TryAddMatchedType(
            GameObject target,
            MonoScript script,
            TmpTextStyleType styleType,
            List<TmpTextStyleType> matchedTypes)
        {
            if (target == null || script == null)
            {
                return;
            }

            Type scriptType = script.GetClass();
            if (scriptType == null)
            {
                return;
            }

            if (!typeof(Component).IsAssignableFrom(scriptType))
            {
                return;
            }

            if (target.GetComponent(scriptType) != null)
            {
                matchedTypes.Add(styleType);
            }
        }

        private TmpTextStylePreset GetStylePreset(TmpTextStyleType styleType)
        {
            switch (styleType)
            {
                case TmpTextStyleType.Header1:
                    return _config.Header1Style;
                case TmpTextStyleType.Header2:
                    return _config.Header2Style;
                case TmpTextStyleType.Header3:
                    return _config.Header3Style;
                case TmpTextStyleType.Body:
                    return _config.BodyStyle;
                default:
                    return null;
            }
        }

        private string GetStyleDisplayName(TmpTextStyleType styleType)
        {
            switch (styleType)
            {
                case TmpTextStyleType.Header1:
                    return "一级标题";
                case TmpTextStyleType.Header2:
                    return "二级标题";
                case TmpTextStyleType.Header3:
                    return "三级标题";
                case TmpTextStyleType.Body:
                    return "正文";
                default:
                    return "未知类型";
            }
        }

        private string BuildTypeList(List<TmpTextStyleType> types)
        {
            if (types == null || types.Count == 0)
            {
                return string.Empty;
            }

            var names = new List<string>(types.Count);
            foreach (TmpTextStyleType type in types)
            {
                names.Add(GetStyleDisplayName(type));
            }

            return string.Join(", ", names);
        }

        private string GetObjectPath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            var sb = new StringBuilder(target.name);
            Transform current = target.parent;

            while (current != null)
            {
                sb.Insert(0, current.name + "/");
                current = current.parent;
            }

            return sb.ToString();
        }
    }
}