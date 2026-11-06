#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class UIPageConfigWindow : EditorWindow
{
    private class Entry
    {
        public GameObject prefab;
        public UIPageBase page;
        public string path;
    }

    private readonly List<Entry> _entries = new();
    private Vector2 _scroll;

    [MenuItem("工具/UI/页面配置")]
    public static void Open()
    {
        var window = GetWindow<UIPageConfigWindow>("UI页面配置");
        window.minSize = new Vector2(900, 500);
        window.Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        _entries.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var page = prefab.GetComponent<UIPageBase>();
            if (page == null) continue;

            _entries.Add(new Entry
            {
                prefab = prefab,
                page = page,
                path = path
            });
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新", GUILayout.Width(80)))
        {
            Refresh();
        }
        if (GUILayout.Button("保存全部", GUILayout.Width(100)))
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        if (GUILayout.Button("全部加入Addressable", GUILayout.Width(150)))
        {
            AddAllUiToAddressables();
        }
        EditorGUILayout.LabelField($"共 {_entries.Count} 个 UI Prefab");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        DrawHeader();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var e in _entries)
        {
            DrawEntry(e);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("Prefab", GUILayout.Width(180));
        GUILayout.Label("PageId", GUILayout.Width(200));
        GUILayout.Label("Layer", GUILayout.Width(120));
        GUILayout.Label("Multi", GUILayout.Width(50));
        GUILayout.Label("Cache", GUILayout.Width(50));
        GUILayout.Label("NativeClose", GUILayout.Width(80));
        GUILayout.Label("ZIndex", GUILayout.Width(70));
        GUILayout.Label("Path");
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawEntry(Entry e)
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.ObjectField(e.prefab, typeof(GameObject), false, GUILayout.Width(180));

        EditorGUI.BeginChangeCheck();
        string pageId = EditorGUILayout.TextField(e.page.RawPageId, GUILayout.Width(200));
        var layer = (E_UILayer)EditorGUILayout.EnumPopup(e.page.Layer, GUILayout.Width(120));
        bool multiPages = EditorGUILayout.Toggle(e.page.MultiPages, GUILayout.Width(50));
        bool cache = EditorGUILayout.Toggle(e.page.Cache, GUILayout.Width(50));
        bool nativeClose = EditorGUILayout.Toggle(e.page.NativeClose, GUILayout.Width(80));
        int zIndex = EditorGUILayout.IntField(e.page.ZIndex, GUILayout.Width(70));

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(e.page, "Modify UI Page Config");
            e.page.RawPageId = pageId;
            e.page.Layer = layer;
            e.page.MultiPages = multiPages;
            e.page.Cache = cache;
            e.page.NativeClose = nativeClose;
            e.page.ZIndex = zIndex;
            EditorUtility.SetDirty(e.page);
            EditorUtility.SetDirty(e.prefab);
        }

        EditorGUILayout.LabelField(e.path);
        EditorGUILayout.EndHorizontal();
    }

    private void AddAllUiToAddressables()
    {
        var settings = GetAddressableSettings();
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Addressable 配置缺失", "未找到 AddressableAssetSettings，无法添加 UI。", "确定");
            return;
        }

        object group = FindOrCreateAddressablesGroup(settings);
        if (group == null)
        {
            EditorUtility.DisplayDialog("Addressable 配置缺失", "无法创建或找到 UIPanel 分组。", "确定");
            return;
        }

        int addedCount = 0;
        int skippedCount = 0;

        foreach (var entry in _entries)
        {
            if (entry.prefab == null || entry.page == null || string.IsNullOrEmpty(entry.path))
            {
                skippedCount++;
                continue;
            }

            string pageId = entry.page.PageId.Value;
            if (string.IsNullOrEmpty(pageId))
            {
                pageId = entry.prefab.name;
            }

            string guid = AssetDatabase.AssetPathToGUID(entry.path);
            if (string.IsNullOrEmpty(guid))
            {
                skippedCount++;
                continue;
            }

            object addressableEntry = CreateOrMoveAddressableEntry(settings, guid, group);
            if (addressableEntry == null)
            {
                skippedCount++;
                continue;
            }

            SetAddressableEntryAddress(addressableEntry, $"UIPanel/{pageId}");
            addedCount++;
        }

        if (settings is UnityEngine.Object settingsObject)
        {
            EditorUtility.SetDirty(settingsObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("处理完成",
            $"已添加/更新 {addedCount} 个 UI 到 Addressable。\n跳过 {skippedCount} 个。", "确定");
    }

    private static object GetAddressableSettings()
    {
        var defaultObjectType = Type.GetType("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
        return defaultObjectType?.GetProperty("Settings", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
    }

    private static object FindOrCreateAddressablesGroup(object settings)
    {
        var settingsType = settings.GetType();
        var findGroup = settingsType.GetMethod("FindGroup", new[] { typeof(string) });
        object group = findGroup?.Invoke(settings, new object[] { "UIPanel" });
        if (group != null)
        {
            return group;
        }

        MethodInfo createGroup = null;
        foreach (var method in settingsType.GetMethods())
        {
            if (method.Name != "CreateGroup") continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 6 && parameters[0].ParameterType == typeof(string))
            {
                createGroup = method;
                break;
            }
        }

        if (createGroup == null)
        {
            return null;
        }

        var bundledSchema = Type.GetType("UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema, Unity.Addressables.Editor");
        var contentSchema = Type.GetType("UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema, Unity.Addressables.Editor");
        var schemaTypes = bundledSchema != null && contentSchema != null
            ? new[] { bundledSchema, contentSchema }
            : Type.EmptyTypes;

        return createGroup.Invoke(settings, new object[]
        {
            "UIPanel",
            false,
            false,
            false,
            null,
            schemaTypes
        });
    }

    private static object CreateOrMoveAddressableEntry(object settings, string guid, object group)
    {
        var settingsType = settings.GetType();
        MethodInfo createOrMoveEntry = null;
        foreach (var method in settingsType.GetMethods())
        {
            if (method.Name != "CreateOrMoveEntry") continue;
            var parameters = method.GetParameters();
            if ((parameters.Length == 2 || parameters.Length == 4) && parameters[0].ParameterType == typeof(string))
            {
                createOrMoveEntry = method;
                break;
            }
        }

        if (createOrMoveEntry == null)
        {
            return null;
        }

        var args = createOrMoveEntry.GetParameters().Length == 4
            ? new[] { guid, group, false, false }
            : new[] { guid, group };
        return createOrMoveEntry.Invoke(settings, args);
    }

    private static void SetAddressableEntryAddress(object entry, string address)
    {
        if (entry == null)
        {
            return;
        }

        var entryType = entry.GetType();
        var addressProperty = entryType.GetProperty("address", BindingFlags.Public | BindingFlags.Instance);
        if (addressProperty != null && addressProperty.CanWrite)
        {
            addressProperty.SetValue(entry, address);
            return;
        }

        var addressField = entryType.GetField("address", BindingFlags.Public | BindingFlags.Instance);
        addressField?.SetValue(entry, address);
    }
}
#endif
