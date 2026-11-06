using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Explicit one-time prefab authoring. No runtime hierarchy repair.
    public static class TutorialHandAnimatorAuthoring
    {
        public static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Resources/Recovered/AnimatorController/tutorial_hand_Controller.controller");
            if (controller == null) throw new InvalidOperationException("The recovered hand controller is missing.");
            var names = new[] { "CoreLevelOneOpeningGuide", "CoreLevelTwentyEightGuide", "CoreLightballTapGuide",
                "CoreDynamicComboGuide", "CoreLevelEightHammerGuide" };
            int authored = 0;
            foreach (string name in names)
            {
                string path = "Assets/Resources/Foundation/Prefabs/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var mesh in root.GetComponentsInChildren<OriginalBakedMeshAnimation>(true))
                    {
                        var data = new SerializedObject(mesh);
                        if (data.FindProperty("texturePath").stringValue != "Recovered/Texture2D/tutorial_hand") continue;
                        var part = (MeshFilter)data.FindProperty("parts").GetArrayElementAtIndex(0).objectReferenceValue;
                        var animator = part.GetComponent<Animator>();
                        if (animator == null) animator = part.gameObject.AddComponent<Animator>();
                        animator.runtimeAnimatorController = controller;
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        animator.updateMode = AnimatorUpdateMode.Normal;
                        animator.applyRootMotion = false;
                        string clipPath = data.FindProperty("dataPath").stringValue;
                        data.FindProperty("playbackAnimator").objectReferenceValue = animator;
                        if (!string.IsNullOrEmpty(clipPath))
                            data.FindProperty("playbackState").stringValue = clipPath.Substring(clipPath.LastIndexOf('/') + 1);
                        data.FindProperty("dataPath").stringValue = string.Empty;
                        data.FindProperty("rigPath").stringValue = "Foundation/Tutorials/TutorialHandRig";
                        data.ApplyModifiedPropertiesWithoutUndo();
                        authored++;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            if (authored != 6) throw new InvalidOperationException("Expected the six authored tutorial hands.");
            AssetDatabase.SaveAssets();
            File.WriteAllText("tutorial_hand_authoring.json", "{\"authored_hands\":6}\n");
            EditorApplication.Exit(0);
        }
    }
}
