using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreNaturalAttemptVerification
    {
        internal const string ReportPath = "live_core_natural_attempts_verification.json";
        private const int CoinGoal = 21;
        private static readonly CoreModalTutorialVerification.GestureSamples Input = new();
        private static readonly HashSet<OriginalGridCell> InitialVacancies = new();
        private static FoundationController controller;
        private static OriginalPlayableBoard game;
        private static OriginalInitialBoardView board;
        private static OriginalGameTouchListener listener;
        private static CoreNaturalMoveSelection selection;
        private static OriginalGoalTuple[] goals;
        private static OriginalGridCell from, to, hammerTarget;
        private static int hammerGoalsBefore, hammerRefillsBefore;
        private static Report report;
        private static Attempt attempt;
        private static Move move;
        private static int index, frame, sample, previousOwner, expectedReload, refillsBefore, swapsBefore, rejectedBefore;
        private static OriginalPlayableBoard previousAttempt;
        private static float deadline, settledAt, afterResultAt, nextSelectionAt;
        private static bool checkingResultButton, reopeningCompletion;
        private static bool hasHoneyGoal, hasBushGoal;

        internal static void Run(FoundationController owner)
        {
            report = new Report { requested_levels = CoreAuthoredOpeningVerification.ReadRequestedLevels(1, allowRetries: true) };
            index = 0;
            frame = -1;
            Begin(owner);
            Save();
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            game = owner.Gameplay;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            int level = report.requested_levels[index];
            var config = OriginalLevelReader.Load("Levels/level_" + level);
            Require(owner.CurrentLevelNumber == level && game.Session.Name == config.Name &&
                game.Moves.LeftMoves == config.Move && game.Moves.MadeMoves == 0 && game.State.IsPlaying(),
                "A fresh authored level is playing with its original move budget.");
            goals = board.Goals.GetFinalGoals();
            hasHoneyGoal = Array.Exists(goals, goal => goal.GoalType == OriginalHoneyBoardItem.HoneyGoal);
            hasBushGoal = Array.Exists(goals, goal => goal.GoalType == 22);
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, game);
            selection = new CoreNaturalMoveSelection(board);
            attempt = new Attempt
            {
                level = level, session_name = config.Name, initial_budget = config.Move,
                level_try_count = owner.Progress.LevelTryCount, difficulty_seed = game.GetLevelRandom().DifficultySeed,
                entered_from_previous_result = checkingResultButton
            };
            report.attempts.Add(attempt);
            ObserveGrass(0);
            ObserveHoney(0);
            move = null;
            hammerTarget = null;
            InitialVacancies.Clear();
            deadline = Time.realtimeSinceStartup + 200f;
            settledAt = -1f;
            afterResultAt = -1f;
            nextSelectionAt = 0f;
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
            float now = Time.realtimeSinceStartup;
            Require(now < deadline, "Timed out in an authored attempt or scene reload.");
            if (controller == null)
            {
                ObserveReload();
                return;
            }
            if (move != null && sample < 3)
            {
                Input.Set(sample == 0 ? from : to ?? from, sample);
                listener.ManualUpdate();
                sample++;
                return;
            }

            bool playing = game.State.IsPlaying() && game.Moves.LeftMoves > 0;
            bool steady = game.Fall.IsAllItemsSteady && !board.State.HasAnyOperation() && !game.Auto.IsThereAWaitingMatch;
            if (!steady) settledAt = -1f;
            else if (settledAt < 0f) settledAt = now;
            bool settled = settledAt >= 0f && now - settledAt >= .30f;
            if (move != null)
            {
                if (!settled && playing) return;
                ObserveMove(settled, playing);
                return;
            }
            if (!playing)
            {
                ObserveOutcome(now);
                return;
            }

            var flow = controller.CoreFlow;
            var modal = flow.ModalTutorial;
            if (modal != null && modal.BlocksBoardInput)
            {
                if (modal.DismissButton.interactable) CoreFlowVerification.Click(modal.DismissButton, flow.UiCamera);
                return;
            }
            bool opening = attempt.moves.Count == 0;
            bool grassOpening = opening && attempt.level == 4;
            bool ballBallOpening = opening && attempt.level == 28;
            bool owlOpening = opening && attempt.level == 61;
            var directed = flow.DirectedTutorial;
            bool focusedGuide = directed != null && directed.IsGuiding && directed.FocusCell != null;
            if (!grassOpening && !ballBallOpening && !owlOpening && !focusedGuide && directed != null &&
                directed.IsGuiding && directed.DismissButton != null)
            {
                if (directed.DismissButton.interactable) CoreFlowVerification.Click(directed.DismissButton, flow.UiCamera);
                return;
            }
            if (!settled || game.IsUiTouching || !game.CanUserMakeMove() || now < nextSelectionAt) return;
            if (attempt.level == 8 && !attempt.free_hammer_used)
            {
                AdvanceHammerGuide(flow);
                return;
            }
            if (attempt.level == 14 && !attempt.free_arrow_used)
            {
                attempt.free_arrow_used = AdvanceLineGuide(flow, OriginalBoosterType.Arrow, 3, 6);
                return;
            }
            if (attempt.level == 17 && !attempt.free_cannon_used)
            {
                attempt.free_cannon_used = AdvanceLineGuide(flow, OriginalBoosterType.Cannon, 6, 4);
                return;
            }
            if (attempt.level == 19 && !attempt.free_jester_used)
            {
                AdvanceJesterGuide(flow);
                return;
            }
            nextSelectionAt = now + .2f;
            if (opening && attempt.level == 14)
            {
                Require(board.Goals.GetGoalLeftCount(11) == 0 && game.Moves.LeftMoves == attempt.initial_budget,
                    "The free Arrow clears its box row before any regular moves.");
                attempt.free_arrow_row_cleared = true;
            }
            if (opening && attempt.level == 17)
            {
                Require(board.Goals.GetGoalLeftCount(11) <= 54 && game.Moves.LeftMoves == attempt.initial_budget,
                    "The free Cannon clears its box column before any regular moves.");
                attempt.free_cannon_column_cleared = true;
            }
            bool guided = focusedGuide || (opening && attempt.level == 1) || grassOpening || ballBallOpening;
            bool rocketOpening = opening && (attempt.level == 50 || owlOpening);
            bool tntOpening = opening && attempt.level == 74;
            bool hinted = !guided && !rocketOpening && !tntOpening && (opening || (attempt.level == 1 && attempt.moves.Count == 1));
            if (tntOpening)
            {
                // The authored board opens with a lone TNT, past the native tap hint cutoff.
                from = board.Grid.GetCell(4, 4);
                to = null;
                Require(OriginalCellTap.CanTapCurrentItem(from, out _) && from.Mediation.CurrentItem is OriginalTntBoardItem,
                    "The original level 74 opens with a tappable TNT at its center.");
            }
            else if (rocketOpening)
            {
                if (owlOpening && (directed == null || !directed.IsGuiding)) return;
                from = owlOpening ? board.Grid.GetCell(0, 9) : board.Grid.GetCell(8, 1);
                to = null;
                Require(from.IsTouchEnabled && from.Mediation.CurrentItem is OriginalRocketBoardItem rocket && rocket.Orientation == 1,
                    "The original opening exposes its authored vertical rocket.");
            }
            else if (guided)
            {
                if (directed == null || !directed.IsGuiding) return;
                if (focusedGuide)
                {
                    from = directed.FocusCell;
                    to = directed.SecondCell;
                }
                else if (grassOpening)
                {
                    if (!selection.TryChoose(out from, out to)) return;
                }
                else
                {
                    var profile = Array.Find(flow.Configuration.directedTutorials, p => p.level == attempt.level);
                    Require(profile?.inputCells?.Length == 2, "The formal opening guide specifies one directed swap.");
                    from = board.Grid.GetCell(profile.inputCells[0].x, profile.inputCells[0].y);
                    to = board.Grid.GetCell(profile.inputCells[1].x, profile.inputCells[1].y);
                }
            }
            else if (hinted || !selection.TryChoose(out from, out to))
            {
                from = flow.MatchHints.ActiveSource;
                to = flow.MatchHints.ActiveDestination;
                if (from == null) return;
                hinted = true;
            }
            if (opening)
                foreach (var cell in EmptyCells()) InitialVacancies.Add(cell);
            sample = 0;
            settledAt = -1f;
            refillsBefore = game.CreatedRefills;
            swapsBefore = game.AcceptedSwaps;
            rejectedBefore = game.RejectedSwaps;
            var fromItem = from.Mediation.CurrentItem;
            var toItem = to?.Mediation.CurrentItem;
            move = new Move
            {
                from = new Vector2Int(from.X, from.Y),
                to = to == null ? new Vector2Int(from.X, from.Y) : new Vector2Int(to.X, to.Y),
                spent_from = game.Moves.LeftMoves,
                is_tap = to == null,
                is_single_power_swap = to != null && ((fromItem is OriginalSpecialBoardItem) !=
                    (toItem is OriginalSpecialBoardItem)),
                from_item = fromItem?.TiledId ?? 0,
                to_item = toItem?.TiledId ?? 0,
                input_source = tntOpening ? "authored_tnt" : rocketOpening ? "authored_rocket" :
                    focusedGuide ? "power_tutorial" : guided ? "tutorial" : hinted ? "hint" : "board",
            };
        }

        private static void AdvanceHammerGuide(CoreGameFlow flow)
        {
            var guide = flow.HammerTutorial;
            if (hammerTarget != null)
            {
                Require(guide != null && !guide.IsGuiding && !game.IsBoosterSelected &&
                    game.Moves.MadeMoves == 0 && game.Moves.LeftMoves == attempt.initial_budget &&
                    board.Goals.GetGoalLeftCount(11) < hammerGoalsBefore &&
                    hammerTarget.Mediation.CurrentItem is OriginalMatchBoardItem &&
                    game.CreatedRefills > hammerRefillsBefore,
                    "The tutorial hammer hits its target and refills the board without spending a move.");
                attempt.hammer_refills = game.CreatedRefills - hammerRefillsBefore;
                attempt.hammer_boxes_removed = hammerGoalsBefore - board.Goals.GetGoalLeftCount(11);
                attempt.free_hammer_used = true;
                Save();
                return;
            }
            if (guide == null || !guide.IsGuiding) return;
            if (!guide.IsSelected)
            {
                SelectFreeBooster(OriginalBoosterType.Hammer, flow.UiCamera);
                Require(guide.IsSelected && game.IsBoosterSelected, "The hammer Button selects the real tutorial tool.");
                return;
            }

            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!cell.IsTouchEnabled) continue;
                Require(hammerTarget == null, "The hammer tutorial exposes exactly one world-space cell.");
                hammerTarget = cell;
            }
            Require(hammerTarget?.Mediation.CurrentItem is OriginalBoxBoardItem && hammerTarget.X == 4 &&
                hammerTarget.Y == 6, "The real tutorial highlights the authored box.");
            hammerGoalsBefore = board.Goals.GetGoalLeftCount(11);
            hammerRefillsBefore = game.CreatedRefills;
            TapBoosterCell(hammerTarget);
            Require(board.State.HasOperation(7), "Tapping the highlighted box starts the hammer animation.");
            settledAt = -1f;
        }

        private static bool AdvanceLineGuide(CoreGameFlow flow, OriginalBoosterType type, int x, int y)
        {
            var guide = flow.OptionalBoosterTutorial;
            if (guide == null || !guide.IsGuiding) return false;
            if (!guide.IsSelected)
            {
                if (!guide.DismissButton.interactable) return false;
                SelectFreeBooster(type, flow.UiCamera);
                Require(guide.IsSelected && game.IsBoosterSelected, "The standard Button selects the real free tool.");
                return false;
            }

            var target = board.Grid.GetCell(x, y);
            Require(target.IsTouchEnabled && target.Mediation.CurrentItem is OriginalBoxBoardItem,
                "The line booster guide selects its authored box target.");
            TapBoosterCell(target);
            Require(!guide.IsGuiding && !guide.IsSelected && board.State.HasOperation(7) &&
                game.Moves.MadeMoves == 0 && game.Moves.LeftMoves == attempt.initial_budget,
                "The real line booster gesture begins without spending a player move.");
            settledAt = -1f;
            return true;
        }

        private static void SelectFreeBooster(OriginalBoosterType type, Camera camera)
        {
            var bottom = controller.GetComponentInChildren<OriginalBottomUi>();
            var slot = Array.Find(bottom.Boosters, candidate => candidate.BoosterType == type);
            Require(slot != null && slot.Button.interactable, "The authored free booster Button is ready.");
            CoreFlowVerification.Click(slot.Button, camera, false);
        }

        private static void AdvanceJesterGuide(CoreGameFlow flow)
        {
            var guide = flow.OptionalBoosterTutorial;
            if (guide == null || !guide.IsGuiding) return;
            if (!guide.IsSelected)
            {
                if (!guide.DismissButton.interactable) return;
                SelectFreeBooster(OriginalBoosterType.JesterHat, flow.UiCamera);
                Require(guide.IsSelected && guide.ConfirmationButton.isActiveAndEnabled,
                    "The Jester Hat Button shows the real Shuffle Button.");
                return;
            }

            if (!guide.ConfirmationButton.interactable) return;
            CoreFlowVerification.Click(guide.ConfirmationButton, flow.UiCamera, false);
            Require(!guide.IsSelected && board.State.HasOperation(6) && board.State.HasOperation(7) &&
                game.Moves.MadeMoves == 0 && game.Moves.LeftMoves == attempt.initial_budget,
                "The Shuffle Button starts the free Jester effect without spending a player move.");
            attempt.free_jester_used = true;
            settledAt = -1f;
            Save();
        }

        private static void TapBoosterCell(OriginalGridCell cell)
        {
            Input.Set(cell, 0);
            listener.ManualUpdate();
            Input.Set(cell, 2);
            listener.ManualUpdate();
        }

        private static void ObserveMove(bool settled, bool playing)
        {
            int expected = attempt.moves.Count + 1;
            Require(game.Moves.MadeMoves == expected && game.AcceptedSwaps == swapsBefore + (move.is_tap ? 0 : 1) &&
                game.RejectedSwaps == rejectedBefore,
                $"Gesture {expected} ({move.from} to {move.to}) must be accepted and spend exactly one player move.");
            if (playing) Require(game.Moves.LeftMoves == move.spent_from - 1, "The live budget decreases by one.");
            move.refills = game.CreatedRefills - refillsBefore;
            move.goals_left = CountGoalsLeft();
            move.settled = settled;
            if (attempt.level == 28 && expected == 1)
            {
                Require(settled && !move.is_tap && move.input_source == "tutorial" &&
                    game.Session.SpecialItemCounts.Combos.TryGetValue(1, out int count) && count == 1 &&
                    board.Goals.GetGoalLeftCount(11) == 0 && move.refills > 0 && !game.BallBallCombo.IsComboActive,
                    "The original guided lightball swap clears the box layer, refills and unlocks the board.");
                attempt.double_lightball_opening = true;
            }
            if (attempt.level == 50 && expected == 1)
            {
                var eggs = Array.Find(goals, goal => goal.GoalType == 16);
                attempt.authored_rocket_eggs_removed = eggs.TargetCount - eggs.LeftCount;
                Require(settled && move.is_tap && attempt.authored_rocket_eggs_removed >= 8 && move.refills > 0,
                    "The authored rocket clears the eight eggs in its column and triggers refill.");
            }
            if (attempt.level == 61 && expected == 1)
            {
                var owls = Array.Find(goals, goal => goal.GoalType == 17);
                attempt.authored_rocket_owls_removed = owls.TargetCount - owls.LeftCount;
                var guide = controller.CoreFlow.DirectedTutorial;
                Require(settled && move.is_tap && attempt.authored_rocket_owls_removed == 9 && move.refills > 0 &&
                    (guide == null || !guide.IsGuiding), "The highlighted rocket clears nine owls, refills and ends the guide.");
            }
            if (attempt.level == 74 && expected == 1)
                Require(settled && move.input_source == "authored_tnt" && move.is_tap && move.refills > 0,
                    "The authored TNT opens the blocked center and refills it after one move.");
            attempt.moves.Add(move);
            if (settled)
            {
                ObserveGrass(expected);
                ObserveHoney(expected);
            }
            if (settled && expected == 1)
            {
                foreach (var cell in EmptyCells())
                    if (!InitialVacancies.Contains(cell))
                        attempt.opening_vacancies.Add(new Vacancy { cell = new Vector2Int(cell.X, cell.Y) });
            }
            if (settled)
            {
                foreach (var vacancy in attempt.opening_vacancies)
                {
                    if (vacancy.filled_at_move != 0) continue;
                    if (board.Grid.GetCell(vacancy.cell.x, vacancy.cell.y).Mediation.CurrentItem != null)
                        vacancy.filled_at_move = expected;
                }
            }
            move = null;
            Save();
        }

        private static void ObserveOutcome(float now)
        {
            var flow = controller.CoreFlow;
            Button button;
            string outcome;
            int nextLevel;
            if (flow.WinDialog != null && flow.WinDialog.Lifecycle.IsShowing && !flow.WinDialog.IsAnimationWorking)
            {
                bool lastLevel = attempt.level == CoreLevelCatalog.SupportedLevelCount;
                Require(game.State.IsWin() && game.Session.State == 3 && CountGoalsLeft() == 0 &&
                    controller.Progress.LevelToPlay == Math.Min(attempt.level + 1, CoreLevelCatalog.SupportedLevelCount) &&
                    controller.Progress.IsComplete == lastLevel,
                    "The actual win commits progress and displays the result.");
                if (game.Session.IsBonus)
                {
                    attempt.bonus_coins = board.Goals.GetGoalLeftCount(CoinGoal);
                    Require(game.Moves.LeftMoves == 0 && attempt.bonus_coins > 0 &&
                        attempt.bonus_coins == board.Goals.GetGoalUiLeftCount(CoinGoal) && !board.State.HasOperation(5),
                        "The authored bonus board spends its full budget and settles its accumulated coins.");
                }
                button = flow.WinDialog.ContinueButton;
                outcome = "win";
                nextLevel = lastLevel ? 1 : attempt.level + 1;
            }
            else if (flow.FailDialog != null && flow.FailDialog.Lifecycle.IsShowing)
            {
                Require(game.State.IsFail() && game.Session.State == 4 && game.Moves.LeftMoves == 0 &&
                    controller.Progress.LevelToPlay == attempt.level && CountGoalsLeft() > 0,
                    "Spending the real budget triggers the failure result and keeps the current level.");
                button = flow.FailDialog.RetryButton;
                outcome = "fail";
                nextLevel = attempt.level;
            }
            else if (flow.NoPossibleMovesDialog != null && flow.NoPossibleMovesDialog.Lifecycle.IsShowing)
            {
                Require(game.State.IsNoPossibleMove() && controller.Progress.LevelToPlay == attempt.level,
                    "The real no-move result keeps the current level.");
                button = flow.NoPossibleMovesDialog.RestartButton;
                outcome = "no_moves";
                nextLevel = attempt.level;
            }
            else return;
            if (afterResultAt < 0f) afterResultAt = now;
            if (now - afterResultAt < .30f || !button.interactable) return;
            Require(attempt.moves.Count == game.Moves.MadeMoves, "Every spent move came from a recorded gesture.");
            attempt.outcome = outcome;
            attempt.goals_left = CountGoalsLeft();
            ObserveGrass(attempt.moves.Count);
            ObserveHoney(attempt.moves.Count);
            foreach (var goal in goals)
                if (goal.GoalType != CoinGoal)
                    attempt.goals_at_result.Add(new Goal { type = goal.GoalType, left = goal.LeftCount });
            if (attempt.level == 99) ObserveRemainingCupboards();
            foreach (var combo in game.Session.SpecialItemCounts.Combos)
                attempt.combos_at_result.Add(new Combo { type = combo.Key, count = combo.Value });
            attempt.result_coins = game.Session.ExtraCoins;
            attempt.moves_left_on_win = outcome == "win" ? game.Session.LeftMoves : 0;
            GrassGameplayVerification.Capture(flow.UiCamera, $"core_natural_{attempt.level}_{outcome}.png");
            previousOwner = controller.GetInstanceID();
            previousAttempt = game;
            expectedReload = nextLevel;
            checkingResultButton = true;
            deadline = now + 30f;
            if (outcome == "win" && attempt.level == CoreLevelCatalog.SupportedLevelCount)
            {
                reopeningCompletion = true;
                SceneManager.LoadSceneAsync(controller.gameObject.scene.path, LoadSceneMode.Single);
            }
            else CoreFlowVerification.Click(button, flow.UiCamera, false);
            controller = null;
            Save();
        }

        private static void ObserveReload()
        {
            var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
            if (reopeningCompletion)
            {
                ObserveReopenedCompletion(owner);
                return;
            }
            if (owner == null || owner.Gameplay == null || ReferenceEquals(owner.Gameplay, previousAttempt)) return;
            bool keptScene = owner.GetInstanceID() == previousOwner;
            Require(keptScene == (checkingResultButton && attempt.outcome != "win"),
                "Retry keeps the current scene, and Continue enters the next scene.");
            if (!checkingResultButton) { Begin(owner); return; }
            Require(owner.CurrentLevelNumber == expectedReload && owner.Gameplay.State.IsPlaying() &&
                owner.Gameplay.Moves.MadeMoves == 0, "The visible result button opens a fresh playable board.");
            if (expectedReload == 1)
                Require(!owner.Progress.IsComplete, "The level 100 result resets progress for the new run.");
            attempt.reloaded_level = expectedReload;
            // Duplicate entries reserve retries. A real win advances past any unused slots.
            do { index++; }
            while (attempt.outcome == "win" && index < report.requested_levels.Length &&
                report.requested_levels[index] == attempt.level);
            if (index == report.requested_levels.Length)
            {
                report.passed = true;
                Finish();
                return;
            }
            Save();
            int next = report.requested_levels[index];
            if (next == expectedReload) { Begin(owner); return; }
            previousOwner = owner.GetInstanceID();
            previousAttempt = owner.Gameplay;
            checkingResultButton = false;
            owner.Progress.CompleteLevel(next - 1);
            SceneManager.LoadSceneAsync(owner.gameObject.scene.path, LoadSceneMode.Single);
        }

        private static void ObserveReopenedCompletion(FoundationController owner)
        {
            if (owner == null || owner.GetInstanceID() == previousOwner || !owner.CoreFlow.IsInitialized) return;
            var flow = owner.CoreFlow;
            Require(owner.Progress.IsComplete && owner.CurrentLevelNumber == CoreLevelCatalog.SupportedLevelCount &&
                owner.Gameplay == null && UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>() == null,
                "Reopening completed progress preserves level 100 without creating another attempt or HUD.");
            var result = flow.WinDialog;
            Require(result != null && result.Lifecycle.IsShowing && flow.BlocksBoardInput,
                "The production scene opens its saved completion result.");
            if (result.IsAnimationWorking) return;
            CoreFlowVerification.RequireCurvedTitle(result, flow.Configuration.completedTitle, 2.3f);
            Require(result.ContinueButton.GetComponentInChildren<TMP_Text>().text == flow.Configuration.completedContinueText,
                "The restored result still offers Play Again.");
            Require(!File.Exists(owner.Progress.DatabasePath + ".level"), "The completed run has no resumable board.");
            GameSceneVerification.RequireGameBackground(GameSceneVerification.CastleGardenBackgroundGuid);
            GrassGameplayVerification.Capture(flow.UiCamera, "core_natural_100_reopened_completion.png");
            attempt.reopened_completion = true;
            previousOwner = owner.GetInstanceID();
            reopeningCompletion = false;
            CoreFlowVerification.Click(result.ContinueButton, flow.UiCamera, false);
        }

        private static void ObserveRemainingCupboards()
        {
            int plates = 0;
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                if (board.Grid.GetCell(x, y).Mediation.CurrentItem is not OriginalCupboardBoardItem cupboard) continue;
                attempt.cupboards_at_result.Add(new Cupboard
                {
                    cell = new Vector2Int(x, y), plates = cupboard.PlateCount, has_door = cupboard.HasDoor
                });
                plates += cupboard.PlateCount;
            }
            Require(plates == board.Goals.GetGoalLeftCount(15),
                "Remaining cupboard plates on level 99 match the settled goal count.");
        }

        private static int CountGoalsLeft()
        {
            int count = 0;
            foreach (var goal in goals)
                if (goal.GoalType != CoinGoal) count += goal.LeftCount;
            return count;
        }

        private static void ObserveGrass(int spent)
        {
            if (!hasBushGoal && attempt.level != 4 && attempt.level != 27) return;
            if (hasBushGoal)
                Require(game.Pool.HasPool(40) && game.Pool.GetSpawnedCount(64) == 0,
                    "Grass pools are initialized and every spread animation has landed on the settled board.");
            var sample = new GrassSample { moves_spent = spent, goal_left = board.Goals.GetGoalLeftCount(13) };
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.StaticTouch.GetStaticItem(2) is not OriginalGrassBoardItem grass) continue;
                Require(grass.HasView, "Grass has a live board view.");
                sample.remaining.Add(new GrassCell
                {
                    cell = new Vector2Int(x, y),
                    layers = grass.LayerCount,
                    blocker_type = cell.Mediation.CurrentItem is { IsObstacle: true } blocker ? blocker.ItemType : 0,
                });
            }
            Require(sample.remaining.Count == sample.goal_left && sample.goal_left == board.Goals.GetGoalUiLeftCount(13),
                "The remaining grass cells, goal model and HUD count agree.");
            attempt.grass.Add(sample);
        }

        private static IEnumerable<OriginalGridCell> EmptyCells()
        {
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.IsNormalCell && cell.Mediation.CurrentItem == null) yield return cell;
            }
        }

        private static void ObserveHoney(int spent)
        {
            if (!hasHoneyGoal) return;
            var state = new HoneySample { moves_spent = spent };
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.StaticTouch.GetStaticItem(OriginalHoneyBoardItem.StaticType) is not OriginalHoneyBoardItem cover)
                    continue;
                Require(cover.HasView && cover.IsTouchBlocking && !cover.DoesBlockFall &&
                    !OriginalCellTouchRules.CanSwapFrom(cell, board.SwapAvailability) &&
                    !OriginalCellTouchRules.CanSwapTo(cell, board.SwapAvailability) &&
                    !OriginalLightballTargeting.IsReadyForLightball(cell),
                    $"Live honey at ({x},{y}) blocks swaps and direct lightball targeting while allowing fall.");
                state.remaining++;
                if (cell.Mediation.CurrentItem == null) state.without_underlay++;
            }
            const int goalType = OriginalHoneyBoardItem.HoneyGoal;
            Require(state.remaining == board.Goals.GetGoalLeftCount(goalType) &&
                state.remaining == board.Goals.GetGoalUiLeftCount(goalType), "The honey covers, goal model and HUD count agree.");
            attempt.honey.Add(state);
        }

        private static void Require(bool value, string detail)
        {
            if (!value) throw new InvalidOperationException($"Level {attempt?.level ?? report.requested_levels[index]}. {detail}");
        }
        private static void Save() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        private static void Finish()
        {
            EditorApplication.update -= Tick;
            report.status = "finished";
            Save();
        }

        [Serializable] private sealed class Move
        {
            public Vector2Int from, to;
            public bool is_tap, settled, is_single_power_swap;
            public string input_source;
            public int spent_from, refills, goals_left, from_item, to_item;
        }
        [Serializable] private sealed class Vacancy
        {
            public Vector2Int cell;
            public int filled_at_move;
        }
        [Serializable] private sealed class Goal
        {
            public int type, left;
        }
        [Serializable] private sealed class Cupboard
        {
            public Vector2Int cell;
            public int plates;
            public bool has_door;
        }
        [Serializable] private sealed class Combo
        {
            public int type, count;
        }
        [Serializable] private sealed class GrassCell
        {
            public Vector2Int cell;
            public int layers, blocker_type;
        }
        [Serializable] private sealed class GrassSample
        {
            public int moves_spent, goal_left;
            public List<GrassCell> remaining = new();
        }
        [Serializable] private sealed class Attempt
        {
            public int level, initial_budget, goals_left, reloaded_level, result_coins, moves_left_on_win, bonus_coins;
            public int authored_rocket_eggs_removed, authored_rocket_owls_removed;
            public int level_try_count, difficulty_seed;
            public string session_name, outcome;
            public bool reopened_completion, entered_from_previous_result, free_hammer_used, free_arrow_used, free_arrow_row_cleared;
            public bool free_cannon_used, free_cannon_column_cleared, free_jester_used;
            public bool double_lightball_opening;
            public int hammer_refills, hammer_boxes_removed;
            public List<Move> moves = new();
            public List<Vacancy> opening_vacancies = new();
            public List<Goal> goals_at_result = new();
            public List<Combo> combos_at_result = new();
            public List<Cupboard> cupboards_at_result = new();
            public List<GrassSample> grass = new();
            public List<HoneySample> honey = new();
        }
        [Serializable] private sealed class HoneySample
        {
            public int moves_spent, remaining, without_underlay;
        }
        [Serializable] private sealed class Report
        {
            public string status = "running", failure;
            public bool passed;
            public int[] requested_levels;
            public List<Attempt> attempts = new();
        }
    }
}
