using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class RemainingMovesReplacementVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameplayHud hud;
        private static OriginalRemainingMovesItemReplacer replacer;
        private static OriginalRemainingMovesItemExploder exploder;
        private static bool goalsDone, converted, exploded;
        private static int frame, phase;
        private static float started;
        private static float rewardHitAt;
        private static int rewardCount;
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_remaining_moves_replacement_verification.json");
        public static void Run()
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            hud = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            GoalUiVerification.OnFinished += GoalsFinished; GoalUiVerification.Run(); EditorApplication.update += Tick;
        }
        private static void GoalsFinished() { goalsDone = true; }
        private static void Tick()
        {
            try
            {
                if (!goalsDone || !EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
                if (phase == 0)
                {
                    if (game.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch) return;
                    // Explicit session-end fixture until the original win owner
                    // is composed; actual conversion uses production services.
                    game.State.SetStateToWin(); game.Session.State = 3;
                    game.Session.LeftMoves = game.Moves.LeftMoves; game.Session.MadeMoves = game.Moves.MadeMoves;
                    board.Goals.ReplaceGoalsWithCoin(10); // Native first normal level base reward fixture.
                    Require(board.Goals.GetFinalGoals().Length == 1 && board.Goals.GetFinalGoals()[0].GoalType == 21 &&
                        board.Goals.GetGoalIndex(11) == -1 && board.Goals.GetGoalLeftCount(21) == 10 && board.Goals.GetFlyingGoalCount(21) == 0,
                        "Actual goal model replaced with the native coin tuple");
                    Require(!board.Goals.IsAllGoalsReached() && game.GoalPanel.LeftGoalCheckCount == 0 &&
                        game.GoalPanel.Views[0].transform.localScale == Vector3.zero && hud.TargetTitle.text == "Reward",
                        "Coin reward does not create another goal check and enters from scale zero");
                    var services = new OriginalRemainingMovesReplacementServices(game, board, hud, UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>());
                    var grid = new OriginalBoardGridAccess(board.Grid, board.Layout);
                    replacer = new OriginalRemainingMovesItemReplacer(new OriginalBottomLeftHorizontalStrategy(grid), board.Presentation.Coroutines, services);
                    exploder = new OriginalRemainingMovesItemExploder(new OriginalTopLeftHorizontalStrategy(grid), game.State,
                        board.Presentation.Coroutines, () => board.Clock.Time, () => board.Clock.DeltaTime, hud.ResetSorting);
                    Require(replacer.StartMoves == 25, "Native Session.LeftMoves capture");
                    started = Time.realtimeSinceStartup; phase = 1;
                    replacer.SendMoves(.3f, () => converted = true); return;
                }
                Require(Time.realtimeSinceStartup - started < 100f, "Real replacement and explosion complete without operation resets");
                if (phase == 1)
                {
                    if (!converted) return;
                    Require(replacer.IsComplete && replacer.ReplacedMoves == 25 && hud.moveText.text == "0", "All25 displayed moves actually land as special items");
                    Require(game.Moves.LeftMoves == 25 && game.Session.LeftMoves == 25, "Remaining conversion preserves model/session move budget");
                    Require(CountSpecials() >= 25, "Real special-item models on board");
                    Require(game.GoalPanel.Views[0].countText.text == "10" && !game.GoalPanel.Views[0].checkImage.gameObject.activeSelf &&
                        Vector3.Distance(game.GoalPanel.Views[0].transform.localScale, Vector3.one) < .001f,
                        "Recovered coin art/count finishes the quarter-second appearance");
                    Capture("remaining_moves_converted.png"); phase = 2;
                    exploder.ExplodeItemsOnBoard(() => exploded = true); return;
                }
                if (!exploded) return;
                Require(!game.State.HasAnyOperation() && game.Fall.IsAllItemsSteady && CountSpecials() == 0, "Real end-phase cascade completed");
                int holes = 0;
                for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
                    if (!board.Grid.GetCell(x, y).Mediation.HasCurrentItem()) holes++;
                Require(holes == 0, "No empty cells after remaining conversion/explosions");
                if (phase == 2)
                {
                    // Explicit arrival callback fixture. Coin pilot/pool and
                    // production RemainingMovesManager are still pending.
                    board.Goals.IncreaseGoalUi(21); rewardCount = board.Goals.GetGoalUiLeftCount(21);
                    Require(rewardCount == 11 && board.Goals.GetGoalLeftCount(21) == 10 && game.GoalPanel.Views[0].countText.text == "11",
                        "Native reward UI arrival is independent of model accounting");
                    Require(game.GoalPanel.Views[0].goalHitParticles.isPlaying && game.GoalPanel.Views[0].goalHitCircle.isPlaying,
                        "Recovered reward landing particles play");
                    rewardHitAt = Time.time; phase = 3; return;
                }
                if (Time.time - rewardHitAt < .3f) return;
                Require(Vector3.Distance(game.GoalPanel.Views[0].goalImage.transform.localScale, Vector3.one) < .001f &&
                    !game.GoalPanel.Views[0].checkImage.gameObject.activeSelf && game.GoalPanel.LeftGoalCheckCount == 0,
                    "Coin icon returns from its hit pulse without releasing another victory check");
                Capture("remaining_moves_reward.png");
                File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { passed = true, converted_moves = replacer.ReplacedMoves,
                    displayed_moves = hud.moveText.text, model_moves = game.Moves.LeftMoves, holes = holes,
                    reward_ui_count = rewardCount, reward_model_count = board.Goals.GetGoalLeftCount(21),
                    elapsed = Time.realtimeSinceStartup - started }, true) + "\n");
                Cleanup();
            }
            catch (Exception e) { File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { failure = e.ToString() }, true)); Cleanup(); throw; }
        }
        private static int CountSpecials()
        { int n = 0; for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++) if (board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalSpecialBoardItem item && item.IsActive && !item.IsExploded) n++; return n; }
        private static void Capture(string name)
        {
            var camera = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().PointerCamera;
            var target = new RenderTexture(Mathf.RoundToInt(768 * camera.aspect), 768, 24);
            var previous = camera.targetTexture; var active = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + name), pixels.EncodeToPNG());
            camera.targetTexture = previous; RenderTexture.active = active; UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(target);
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void Cleanup() { EditorApplication.update -= Tick; GoalUiVerification.OnFinished -= GoalsFinished; }
        [Serializable] private sealed class Report
        {
            public bool passed; public string failure, displayed_moves; public int converted_moves, model_moves, holes, reward_ui_count, reward_model_count; public float elapsed;
            public string scope = "Formal36box goal completion then explicit GameState/Session win fixture; production coin goal replacement, original coin/shadow resources, reward title/appearance,25actual pooled particle flights/replacements and real special explosion/cascade/refill. Explicit single reward arrival callback verifies UI/model count separation and hit particles/pulse. Coin pilots/remaining manager, production WinFail/Flow/account/settlement binding, localization/curved title and rendered-original parity remain pending.";
        }
    }
}
