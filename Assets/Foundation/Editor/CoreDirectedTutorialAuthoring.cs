using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit import of recovered opening-guide layouts.
    public static class CoreDirectedTutorialAuthoring
    {
        private const string Source = "Assets/Resources/Recovered/GameObject/";
        private const string Prefabs = "Assets/Resources/Foundation/Prefabs/";
        private const string HandData = "Foundation/Tutorials/";
        private static readonly Vector2Int[] LevelOneBoxMaskOffsets =
        {
            new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(5, 3)
        };
        private static readonly Vector2Int[] OwlPowerUps =
        {
            new Vector2Int(0, 9), new Vector2Int(1, 0), new Vector2Int(4, 7),
            new Vector2Int(7, 0), new Vector2Int(8, 9)
        };

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the independent batch project.");
            var first = new Vector2Int(5, 5);
            var second = new Vector2Int(5, 4);
            // ShowStep1 0x223bd3c creates seven masks and raises all 36 boxes separately.
            var openingMask = new HashSet<Vector2Int> { first, second, new Vector2Int(3, 4), new Vector2Int(4, 4) };
            openingMask.UnionWith(LevelOneBoxMaskOffsets);
            var raisedBoxes = new Vector2Int[36];
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 4; y++)
                    raisedBoxes[x * 4 + y] = new Vector2Int(x, y);
            var lightball = new Vector2Int(4, 5);
            var neighbor = new Vector2Int(3, 5);
            var grass = new List<Vector2Int>(49);
            for (int x = 1; x <= 7; x++)
                for (int y = 1; y <= 7; y++)
                    grass.Add(new Vector2Int(x, y));
            string opening = Author("CoreLevelOneOpeningGuide", openingMask, first, false);
            string combo = Author("CoreLevelTwentyEightGuide", new HashSet<Vector2Int> { lightball, neighbor }, lightball, true);
            string grassGuide = AuthorOptional("CoreLevelFourGrassGuide", new HashSet<Vector2Int>(grass));
            string owlGuide = AuthorOptional("CoreLevelSixtyOneOwlGuide", null);
            AuthorOptional("CoreLightballTapGuide", new HashSet<Vector2Int> { Vector2Int.zero }, true);
            AuthorDynamicCombo();
            var settings = AssetDatabase.LoadAssetAtPath<CoreFlowSettings>("Assets/Foundation/Settings/CoreFlowSettings.asset");
            settings.directedTutorials = new[]
            {
                new OriginalDirectedTutorialProfile
                {
                    level = 1, stableBoardSeconds = .1f, prefabPath = opening,
                    inputCells = new[] { first, second }, raisedBoxes = raisedBoxes
                },
                new OriginalDirectedTutorialProfile
                {
                    level = 4, prefabPath = grassGuide, inputCells = grass.ToArray(), raisedBoxes = Array.Empty<Vector2Int>()
                },
                new OriginalDirectedTutorialProfile
                {
                    level = 20, prefabPath = "Foundation/Prefabs/CoreLevelTwentyBonusGuide",
                    inputCells = new[] { new Vector2Int(4, 4), new Vector2Int(4, 5) },
                    raisedBoxes = Array.Empty<Vector2Int>(), highlightWholeBoard = true
                },
                new OriginalDirectedTutorialProfile
                {
                    level = 28, prefabPath = combo, inputCells = new[] { lightball, neighbor },
                    delay = 4f, lightballTaps = 3, showWhileBoardBusy = true, raisedBoxes = Array.Empty<Vector2Int>()
                },
                new OriginalDirectedTutorialProfile
                {
                    level = 61, prefabPath = owlGuide, inputCells = OwlPowerUps,
                    raisedBoxes = Array.Empty<Vector2Int>(), highlightWholeBoard = true
                }
            };
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static string Author(string name, HashSet<Vector2Int> mask, Vector2Int handCell, bool combo)
        {
            string path = Prefabs + name + ".prefab";
            if (File.Exists(path)) return "Foundation/Prefabs/" + name;
            var root = PrefabUtility.LoadPrefabContents(Source + "TutorialBlack.prefab");
            try
            {
                root.name = name;
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                int layer = SortingLayer.NameToID("Ui");
                var shade = root.transform.Find("Black").GetComponent<SpriteRenderer>();
                shade.sortingLayerID = layer;
                shade.sortingOrder = 500;
                shade.color = new Color(0f, 0f, 0f, .7f);
                shade.sharedMaterial = LoadMaskMaterial();
                Object.DestroyImmediate(root.transform.Find("Mask").gameObject);
                var (camera, cells) = AuthorMask(root, mask, !combo);

                var guide = new SerializedObject(root.AddComponent<OriginalDirectedTutorialView>());
                guide.FindProperty("shade").objectReferenceValue = shade;
                guide.FindProperty("maskCamera").objectReferenceValue = camera;
                guide.FindProperty("maskCells").objectReferenceValue = cells;
                AuthorHand(root, guide, combo ? "swipe_left" : "swipe_down",
                    combo ? new Vector2(.2f, 0f) : Vector2.zero);
                guide.FindProperty("handCell").vector2IntValue = handCell;
                guide.FindProperty("fadeSeconds").floatValue = .2f;
                guide.FindProperty("highlightOffset").intValue = 10;
                if (combo)
                {
                    guide.FindProperty("handAtLandscapeGridTop").boolValue = true;
                    guide.FindProperty("landscapeHandOffset").vector2Value = new Vector2(.5f, -4.5f);
                }
                else
                {
                    AuthorDialog(root, guide, "Make a match next to a BOX to break it!");
                    guide.FindProperty("tallDialogCell").vector2IntValue = new Vector2Int(4, 6);
                    guide.FindProperty("shortDialogCell").vector2IntValue = new Vector2Int(4, 1);
                    guide.FindProperty("landscapeDialogCell").vector2IntValue = new Vector2Int(4, 0);
                    guide.FindProperty("tallDialogOffset").floatValue = 1.7f;
                }
                guide.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return "Foundation/Prefabs/" + name;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void AuthorHand(GameObject root, SerializedObject guide, string clip, Vector2 offset, bool vertical = false)
        {
            var hand = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "TutorialHand.prefab"),
                root.transform, false);
            hand.name = vertical ? "Vertical Tutorial Hand" : "Tutorial Hand";
            hand.transform.localPosition = Vector3.zero;
            var handSorting = hand.GetComponent<SortingGroup>();
            handSorting.sortingLayerID = SortingLayer.NameToID("Ui");
            handSorting.sortingOrder = 510;
            var handPart = hand.GetComponentInChildren<MeshFilter>();
            handPart.transform.localPosition = offset;
            var animator = handPart.GetComponent<Animator>();
            var animation = hand.AddComponent<OriginalBakedMeshAnimation>();
            var handData = new SerializedObject(animation);
            handData.FindProperty("rigPath").stringValue = HandData + "TutorialHandRig";
            handData.FindProperty("playbackAnimator").objectReferenceValue = animator;
            handData.FindProperty("playbackState").stringValue = clip;
            handData.FindProperty("texturePath").stringValue = "Recovered/Texture2D/tutorial_hand";
            handData.FindProperty("materialTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Foundation/Settings/OriginalWinLogoMaterial.mat");
            var parts = handData.FindProperty("parts");
            parts.arraySize = 1;
            parts.GetArrayElementAtIndex(0).objectReferenceValue = handPart;
            handData.ApplyModifiedPropertiesWithoutUndo();
            guide.FindProperty(vertical ? "verticalHand" : "hand").objectReferenceValue = hand.transform;
            guide.FindProperty(vertical ? "verticalHandAnimation" : "handAnimation").objectReferenceValue = animation;
        }

        public static void AuthorDynamicCombo() => AuthorOptional("CoreDynamicComboGuide",
            new HashSet<Vector2Int> { Vector2Int.zero, Vector2Int.left }, true, true);

        private static string AuthorOptional(string name, HashSet<Vector2Int> mask, bool focused = false, bool combo = false)
        {
            string path = Prefabs + name + ".prefab";
            if (File.Exists(path)) return "Foundation/Prefabs/" + name;
            var root = PrefabUtility.LoadPrefabContents(Source + "TutorialBlack.prefab");
            try
            {
                root.name = name;
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var black = root.transform.Find("Black").GetComponent<SpriteRenderer>();
                var sprite = black.sprite;
                Object.DestroyImmediate(black.gameObject);
                Object.DestroyImmediate(root.transform.Find("Mask").gameObject);

                var shade = new GameObject("Tutorial Dim", typeof(RectTransform));
                shade.layer = 5;
                shade.transform.SetParent(root.transform, false);
                var canvas = shade.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingLayerID = SortingLayer.NameToID("Ui");
                canvas.sortingOrder = 500;
                var image = focused ? shade.AddComponent<OriginalTutorialDismissImage>() : shade.AddComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(0f, 0f, 0f, .7f);
                shade.AddComponent<GraphicRaycaster>();
                var button = shade.AddComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;

                var guide = new SerializedObject(root.AddComponent<OriginalDirectedTutorialView>());
                guide.FindProperty("dismissCanvas").objectReferenceValue = canvas;
                guide.FindProperty("dismissGraphic").objectReferenceValue = image;
                guide.FindProperty("dismissButton").objectReferenceValue = button;
                guide.FindProperty("minimumDismissSeconds").floatValue = focused ? 3f : .5f;
                guide.FindProperty("fadeSeconds").floatValue = .2f;
                guide.FindProperty("highlightOffset").intValue = 10;
                if (mask != null)
                {
                    image.material = LoadMaskMaterial();
                    var (camera, cells) = AuthorMask(root, mask, false);
                    guide.FindProperty("maskCamera").objectReferenceValue = camera;
                    guide.FindProperty("maskCells").objectReferenceValue = cells;
                    if (focused)
                    {
                        if (combo)
                        {
                            var text = AuthorDialog(root, guide, "You can swap two power-ups to make a more powerful explosion!");
                            // DynamicComboTutorial.ShowStep1 0x22443f0, SetSize(6.6, 3.8).
                            text.rectTransform.sizeDelta = new Vector2(6.6f, text.rectTransform.sizeDelta.y);
                            foreach (var side in text.transform.parent.GetComponentsInChildren<SpriteRenderer>())
                                side.size = new Vector2(3.8f, side.size.y);
                            AuthorHand(root, guide, "swipe_left", new Vector2(.2f, 0f));
                            AuthorHand(root, guide, "swipe_down", Vector2.zero, true);
                            var (_, verticalCells) = AuthorMask(root,
                                new HashSet<Vector2Int> { Vector2Int.zero, Vector2Int.down }, false);
                            verticalCells.name = "Vertical cell masks";
                            guide.FindProperty("verticalMaskCells").objectReferenceValue = verticalCells;
                        }
                        else
                        {
                            AuthorDialog(root, guide, "You can tap on power-ups to explode them!");
                            AuthorHand(root, guide, "tap_animation_2", new Vector2(.03f, -.08f));
                        }
                        guide.FindProperty("focusedDialogThreshold").floatValue = 2f;
                        guide.FindProperty("focusedDialogAbove").floatValue = 2f;
                        guide.FindProperty("focusedDialogBelow").floatValue = -3.5f;
                    }
                    else
                    {
                        AuthorDialog(root, guide, "Make a match on the GRASS to remove it!");
                        guide.FindProperty("portraitAnchor").enumValueIndex = 1; // GridTop
                        guide.FindProperty("landscapeAnchor").enumValueIndex = 1;
                        guide.FindProperty("portraitDialogOffset").vector2Value = new Vector2(0f, .6f);
                        guide.FindProperty("landscapeDialogOffset").vector2Value = new Vector2(0f, .4f);
                    }
                }
                else
                {
                    Object.DestroyImmediate(root.transform.Find("RenderTextureSetup").gameObject);
                    var text = AuthorDialog(root, guide, "Hit OWL STATUE with power-ups to break it!");
                    text.rectTransform.sizeDelta = new Vector2(7.5f, text.rectTransform.sizeDelta.y);
                    var background = text.transform.parent;
                    foreach (var side in background.GetComponentsInChildren<SpriteRenderer>())
                        side.size = new Vector2(4.2f, side.size.y);
                    var center = new Vector2Int(4, 5);
                    guide.FindProperty("shortDialogCell").vector2IntValue = center;
                    guide.FindProperty("tallDialogCell").vector2IntValue = center;
                    guide.FindProperty("portraitDialogOffset").vector2Value = new Vector2(0f, -.6f);
                    guide.FindProperty("landscapeAnchor").enumValueIndex = 2; // WorldOrigin
                    guide.FindProperty("boardHighlightOffset").intValue = 1;
                }
                guide.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return "Foundation/Prefabs/" + name;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static TextMeshPro AuthorDialog(GameObject root, SerializedObject guide, string instruction)
        {
            var dialog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source + "TutorialDialog.prefab"),
                root.transform, false);
            dialog.name = "Tutorial Instruction";
            dialog.transform.localPosition = Vector3.zero;
            var sorting = dialog.GetComponent<SortingGroup>();
            sorting.sortingLayerID = SortingLayer.NameToID("Ui");
            sorting.sortingOrder = 520;
            var text = dialog.GetComponentInChildren<TextMeshPro>();
            text.text = instruction;
            guide.FindProperty("dialog").objectReferenceValue = dialog.transform;
            guide.FindProperty("instruction").objectReferenceValue = text;
            guide.FindProperty("dialogAnchorOffset").floatValue = .1f;
            guide.FindProperty("dialogTopClearance").floatValue = 1.1f;
            return text;
        }

        private static Material LoadMaskMaterial() => AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Resources/Recovered/Material/TutorialBlackMasked.mat");

        private static (Camera camera, Transform cells) AuthorMask(GameObject root, HashSet<Vector2Int> mask, bool boxOffsets)
        {
            var camera = root.GetComponentInChildren<Camera>();
            camera.enabled = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            var cells = new GameObject("Cell masks").transform;
            cells.SetParent(camera.transform.parent, false);
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "TutorialCellMask.prefab");
            var corner = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath("1fe1cb0f61b41bf48a8c3451babe1bd6"));
            foreach (var point in mask)
                AuthorCell(cells, template, corner, point, mask,
                    boxOffsets && Array.IndexOf(LevelOneBoxMaskOffsets, point) >= 0 ? .5f : 0f);
            return (camera, cells);
        }

        private static void AuthorCell(Transform parent, GameObject template, Sprite corner, Vector2Int point,
            HashSet<Vector2Int> mask, float verticalOffset)
        {
            var cell = Object.Instantiate(template, parent, false).transform;
            cell.name = $"Mask {point.x},{point.y}";
            cell.localPosition = new Vector3(point.x, point.y + verticalOffset, 0);
            var topLeft = cell.Find("TopLeftPatch").GetComponent<SpriteRenderer>();
            var topRight = cell.Find("TopRightPatch").GetComponent<SpriteRenderer>();
            var bottom = cell.Find("BottomPatch").GetComponent<SpriteRenderer>();
            bool left = mask.Contains(point + Vector2Int.left);
            bool right = mask.Contains(point + Vector2Int.right);
            bool up = mask.Contains(point + Vector2Int.up);
            bool upLeft = mask.Contains(point + Vector2Int.up + Vector2Int.left);
            bool upRight = mask.Contains(point + Vector2Int.up + Vector2Int.right);

            // TutorialCellMask.CalculateTopLeftPatch / CalculateTopRightPatch / CalculateBottomPatch.
            if (!left && up && upLeft) Patch(topLeft, -.5615f, .4393f, corner, -1f);
            else if (!left && up) Patch(topLeft, -.4581f, .5f);
            else Object.DestroyImmediate(topLeft.gameObject);

            if (upRight && right && up) Patch(topRight, .508f, .498f);
            else if (upRight && up) Patch(topRight, .4394f, .561f, corner);
            else if (upRight && right) Patch(topRight, .5604f, .439f, corner, -1f, -1f);
            else if (!upRight && right && up) Patch(topRight, .5606f, .5614f, corner, 1f, -1f);
            else if (!upRight && up) Patch(topRight, .5f, .4581f);
            else if (!upRight && right) Patch(topRight, .4581f, .503f, rotation: 90f);
            else Object.DestroyImmediate(topRight.gameObject);

            var below = point + Vector2Int.down;
            if (left && !mask.Contains(below) && !mask.Contains(below + Vector2Int.left) &&
                !mask.Contains(below + Vector2Int.right))
                Patch(bottom, -.4582f, -.498f, rotation: 90f);
            else Object.DestroyImmediate(bottom.gameObject);
        }

        private static void Patch(SpriteRenderer renderer, float x, float y, Sprite sprite = null,
            float scaleX = 1f, float scaleY = 1f, float rotation = 0f)
        {
            if (sprite != null) renderer.sprite = sprite;
            renderer.transform.localPosition = new Vector3(x, y, 0);
            renderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            renderer.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        }
    }
}
