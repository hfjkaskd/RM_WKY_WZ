using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Explicit dead-board coverage, separate from the natural bonus route.
    // Only fourteen color pieces change; the forty-two authored coins remain.
    internal static class CoreBonusShuffleVerification
    {
        internal const string TestName = "core-bonus-shuffle";
        internal const string ReportPath = "live_core_bonus_shuffle_verification.json";
        private static FoundationController owner;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalCoinBoardItem[,] coins;
        private static Vector3[,] coinPositions;
        private static OriginalMatchBoardItem[] colorItems;
        private static Vector3[] colorPositions, lastMotionPositions;
        private static readonly CoreModalTutorialVerification.GestureSamples input = new();
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell from, to;
        private static Result result;
        private static int phase, frame = -1, operationFrame, sample, initialDraws, initialRefills;
        private static float deadline, settledAt = -1f, captureAt;

        private static State Snapshot(OriginalInitialBoardView view, OriginalPlayableBoard playable)
        {
            var state = new State { made = playable.Moves.MadeMoves, left = playable.Moves.LeftMoves,
                difficulty_seed = playable.GetLevelRandom().DifficultySeed,
                difficulty_draws = playable.GetLevelRandom().DifficultySeedGenerationCount,
                possible = Possible(view), immediate_match = view.Matches.IsThereAnyMatch(false),
                ordinary_shuffles = playable.Session.NoPossibleMoveShuffleCount,
                colored_shuffles = playable.Session.NoPossibleMoveColoredShuffleCount,
                goal = view.Goals.GetGoalLeftCount(21), rows = new string[view.Grid.Height] };
            for (int y = 0; y < view.Grid.Height; y++)
            {
                var row = new string[view.Grid.Width];
                for (int x = 0; x < view.Grid.Width; x++)
                    row[x] = (view.Grid.GetCell(x, y).Mediation.CurrentItem?.TiledId ?? -1).ToString();
                state.rows[y] = string.Join(",", row);
            }
            return state;
        }
        private static int Possible(OriginalInitialBoardView view) => view.PossibleMoves.CheckBoard(
            new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(view.Grid, view.Layout))));

        internal static void Run(FoundationController controller)
        {
            phase = sample = 0;
            frame = -1;
            settledAt = -1f;
            owner = controller;
            game = owner.Gameplay;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            result = new Result();
            Require(owner.CurrentLevelNumber == 80 && game.Session.IsBonus && board.Grid.Width == 8 && board.Grid.Height == 7,
                "Open the real level 80 bonus board.");
            coins = new OriginalCoinBoardItem[8, 7];
            coinPositions = new Vector3[8, 7];
            colorItems = new OriginalMatchBoardItem[14];
            colorPositions = new Vector3[14];
            lastMotionPositions = new Vector3[14];
            deadline = Time.realtimeSinceStartup + 45f;
            Write();
            EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { Advance(); }
            catch (Exception error) { result.failure = error.ToString(); Finish(); }
        }
        private static bool Settled() => game.Fall.IsAllItemsSteady && !board.State.HasAnyOperation() && !game.Auto.IsThereAWaitingMatch;
        private static void Advance()
        {
            Require(Time.realtimeSinceStartup < deadline, "Bonus shuffle fixture timed out at phase " + phase);
            if (phase == 0)
            {
                if (owner.CoreFlow.IsEnteringLevel || !Settled()) { settledAt = -1f; return; }
                if (settledAt < 0f) settledAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - settledAt < .3f) return;
                var views = new OriginalItemViewCreation(board);
                for (int x = 0; x < 8; x++)
                for (int y = 0; y < 7; y++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    var item = cell.Mediation.CurrentItem;
                    if (item is OriginalCoinBoardItem coin)
                    {
                        coins[x, y] = coin;
                        coinPositions[x, y] = coin.ItemView.transform.position;
                        result.coin_count++;
                    }
                    else
                    {
                        Require(item is OriginalMatchBoardItem && (x == 2 || x == 5), "Only the two seven-cell color columns may be changed.");
                        item.Mediator.ClearFromAllRegisteredCells();
                        ((OriginalMatchBoardItem)item).RecycleView();
                        views.CreateItemAtCell((y + x / 3) % 2 == 0 ? 1 : 3, cell);
                        colorItems[result.replaced_colors] = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
                        colorPositions[result.replaced_colors] = ((OriginalMatchItemSpriteView)cell.Mediation.CurrentItem.SwapView).transform.position;
                        result.replaced_colors++;
                    }
                }
                result.fixture = Snapshot(board, game);
                Require(result.coin_count == 42 && result.replaced_colors == 14 && result.fixture.possible == 0 && !result.fixture.immediate_match,
                    "The explicit color fixture has no move or match and retains all forty-two authored coins.");
                initialDraws = game.GetLevelRandom().DifficultySeedGenerationCount;
                initialRefills = game.CreatedRefills;
                board.State.Operations.OperationStart(2);
                operationFrame = board.Clock.FrameCount;
                phase = 1;
            }
            else if (phase == 1)
            {
                if (board.Clock.FrameCount - operationFrame < 12) return;
                Require(!board.State.HasOperation(6), "The normal swap operation must hold the shuffle gate.");
                board.State.Operations.OperationFinish(2);
                phase = 2;
            }
            else if (phase == 2)
            {
                if (!board.State.HasOperation(6)) return;
                Require(!game.CanUserMakeMove() && game.Moves.MadeMoves == 0 && game.Moves.LeftMoves == 20,
                    "The real runtime shuffle locks input without spending a move.");
                result.input_locked = true;
                result.shuffle = Snapshot(board, game);
                result.difficulty_draws = game.GetLevelRandom().DifficultySeedGenerationCount - initialDraws;
                captureAt = board.Clock.Time + .6f;
                phase = 3;
            }
            else if (phase == 3)
            {
                if (board.Clock.Time < captureAt) return;
                Require(board.State.HasOperation(6), "The shuffle animation remains live at the mid-motion capture.");
                int inMotion = 0;
                for (int i = 0; i < colorItems.Length; i++)
                {
                    var item = colorItems[i];
                    var current = ((OriginalMatchItemSpriteView)item.SwapView).transform.position;
                    var target = item.Mediator.CurrentCell.Owner.GetViewPosition();
                    if ((current - colorPositions[i]).sqrMagnitude > .0001f && (current - target).sqrMagnitude > .0001f) inMotion++;
                }
                if (inMotion == 0) return;
                result.moving_views_at_capture = inMotion;
                VerifyCoins();
                GrassGameplayVerification.Capture(owner.CoreFlow.UiCamera, "core_bonus80_dead_shuffle_motion.png");
                result.animation_captured = true;
                CaptureLastMotion();
                phase = 4;
            }
            else if (phase == 4)
            {
                if (board.State.HasOperation(6)) { CaptureLastMotion(); return; }
                VerifyCoins();
                result.settled = Snapshot(board, game);
                Require(game.CanUserMakeMove() && result.settled.possible == 2 && !result.settled.immediate_match &&
                    game.Moves.LeftMoves == 20 && game.Moves.MadeMoves == 0 && board.Goals.GetGoalLeftCount(21) == 0 &&
                    board.Goals.GetGoalUiLeftCount(21) == 0 && game.CreatedRefills == initialRefills,
                    "The real shuffle restores a match without collecting coins, generating refills or charging a move.");
                // Native MoveAllItems retains its last eased sample, including frame overshoot.
                for (int i = 0; i < colorItems.Length; i++)
                {
                    var position = ((OriginalMatchItemSpriteView)colorItems[i].SwapView).transform.position;
                    Require((position - lastMotionPositions[i]).sqrMagnitude < 1e-10f,
                        "Completing shuffle retains the actual final motion sample without an extra snap.");
                    result.maximum_final_cell_distance = Mathf.Max(result.maximum_final_cell_distance,
                        Vector3.Distance(position, colorItems[i].Mediator.CurrentCell.Owner.GetViewPosition()));
                }
                result.last_motion_sample_retained = true;
                foreach (var item in colorItems)
                {
                    var cell = item.Mediator.CurrentCell;
                    Require(cell != null && ReferenceEquals(cell, item.Mediator.NextCell) && ReferenceEquals(cell, item.Mediator.TargetCell) &&
                        ReferenceEquals(cell.CurrentItem, item) && ReferenceEquals(cell.NextItem, item) && ReferenceEquals(cell.TargetItem, item),
                        "All three bidirectional mediation slots follow the shuffled model.");
                }
                GrassGameplayVerification.Capture(owner.CoreFlow.UiCamera, "core_bonus80_dead_shuffle_settled.png");
                Require(CoreCupboardVerification.FindNaturalSwap(board, out from, out to, true), "A real legal follow-up gesture exists.");
                UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
                listener = new OriginalGameTouchListener(input, game);
                result.from = new Vector2Int(from.X, from.Y);
                result.to = new Vector2Int(to.X, to.Y);
                result.coin_identities_preserved = true;
                phase = 5;
                settledAt = -1f;
            }
            else if (phase == 5)
            {
                if (sample < 3)
                {
                    input.Set(sample == 0 ? from : to, sample++);
                    listener.ManualUpdate();
                    return;
                }
                if (!Settled()) { settledAt = -1f; return; }
                if (settledAt < 0f) settledAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - settledAt < .3f) return;
                Require(game.Moves.MadeMoves == 1 && game.Moves.LeftMoves == 19 && game.CreatedRefills > initialRefills,
                    "The restored board accepts the ordinary touch swap, spends one move and refills.");
                result.after_touch = Snapshot(board, game);
                result.refills_after_touch = game.CreatedRefills - initialRefills;
                result.passed = true;
                GrassGameplayVerification.Capture(owner.CoreFlow.UiCamera, "core_bonus80_after_touch.png");
                Finish();
            }
        }
        private static void CaptureLastMotion()
        {
            for (int i = 0; i < colorItems.Length; i++)
                lastMotionPositions[i] = ((OriginalMatchItemSpriteView)colorItems[i].SwapView).transform.position;
        }
        private static void VerifyCoins()
        {
            for (int x = 0; x < 8; x++)
            for (int y = 0; y < 7; y++)
            {
                var current = board.Grid.GetCell(x,y).Mediation.CurrentItem as OriginalCoinBoardItem;
                Require(ReferenceEquals(coins[x,y],current) && (current == null || (current.HasView &&
                    (current.ItemView.transform.position - coinPositions[x,y]).sqrMagnitude < .0001f)),
                    "Every authored coin retains its model identity, view and world position through shuffle.");
            }
        }
        private static void Require(bool condition, string detail) { if (!condition) throw new InvalidOperationException(detail); }
        private static void Write() => File.WriteAllText(ReportPath, JsonUtility.ToJson(result, true) + "\n");
        private static void Finish() { EditorApplication.update -= Tick; result.status = "finished"; Write(); }
        [Serializable] private sealed class State
        {
            public int made, left, difficulty_seed, difficulty_draws, possible, ordinary_shuffles, colored_shuffles, goal;
            public bool immediate_match;
            public string[] rows;
        }
        [Serializable] private sealed class Result
        {
            public string status = "running", failure;
            public bool passed, input_locked, animation_captured, coin_identities_preserved, last_motion_sample_retained;
            public float maximum_final_cell_distance;
            public int coin_count, replaced_colors, difficulty_draws, refills_after_touch, moving_views_at_capture;
            public Vector2Int from, to;
            public State fixture, shuffle, settled, after_touch;
            public string scope = "Explicit fourteen-color dead-board fixture on the authored level 80 geometry. Forty-two coins remain untouched. The actual production finder, gate, shuffle, views and subsequent normal touch gesture execute. This is not an unmodified authored-board attempt or original rendered-frame comparison.";
        }
    }
}
