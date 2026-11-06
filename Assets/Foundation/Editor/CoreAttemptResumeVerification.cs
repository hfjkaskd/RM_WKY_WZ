using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreAttemptResumeVerification
    {
        internal const string ReportPath = "live_core_attempt_resume_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Samples = new();
        private static FoundationController owner;
        private static OriginalPlayableBoard game;
        private static OriginalInitialBoardView board;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell from, to;
        private static string expected, file, sampleFile;
        private static string expectedStatistics;
        private static int phase, sample, frame, firstMoves, cursor, seed, attemptCount, recoverySeed;
        private static int restoredGoalChecks;
        private static float deadline, steadySince;
        private static bool optionalReady, corruptionRejected, statisticsRestored;
        private static bool useArrowBeforeSave, arrowUsedBeforeSave;

        internal static int ReadLevel()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-recoveryResumeLevel");
            return index >= 0 ? int.Parse(arguments[index + 1]) : 3;
        }

        internal static void Run(FoundationController controller)
        {
            phase = 0;
            frame = -1;
            deadline = Time.realtimeSinceStartup + 40f;
            Bind(controller);
            Require(game.Moves.MadeMoves == 0, "The private progress file starts fresh.");
            file = owner.Progress.DatabasePath + ".level";
            sampleFile = Path.GetFullPath("core_attempt_resume_sample.bytes");
            optionalReady = corruptionRejected = statisticsRestored = false;
            restoredGoalChecks = 0;
            arrowUsedBeforeSave = false;
            useArrowBeforeSave = Array.IndexOf(Environment.GetCommandLineArgs(), "-recoveryResumeSpentArrow") >= 0;
            Require(!useArrowBeforeSave || ReadLevel() == 14, "The spent Arrow restore case uses level 14.");
            EditorApplication.update += Tick;
        }

        private static void Bind(FoundationController controller)
        {
            owner = controller;
            game = owner.Gameplay;
            board = owner.GetComponentInChildren<OriginalInitialBoardView>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Samples, game);
            sample = 0;
            steadySince = -1f;
        }

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { Step(); }
            catch (Exception error) { Finish(false, error.ToString()); }
        }

        private static void Step()
        {
            Require(Time.realtimeSinceStartup < deadline, "Save or resume timed out.");
            if (phase == 2 || phase == 5)
            {
                if (owner != null) return;
                var loaded = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (loaded?.Gameplay == null) return;
                if (phase == 2 && optionalReady && loaded.CoreFlow.OptionalBoosterTutorial == null) return;
                Bind(loaded);
                if (phase == 5)
                {
                    Require(board.InitialPopulationAttempts > 0 && game.Moves.MadeMoves == 0 &&
                        game.Moves.LeftMoves == game.Session.OriginalMoveCount,
                        "A truncated snapshot is discarded before initializing the authored board.");
                    recoverySeed = game.GetLevelRandom().DifficultySeed;
                    Require(owner.Progress.LevelTryCount == attemptCount + 1 && recoverySeed != seed,
                        "A rejected snapshot starts a new attempt with the next difficulty seed.");
                    corruptionRejected = true;
                    Finish(true, "");
                    return;
                }
                var difficulty = game.GetLevelRandom();
                Require(board.InitialPopulationAttempts == 0 && Signature() == expected &&
                    owner.Progress.LevelTryCount == attemptCount && difficulty.DifficultySeed == seed &&
                    difficulty.DifficultySeedGenerationCount == cursor,
                    "Reopened scene restores the saved board, goals, budget, attempt count, seed and difficulty cursor.");
                Require(StatisticsSignature() == expectedStatistics,
                    "The reopened board carries the previous move, hint, item and combo counters.");
                statisticsRestored = true;
                VerifyRestoredGoalChecks();
                if (optionalReady)
                    Require(owner.CoreFlow.OptionalBoosterTutorial != null && !owner.CoreFlow.OptionalBoosterTutorial.IsGuiding,
                        "The unused optional booster is rebound without replaying its prompt.");
                if (useArrowBeforeSave)
                    Require(restoredGoalChecks == 1 && owner.CoreFlow.OptionalBoosterTutorial == null && game.State.IsPlaying(),
                        "The spent Arrow and completed box goal reopen directly into the board.");
                phase = 3;
            }

            var modal = owner.CoreFlow.ModalTutorial;
            if (modal != null && modal.BlocksBoardInput)
            {
                if (modal.DismissButton.interactable) CoreFlowVerification.Click(modal.DismissButton, owner.CoreFlow.UiCamera);
                return;
            }
            var directed = owner.CoreFlow.DirectedTutorial;
            if (directed != null && directed.IsGuiding && directed.DismissButton != null)
            {
                if (directed.DismissButton.interactable) CoreFlowVerification.Click(directed.DismissButton, owner.CoreFlow.UiCamera);
                return;
            }
            var booster = owner.CoreFlow.OptionalBoosterTutorial;
            if (booster != null && booster.IsGuiding)
            {
                if (!booster.DismissButton.interactable) return;
                if (useArrowBeforeSave && !arrowUsedBeforeSave)
                {
                    var slot = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>().Boosters[1];
                    Require(slot.BoosterType == OriginalBoosterType.Arrow, "Use the real Arrow button.");
                    CoreFlowVerification.Click(slot.Button, owner.CoreFlow.UiCamera, false);
                    var cell = board.Grid.GetCell(3, 6);
                    Samples.Set(cell, 0); listener.ManualUpdate();
                    Samples.Set(cell, 2); listener.ManualUpdate();
                    Require(game.State.HasOperation(7) && !slot.Button.interactable,
                        "The guided row tap spends the free Arrow.");
                    arrowUsedBeforeSave = true;
                }
                else CoreFlowVerification.Click(booster.DismissButton, owner.CoreFlow.UiCamera);
                return;
            }

            if ((phase == 1 || phase == 4) && sample < 3)
            {
                Samples.Set(sample == 0 ? from : to ?? from, sample);
                listener.ManualUpdate();
                sample++;
                return;
            }
            if (!game.Fall.IsAllItemsSteady || game.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            { steadySince = -1f; return; }
            if (steadySince < 0f) steadySince = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - steadySince < .35f) return;
            if (phase == 0 || phase == 3)
            {
                if (game.IsUiTouching || !game.CanUserMakeMove()) return;
                if (!new CoreNaturalMoveSelection(board).TryChoose(out from, out to)) return;
                phase++;
                steadySince = -1f;
                return;
            }
            if (phase == 1)
            {
                Require(game.Moves.MadeMoves == 1, "The real gesture spends exactly one move.");
                if (useArrowBeforeSave)
                    Require(arrowUsedBeforeSave && board.Goals.GetGoalLeftCount(11) == 0,
                        "The production Arrow completes the box goal before saving.");
                Require(File.Exists(file), "A saved attempt was written by the production background writer.");
                expectedStatistics = StatisticsSignature();
                firstMoves = game.Moves.LeftMoves;
                expected = Signature();
                var difficulty = game.GetLevelRandom();
                cursor = difficulty.DifficultySeedGenerationCount;
                seed = difficulty.DifficultySeed;
                attemptCount = owner.Progress.LevelTryCount;
                optionalReady = booster != null && !arrowUsedBeforeSave;
                phase = 2;
                SceneManager.LoadScene(owner.gameObject.scene.name);
                return;
            }
            Require(game.Moves.MadeMoves == 2 && game.Moves.LeftMoves == firstMoves - 1,
                "A player gesture continues the reopened attempt.");
            phase = 5;
            SceneManager.sceneLoaded += CorruptSnapshotBeforeStart;
            SceneManager.LoadScene(owner.gameObject.scene.name);
        }

        private static void VerifyRestoredGoalChecks()
        {
            var entries = board.Goals.GetFinalGoals();
            var panel = game.GoalPanel;
            for (int i = 0; i < entries.Length; i++)
            {
                var goal = entries[i];
                bool completed = board.Layout.IsFinalBoard && goal.IsGoalCompleted() && !board.Goals.HasPrerequisite(goal.GoalType);
                var view = panel.Views[i];
                Require(view.checkImage.gameObject.activeSelf == completed &&
                    view.countText.gameObject.activeSelf != completed && !view.IsCheckAnimationWorking,
                    "Restored goal marks must be present immediately, without replaying their animations.");
                if (completed) restoredGoalChecks++;
            }
            Require(!panel.ShouldSpeedUpLevelEnd || panel.LeftGoalCheckCount == entries.Length - restoredGoalChecks,
                "Restored goals must be removed from the pending victory check count.");
        }

        private static void CorruptSnapshotBeforeStart(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= CorruptSnapshotBeforeStart;
            // The old scene has flushed its writer and the new controller has not started.
            try
            {
                byte[] data = File.ReadAllBytes(file);
                File.WriteAllBytes(sampleFile, data);
                Array.Resize(ref data, 16);
                File.WriteAllBytes(file, data);
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }

        private static string Signature()
        {
            var value = new StringBuilder().Append(game.Moves.LeftMoves).Append('/').Append(game.Moves.MadeMoves);
            for (int y = 0; y < game.Grid.Height; y++)
            for (int x = 0; x < game.Grid.Width; x++)
            {
                var cell = game.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                value.Append(';').Append(item?.TiledId ?? 0).Append(':').Append(item?.LayerCount ?? 0);
                if (item is OriginalPotionBoardItem potion)
                    for (int color = 1; color <= 6; color++) value.Append(potion.HasBottle(color) ? '1' : '0');
                if (cell.StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem grass)
                    value.Append('g').Append(grass.TiledId).Append(':').Append(grass.LayerCount);
                if (cell.StaticTouch.GetStaticItem(OriginalHoneyBoardItem.StaticType) != null) value.Append('h');
            }
            foreach (var goal in board.Goals.GetFinalGoals())
                value.Append(';').Append(goal.GoalType).Append(':').Append(goal.LeftCount).Append(':').Append(goal.TargetCount)
                    .Append(':').Append(board.Goals.GetGoalDisplayCount(goal.GoalType));
            return value.ToString();
        }

        private static string StatisticsSignature()
        {
            var session = game.Session;
            var special = session.SpecialItemCounts;
            var value = new StringBuilder().Append(session.NoPossibleMoveShuffleCount).Append(':')
                .Append(session.NoPossibleMoveColoredShuffleCount).Append(':').Append(session.FalseSwipeCount).Append(':')
                .Append(session.ShownHintCount).Append(':').Append(session.UsedHintCount).Append(':')
                .Append(session.IsUsefulMoveMade).Append(':').Append(session.UselessMoveCount).Append(':')
                .Append(session.CurrentUselessMoveStreak).Append(':').Append(session.MaxUselessMoveStreak).Append(':')
                .Append(special.TappedLightballCount).Append(':').Append(special.SwappedItemCount).Append(':')
                .Append(special.AutoTriggeredItemCount);
            for (int color = 1; color <= 5; color++)
                value.Append(':').Append(session.MatchItemCounts.GetCount(color));
            for (int type = 2; type <= 5; type++)
                value.Append(':').Append(special.Items.TryGetValue(type, out var item) ? item.GetSaveData() : 0);
            for (int combo = 1; combo <= 10; combo++)
                value.Append(':').Append(special.Combos.TryGetValue(combo, out int count) ? count : 0);
            return value.ToString();
        }

        private static void Finish(bool passed, string failure)
        {
            EditorApplication.update -= Tick;
            SceneManager.sceneLoaded -= CorruptSnapshotBeforeStart;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report {
                passed = passed, level = ReadLevel(), moves_after_reopen = firstMoves,
                difficulty_draws = cursor, difficulty_seed = seed, level_try_count = attemptCount,
                recovery_seed = recoverySeed, failure = failure, save_file = file,
                sample_file = sampleFile, corruption_rejected = corruptionRejected, optional_ready = optionalReady,
                statistics_restored = statisticsRestored, restored_goal_checks = restoredGoalChecks,
                arrow_used_before_save = arrowUsedBeforeSave
            }, true));
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        [Serializable]
        private sealed class Report
        {
            public bool passed;
            public int level, moves_after_reopen, difficulty_draws, difficulty_seed, level_try_count, recovery_seed, restored_goal_checks;
            public bool corruption_rejected, optional_ready, statistics_restored, arrow_used_before_save;
            public string failure, save_file, sample_file;
        }
    }
}
