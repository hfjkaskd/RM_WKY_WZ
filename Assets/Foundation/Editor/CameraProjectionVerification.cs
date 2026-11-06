using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class CameraProjectionVerification
    {
        public static void Run()
        {
            var report = new Report();
            var camera = Camera.main;
            var projection = UnityEngine.Object.FindObjectOfType<OriginalGameSceneProjection>();
            var hud = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            Require(camera != null && projection != null && hud != null, "Actual scene camera/projection/HUD owners exist");
            var previous = camera.targetTexture;
            try
            {
                report.preview_width = Screen.width; report.preview_height = Screen.height;
                Require(Screen.width == 720 && Screen.height == 1280, "Explicit Editor preview transport uses the target portrait dimensions");
                Require((int)PlayerSettings.defaultInterfaceOrientation == 0, "Original serialized default portrait orientation is retained");
                Require(!OriginalCameraProjection.IsLandscape && hud.target.localPosition == hud.targetPortraitPosition &&
                    hud.moves.localPosition == hud.movesPortraitPosition && hud.target.localScale.x == hud.portraitScale,
                    "This original build retains portrait HUD even when the host window is wide");
                var background = Array.Find(hud.GetComponentsInChildren<SpriteRenderer>(true), value => value.name == "Background");
                Require(background != null && background.gameObject.activeSelf, "Actual portrait background is active");
                report.portrait_hud_and_background_verified = true;
                var hudPose = hud.transform.position;
                hud.SetOrientationEnabled(false);
                hud.transform.position = hudPose + Vector3.up * .125f;
                projection.RefreshProjection();
                Require(hud.transform.position == hudPose + Vector3.up * .125f, "Projection events retain a locked result HUD pose");
                hud.SetOrientationEnabled(true); hud.RefreshLayout();
                report.locked_hud_pose_verified = true;
                var sizes = new[] { new Vector2Int(720, 1280), new Vector2Int(640, 480), new Vector2Int(480, 480),
                    new Vector2Int(1280, 720), new Vector2Int(720, 1440) };
                Camera flagCamera = null;
                report.samples = new Sample[sizes.Length];
                for (int i = 0; i < sizes.Length; i++)
                {
                    var target = new RenderTexture(sizes[i].x, sizes[i].y, 24);
                    try
                    {
                        camera.targetTexture = target;
                        projection.RefreshProjection();
                        if (sizes[i].x == sizes[i].y)
                        {
                            var squareRect = camera.rect;
                            projection.RefreshProjection();
                            Require(projection.Projection.CroppedViewport && camera.rect == squareRect &&
                                Mathf.Abs(squareRect.width - .7f) < .0001f,
                                "Refreshing a square viewport retains its centered native crop.");
                            flagCamera = FindFlagCamera(camera);
                            Require(flagCamera != null && flagCamera.enabled && flagCamera.depth < camera.depth &&
                                flagCamera.orthographicSize == camera.orthographicSize &&
                                flagCamera.clearFlags == camera.clearFlags && flagCamera.backgroundColor == camera.backgroundColor &&
                                flagCamera.cullingMask == (1 << 22) && (camera.cullingMask & flagCamera.cullingMask) == 0,
                                "The recovered flag camera renders only the flag layer behind the game.");
                            Require(projection.IsOverSquareSideBand(new Vector2(1, 240)) &&
                                projection.IsOverSquareSideBand(new Vector2(479, 240)) &&
                                !projection.IsOverSquareSideBand(new Vector2(240, 240)),
                                "Both square side strips are blocked while the centered gameplay viewport remains interactive.");
                        }
                        else
                        {
                            Require(!projection.Projection.CroppedViewport && camera.rect == new Rect(0, 0, 1, 1),
                                $"The {sizes[i]} viewport retains full-width gameplay. Actual: {camera.rect}.");
                            Require(flagCamera == null ? FindFlagCamera(camera) == null : !flagCamera.enabled,
                                "The flags load only on the first crop and stay disabled on ordinary viewports.");
                            Require(!projection.IsOverSquareSideBand(new Vector2(1, 240)),
                                "Returning from the crop releases the side strips.");
                        }
                        Require(Mathf.Abs(camera.orthographicSize * 2f - projection.Projection.Height) < .0001f,
                            "Native result reaches actual orthographic camera");
                        var left = camera.ViewportToWorldPoint(new Vector3(0f, .5f));
                        var right = camera.ViewportToWorldPoint(new Vector3(1f, .5f));
                        Require(Mathf.Abs(right.x - left.x - projection.Projection.Width) < .001f,
                            "Actual Unity viewport world width agrees with published native projection");
                        for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                        {
                            var cell = board.Grid.GetCell(x, y);
                            var world = board.BoardRoot.TransformPoint(new Vector3(x, y));
                            var restored = camera.ScreenToWorldPoint(camera.WorldToScreenPoint(world));
                            Require(ReferenceEquals(cell, game.GetCellFromWorldPosition(new Vector2(restored.x, restored.y))),
                                "All actual camera coordinate round trips still address the same playable cells");
                            report.cell_round_trips++;
                            if (sizes[i].x == 720)
                            {
                                var p = camera.WorldToViewportPoint(world);
                                Require(p.x > 0f && p.x < 1f && p.y > 0f && p.y < 1f, "Portrait projections retain all72 cell centers");
                            }
                        }
                        report.samples[i] = new Sample { width = sizes[i].x, height = sizes[i].y,
                            camera_width = projection.Projection.Width, camera_height = projection.Projection.Height,
                            aspect = camera.aspect, cropped = projection.Projection.CroppedViewport, viewport_width = camera.rect.width };
                    }
                    finally { camera.targetTexture = previous; UnityEngine.Object.DestroyImmediate(target); }
                }
                report.actual_camera_ports_and_round_trips_verified = true;
                VerifyRocketTrails(projection, game, board.Presentation);
                report.rocket_orientation_restart_and_pool_release_verified = true;
                report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally
            {
                camera.targetTexture = previous; projection.RefreshProjection();
                File.WriteAllText(Path.Combine(Application.dataPath, "../live_camera_projection_verification.json"), JsonUtility.ToJson(report, true) + "\n");
            }
        }

        private static void VerifyRocketTrails(OriginalGameSceneProjection projection, OriginalPlayableBoard game,
            OriginalLevelPresentation presentation)
        {
            projection.RefreshProjection(ScreenOrientation.Portrait);
            var first = game.SpawnRocketParticles();
            var second = game.SpawnRocketParticles();
            try
            {
                WarmRocketTrail(first);
                WarmRocketTrail(second);
                float firstTime = first.circleParticle.time, secondTime = second.circleParticle.time;
                projection.RefreshProjection(ScreenOrientation.Portrait);
                Require(first.circleParticle.time == firstTime && second.circleParticle.time == secondTime,
                    "Refreshing the same orientation preserves both existing rocket trails.");
                projection.RefreshProjection(ScreenOrientation.PortraitUpsideDown);
                Require(first.circleParticle.isPlaying && first.circleParticle.time < firstTime &&
                    second.circleParticle.isPlaying && second.circleParticle.time < secondTime,
                    "An accepted portrait orientation change clears and replays both rocket trails.");
            }
            finally
            {
                game.Pool.Recycle(first);
                game.Pool.Recycle(second);
            }

            var reused = presentation.SpawnRocketParticles();
            try
            {
                Require(ReferenceEquals(reused, second), "The recycled rocket trail is reused from its source pool.");
                WarmRocketTrail(reused);
                float reusedTime = reused.circleParticle.time;
                projection.RefreshProjection(ScreenOrientation.Portrait);
                Require(reused.circleParticle.time == reusedTime,
                    "Recycling removes the old orientation subscription before reuse.");
            }
            finally { game.Pool.Recycle(reused); }
        }

        private static void WarmRocketTrail(OriginalRocketItemParticles trail)
        {
            trail.circleParticle.Simulate(.15f, true, true, false);
            Require(trail.circleParticle.time > .1f, "The existing rocket trail has advanced.");
        }

        private static Camera FindFlagCamera(Camera game) =>
            Array.Find(game.GetComponentsInChildren<Camera>(), value => value.name == "SquareDeviceFlagCamera(Clone)");

        public static void RunSquareBackground()
        {
            var report = new SquareReport();
            var game = Camera.main;
            var originalTarget = game.targetTexture;
            var target = new RenderTexture(480, 480, 24);
            try
            {
                Require(Screen.width == 480 && Screen.height == 480 && Mathf.Abs(game.rect.width - .7f) < .0001f,
                    "The formal scene started at a square game resolution with its native crop.");
                var flags = FindFlagCamera(game);
                Require(flags != null && flags.isActiveAndEnabled, "Square startup automatically enables the flags.");
                var hud = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();
                var bottom = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>();
                var king = hud.KingVisual.Renderers[0].bounds.center;
                float kingY = game.WorldToViewportPoint(king).y;
                Require(kingY > .75f && kingY < 1f && bottom.Boosters.Length == 4,
                    "The portrait HUD and four tool slots are present at the actual square game resolution.");
                game.targetTexture = target;
                VerifySquareFlagRendering(game, flags, target, report);
                report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally
            {
                game.targetTexture = originalTarget;
                UnityEngine.Object.DestroyImmediate(target);
                File.WriteAllText(Path.Combine(Application.dataPath, "../live_square_background_verification.json"),
                    JsonUtility.ToJson(report, true) + "\n");
            }
        }

        private static void VerifySquareFlagRendering(Camera game, Camera flags, RenderTexture target, SquareReport report)
        {
            var renderers = flags.GetComponentsInChildren<SpriteRenderer>();
            Require(renderers.Length == 4, "Two instances of the recovered flag each have a body and diamonds.");
            foreach (var renderer in renderers)
            {
                Require(renderer.sprite != null && renderer.size.x == 7f &&
                    renderer.size.y == game.orthographicSize * 2f &&
                    (renderer.sprite.name == "square_flag_body" || renderer.sprite.name == "square_flag_tile"),
                    "Original flag sprites retain authored width and follow the current game height.");
            }
            var active = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                flags.targetTexture = target;
                flags.Render();
                game.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                var withFlags = image.GetPixels32();
                File.WriteAllBytes(Path.Combine(Application.dataPath, "../game_scene_square_background.png"), image.EncodeToPNG());
                foreach (var renderer in renderers) renderer.enabled = false;
                flags.Render();
                game.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                var withoutFlags = image.GetPixels32();
                int firstGameColumn = Mathf.RoundToInt(game.pixelRect.xMin);
                int afterGameColumn = Mathf.RoundToInt(game.pixelRect.xMax);
                for (int y = 0; y < target.height; y++)
                for (int x = 0; x < target.width; x++)
                {
                    int i = y * target.width + x;
                    var a = withFlags[i];
                    var b = withoutFlags[i];
                    if (Math.Abs(a.r - b.r) <= 8 && Math.Abs(a.g - b.g) <= 8 && Math.Abs(a.b - b.b) <= 8) continue;
                    if (x < firstGameColumn) report.changed_left_pixels++;
                    else if (x >= afterGameColumn) report.changed_right_pixels++;
                    else report.changed_game_pixels++;
                }
                Require(report.changed_left_pixels > 1000 && report.changed_right_pixels > 1000 && report.changed_game_pixels == 0,
                    "Original flags fill both side strips without changing any gameplay pixels.");
            }
            finally
            {
                foreach (var renderer in renderers) renderer.enabled = true;
                flags.targetTexture = null;
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Sample
        { public int width, height; public float camera_width, camera_height, aspect, viewport_width; public bool cropped; }
        [Serializable] private sealed class Report
        {
            public bool passed, portrait_hud_and_background_verified, actual_camera_ports_and_round_trips_verified, locked_hud_pose_verified;
            public bool rocket_orientation_restart_and_pool_release_verified;
            public int cell_round_trips, preview_width, preview_height;
            public Sample[] samples;
            public string failure;
            public string scope = "Five RenderTexture camera targets with 360 cell round trips. Square verification covers repeat crop and side input bounds. Physical display rotation and safe areas require device verification.";
        }
        [Serializable] private sealed class SquareReport
        {
            public bool passed;
            public int changed_left_pixels, changed_right_pixels, changed_game_pixels;
            public string failure;
        }
    }
}
