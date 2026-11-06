using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Whitebox.Foundation.Editor
{
    public static class GameSceneVerification
    {
        internal const string CastleGardenBackgroundGuid = "365375d749bf4afe8c8e0bca749b1127";

        public static void ConfigurePortraitPreview() =>
            PlayModeWindow.SetCustomRenderingResolution(720, 1280, "Royal Match Portrait");

        internal static void VerifyAuthoredBackground()
        {
            var views = UnityEngine.Object.FindObjectsOfType<OriginalGameBackground>();
            Require(views.Length == 1 && PrefabUtility.IsPartOfPrefabInstance(views[0]),
                "GameScene inherits a single active background prefab before Play.");
            var view = views[0];
            var foundation = view.GetComponentInParent<FoundationController>();
            var projection = foundation != null ? foundation.GetComponent<OriginalGameSceneProjection>() : null;
            Require(foundation != null && view.transform.parent == foundation.transform &&
                projection != null && projection.enabled && PrefabUtility.IsPartOfPrefabInstance(projection) &&
                new SerializedObject(foundation).FindProperty("gameSceneProjection").objectReferenceValue == projection &&
                new SerializedObject(projection).FindProperty("background").objectReferenceValue == view,
                "The Foundation prefab owns and wires projection and background independently of the scene.");
            var renderer = view.GetComponent<SpriteRenderer>();
            var prefabRenderer = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
            Require(renderer != null && renderer.enabled && renderer.sprite != null &&
                prefabRenderer != null && prefabRenderer.sprite == renderer.sprite &&
                AssetDatabase.GetAssetPath(renderer.sprite) == "Assets/Foundation/Scenes/CastleGardenPreview.png" &&
                renderer.sprite.rect.size == new Vector2(868f, 1447f) && renderer.sprite.pixelsPerUnit == 70f,
                "The prefab supplies the lightweight original garden at its full world size.");
            RequireBackgroundPixels(Camera.main, renderer);
            Capture(Camera.main, 720, 1280, "game_scene_background_before_play.png");
        }

        internal static void VerifyPreviewBackground()
        {
            var view = UnityEngine.Object.FindObjectOfType<OriginalGameBackground>();
            var renderer = view != null ? view.GetComponent<SpriteRenderer>() : null;
            Require(renderer != null && renderer.enabled && renderer.sprite != null &&
                AssetDatabase.GetAssetPath(renderer.sprite) == "Assets/Foundation/Scenes/CastleGardenPreview.png",
                "The test scene inherits the original garden preview from Foundation.");
            RequireBackgroundPixels(Camera.main, renderer);
            Capture(Camera.main, Screen.width, Screen.height, "recovery_preview_background.png");
        }

        [MenuItem("Royal Match/Open Game Scene")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Foundation/Scenes/GameScene.unity", OpenSceneMode.Single);
            ConfigurePortraitPreview();
        }

        // Explicit asset authoring for Unity2022's LightingSettings format.
        // No runtime callback or Editor-specific gameplay fallback uses this.
        public static void AuthorLightingSettings()
        {
            const string path = "Assets/Foundation/Settings/GameSceneLighting.lighting";
            if (File.Exists(path)) throw new InvalidOperationException("Lighting asset already exists; inspect before editing.");
            EditorSceneManager.OpenScene("Assets/Foundation/Scenes/GameScene.unity", OpenSceneMode.Single);
            var report = new LightingReport {
                before_ambient_mode = (int)RenderSettings.ambientMode,
                before_skybox_exists = RenderSettings.skybox != null,
                before_baked_gi = Lightmapping.bakedGI,
                before_realtime_gi = Lightmapping.realtimeGI
            };
            var lighting = new LightingSettings { name = "GameSceneLighting", bakedGI = false, realtimeGI = false };
            AssetDatabase.CreateAsset(lighting, path);
            Lightmapping.lightingSettings = lighting;
            AssetDatabase.SaveAssets();
            report.asset_guid = AssetDatabase.AssetPathToGUID(path);
            report.after_baked_gi = Lightmapping.bakedGI;
            report.after_realtime_gi = Lightmapping.realtimeGI;
            File.WriteAllText("game_scene_lighting_authoring.json", JsonUtility.ToJson(report, true) + "\n");
            Require(!report.after_baked_gi && !report.after_realtime_gi, "Authored asset retains original disabled GI");
        }

        public static void Run()
        {
            var report = new Report();
            try
            {
                Require(SceneManager.GetActiveScene().path == "Assets/Foundation/Scenes/GameScene.unity", "Actual normal game scene is active");
                Require(!Input.multiTouchEnabled, "The formal scene applies the original single-touch input policy.");
                report.original_single_touch_input_verified = true;
                var controller = UnityEngine.Object.FindObjectOfType<FoundationController>();
                var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
                var camera = Camera.main;
                Require(controller != null && controller.Gameplay != null && board.CreatedModelCount == 72 && board.CreatedViewCount == 72,
                    "Normal entry owns real playable models and prefab views");
                var projection = UnityEngine.Object.FindObjectOfType<OriginalGameSceneProjection>();
                Require(camera != null && camera.name == "GameCamera" && camera.cullingMask == 14135 && camera.depth == -1f &&
                    camera.clearFlags == CameraClearFlags.Skybox && projection != null &&
                    Mathf.Abs(camera.orthographicSize - projection.Projection.Height * .5f) < .00001f,
                    "Original camera settings and actual native projection owner are bound");
                Require(RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat && RenderSettings.skybox == null &&
                    !Lightmapping.bakedGI && !Lightmapping.realtimeGI, "Original render and baking settings are active");
                var lightingData = Lightmapping.lightingDataAsset;
                var lightingDataPath = AssetDatabase.GetAssetPath(lightingData);
                Require(lightingData != null && Array.IndexOf(AssetDatabase.GetDependencies(lightingDataPath, false),
                    SceneManager.GetActiveScene().path) >= 0, "The recovered lighting data belongs to the formal scene.");
                var probes = LightmapSettings.lightProbes;
                Require((LightmapSettings.lightmaps?.Length ?? 0) == 0 && (probes == null || probes.count == 0) &&
                    LightmapSettings.lightmapsMode == LightmapsMode.CombinedDirectional,
                    "The formal scene retains the original empty directional bake data.");
                report.original_empty_lighting_data_verified = true;
                report.normal_scene_and_72_models_verified = true;
                RequireGameBackground(CastleGardenBackgroundGuid);
                report.castle_garden_background_verified = true;
                var hud = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();
                RequireHudTitleCurve(hud.TargetTitle);
                RequireHudTitleCurve(Array.Find(hud.moves.GetComponentsInChildren<TextMeshPro>(), text => text.text == "Moves"));
                report.original_hud_title_curves_verified = true;
                var sprites = hud.GetComponentsInChildren<SpriteRenderer>(true);
                SpriteRenderer background = null;
                foreach (var sprite in sprites) if (sprite.name == "Background") background = sprite;
                Require(background != null && background.sprite != null &&
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(background.sprite)) == "0a933794f44b15945b7b6b1d4e4de892" &&
                    background.sortingLayerID == SortingLayer.NameToID("Ui") && background.sortingOrder == 10,
                    "Original top background subtree imports with its original sprite and sorting");
                report.original_top_background_verified = true;
                var king=hud.KingVisual;
                Require(king!=null&&king.IsInitialized&&king.BoneCount==29&&king.AnimationCount==32&&king.VertexCount>0,
                    "Normal HUD owns actual original King rig and evaluated mesh");
                foreach(var renderer in king.Renderers)
                {
                    Require(renderer!=null&&renderer.enabled&&renderer.sharedMaterial!=null&&renderer.sharedMaterial.mainTexture!=null,
                        "Both original separated mesh parts use a loaded atlas");
                    Require(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture))=="5d740dd8271b82d4685d02df4d7d0ef4",
                        "Original King texture is demand loaded");
                    var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture));
                    Require(!importer.alphaIsTransparency,"PMA texture must preserve transparent RGB rather than dilating it");
                }
                var kingBounds=king.Renderers[0].bounds;kingBounds.Encapsulate(king.Renderers[1].bounds);
                report.king_viewport_center=camera.WorldToViewportPoint(kingBounds.center);
                Require(report.king_viewport_center.x>0&&report.king_viewport_center.x<1&&report.king_viewport_center.y>0&&report.king_viewport_center.y<1,
                    "King mesh is inside the normal scene camera");
                report.king_vertices=king.VertexCount;report.original_king_rig_and_atlas_verified=true;
                var bottom=UnityEngine.Object.FindObjectOfType<OriginalBottomUi>();
                Require(bottom!=null&&bottom.IsInitialized&&bottom.Boosters.Length==4,"The formal scene restores the four original bottom tool slots");
                foreach(var slot in bottom.Boosters)
                {
                    Require(slot.Icon.sprite!=null&&slot.Icon.gameObject.activeInHierarchy&&
                        !slot.Presentation.Unlocked&&!slot.Button.interactable&&!slot.Presentation.ShowAdd,
                        "The original level 1 tools show their locked art without a purchase action");
                }
                report.original_bottom_locked_presentation_verified=true;
                report.core_bottom_frame_without_inventory_verified=true;
                Capture(camera, Screen.width, Screen.height, "game_scene_initial.png");
                // Save the unmodified frame before the visibility probe toggles the
                // nested sorting-group renderers; their re-enable can settle next frame.
                report.king_visible_pixels = CountVisiblePixels(camera, king.Renderers, out int probeWidth, out int probeHeight);
                report.king_probe_width = probeWidth;
                report.king_probe_height = probeHeight;
                Require(report.king_visible_pixels > probeWidth * probeHeight / 300,
                    "The initialized King contributes visible pixels after the production HUD entrance.");
                report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally { File.WriteAllText(Path.Combine(Application.dataPath, "../live_game_scene_verification.json"), JsonUtility.ToJson(report, true) + "\n"); }
        }

        internal static void RequireHudTitleCurve(TextMeshPro title)
        {
            var curve = title != null ? title.GetComponent<OriginalCurvedText>() : null;
            Require(curve != null && curve.isActiveAndEnabled, "The original HUD title owns a curve component.");
            title.ForceMeshUpdate();
            var center = title.textInfo.characterInfo[title.textInfo.characterCount / 2];
            var edge = title.textInfo.characterInfo[0];
            var centerVertices = title.textInfo.meshInfo[center.materialReferenceIndex].vertices;
            var edgeVertices = title.textInfo.meshInfo[edge.materialReferenceIndex].vertices;
            float curvedY = (centerVertices[center.vertexIndex].y + centerVertices[center.vertexIndex + 2].y) * .5f;
            var curvedEdge = edgeVertices[edge.vertexIndex + 3] - edgeVertices[edge.vertexIndex];
            try
            {
                curve.enabled = false;
                title.ForceMeshUpdate();
                centerVertices = title.textInfo.meshInfo[center.materialReferenceIndex].vertices;
                edgeVertices = title.textInfo.meshInfo[edge.materialReferenceIndex].vertices;
                float flatY = (centerVertices[center.vertexIndex].y + centerVertices[center.vertexIndex + 2].y) * .5f;
                var flatEdge = edgeVertices[edge.vertexIndex + 3] - edgeVertices[edge.vertexIndex];
                Require(curvedY - flatY > .005f && Vector3.Angle(flatEdge, curvedEdge) > 1f,
                    "The HUD title lifts its center and rotates its outside glyphs along the authored curve.");
            }
            finally
            {
                curve.enabled = true;
                title.ForceMeshUpdate();
            }
        }

        internal static void RequireGameBackground(string spriteGuid)
        {
            var view = UnityEngine.Object.FindObjectOfType<OriginalGameBackground>();
            Require(view != null, "The formal scene owns its original background prefab.");
            var renderer = view.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.enabled && renderer.sprite != null &&
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renderer.sprite)) == spriteGuid &&
                renderer.sprite.rect.size == new Vector2(1736f, 2894f) && renderer.sprite.pixelsPerUnit == 140f &&
                renderer.sortingLayerID == SortingLayer.NameToID("Background") && renderer.sortingOrder == 100,
                "Original background crop, world dimensions and sorting are active.");
            var material = renderer.sharedMaterial;
            Require(material != null && material.shader.isSupported && material.shader.name == "Whitebox/OriginalGameBackground" &&
                renderer.color == new Color(.8f, .8f, .8f, 1f) && Mathf.Approximately(material.GetColor("_BlendColor").a, .2f),
                "The original achromatic game tint and saturation pass are active.");
            var bounds = renderer.bounds;
            var camera = Camera.main;
            var minimum = camera.WorldToViewportPoint(bounds.min);
            var maximum = camera.WorldToViewportPoint(bounds.max);
            Require(minimum.x <= .0001f && minimum.y <= .0001f && maximum.x >= .9999f && maximum.y >= .9999f,
                "The authored background covers the active camera viewport.");
            float aspect = Mathf.Min(camera.aspect, 1f / camera.aspect);
            float expectedScale = aspect < .57f
                ? camera.orthographicSize * 2f / (2894f / 140f)
                : camera.orthographicSize * 2f * camera.aspect / 12.4f + (aspect < .6f ? .045f : 0f);
            Require((view.transform.localScale - Vector3.one * expectedScale).sqrMagnitude < .00000001f,
                "The area uses its native uniform height or width scale at the active camera aspect.");
            RequireBackgroundPixels(camera, renderer);
            RequireGardenClouds(view, renderer, spriteGuid == CastleGardenBackgroundGuid);
            RequireGardenDetails(view, renderer, spriteGuid == CastleGardenBackgroundGuid);
        }

        private static void RequireGardenClouds(OriginalGameBackground view, SpriteRenderer backdrop, bool useGarden)
        {
            var animators = view.GetComponentsInChildren<Animator>(true);
            Require(animators.Length == (useGarden ? 3 : 0),
                "The garden loads its three cloud layers and the hall skips the garden prefab.");
            for (int i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                var cloud = animator.GetComponent<SpriteRenderer>();
                Require(animator.isActiveAndEnabled && animator.runtimeAnimatorController != null &&
                    animator.speed == 0f && cloud != null && cloud.enabled && cloud.sprite != null,
                    "The garden cloud uses the authored Animator paused at gameplay activation.");
                Require(cloud.sortingLayerID == backdrop.sortingLayerID && cloud.sortingOrder == 500 + i * 100 &&
                    cloud.sharedMaterial == backdrop.sharedMaterial && cloud.color == backdrop.color &&
                    cloud.sprite.pixelsPerUnit == 140f && cloud.sprite.texture.name == "CastleGardenCloudAtlas",
                    "The cloud layer uses the original order, scale and tint with the compact atlas.");
            }
        }

        private static void RequireGardenDetails(OriginalGameBackground view, SpriteRenderer backdrop, bool useGarden)
        {
            var renderers = view.GetComponentsInChildren<SpriteRenderer>(true);
            Require(renderers.Length == (useGarden ? 8 : 1),
                "The garden includes its original sea, shoreline, shadows and clouds while the hall uses its single backdrop.");
            if (!useGarden) return;
            var names = new[] { "00200_Z_deniz", "00300_Z_deniz_land", "00400_Z_deniz_S", "15100_Z_s" };
            var positions = new[] { .682f, 1.2714281f, .11071396f, -8.328571f };
            var orders = new[] { 200, 300, 400, 15100 };
            for (int i = 0; i < names.Length; i++)
            {
                SpriteRenderer detail = null;
                for (int j = 0; j < renderers.Length; j++)
                    if (renderers[j].name == names[i]) detail = renderers[j];
                Require(detail != null && detail.enabled && detail.sprite != null &&
                    detail.sprite.texture != null && detail.sprite.texture.name == "CastleGardenDetailsAtlas",
                    "Each original garden detail uses the compact native texture.");
                Require(detail.transform.localPosition == new Vector3(0f, positions[i], 0f) &&
                    detail.transform.localScale == Vector3.one && detail.sortingOrder == orders[i] &&
                    detail.sortingLayerID == backdrop.sortingLayerID && detail.sharedMaterial == backdrop.sharedMaterial &&
                    detail.color == backdrop.color && detail.sprite.pixelsPerUnit == 140f,
                    "The garden detail retains its original transform, order and static gameplay material.");
            }
        }

        private static void RequireBackgroundPixels(Camera camera, SpriteRenderer renderer)
        {
            int changed = CountVisiblePixels(camera, new Renderer[] { renderer }, out int width, out int height);
            Require(changed > width * height / 16,
                $"The original background contributes visible pixels to the game camera ({changed}/{width * height}, Game view {Screen.width}x{Screen.height}).");
        }

        private static int CountVisiblePixels(Camera camera, Renderer[] renderers, out int width, out int height)
        {
            // Preserve the active Game view aspect while comparing the same frame.
            float sampleScale = 160f / Mathf.Max(Screen.width, Screen.height);
            width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * sampleScale));
            height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * sampleScale));
            var target = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var wasEnabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) wasEnabled[i] = renderers[i].enabled;
            try
            {
                camera.targetTexture = target;
                ReadCamera(camera, target, pixels);
                var withRenderers = pixels.GetPixels32();
                for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
                ReadCamera(camera, target, pixels);
                var withoutRenderers = pixels.GetPixels32();
                int changed = 0;
                for (int i = 0; i < withRenderers.Length; i++)
                {
                    Color32 a = withRenderers[i], b = withoutRenderers[i];
                    if (Math.Abs(a.r - b.r) > 8 || Math.Abs(a.g - b.g) > 8 || Math.Abs(a.b - b.b) > 8) changed++;
                }
                return changed;
            }
            finally
            {
                for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = wasEnabled[i];
                camera.targetTexture = previous;
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ReadCamera(Camera camera, RenderTexture target, Texture2D pixels)
        {
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        }

        private static void Capture(Camera camera, int width, int height, string fileName)
        {
            var target = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                ReadCamera(camera, target, pixels);
                File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + fileName), pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous; RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(target);
            }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Report
        {
            public bool passed, normal_scene_and_72_models_verified, original_top_background_verified,original_king_rig_and_atlas_verified;
            public bool castle_garden_background_verified, original_hud_title_curves_verified, original_empty_lighting_data_verified;
            public bool original_single_touch_input_verified;
            public int king_vertices, king_visible_pixels, king_probe_width, king_probe_height;
            public Vector3 king_viewport_center;
            public bool original_bottom_locked_presentation_verified;
            public bool core_bottom_frame_without_inventory_verified;
            public string failure;
            public string scope = "After the production entrance settles, the formal GameScene verifies the original camera, top HUD, visible King mesh pixels, playable first board, and four locked standard Button tool slots. Separate scenarios verify the actual player input, result flow and in-board power activation. Original-device rendered parity remains outside this check.";
        }
        [Serializable] private sealed class LightingReport
        {
            public int before_ambient_mode;
            public bool before_skybox_exists, before_baked_gi, before_realtime_gi, after_baked_gi, after_realtime_gi;
            public string asset_guid;
        }
    }
}
