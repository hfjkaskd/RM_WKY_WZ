using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class WinLogoVisualVerification
    {
        private static OriginalWinLogoVisual visual;
        private static float started;
        private static int frame;
        private static bool captured;
        private static int closeRequests;
        private static float closedAt;
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_win_logo_visual_verification.json");
        public static void Run()
        {
            var prefab = Resources.Load<GameObject>("Foundation/Prefabs/OriginalWinLogoVisual");
            Require(prefab != null, "Original visual prefab imports");
            visual = UnityEngine.Object.Instantiate(prefab).GetComponent<OriginalWinLogoVisual>();
            visual.Initialize();
            visual.OnCloseRequested += CloseRequested;
            Require(visual.AnimationView.BoneCount == 43 && visual.AnimationView.PartCount == 2, "Original 43-bone logo and both mesh parts");
            Require(Math.Abs(visual.AnimationView.Duration - 3.766667f) < .001f, "Original duration");
            visual.Play(); started = Time.time; frame = Time.frameCount;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
                visual.ManualUpdate(Time.deltaTime);
                float elapsed = Time.time - started;
                if (elapsed < .7f) Require(!visual.CanSkip, "Original skip delay");
                if (elapsed >= 1.5f && !captured)
                {
                    Require(visual.CanSkip && visual.AnimationView.gameObject.activeSelf, "Original skip enabled and visible logo");
                    var filters = visual.AnimationView.GetComponentsInChildren<MeshFilter>();
                    int vertices = 0;
                    foreach (var filter in filters)
                    {
                        Require(filter.sharedMesh != null, "Original mesh part bound"); vertices += filter.sharedMesh.vertexCount;
                        var renderer = filter.GetComponent<MeshRenderer>();
                        Require(renderer.sharedMaterial.mainTexture != null && renderer.sharedMaterial.shader.isSupported, "Atlas and PMA shader supported");
                        Require(renderer.sharedMaterial.mainTexture.width > 1000 && renderer.sharedMaterial.mainTexture.height > 1000, "Actual resource atlas replaces recovered placeholder");
                        Require(renderer.sortingLayerID == SortingLayer.NameToID("Dialog"), "Original dialog sorting above board");
                    }
                    Require(vertices > 500, "Original clipped mesh geometry restored");
                    Capture(); captured = true;
                }
                if (elapsed < 3.2f) return;
                Require(captured && closeRequests == 1 && !visual.AnimationView.gameObject.activeSelf, "Original3second Close request reaches owner exactly once");
                Require(closedAt >= 3f && closedAt < 3.08f, "Native close timer, not a mesh-only stop");
                File.WriteAllText(ReportPath, "{\"passed\":true,\"rig_bones\":43,\"parts\":2,\"dynamic_clipping\":true,\"close_requests\":1,\"scope\":\"Original win logo prefab, analytical skeleton and continuous clipping/PMA, original particle timing; actual3second native Close literal reaches explicit test owner. Does not establish native dynamic screenshot backdrop, camera-dependent trumpet relocation, skip owner, production flow or result parity.\"}\n");
                EditorApplication.update -= Tick; UnityEngine.Object.Destroy(visual.gameObject);
            }
            catch (Exception e)
            { EditorApplication.update -= Tick; File.WriteAllText(ReportPath, JsonUtility.ToJson(new Failure { failure = e.ToString() })); throw; }
        }
        private static void CloseRequested() { closeRequests++; closedAt = visual.AnimationTime; visual.CloseVisual(); }
        private static void Capture()
        {
            var camera = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().PointerCamera;
            int height = 768, width = Mathf.RoundToInt(height * camera.aspect);
            var target = new RenderTexture(width, height, 24); var previous = camera.targetTexture; var active = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../win_logo_visual.png"), pixels.EncodeToPNG());
            camera.targetTexture = previous; RenderTexture.active = active; UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(target);
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Failure { public bool passed; public string failure; }
    }
}
