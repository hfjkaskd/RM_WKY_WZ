using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit, isolated visual diagnostic. Does not run unless requested,
    // change Play Mode, save scenes, or alter the production prefab.
    [InitializeOnLoad]
    internal static class WinPresentationVerification
    {
        private const string PrefabPath = "Assets/Resources/Foundation/Prefabs/CoreWinLogo.prefab";
        private const string LogoPath = "Assets/RMWKY/Loading/GemwinkCastleLoadingLogo.png";
        private static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/WinPresentationVerification"));
        private static double nextCheck;
        private static bool running;

        static WinPresentationVerification() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (running || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) ||
                EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            string request = Path.Combine(Output, "run_v2.request");
            if (!File.Exists(request)) return;
            File.Delete(request);
            Run();
        }

        [MenuItem("Tools/Gemwink/Verify Win Presentation")]
        public static void Run()
        {
            if (running || (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)) return;
            running = true;
            Directory.CreateDirectory(Output);
            var report = new Report { timestamp = DateTime.UtcNow.ToString("O"), editor_was_playing = EditorApplication.isPlaying };
            try
            {
                VerifyNormal(report);
                VerifySkip(report);
                Require(EditorApplication.isPlaying == report.editor_was_playing, "Editor Play state is unchanged.");
                report.passed = true;
                Debug.Log("[WinPresentationVerification] Passed: " + Output);
            }
            catch (Exception error)
            {
                report.failure = error.ToString();
                Debug.LogException(error);
            }
            finally
            {
                File.WriteAllText(Path.Combine(Output, "report.json"), JsonUtility.ToJson(report, true));
                running = false;
            }
        }

        private static void VerifyNormal(Report report)
        {
            using (var scene = new Preview())
            {
                var visual = scene.Visual;
                var replacement = scene.Replacement;
                Require(replacement.sprite == AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath), "Existing Gemwink Castle sprite is bound.");
                report.sprite_path = AssetDatabase.GetAssetPath(replacement.sprite);
                visual.Initialize();
                Require(!replacement.gameObject.activeInHierarchy, "Logo is hidden after initialization.");
                report.hidden_before_play = true;
                visual.Play();
                visual.ManualUpdate(1.5f);
                Require(replacement.gameObject.activeInHierarchy && replacement.enabled, "Replacement is visible during victory.");
                Require(visual.CanSkip, "Skip becomes available on the original timeline.");
                Require(replacement.bounds.size.x > .1f && replacement.bounds.size.y > .1f, "Replacement has a visible size.");
                scene.FrameCamera();
                var visible = scene.Capture("win_1_50.png");
                replacement.enabled = false;
                var hidden = scene.Capture(null);
                replacement.enabled = true;
                int changed = 0;
                for (int p = 0; p < visible.Length; p++)
                    if (Math.Abs(visible[p].r - hidden[p].r) + Math.Abs(visible[p].g - hidden[p].g) + Math.Abs(visible[p].b - hidden[p].b) > 12) changed++;
                Require(changed > 1000, "Actual render contains the replacement sprite.");
                report.rendered_logo_pixels = changed;
                var logoPart = replacement.GetComponentInParent<MeshFilter>();
                Require(logoPart != null && logoPart.sharedMesh != null && logoPart.sharedMesh.vertexCount == 0,
                    "Old logo mesh partition has no remaining geometry.");
                report.old_logo_mesh_vertices = logoPart.sharedMesh.vertexCount;
                int closes = 0;
                visual.OnCloseRequested += () => { closes++; visual.CloseDialogVisual(); };
                visual.ManualUpdate(1.6f);
                Require(closes == 1 && !replacement.gameObject.activeInHierarchy, "Natural close hides the replacement exactly once.");
                report.natural_close_hides_logo = true;
                scene.Capture("win_closed.png");
            }
        }

        private static void VerifySkip(Report report)
        {
            using (var scene = new Preview())
            {
                var visual = scene.Visual;
                visual.Initialize();
                visual.Play();
                visual.ManualUpdate(1f);
                float before = visual.AnimationView.TrackTime;
                visual.ReverseForSkip();
                visual.AnimationView.Advance(.1f);
                Require(visual.AnimationView.TrackTime < before, "Skip reverses the original animation track.");
                report.skip_reverses_track = true;
                scene.FrameCamera();
                scene.Capture("win_skip_reverse.png");
                visual.CloseDialogVisual();
                Require(!scene.Replacement.gameObject.activeInHierarchy, "Skip close hides replacement.");
                visual.StopAnimation();
                Require(!visual.AnimationView.gameObject.activeSelf, "Skip stop hides animation.");
                report.skip_close_hides_logo = true;
                scene.Capture("win_skip_closed.png");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class Preview : IDisposable
        {
            private readonly PreviewRenderUtility utility;
            private readonly GameObject root;
            private readonly List<Object> transientAssets = new List<Object>();
            public readonly OriginalWinLogoVisual Visual;
            public readonly SpriteRenderer Replacement;

            public Preview()
            {
                utility = new PreviewRenderUtility();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Require(prefab != null, "Victory prefab imports.");
                // Unity 2022.3 creates the instance directly in its isolated
                // preview scene, never in the user's active gameplay scene.
                root = utility.InstantiatePrefabInScene(prefab);
                Require(EditorSceneManager.IsPreviewScene(root.scene) && !Application.IsPlaying(root),
                    "Prefab belongs only to the non-playing preview scene.");
                Visual = root.GetComponent<OriginalWinLogoVisual>();
                Require(Visual != null, "Victory visual exists.");
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.gameObject.name == "Gemwink Castle") Replacement = renderer;
                Require(Replacement != null, "Authored replacement renderer exists.");
                utility.camera.orthographic = true;
                utility.camera.clearFlags = CameraClearFlags.SolidColor;
                utility.camera.backgroundColor = new Color(.025f, .075f, .11f, 1f);
                utility.camera.nearClipPlane = .01f;
                utility.camera.farClipPlane = 100f;
                utility.camera.transform.rotation = Quaternion.identity;
                utility.camera.transform.position = new Vector3(0, 0, -30);
            }

            public void FrameCamera()
            {
                Bounds bounds = Replacement.bounds;
                foreach (var renderer in Visual.AnimationView.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.gameObject.activeInHierarchy) bounds.Encapsulate(renderer.bounds);
                utility.camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -30);
                utility.camera.orthographicSize = Mathf.Max(10f, bounds.extents.y * 1.1f, bounds.extents.x / (720f / 1280f) * 1.1f);
            }

            public Color32[] Capture(string filename)
            {
                utility.BeginPreview(new Rect(0, 0, 720, 1280), GUIStyle.none);
                utility.camera.Render();
                var result = (RenderTexture)utility.EndPreview();
                var old = RenderTexture.active;
                var pixels = new Texture2D(result.width, result.height, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = result;
                    pixels.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);
                    pixels.Apply();
                    if (filename != null) File.WriteAllBytes(Path.Combine(Output, filename), pixels.EncodeToPNG());
                    return pixels.GetPixels32();
                }
                finally { RenderTexture.active = old; Object.DestroyImmediate(pixels); }
            }

            public void Dispose()
            {
                // Clean runtime-created meshes/materials before destroying the
                // preview objects, so their runtime OnDestroy sees null assets.
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && !AssetDatabase.Contains(filter.sharedMesh)) transientAssets.Add(filter.sharedMesh);
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.sharedMaterial != null && !AssetDatabase.Contains(renderer.sharedMaterial) && !transientAssets.Contains(renderer.sharedMaterial))
                        transientAssets.Add(renderer.sharedMaterial);
                if (Visual != null) Visual.TransferFireworksOwnership();
                foreach (var asset in transientAssets) if (asset != null) Object.DestroyImmediate(asset);
                utility.Cleanup();
            }
        }

        [Serializable]
        private sealed class Report
        {
            public bool passed, editor_was_playing, hidden_before_play, natural_close_hides_logo, skip_reverses_track, skip_close_hides_logo;
            public int rendered_logo_pixels, old_logo_mesh_vertices;
            public string timestamp, sprite_path, failure;
            public string scope = "Isolated non-playing preview-scene render of the production victory prefab and its public visual lifecycle. Editor Play state is preserved. Does not verify the live HUD, dialog manager, touch input, or a device APK.";
        }
    }
}
