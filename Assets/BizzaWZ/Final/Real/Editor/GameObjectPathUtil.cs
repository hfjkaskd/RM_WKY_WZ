#if BIZZA_REAL_WITHDRAW
using UnityEngine;
using UnityEditor;
using System.Text;

 
public static class CopyGameObjectPath
{
    [MenuItem("工具/编辑器/复制选中对象完整路径", false, 20)]
    private static void CopyPath()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogWarning("未选中任何 GameObject");
            return;
        }

        string path = GetPath(go.transform);

        EditorGUIUtility.systemCopyBuffer = path;
        Debug.Log($"已复制路径：{path}");
    }

    // 只有选中 GameObject 时才可点击
    [MenuItem("工具/编辑器/复制选中对象完整路径", true)]
    private static bool ValidateCopyPath()
    {
        return Selection.activeGameObject != null;
    }

    private static string GetPath(Transform transform)
    {
        StringBuilder sb = new StringBuilder(transform.name);
        while (transform.parent != null)
        {
            transform = transform.parent;
            sb.Insert(0, transform.name + "/");
        }
        return sb.ToString();
    }
}
#endif
