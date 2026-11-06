using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreCoinVerification
    {
        private const string ReportPath = "live_core_coin_verification.json";
        private enum Phase { Guide, Tap, Reject, InitialCombo, Playing }
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalGameAudioHost audio;
        private static OriginalGameLateUpdateDispatcher lateUpdates;
        private static readonly List<OriginalCoinBoardItem> openingCoins = new List<OriginalCoinBoardItem>(79);
        private static readonly List<OriginalCoinItemSpriteView> firstBatch = new List<OriginalCoinItemSpriteView>();
        private static readonly TouchSamples input = new TouchSamples();
        private static readonly int FlightFlip = Animator.StringToHash("Base Layer.CoinFlipNoLoopAnimation");
        private static OriginalGridCell from, to;
        private static int step, frame, movesBefore;
        private static float started, settledAt, firstFlightAt, initialCoinWidth;
        private static float previousCaptureDelta, firstFlipAt, previousFlipPhase;
        private static OriginalCoinItemSpriteView arrivalCoin;
        private static Vector3 coinGoalPosition, previousFlightPosition, lastFlightPosition;
        private static int arrivalSamples;
        private static Phase phase;
        private static Report report;

        public static void Run(FoundationController owner)
        {
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            controller = owner;
            game = owner.Gameplay;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            report = new Report();
            arrivalSamples = 0;
            Require(owner.CurrentLevelNumber == 20 && game.Session.Name == "00020v02_L" && game.Session.IsBonus &&
                owner.CoreFlow.IsBonus && game.Moves.LeftMoves == 20 && game.State.IsPlaying(), "Original numbered bonus level is playing.");
            var config = OriginalLevelReader.Load("Levels/level_20");
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                int id = config.Grid.Items[x + y * board.Grid.Width];
                Require(item != null && item.TiledId == id && item.HasView, "Every original starting tile has its model and prefab view.");
                if (item is OriginalCoinBoardItem coin)
                {
                    openingCoins.Add(coin);
                    report.initial_coins++;
                    initialCoinWidth = coin.ItemView.baseView.bounds.size.x;
                }
                if (item is OriginalTntBoardItem) report.initial_tnt++;
            }
            Require(report.initial_coins == 79 && report.initial_tnt == 2 && board.Goals.GetGoalUiLeftCount(21) == 0 &&
                board.Presentation.Pool.GetSpawnedCount(68) == 79, "The original 79 coins, two TNTs, and zero collected coins are visible.");
            var title = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().TargetTitle;
            Require(title.text == "Reward", "The original bonus goal title is visible.");
            GameSceneVerification.RequireHudTitleCurve(title);
            Capture("core_coin_level20_start.png");
            phase = Phase.Guide;
            device.Unbind();
            listener = new OriginalGameTouchListener(input, game);
            started = Time.realtimeSinceStartup;
            frame = -1;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            lateUpdates = UnityEngine.Object.FindObjectOfType<OriginalContextUpdateHost>().LateUpdates;
            lateUpdates.RegisterToLateUpdate(ObserveFirstCoinBatch);
            EditorApplication.update += Tick;
        }

        private static void ObserveFirstCoinBatch()
        {
            try
            {
                if (firstBatch.Count == 0)
                {
                    foreach (var coin in openingCoins)
                    {
                        var view = coin.ItemView;
                        if (!coin.HasView && view.gameObject.activeInHierarchy)
                            firstBatch.Add(view);
                    }
                    if (firstBatch.Count == 0) return;
                    report.first_coin_batch_size = firstBatch.Count;
                    report.first_coin_batch_frame = Time.frameCount;
                    foreach (var view in firstBatch)
                        Require(view.sortingGroup.sortingOrder < 12000,
                            "The first coin batch registers in LateUpdate before switching to collection sorting.");
                    arrivalCoin = firstBatch[0];
                    foreach (var view in firstBatch)
                        if (view.transform.position.y > arrivalCoin.transform.position.y) arrivalCoin = view;
                    coinGoalPosition = UnityEngine.Object.FindObjectOfType<OriginalGoalPanelUi>().GetGoalPosition(21);
                    return;
                }
                if (!report.coin_batch_defer_verified)
                {
                    Require(Time.frameCount == report.first_coin_batch_frame + 1,
                        "The first coin batch resumes on the following frame.");
                    foreach (var view in firstBatch)
                    {
                        Require(view.sortingGroup.sortingOrder >= 12000,
                            "Every registered coin begins its collection flight on the next frame.");
                        Require(view.animator.speed >= 1f,
                            "Coin flight starts before the first application flip callback.");
                    }
                    report.coin_batch_defer_verified = true;
                    return;
                }
                ObserveCoinArrival();
                if (report.second_flip_verified) return;

                var animator = firstBatch[0].animator;
                var state = animator.GetCurrentAnimatorStateInfo(0);
                Require(state.fullPathHash == FlightFlip && animator.speed >= .6f && animator.speed <= .7f,
                    "The application update plays the original no-loop coin flip.");
                if (report.first_flip_frame == 0)
                {
                    report.first_flip_frame = Time.frameCount;
                    Require(report.first_flip_frame == report.first_coin_batch_frame + 2,
                        "The first flip is scheduled on the update after flight starts.");
                    firstFlipAt = board.Clock.Time;
                }
                else if (state.normalizedTime < previousFlipPhase)
                {
                    report.second_flip_delay = board.Clock.Time - firstFlipAt;
                    report.flip_speed = animator.speed;
                    float expected = .2f / animator.speed;
                    Require(report.second_flip_delay >= expected - board.Clock.DeltaTime - .001f &&
                        report.second_flip_delay <= expected + .001f,
                        "The second application flip restarts the same clip at the original sequence time.");
                    report.second_flip_verified = true;
                    if (report.coin_arrival_verified) lateUpdates.UnregisterFromLateUpdate(ObserveFirstCoinBatch);
                }
                Require(board.Clock.Time - firstFlipAt < .5f, "The second flight flip must restart within the first half second.");
                previousFlipPhase = state.normalizedTime;
            }
            catch (Exception error) { report.failure = error.ToString(); Finish(); }
        }

        private static void ObserveCoinArrival()
        {
            if (report.coin_arrival_verified) return;
            if (arrivalCoin.gameObject.activeInHierarchy && arrivalCoin.sortingGroup.sortingOrder >= 12000)
            {
                previousFlightPosition = lastFlightPosition;
                lastFlightPosition = arrivalCoin.transform.position;
                arrivalSamples++;
                return;
            }
            report.arrival_previous_y_offset = previousFlightPosition.y - coinGoalPosition.y;
            report.arrival_last_y_offset = lastFlightPosition.y - coinGoalPosition.y;
            // The final frame can overshoot. The native endpoint must lie between
            // the last two rendered positions, immediately before pool recycling.
            Require(arrivalSamples >= 2 && report.arrival_previous_y_offset <= -.099f && report.arrival_last_y_offset >= -.101f &&
                Mathf.Abs(lastFlightPosition.x - coinGoalPosition.x) < .1f,
                $"The coin flight reaches the native endpoint 0.1 below the HUD anchor. " +
                $"Final Y offsets {report.arrival_previous_y_offset}, {report.arrival_last_y_offset}.");
            report.coin_arrival_verified = true;
            if (report.second_flip_verified) lateUpdates.UnregisterFromLateUpdate(ObserveFirstCoinBatch);
        }

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { Advance(); }
            catch (Exception error) { report.failure = error.ToString(); Finish(); }
        }

        private static void Advance()
        {
            Require(Time.realtimeSinceStartup - started < 440f, "Timed out in coin flow at " + phase + ", moves " + game.Moves.LeftMoves);
            if (phase == Phase.Guide)
            {
                CheckOpeningGuide();
                return;
            }
            if (step < 3)
            {
                var point = (step == 0 ? from : to).GetViewPosition();
                input.Position = new Vector2(point.x, point.y);
                input.Down = step == 0;
                input.Held = step < 2;
                input.Up = step == 2;
                listener.ManualUpdate();
                step++;
                return;
            }
            if (phase >= Phase.InitialCombo && board.Goals.GetGoalLeftCount(21) > board.Goals.GetGoalUiLeftCount(21) &&
                board.State.HasOperation(5) && !report.coin_flight_verified)
            {
                if (firstFlightAt == 0f) firstFlightAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - firstFlightAt >= .35f)
                {
                    report.coin_flight_verified = true;
                    Capture("core_coin_level20_flight.png");
                }
            }
            if (phase == Phase.Playing && game.Moves.LeftMoves == 0)
            {
                VerifyResult();
                return;
            }
            if (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            { settledAt = -1f; return; }
            if (settledAt < 0f) settledAt = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - settledAt < .35f) return;
            switch (phase)
            {
                case Phase.Tap:
                    Require(game.Moves.LeftMoves == 20 && audio.Playback.GetClipLastPlayedTime(88) > 0f, "Coin tap plays its native sound without spending a move.");
                    report.coin_tap_verified = true;
                    phase = Phase.Reject;
                    Gesture(board.Grid.GetCell(0, 8), board.Grid.GetCell(1, 8));
                    break;
                case Phase.Reject:
                    Require(game.Moves.LeftMoves == 20 && game.RejectedSwaps == 1 && game.AcceptedSwaps == 0, "Two coins without a match swap back without spending a move.");
                    report.invalid_coin_swap_verified = true;
                    phase = Phase.InitialCombo;
                    Gesture(board.Grid.GetCell(4, 4), board.Grid.GetCell(4, 5));
                    break;
                case Phase.InitialCombo:
                    Require(game.Moves.LeftMoves == 19 && game.AcceptedSwaps == 1 && report.coin_flight_verified &&
                        report.coin_batch_defer_verified &&
                        board.Goals.GetGoalLeftCount(21) == board.Goals.GetGoalUiLeftCount(21), "Preset TNT combo spends one move and settles real coin flights.");
                    report.collected_after_initial_combo = board.Goals.GetGoalUiLeftCount(21);
                    Require(report.collected_after_initial_combo > 0 && ColumnContains(0, true) && ColumnContains(8, true) &&
                        ColumnContains(3, false) && ColumnContains(4, false) && ColumnContains(5, false), "Original refill produces both coin columns and center colors.");
                    VerifyRefilledTiles();
                    report.initial_combo_and_refill_verified = true;
                    Capture("core_coin_level20_refill.png");
                    phase = Phase.Playing;
                    PlayNextMove();
                    break;
                case Phase.Playing:
                    Require(game.Moves.LeftMoves == movesBefore - 1, "One native input spent exactly one move.");
                    PlayNextMove();
                    break;
            }
        }

        private static void CheckOpeningGuide()
        {
            var flow = controller.CoreFlow;
            var guide = flow.DirectedTutorial;
            if (guide != null && guide.IsGuiding)
            {
                if (!guide.DismissButton.interactable) return;
                for (int x = 0; x < board.Grid.Width; x++)
                for (int y = 0; y < board.Grid.Height; y++)
                {
                    bool canTouch = x == 4 && (y == 4 || y == 5);
                    Require(board.Grid.GetCell(x, y).IsTouchEnabled == canTouch,
                        "The original bonus guide allows only the two authored TNT cells.");
                }
                var instruction = guide.GetComponentInChildren<TMPro.TextMeshPro>();
                Require(instruction != null && instruction.text ==
                    "This is a bonus level! Collect as many COINS as possible in given moves!",
                    "The original bonus instruction is visible.");
                var properties = new SerializedObject(guide);
                var left = properties.FindProperty("leftDialogBackground").objectReferenceValue as SpriteRenderer;
                var right = properties.FindProperty("rightDialogBackground").objectReferenceValue as SpriteRenderer;
                Require(left != null && right != null && left.transform.IsChildOf(guide.transform) &&
                    right.transform.IsChildOf(guide.transform), "The guide owns both panel halves in its prefab instance.");
                var expectedSize = OriginalCameraProjection.IsLandscape ? new Vector2(8.1f, 4.5f) : new Vector2(7.5f, 4.2f);
                Require(Mathf.Approximately(instruction.rectTransform.sizeDelta.x, expectedSize.x) &&
                    Mathf.Approximately(left.size.x, expectedSize.y) && Mathf.Approximately(right.size.x, expectedSize.y),
                    "The guide uses the original widths for the active orientation.");
                Capture("core_coin_level20_guide.png");
                CoreFlowVerification.Click(guide.DismissButton, flow.UiCamera, false);
                report.bonus_guide_verified = true;
                return;
            }
            if (flow.BlocksBoardInput) return;
            Require(report.bonus_guide_verified && game.Moves.LeftMoves == 20 &&
                board.Goals.GetGoalUiLeftCount(21) == 0,
                "Dismissing the bonus guide preserves the opening budget and reward.");
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
                Require(board.Grid.GetCell(x, y).IsTouchEnabled, "Guide dismissal restores the original board touch state.");
            phase = Phase.Tap;
            Gesture(board.Grid.GetCell(0, 8), board.Grid.GetCell(0, 8));
        }

        private static void PlayNextMove()
        {
            movesBefore = game.Moves.LeftMoves;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.Mediation.CurrentItem is OriginalSpecialBoardItem special &&
                    (!(special is OriginalLightballBoardItem) || ColumnContains(4, false)))
                {
                    Gesture(cell, cell);
                    return;
                }
            }
            Require(CoreCupboardVerification.FindNaturalSwap(board, out var source, out var destination, true),
                "A legal move is available on the live coin board at " + game.Moves.LeftMoves);
            Gesture(source, destination);
        }

        private static bool ColumnContains(int x, bool coin)
        {
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                if (coin ? item is OriginalCoinBoardItem : item is OriginalMatchBoardItem) return true;
            }
            return false;
        }

        private static void VerifyRefilledTiles()
        {
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                Require(item != null && item.HasView, $"Refilled cell ({x},{y}) has a model and view.");
                if (item is OriginalCoinBoardItem coin)
                {
                    var sprite = coin.ItemView.baseView;
                    Require(sprite.enabled && sprite.gameObject.activeInHierarchy && sprite.sprite != null &&
                        sprite.bounds.size.x >= initialCoinWidth * .9f && !coin.ItemView.IsFillMaskEnabled,
                        $"Refilled coin ({x},{y}) remains visible after falling and landing.");
                }
                report.refilled_tiles++;
            }
        }

        private static void VerifyResult()
        {
            var dialog = controller.CoreFlow.WinDialog;
            if (dialog == null || !dialog.Lifecycle.IsShowing || dialog.IsAnimationWorking) return;
            Require(game.Session.State == 3 && game.Moves.MadeMoves == 20 && controller.Progress.LevelToPlay == 21 &&
                controller.CoreFlow.FailDialog == null && !board.State.HasOperation(5), "Twenty actual moves end in the bonus win with no unfinished coin flights.");
            Require(report.second_flip_verified && report.coin_arrival_verified,
                "The level records both flight flips and the native endpoint before settlement.");
            report.result_coins = board.Goals.GetGoalUiLeftCount(21);
            Require(report.result_coins > report.collected_after_initial_combo &&
                report.result_coins == board.Goals.GetGoalLeftCount(21), "Coin target and displayed count agree in settlement.");
            using (var db = OriginalRuntimeStorage.Open(controller.Progress.DatabasePath))
                Require(db.Find("Level").Value == "21" && db.Find("Coins").Key == null, "Core SQLite stores only level progression.");
            report.moves_played = game.Moves.MadeMoves;
            Capture("core_coin_level20_win.png");
            report.passed = true;
            Finish();
        }

        private static void Gesture(OriginalGridCell source, OriginalGridCell destination)
        { from = source; to = destination; step = 0; settledAt = -1f; }
        private static void Capture(string file) => GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, file);
        private static void Require(bool condition, string detail)
        { if (!condition) throw new InvalidOperationException(detail); }
        private static void Finish()
        {
            EditorApplication.update -= Tick;
            Time.captureDeltaTime = previousCaptureDelta;
            lateUpdates.UnregisterFromLateUpdate(ObserveFirstCoinBatch);
            openingCoins.Clear();
            firstBatch.Clear();
            if (device != null && device.Listener == null) device.Bind(game);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        private sealed class TouchSamples : IOriginalGameTouchInput
        {
            public Vector2 Position;
            public bool Down, Held, Up;
            public Vector2 GetMouseWorldPosition() => Position;
            public bool GetMouseButtonDown() => Down;
            public bool GetMouseButton() => Held;
            public bool GetMouseButtonUp() => Up;
        }
        [Serializable]
        private sealed class Report
        {
            public bool passed, bonus_guide_verified, coin_tap_verified, invalid_coin_swap_verified, coin_flight_verified, initial_combo_and_refill_verified;
            public bool coin_batch_defer_verified;
            public bool second_flip_verified, coin_arrival_verified;
            public int first_coin_batch_size, first_coin_batch_frame;
            public int first_flip_frame;
            public float second_flip_delay, flip_speed, arrival_previous_y_offset, arrival_last_y_offset;
            public int initial_coins, initial_tnt, collected_after_initial_combo, refilled_tiles, moves_played, result_coins;
            public string failure;
            public string scope = "Formal GameScene seeded to level 20 in isolated local progress. Input samples drive the original preset TNT combo and legal board moves, without placing specials or forcing the move counter.";
        }
    }
}
