using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class RemainingMovesExploderVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalRemainingMovesItemExploder exploder;
        private static bool goalsDone, started, completed;
        private static int frame, resets, initialCoins, initialSpecials;
        private static float startTime;
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_remaining_moves_exploder_verification.json");
        public static void Run()
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            GoalUiVerification.OnFinished += GoalsFinished;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            GoalUiVerification.Run(); EditorApplication.update += Tick;
        }
        private static void GoalsFinished() { goalsDone = true; }
        private static void Tick()
        {
            try
            {
                if (!goalsDone || !EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
                if (!started)
                {
                    if (!game.Fall.IsAllItemsSteady || game.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch) return;
                    Require(board.Goals.IsAllGoalsReached() && board.Goals.AreAllGoalChecksShown(), "Actual36box goal/check completion precedes remaining explosions");
                    // Explicit end-state fixture until the original session and
                    // presentation flow owners are composed. Not a player fallback.
                    game.State.SetStateToWin();
                    game.Session.State = 3; // Session.Win precedes the original remaining-moves action.
                    AddSpecialFixtures();
                    var strategy = new OriginalTopLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout));
                    exploder = new OriginalRemainingMovesItemExploder(strategy, game.State, board.Presentation.Coroutines,
                        () => board.Clock.Time, () => board.Clock.DeltaTime, () => resets++);
                    initialCoins = exploder.CalculateExtraCoinAmountOnBoard(); initialSpecials = CountSpecials();
                    Require(initialCoins >= 13 && initialSpecials >= 5, "All five original special variants and their native coin values admitted");
                    startTime = Time.realtimeSinceStartup; started = true;
                    exploder.ExplodeItemsOnBoard(() => completed = true); return;
                }
                Require(Time.realtimeSinceStartup - startTime < 80f, "Remaining special explosions terminate without resetting operations");
                if (!completed) return;
                Require(exploder.IsComplete && resets == 1, "Original reset callback and one completion");
                Require(!game.State.HasAnyOperation() && game.Fall.IsAllItemsSteady, "Real board operations and falls finished");
                Require(CountSpecials() == 0 && exploder.CalculateExtraCoinAmountOnBoard() == 0, "All active special items exploded");
                int holes = 0;
                for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
                    if (!board.Grid.GetCell(x, y).Mediation.HasCurrentItem()) holes++;
                Require(holes == 0 && game.Moves.LeftMoves == 25, "Remaining explosions leave no holes and spend no player moves");
                var report = new Report { passed = true, initial_specials = initialSpecials, initial_extra_coins = initialCoins,
                    final_specials = CountSpecials(), holes = holes, left_moves = game.Moves.LeftMoves, elapsed = Time.realtimeSinceStartup - startTime };
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n"); Cleanup();
            }
            catch (Exception e)
            { File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { failure = e.ToString() }) + "\n"); Cleanup(); throw; }
        }
        private static int CountSpecials()
        {
            int count = 0;
            for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
                if (board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalSpecialBoardItem item && !item.IsExploded && item.IsActive) count++;
            return count;
        }
        private static void AddSpecialFixtures()
        {
            int[] ids = { 10, 20, 30, 40, 50 }; int index = 0;
            for (int y = board.Grid.Height - 1; y >= 0 && index < ids.Length; y--)
                for (int x = 0; x < board.Grid.Width && index < ids.Length; x++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    if (!(cell.Mediation.CurrentItem is OriginalMatchBoardItem item)) continue;
                    item.Mediator.ClearFromAllRegisteredCells();
                    board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)item.SwapView);
                    game.CreateItemAtCell(ids[index++], cell);
                }
            Require(index == ids.Length, "Fixture only replaces ordinary pieces with configured original special prefabs");
        }
        private static void Cleanup() { EditorApplication.update -= Tick; GoalUiVerification.OnFinished -= GoalsFinished; }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Report
        {
            public bool passed; public string failure; public int initial_specials, initial_extra_coins, final_specials, holes, left_moves; public float elapsed;
            public string scope = "Actual formal36box goal completion through four combo fixture drags and TNT tap, then explicit GameState.Win/Session.Win fixture and configured propeller/horizontal rocket/vertical rocket/TNT/lightball replacements run production remaining-special exploder, original grid traversal/activation/fall timing, real special explosions/cascades/refill and operation completion. Does not establish production WinFail/Flow/Session binding, move replacement/coin collection/result UI, physical input or complete original rendered parity.";
        }
    }
}
