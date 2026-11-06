using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TmpTextStyleReplaceTool
{
    public class TmpTextStyleReplaceWindow : EditorWindow
    {
        private const string Header1TagClassName = "TitleLevel1Tag";
        private const string Header2TagClassName = "TitleLevel2Tag";
        private const string Header3TagClassName = "TitleLevel3Tag";
        private const string BodyTagClassName = "BodyTextTag";

        private TmpTextStyleReplaceConfig _config = new TmpTextStyleReplaceConfig();
        private Vector2 _scrollPos;

        [MenuItem("工具/UI/TMP样式替换")]
        public static void Open()
        {
            var window = GetWindow<TmpTextStyleReplaceWindow>("TMP Style Replace");
            window.minSize = new Vector2(620f, 720f);
            window.Show();
        }

        private void OnEnable()
        {
            EnsureFixedTagScripts();
        }

        private void OnGUI()
        {
            EnsureFixedTagScripts();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("TMP 文本样式批量替换工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "扫描指定目录下的 Prefab，根据 TMP_Text 所在 GameObject 上挂载的固定类型脚本，批量替换统一字体资源、颜色和 Material Preset。",
                MessageType.Info);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawScanFolder();
            EditorGUILayout.Space(8f);

            DrawFixedTagScriptInfo();
            EditorGUILayout.Space(8f);

            DrawStyleConfig();
            EditorGUILayout.Space(12f);

            DrawOptions();
            EditorGUILayout.Space(12f);

            DrawExecuteArea();

            EditorGUILayout.EndScrollView();
        }

        private void DrawScanFolder()
        {
            EditorGUILayout.LabelField("1. 扫描目录", EditorStyles.boldLabel);

            _config.ScanFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                new GUIContent("Scan Folder", "只能选择 Assets 目录下的文件夹"),
                _config.ScanFolder,
                typeof(DefaultAsset),
                false);

            if (_config.ScanFolder != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(_config.ScanFolder);
                EditorGUILayout.LabelField("Folder Path", folderPath);
            }
        }

        private void DrawFixedTagScriptInfo()
        {
            EditorGUILayout.LabelField("2. 固定文本类型脚本", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "文本类型脚本已固定，不再手动配置。工具会自动查找并绑定以下脚本。",
                MessageType.None);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("一级标题脚本", _config.Header1Script, typeof(MonoScript), false);
                EditorGUILayout.ObjectField("二级标题脚本", _config.Header2Script, typeof(MonoScript), false);
                EditorGUILayout.ObjectField("三级标题脚本", _config.Header3Script, typeof(MonoScript), false);
                EditorGUILayout.ObjectField("正文脚本", _config.BodyScript, typeof(MonoScript), false);
            }

            if (_config.Header1Script == null ||
                _config.Header2Script == null ||
                _config.Header3Script == null ||
                _config.BodyScript == null)
            {
                EditorGUILayout.HelpBox(
                    "存在未找到的固定类型脚本，请确认项目中已存在以下脚本文件并且类名完全一致：\n" +
                    "- TitleLevel1Tag\n" +
                    "- TitleLevel2Tag\n" +
                    "- TitleLevel3Tag\n" +
                    "- BodyTextTag",
                    MessageType.Warning);
            }
        }

        private void DrawStyleConfig()
        {
            EditorGUILayout.LabelField("3. 文本样式预设", EditorStyles.boldLabel);

            _config.SharedFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(
                new GUIContent("统一字体资源", "所有类型共用同一个 TMP_FontAsset"),
                _config.SharedFontAsset,
                typeof(TMP_FontAsset),
                false);

            EditorGUILayout.Space(6f);

            DrawStylePreset("一级标题样式", _config.Header1Style);
            DrawStylePreset("二级标题样式", _config.Header2Style);
            DrawStylePreset("三级标题样式", _config.Header3Style);
            DrawStylePreset("正文样式", _config.BodyStyle);
        }

        private void DrawOptions()
        {
            EditorGUILayout.LabelField("4. 执行选项", EditorStyles.boldLabel);

            _config.EnableDetailedLog = EditorGUILayout.ToggleLeft(
                "输出详细处理日志",
                _config.EnableDetailedLog);

            _config.LogSkipNoTag = EditorGUILayout.ToggleLeft(
                "输出“未挂任何类型脚本”跳过日志",
                _config.LogSkipNoTag);
        }

        private void DrawExecuteArea()
        {
            EditorGUILayout.LabelField("5. 执行", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!ValidateConfig(showHelpBox: true)))
            {
                if (GUILayout.Button("开始扫描并替换", GUILayout.Height(36f)))
                {
                    ExecuteReplace();
                }
            }
        }

        private void DrawStylePreset(string title, TmpTextStylePreset preset)
        {
            if (preset == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            preset.Color = EditorGUILayout.ColorField("Color", preset.Color);
            preset.MaterialPreset = (Material)EditorGUILayout.ObjectField(
                "Material Preset",
                preset.MaterialPreset,
                typeof(Material),
                false);

            EditorGUILayout.EndVertical();
        }

        private bool ValidateConfig(bool showHelpBox)
        {
            string error = GetValidationError();
            if (!string.IsNullOrEmpty(error) && showHelpBox)
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }

            return string.IsNullOrEmpty(error);
        }

        private string GetValidationError()
        {
            if (_config == null)
            {
                return "配置为空。";
            }

            if (_config.ScanFolder == null)
            {
                return "请先选择 Scan Folder。";
            }

            string folderPath = AssetDatabase.GetAssetPath(_config.ScanFolder);
            if (string.IsNullOrEmpty(folderPath) ||
                !AssetDatabase.IsValidFolder(folderPath) ||
                !folderPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                return "Scan Folder 必须是 Assets 目录下的有效文件夹。";
            }

            if (_config.Header1Script == null)
            {
                return $"未找到固定脚本: {Header1TagClassName}";
            }

            if (_config.Header2Script == null)
            {
                return $"未找到固定脚本: {Header2TagClassName}";
            }

            if (_config.Header3Script == null)
            {
                return $"未找到固定脚本: {Header3TagClassName}";
            }

            if (_config.BodyScript == null)
            {
                return $"未找到固定脚本: {BodyTagClassName}";
            }

            if (_config.SharedFontAsset == null)
            {
                return "请先配置统一字体资源。";
            }

            return null;
        }

        private void ExecuteReplace()
        {
            string validationError = GetValidationError();
            if (!string.IsNullOrEmpty(validationError))
            {
                Debug.LogError($"[TMP Style Replace] 配置无效: {validationError}");
                return;
            }

            try
            {
                var processor = new TmpTextStyleReplaceProcessor(_config);
                TmpTextStyleReplaceResult result = processor.Execute();
                Debug.Log(result.BuildSummary());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TMP Style Replace] 执行失败: {ex}");
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void EnsureFixedTagScripts()
        {
            _config.Header1Script = FindMonoScriptByClassName(Header1TagClassName);
            _config.Header2Script = FindMonoScriptByClassName(Header2TagClassName);
            _config.Header3Script = FindMonoScriptByClassName(Header3TagClassName);
            _config.BodyScript = FindMonoScriptByClassName(BodyTagClassName);
        }

        private MonoScript FindMonoScriptByClassName(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets($"{className} t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (monoScript == null)
                {
                    continue;
                }

                Type scriptClass = monoScript.GetClass();
                if (scriptClass == null)
                {
                    continue;
                }

                if (string.Equals(scriptClass.Name, className, StringComparison.Ordinal))
                {
                    return monoScript;
                }
            }

            return null;
        }
    }
}
