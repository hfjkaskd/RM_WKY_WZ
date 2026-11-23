// Build-copy-only Editor preflight. This file is not part of the game runtime.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class GemwinkFilteredRelease
{
    [Serializable]
    private sealed class CheckResult
    {
        public bool passed;
        public int levels;
        public int prefabs;
        public int dependencies;
    }

    public static void BuildAab()
    {
        var roots = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null)
                throw new BuildFailedException("Missing build scene: " + scene.path);
            roots.Add(scene.path);
        }

        string[] prefabs = {
            "Assets/Resources/Foundation/Prefabs/CoreLevelQuitDialog.prefab",
            "Assets/Resources/Foundation/Prefabs/CoreWinLogo.prefab",
            "Assets/BizzaWZ/Final/Real/UI/GetRewardPanel/GetRewardPanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/NewbieGiftPage/NewbieGiftPage.prefab",
            "Assets/BizzaWZ/Final/Real/UI/DailyMissionPanel/DailyMissionPanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/ServicePanel/ServicePanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/ServiceSelectPanel/ServiceSelectPanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/FAQPanel/FAQPanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/WithdrawFillPanel/WithdrawFillPanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/UIWithdrawalConfirmPanel/UIWithdrawalConfirmPanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/UIWithdrawalPendingPanel/UIWithdrawalPendingPanel.prefab",
            "Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistory.prefab",
            "Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistoryItem.prefab"
        };
        foreach (string path in prefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new BuildFailedException("Missing current UI prefab: " + path);
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    throw new BuildFailedException("Missing UI script: " + path + "/" + child.name);
            roots.Add(path);
        }
        for (int level = 1; level <= 100; level++)
        {
            string path = "Assets/Resources/Levels/level_" + level + ".json";
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (text == null || string.IsNullOrWhiteSpace(text.text))
                throw new BuildFailedException("Missing or empty level: " + path);
            roots.Add(path);
        }

        string[] dependencies = AssetDatabase.GetDependencies(roots.ToArray(), true);
        foreach (string path in dependencies)
            if (path.StartsWith("Assets/", StringComparison.Ordinal) && !File.Exists(path))
                throw new BuildFailedException("Missing build dependency: " + path);
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/resource-preflight.json", JsonUtility.ToJson(new CheckResult {
            passed = true, levels = 100, prefabs = prefabs.Length, dependencies = dependencies.Length
        }, true));
        Debug.Log("[GemwinkFilteredRelease] Resource preflight passed: 100 levels and current UI prefabs.");
        AutoBuild_Android.BuildAndroidAabBatch();
    }
}
