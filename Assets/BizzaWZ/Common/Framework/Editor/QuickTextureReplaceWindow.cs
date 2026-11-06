#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class QuickTextureReplaceWindow : EditorWindow
{
    private const string EnableReplaceKey = "BizzaWZ.QuickTextureReplace.Enabled";

    private static readonly HashSet<string> ImageExtensions = new HashSet<string>
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".tga",
        ".psd",
        ".psb",
        ".tif",
        ".tiff",
        ".bmp",
        ".gif",
        ".exr",
        ".hdr"
    };

    private Vector2 _scrollPos;
    private string _lastMessage = "先在 Project 中选中要被替换的图片，再把新图片拖到下方区域。";
    private bool _enableReplace;

    [MenuItem("工具/UI/快速替换图片")]
    public static void ShowWindow()
    {
        GetWindow<QuickTextureReplaceWindow>("快速替换图片");
    }

    private void OnEnable()
    {
        _enableReplace = EditorPrefs.GetBool(EnableReplaceKey, false);
    }

    private void OnGUI()
    {
        var selectedTexturePaths = GetSelectedTexturePaths();

        DrawEnableState();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前选中的目标图片", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"数量：{selectedTexturePaths.Count}");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (selectedTexturePaths.Count <= 0)
            {
                EditorGUILayout.LabelField("未选中图片资源。");
            }
            else
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(140));
                foreach (var path in selectedTexturePaths)
                {
                    EditorGUILayout.LabelField(path);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        EditorGUILayout.Space();
        DrawDropArea(selectedTexturePaths);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(_lastMessage, MessageType.Info);
    }

    private void DrawEnableState()
    {
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = _enableReplace ? new Color(1f, 0.45f, 0.35f) : new Color(0.65f, 0.65f, 0.65f);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.backgroundColor = oldColor;

            var stateStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal =
                {
                    textColor = _enableReplace ? new Color(0.9f, 0.05f, 0.02f) : Color.gray
                }
            };

            EditorGUILayout.LabelField(
                _enableReplace ? "状态：替换功能已开启" : "状态：替换功能已关闭",
                stateStyle);

            EditorGUILayout.HelpBox(
                _enableReplace
                    ? "注意：现在拖入图片会立刻覆盖当前选中的图片资源，并保留原引用。"
                    : "当前不会执行拖拽替换，需要先勾选启用。",
                _enableReplace ? MessageType.Warning : MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _enableReplace = EditorGUILayout.ToggleLeft("启用拖拽替换", _enableReplace);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(EnableReplaceKey, _enableReplace);
                _lastMessage = _enableReplace
                    ? "已启用。现在拖入图片会替换当前选中的目标图片。"
                    : "已关闭。拖入图片不会执行替换。";
            }
        }
    }

    private void DrawDropArea(List<string> selectedTexturePaths)
    {
        var rect = GUILayoutUtility.GetRect(0, 90, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "拖入新图片到这里\n保留目标资源路径、名称和 .meta 引用");

        var currentEvent = Event.current;
        if (!rect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform)
        {
            return;
        }

        var sourcePaths = GetDraggedImagePaths();
        DragAndDrop.visualMode = _enableReplace && selectedTexturePaths.Count > 0 && sourcePaths.Count > 0
            ? DragAndDropVisualMode.Copy
            : DragAndDropVisualMode.Rejected;

        if (currentEvent.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            if (!_enableReplace)
            {
                _lastMessage = "开关未启用，已忽略本次拖拽。";
                Repaint();
                currentEvent.Use();
                return;
            }

            ReplaceTextures(selectedTexturePaths, sourcePaths);
        }

        currentEvent.Use();
    }

    private static List<string> GetSelectedTexturePaths()
    {
        return Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(IsImageAssetPath)
            .Distinct()
            .OrderBy(path => path)
            .ToList();
    }

    private static List<string> GetDraggedImagePaths()
    {
        var paths = new List<string>();

        foreach (var path in DragAndDrop.paths)
        {
            if (IsImageFilePath(path))
            {
                paths.Add(path);
            }
        }

        foreach (var obj in DragAndDrop.objectReferences)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (IsImageAssetPath(path) && !paths.Contains(path))
            {
                paths.Add(path);
            }
        }

        return paths.Distinct().OrderBy(path => path).ToList();
    }

    private static bool IsImageAssetPath(string path)
    {
        return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/") && IsImageFilePath(path);
    }

    private static bool IsImageFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return ImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
    }

    private void ReplaceTextures(List<string> targetPaths, List<string> sourcePaths)
    {
        if (targetPaths.Count <= 0)
        {
            _lastMessage = "没有选中的目标图片。请先在 Project 中选中要被替换的图片。";
            Repaint();
            return;
        }

        if (sourcePaths.Count <= 0)
        {
            _lastMessage = "拖入内容中没有识别到图片文件。";
            Repaint();
            return;
        }

        if (sourcePaths.Count != 1 && sourcePaths.Count != targetPaths.Count)
        {
            _lastMessage = $"数量不匹配：已选 {targetPaths.Count} 张目标图，拖入 {sourcePaths.Count} 张新图。请拖入 1 张，或拖入和目标数量相同的图片。";
            Repaint();
            return;
        }

        if (sourcePaths.Count == 1 && targetPaths.Count > 1)
        {
            var ok = EditorUtility.DisplayDialog(
                "快速替换图片",
                $"将用同一张图片替换当前选中的 {targetPaths.Count} 张图片，是否继续？",
                "继续",
                "取消");

            if (!ok)
            {
                _lastMessage = "已取消替换。";
                Repaint();
                return;
            }
        }

        var extensionError = GetExtensionError(targetPaths, sourcePaths);
        if (!string.IsNullOrEmpty(extensionError))
        {
            _lastMessage = extensionError;
            Repaint();
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();

            for (var i = 0; i < targetPaths.Count; i++)
            {
                var targetPath = targetPaths[i];
                var sourcePath = sourcePaths.Count == 1 ? sourcePaths[0] : sourcePaths[i];

                if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath), System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                FileUtil.ReplaceFile(sourcePath, targetPath);
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        _lastMessage = $"替换完成：{targetPaths.Count} 张图片已保留原路径、原名称和原 .meta。";
        Repaint();
    }

    private static string GetExtensionError(List<string> targetPaths, List<string> sourcePaths)
    {
        for (var i = 0; i < targetPaths.Count; i++)
        {
            var targetPath = targetPaths[i];
            var sourcePath = sourcePaths.Count == 1 ? sourcePaths[0] : sourcePaths[i];
            var targetExt = Path.GetExtension(targetPath);
            var sourceExt = Path.GetExtension(sourcePath);

            if (!string.Equals(targetExt, sourceExt, System.StringComparison.OrdinalIgnoreCase))
            {
                return $"扩展名不一致，已停止替换：{sourceExt} -> {targetExt}。请拖入和目标图片格式一致的文件，避免引用或导入异常。";
            }
        }

        return string.Empty;
    }
}
#endif
