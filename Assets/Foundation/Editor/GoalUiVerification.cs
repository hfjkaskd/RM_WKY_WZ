using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class GoalUiVerification
    {
        public static event Action OnFinished;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell tapping;
        private static Samples samples;
        private static OriginalGameplayHud hud;
        private static bool comboFinished, sawDelay, sawVisible, sawParticles;
        private static int frame, sampleFrame, phase, taps;
        private static float started, goalZeroAt, checkVisibleAt;
        private static readonly Report report = new Report();
        private static string PathName => Path.Combine(Application.dataPath, "../live_goal_ui_verification.json");
        public static void Run(bool afterOpening = false)
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            hud = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            Require(hud != null && game.GoalPanel != null && game.GoalPanel.Views.Count == 1, "Real original HUD/goal panel bound");
            var goal = game.GoalPanel.Views[0]; goal.countText.ForceMeshUpdate();
            int expectedGoals = afterOpening ? board.Goals.GetGoalLeftCount(11) : 36;
            Require(goal.countText.text == expectedGoals.ToString() && goal.countText.textInfo.characterCount == 2,
                "Original font renders the remaining box goal");
            Require(!board.Goals.AreAllGoalChecksShown() && game.GoalPanel.LeftGoalCheckCount == 1, "Native pending check count1");
            Require(goal.goalImage.sprite != null && goal.shadowImage.sprite != null && !goal.checkImage.gameObject.activeSelf, "Original goal art and hidden check");
            Require(hud.moveText.text == (afterOpening ? "29" : "30"), "Production move event drives original count");
            File.WriteAllText(PathName, "{\"passed\":false,\"status\":\"running\"}\n");
            started = Time.realtimeSinceStartup; frame = Time.frameCount;
            TntRocketComboVerification.OnFinished += ComboFinished;
            TntRocketComboVerification.Run(afterOpening); EditorApplication.update += Tick;
        }
        private static void ComboFinished()
        {
            comboFinished = true; device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            Capture("goal_ui_before_complete.png");
        }
        private static void Tick()
        {
            try { Step(); }
            catch (Exception e)
            { report.passed = false; report.failure = e.ToString(); File.WriteAllText(PathName, JsonUtility.ToJson(report, true)); Cleanup(); throw; }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
            Require(Time.realtimeSinceStartup - started < 100f, "Formal goal scenario completes");
            if (!comboFinished) return;
            var goal = game.GoalPanel.Views[0];
            // One runtime Tick may cross both the particle stage and sequence
            // completion. Observe the real ParticleSystem on that frame too.
            if (goal.goalCheckParticles.isPlaying) sawParticles = true;
            if (board.Goals.GetGoalUiLeftCount(11) == 0)
            {
                if (phase != 2) { phase = 2; goalZeroAt = Time.time; }
                if (goal.IsCheckAnimationWorking)
                {
                    Require(!board.Goals.AreAllGoalChecksShown() && game.GoalPanel.LeftGoalCheckCount == 1, "Victory check gate waits for actual sequence");
                    if (!goal.checkImage.gameObject.activeSelf) sawDelay = true;
                    else if (!sawVisible) { sawVisible = true; checkVisibleAt = Time.time; report.delay_seconds = checkVisibleAt - goalZeroAt; }
                    return;
                }
                Require(sawDelay && sawVisible && sawParticles, "Delay, actual check sprite and original particles observed");
                Require(board.Goals.IsAllGoalsReached() && board.Goals.AreAllGoalChecksShown() && game.GoalPanel.LeftGoalCheckCount == 0, "Goal model and completed-check gate agree");
                Require(!goal.countText.gameObject.activeSelf && goal.checkImage.gameObject.activeSelf, "Completed goal keeps its original check");
                Require(Vector3.Distance(goal.checkImage.transform.localScale, Vector3.one * .6f) < .001f, "Check returns to original scale");
                Require(hud.moveText.text == game.Moves.LeftMoves.ToString(), "Actual move count remains synchronized");
                Require(report.delay_seconds > .14f && report.delay_seconds < .26f, "Original .2second check delay");
                report.check_seconds = Time.time - goalZeroAt;
                Require(report.check_seconds > .56f && report.check_seconds < .73f, "Original .6355second completion sequence");
                Require(game.Fall.IsAllItemsSteady || game.State.HasAnyOperation(), "No test-forced board settling");
                Capture("goal_ui_completed.png"); report.passed = true; report.tnt_taps = taps;
                report.left_moves = game.Moves.LeftMoves; report.goal_count = board.Goals.GetGoalUiLeftCount(11);
                report.pending_checks = game.GoalPanel.LeftGoalCheckCount;
                File.WriteAllText(PathName, JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("Formal goal UI verification passed: original prefab/font/icons, actual combo/TNT damage, delayed check particles and completion gate."); Cleanup(); OnFinished?.Invoke(); return;
            }
            // Native WinFail stops player moves as soon as the MODEL reaches
            // the goal, before the last flying goal/UI check has arrived.
            if (board.Goals.IsAllGoalsReached()) return;
            if (phase == 1)
            {
                if (sampleFrame < 3)
                {
                    var p = tapping.GetViewPosition(); samples.Position = new Vector2(p.x, p.y);
                    samples.Down = sampleFrame == 0; samples.Held = sampleFrame < 2; samples.Up = sampleFrame == 2;
                    listener.ManualUpdate(); sampleFrame++; return;
                }
                if (!game.Fall.IsAllItemsSteady || game.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch) return;
                phase = 0;
            }
            tapping = FindTntCell(); Require(tapping != null && taps < 10, "Remaining original boxes reachable by native TNT");
            var old = (OriginalMatchBoardItem)tapping.Mediation.CurrentItem;
            old.Mediator.ClearFromAllRegisteredCells(); board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)old.SwapView);
            game.CreateItemAtCell(40, tapping); taps++; sampleFrame = 0; phase = 1;
        }
        private static OriginalGridCell FindTntCell()
        {
            for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
            {
                if (!(board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBoxBoardItem)) continue;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    var c = board.Grid.GetCell(x + dx, y + dy);
                    if (c != null && c.Mediation.CurrentItem is OriginalMatchBoardItem) return c;
                }
            }
            return null;
        }
        private static void Capture(string name)
        {
            var camera = device.PointerCamera;
            int height = 768, width = Mathf.RoundToInt(height * camera.aspect);
            var target = new RenderTexture(width, height, 24); var previous = camera.targetTexture; var active = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + name), pixels.EncodeToPNG());
            camera.targetTexture = previous; RenderTexture.active = active; UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(target);
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick; TntRocketComboVerification.OnFinished -= ComboFinished;
            if (device != null && game != null && device.Listener == null) device.Bind(game);
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private sealed class Samples : IOriginalGameTouchInput
        {
            public Vector2 Position; public bool Down, Held, Up;
            public Vector2 GetMouseWorldPosition() => Position;
            public bool GetMouseButtonDown() => Down;
            public bool GetMouseButton() => Held;
            public bool GetMouseButtonUp() => Up;
        }
        [Serializable] private sealed class Report
        {
            public bool passed; public string failure; public int tnt_taps, left_moves, goal_count, pending_checks;
            public float delay_seconds, check_seconds;
            public string scope = "Formal first-board scene and unchanged target counts: four production TNT/rocket fixture drags then actual TNT tap damage until all36box goals finish. Original GoalUi prefab, font/material, resource icons and particles, move events, manual level clock, delayed check sequence and GoalManager completion gate. Does not establish natural special generation, physical input, full camera/King/background/original rendered parity, WinFail flow/remaining moves/settlement/progression.";
        }
    }
}
