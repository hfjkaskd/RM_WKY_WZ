using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class UIPropEntryMissingScriptRepair
{
    private const string SessionKey = "BizzaGame.UIPropEntryMissingScriptRepair.AutoRan";
    private const string ScriptPath = "Assets/BizzaWZ/Common/UI/GamePanel/UIPropEntry.cs";
    private const string PrefabPath = "Assets/BizzaWZ/Common/UI/GamePanel/UIPropEntry.prefab";

    static UIPropEntryMissingScriptRepair()
    {
        EditorApplication.delayCall += AutoRepairOnce;
    }

    [MenuItem("工具/UI/修复道具入口丢失脚本")]
    public static void RepairFromMenu()
    {
        Repair(true);
    }

    private static void AutoRepairOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        Repair(false);
    }

    private static void Repair(bool forceLog)
    {
        AssetDatabase.ImportAsset(ScriptPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(ScriptPath);
        Type scriptType = script != null ? script.GetClass() : null;
        if (scriptType != typeof(UIPropEntry))
        {
            Debug.LogError($"UIPropEntry script import failed. path:{ScriptPath} resolvedType:{scriptType}");
            return;
        }

        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"UIPropEntry prefab not found. path:{PrefabPath}");
            return;
        }

        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
        UIPropEntry entry = prefab.GetComponent<UIPropEntry>();
        if (entry == null || missingCount > 0)
        {
            Debug.LogError($"UIPropEntry prefab still has missing script. path:{PrefabPath} component:{entry != null} missingCount:{missingCount}");
            return;
        }

        if (forceLog)
        {
            AssetDatabase.ForceReserializeAssets(new[] { PrefabPath });
            AssetDatabase.SaveAssets();
            Debug.Log($"UIPropEntry missing script repaired. path:{PrefabPath}");
        }
    }
}
