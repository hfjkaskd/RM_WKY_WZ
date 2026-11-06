using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Whitebox.Foundation;

namespace Whitebox.Authoring
{
    // One-time authoring of saved prefabs/configuration. Runtime never builds UI hierarchy.
    public static class BuildFoundation
    {
        public static void Run()
        {
            Directory.CreateDirectory("Assets/Foundation/Prefabs");
            Directory.CreateDirectory("Assets/Foundation/Settings");
            Directory.CreateDirectory("Assets/Foundation/Scenes");
            AssetDatabase.Refresh();
            var settings = ScriptableObject.CreateInstance<FoundationSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Foundation/Settings/FoundationSettings.asset");
            var scenario = ScriptableObject.CreateInstance<AdScenario>();
            AssetDatabase.CreateAsset(scenario, "Assets/Foundation/Settings/LocalAdScenario.asset");
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            AssetDatabase.CreateAsset(panel, "Assets/Foundation/Settings/PanelSettings.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.22f); camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0, 0, -10); cameraObject.tag = "MainCamera";
            var root = new GameObject("Foundation", typeof(UIDocument), typeof(FoundationController));
            var preview = new GameObject("WorldPrefabPreview"); preview.transform.SetParent(root.transform, false);
            var document = root.GetComponent<UIDocument>(); document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Foundation/UI/RecoveryView.uxml");
            var serialized = new SerializedObject(root.GetComponent<FoundationController>());
            serialized.FindProperty("settings").objectReferenceValue = settings;
            serialized.FindProperty("adScenario").objectReferenceValue = scenario;
            serialized.FindProperty("previewRoot").objectReferenceValue = preview.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Foundation/Prefabs/Foundation.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, "Assets/Foundation/Scenes/RecoveryPreview.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Foundation/Scenes/RecoveryPreview.unity", true) };
            AssetDatabase.SaveAssets();
            Verify();
        }

        public static void Verify()
        {
            var scenario = ScriptableObject.CreateInstance<AdScenario>();
            scenario.Configure(AdOutcome.Completed, 3);
            var facade = new AdFacade(new LocalAdProvider(scenario), scenario.RewardAmount);
            if (!facade.Show("a") || facade.EarnedRewards != 3 || facade.Show("a") || facade.EarnedRewards != 3)
                throw new Exception("Exactly-once ad reward check failed.");
            scenario.Configure(AdOutcome.Cancelled, 3);
            facade.Show("b"); if (facade.EarnedRewards != 3) throw new Exception("Cancelled ad rewarded unexpectedly.");
            scenario.Configure(AdOutcome.Unavailable, 3);
            facade.Show("c"); if (facade.EarnedRewards != 3) throw new Exception("Unavailable ad rewarded unexpectedly.");
            var hostile = new AdFacade(new DoubleCallbackProvider(), 2);
            hostile.Show("x"); if (hostile.EarnedRewards != 2) throw new Exception("Duplicate provider callback duplicated reward.");
            var level = OriginalLevelReader.Load("Levels/level_1");
            if (level.Move != 30 || level.Grid.Width != 9 || level.Grid.Height != 8 || level.Grid.Items.Length != 72)
                throw new Exception("Original first level data verification failed.");
            if (level.Grid.Cells == null || level.Grid.Cells.Length != 72 || level.Goals[0].Goal != 11 || level.Goals[0].Count != 36)
                throw new Exception("Full nested level structure verification failed.");
            UnityEngine.Object.DestroyImmediate(scenario);
            File.WriteAllText("foundation_verification.json", "{\"passed\":true,\"ad_success\":true,\"ad_cancel\":true,\"ad_unavailable\":true,\"idempotency\":true,\"duplicate_callback\":true,\"original_level_1\":true,\"reconstructed_gameplay\":false}");
            Debug.Log("FOUNDATION_VERIFICATION_PASSED");
        }

        public static void UpgradePreview()
        {
            // Authoring only: layer-one box sprite comes from the original BoxItemAssets goal configuration.
            Directory.CreateDirectory("Assets/Resources/Foundation");
            var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Recovered/GameObject/BoxItemPrefab.prefab");
            if (original == null) throw new Exception("Original box prefab did not import.");
            var spritePath = AssetDatabase.GUIDToAssetPath("b405aed277332d244a95b77467fa27d7");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) throw new Exception("Original layer-one box sprite did not import.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(original);
            bool assigned = false;
            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name == "Base") { renderer.sprite = sprite; assigned = true; }
            if (!assigned) throw new Exception("Original box Base renderer was not found.");
            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Resources/Foundation/BoxPreview.prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            var settings = AssetDatabase.LoadAssetAtPath<FoundationSettings>("Assets/Foundation/Settings/FoundationSettings.asset");
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("visualPrefabResourcePath").stringValue = "Foundation/BoxPreview";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Verify();
            var preview = Resources.Load<GameObject>("Foundation/BoxPreview");
            if (preview == null || preview.GetComponentsInChildren<SpriteRenderer>(true).Length < 2)
                throw new Exception("World prefab visual verification failed.");
            File.WriteAllText("visual_verification.json", "{\"passed\":true,\"world_prefab\":true,\"original_box_sprite_guid\":\"b405aed277332d244a95b77467fa27d7\",\"full_rendering_parity\":false}");
            Debug.Log("FOUNDATION_VISUAL_VERIFICATION_PASSED");
        }

        private sealed class DoubleCallbackProvider : IRewardedAdProvider
        {
            public void Show(string id, Action<AdOutcome> callback) { callback(AdOutcome.Completed); callback(AdOutcome.Completed); }
        }
    }
}
