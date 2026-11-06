#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class EditorUtils
{

    [MenuItem("工具/编辑器/复制层级路径")]
    static void CopyHierarchyPath()
    {
        // 获取当前选择的GameObject
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject != null)
        {
            // 构建路径字符串
            string path = GetHierarchyPath(selectedObject);

            // 将路径复制到剪贴板
            EditorGUIUtility.systemCopyBuffer = path;
        }
    }

    static string GetHierarchyPath(GameObject obj)
    {
        // 从父物体开始构建路径
        string path = obj.name;

        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }

        return path;
    }


    public static string GetCurPath()
    {
        string[] guids = Selection.assetGUIDs;
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return path;
        }
        else
        {
            // Debug.Log("Nothing selected.");
        }

        return "";
    }

    public static List<T> GetSelectedObjects<T>(string filter) where T : Object
    {
        string[] guids = Selection.assetGUIDs;
        var ret = new List<T>();
        if (guids.Length <= 0) return ret;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            //文件夹
            if (obj is DefaultAsset)
            {
                string[] guids2 = AssetDatabase.FindAssets($"t:{filter}", new string[] {path});
                foreach (var guid2 in guids2)
                {
                    string path2 = AssetDatabase.GUIDToAssetPath(guid2);
                    var t = AssetDatabase.LoadAssetAtPath<T>(path2);
                    ret.Add(t);
                }
            }
            else if (obj is T t)
            {
                ret.Add(t);
            }
        }
        return ret;
    }

    public static T FindObject<T>() where T : Object
    {
        var list = FindObjects<T>();
        if (list.Count == 0) return null;
        return list[0];
    }

    public static T FindObjectWithName<T>(string name) where T : Object
    {
        var list = FindObjects<T>();
        if (list.Count == 0) return null;
        foreach (var v in list)
        {
            Debug.LogError(v.name);
            if (v.name == name)
            {
                return v;
            }
        }

        return null;
    }

    public static Object FindObjectWithName(string type, string name)
    {
        var list = FindObjects(type);
        if (list.Count == 0) return null;
        foreach (var v in list)
        {
            if (v.name == name)
            {
                return v;
            }
        }

        return null;
    }

    public static List<Object> FindObjects(string type)
    {
        string[] guids = AssetDatabase.FindAssets($"t:{type}");

        // 创建一个列表来存储找到的 A 类型的 ScriptableObject
        List<Object> typeAObjects = new List<Object>();

        foreach (string guid in guids)
        {
            // 通过 GUID 获取资源的路径
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 通过路径加载资源并转换为 A 类型
            var scriptableObjectA = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            if (scriptableObjectA != null)
            {
                // 如果加载成功，将其添加到列表中
                typeAObjects.Add(scriptableObjectA);
            }
        }

        return typeAObjects;
    }

    public static List<T> FindObjects<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).ToString()}");

        // 创建一个列表来存储找到的 A 类型的 ScriptableObject
        List<T> typeAObjects = new List<T>();

        foreach (string guid in guids)
        {
            // 通过 GUID 获取资源的路径
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // 通过路径加载资源并转换为 A 类型
            T scriptableObjectA = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (scriptableObjectA != null)
            {
                // 如果加载成功，将其添加到列表中
                typeAObjects.Add(scriptableObjectA);
            }
        }

        return typeAObjects;
    }

    public static T GetObjBySamePathScript<T>(string scriptName, string objName) where T : Object
    {
        string[] assetIds = AssetDatabase.FindAssets($"{scriptName} t:Script");
        if (assetIds.Length == 0)
        {
            Debug.LogError($"{objName}没有找到");
        }
        string editorPath = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(assetIds[0]));
        var ret = AssetDatabase.LoadAssetAtPath<T>(Path.Combine(editorPath, objName));
        if (ret == null)
        {
            Debug.LogError($"{objName}没有找到");
        }
        return ret;
    }

}
#endif
