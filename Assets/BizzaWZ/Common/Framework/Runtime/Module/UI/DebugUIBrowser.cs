#if DEBUG_MODE
using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Self-contained GM entry, available in editor and on devices only with DEBUG_MODE.
public sealed class DebugUIBrowser : MonoBehaviour
{
    private static DebugUIBrowser instance;

    private string[] pageIds;
    private string filter = string.Empty;
    private string customPageId = string.Empty;
    private Vector2 scrollPosition;
    private bool expanded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;

        var gameObject = new GameObject("[GM UI Browser]");
        instance = gameObject.AddComponent<DebugUIBrowser>();
        DontDestroyOnLoad(gameObject);
    }

    private void Awake()
    {
        pageIds = CollectPageIds();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10)) expanded = !expanded;
    }

    private void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.width / 720f);
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        var oldMatrix = GUI.matrix;
        int oldDepth = GUI.depth;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUI.depth = -1000;

        if (expanded)
        {
            float panelWidth = Mathf.Min(400f, width - 24f);
            float panelHeight = Mathf.Min(610f, height - 24f);
            GUI.Window(120092, new Rect(width - panelWidth - 12f, 12f, panelWidth, panelHeight),
                DrawWindow, "GM · UI");
        }
        else if (GUI.Button(new Rect(width - 94f, 12f, 82f, 42f), "GM UI"))
        {
            expanded = true;
        }

        GUI.matrix = oldMatrix;
        GUI.depth = oldDepth;
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label("选择页面预览（F10 开关）");
        GUILayout.Label("筛选页面");
        GUILayout.BeginHorizontal();
        filter = GUILayout.TextField(filter);
        if (GUILayout.Button("清空", GUILayout.Width(52f))) filter = string.Empty;
        GUILayout.EndHorizontal();

        GUILayout.Label("输入页面 ID");
        GUILayout.BeginHorizontal();
        customPageId = GUILayout.TextField(customPageId);
        if (GUILayout.Button("打开 ID", GUILayout.Width(75f))) Open(customPageId);
        GUILayout.EndHorizontal();

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        foreach (string pageId in pageIds)
        {
            if (!string.IsNullOrEmpty(filter) &&
                pageId.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            if (GUILayout.Button(pageId, GUILayout.Height(34f))) Open(pageId);
        }
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("关闭当前预览", GUILayout.Height(38f)))
        {
            if (UIModule.Instance != null) UIModule.Instance.DebugClosePage();
        }
        if (GUILayout.Button("收起", GUILayout.Width(70f), GUILayout.Height(38f))) expanded = false;
        GUILayout.EndHorizontal();
    }

    private void Open(string pageId)
    {
        pageId = pageId?.Trim();
        if (string.IsNullOrEmpty(pageId)) return;

        var ui = UIModule.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[GM UI] UI 模块尚未初始化。");
            return;
        }

        ui.DebugOpenPage(new PageId(pageId)).Forget();
        expanded = false;
    }

    private static string[] CollectPageIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in typeof(UIPageIds).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(PageId)) continue;
            var pageId = (PageId)field.GetValue(null);
            if (!pageId.IsEmpty) ids.Add(pageId.Value);
        }

        var result = new string[ids.Count];
        ids.CopyTo(result);
        Array.Sort(result, StringComparer.Ordinal);
        return result;
    }
}
#endif
