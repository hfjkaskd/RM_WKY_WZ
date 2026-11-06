using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreDirectedTutorialVerification
    {
        private const string ReportPath = "live_core_directed_tutorials_verification.json";
        // Bottom to top. The three affected columns drop two rows after the
        // guided red match and take their first two authored queue entries.
        private static readonly int[] LevelOneAfterGuidedSwap =
        {
            21, 21, 21, 21, 21, 21, 21, 21, 21,
            21, 21, 21, 21, 21, 21, 21, 21, 21,
            21, 21, 21, 21, 21, 21, 21, 21, 21,
            21, 21, 21, 2, 4, 2, 21, 21, 21,
            1, 2, 2, 1, 2, 1, 4, 3, 4,
            3, 1, 4, 2, 3, 2, 2, 4, 2,
            2, 4, 3, 4, 1, 4, 4, 3, 1,
            2, 1, 3, 3, 4, 2, 4, 1, 3
        };
        private static readonly CoreModalTutorialVerification.GestureSamples Input =
            new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalContextUpdateHost contextHost;
        private static OriginalPlayableBoard restartingAttempt;
        private static OriginalContextContainer restartingContext;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell from, to;
        private static Report report;
        private static int scenario, phase, sample, frame, ownerId, moves, refills;
        private static int initialUnityFrame, initialLevelFrame, initialLateUpdates;
        private static float deadline, began, presented, levelOneOperationFinishedAt;
        private static bool levelOneOperationStarted;
        private static bool observedCombo;
        private static int boxSortingLayer, boxSortingOrder;

        internal static void ObserveInitialEntrance(FoundationController owner)
        {
            if (levelOneOperationStarted || owner.CurrentLevelNumber != 1) return;
            var enteringBoard = owner.GetComponentInChildren<OriginalInitialBoardView>();
            enteringBoard.State.Operations.OperationStart(7);
            levelOneOperationStarted = true;
        }

        public static void Run(FoundationController owner)
        {
            report = new Report();
            Begin(owner);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            ownerId = owner.GetInstanceID();
            Require(owner.CurrentLevelNumber == (scenario == 0 ? 1 : 28), "Formal GameScene uses the intended original level.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            contextHost = UnityEngine.Object.FindObjectOfType<OriginalContextUpdateHost>();
            var context = contextHost?.Context;
            Require(context != null && context.Get<IOriginalContextBehaviour>(17) != null &&
                context.Get<IOriginalContextBehaviour>(51) != null && context.Get<IOriginalContextBehaviour>(74) != null,
                "The formal level context owns the match hints, dynamic tutorials and checkpoints.");
            report.level_flow_units_registered = true;
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, owner.Gameplay);
            moves = owner.Gameplay.Moves.LeftMoves;
            refills = owner.Gameplay.CreatedRefills;
            if (scenario == 0)
            {
                var eventSystem = EventSystem.current as OriginalUiEventSystem;
                var lateHost = UnityEngine.Object.FindObjectOfType<OriginalGameLateUpdateHost>();
                var cellView = UnityEngine.Object.FindObjectOfType<OriginalCellView>();
                Require(eventSystem != null && eventSystem.currentInputModule is StandaloneInputModule &&
                    MonoImporter.GetExecutionOrder(MonoScript.FromMonoBehaviour(eventSystem)) == -1000 &&
                    contextHost != null && lateHost != null && cellView != null &&
                    MonoImporter.GetExecutionOrder(MonoScript.FromMonoBehaviour(contextHost)) == -90 &&
                    MonoImporter.GetExecutionOrder(MonoScript.FromMonoBehaviour(lateHost)) == -70 &&
                    MonoImporter.GetExecutionOrder(MonoScript.FromMonoBehaviour(cellView)) == 0,
                    "The standard EventSystem and game callbacks use their recovered orders.");
                report.original_ui_input_order = true;
                report.original_context_before_cell_updates = true;
                report.original_game_late_update_order = true;
                initialUnityFrame = Time.frameCount;
                initialLevelFrame = board.Clock.FrameCount;
                initialLateUpdates = contextHost.CompletedLateUpdateCount;
                Require(moves == 30 && board.Goals.GetGoalLeftCount(11) == 36, "The original first level opens with 30 moves and 36 boxes.");
                RequireLevelOneTiles(OriginalLevelReader.Load("Levels/level_1").Grid.Items);
                report.level1_opening_tiles_verified = true;
                Require(levelOneOperationStarted && board.State.HasOperation(7), "An operation was started during the level-one entrance.");
                var cornerBox = (OriginalBoxItemSpriteView)board.Grid.GetCell(0, 0).Mediation.CurrentItem.SwapView;
                boxSortingLayer = cornerBox.sortingGroup.sortingLayerID;
                boxSortingOrder = cornerBox.sortingGroup.sortingOrder;
                levelOneOperationFinishedAt = -1f;
            }
            else if (scenario == 1) owner.Gameplay.State.Operations.OperationStart(7);
            phase = sample = 0;
            began = Time.time;
            presented = -1f;
            observedCombo = false;
            deadline = Time.realtimeSinceStartup + 30f;
        }

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { Advance(); }
            catch (Exception exception) { report.failure = exception.ToString(); Finish(); }
        }

        private static void Advance()
        {
            Require(Time.realtimeSinceStartup < deadline, $"Directed tutorial timed out, scenario {scenario}, phase {phase}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.GetInstanceID() == ownerId || owner.Gameplay == null) return;
                Begin(owner);
            }
            if (restartingAttempt != null)
            {
                var restarted = controller.Gameplay;
                if (restarted == null || ReferenceEquals(restarted, restartingAttempt)) return;
                Require(restartingContext != null && !restartingContext.Has(17) &&
                    !restartingContext.Has(51) && !restartingContext.Has(74),
                    "Retry removes the previous attempt's hint, tutorial and save updates.");
                report.level_flow_units_released = true;
                restartingContext = null;
                restartingAttempt = null;
                Begin(controller);
            }
            var game = controller.Gameplay;
            var flow = controller.CoreFlow;
            var guide = flow.DirectedTutorial;
            var first = scenario == 0 ? board.Grid.GetCell(5, 5) : board.Grid.GetCell(4, 5);
            var second = scenario == 0 ? board.Grid.GetCell(5, 4) : board.Grid.GetCell(3, 5);
            var outside = scenario == 0 ? board.Grid.GetCell(0, 6) : board.Grid.GetCell(0, 5);

            if (phase == 0)
            {
                if (scenario == 0 && levelOneOperationFinishedAt < 0f)
                {
                    Require(guide == null && flow.BlocksBoardInput, "An active operation keeps the level-one guide and input pending.");
                    if (Time.time - began < .15f) return;
                    game.State.Operations.OperationFinish(7);
                    levelOneOperationFinishedAt = board.Clock.Time;
                    return;
                }
                if (scenario == 0 && guide != null && !report.level1_waited_for_stable_board)
                {
                    report.level1_stable_wait_seconds = board.Clock.Time - levelOneOperationFinishedAt;
                    Require(report.level1_stable_wait_seconds > .1f,
                        "The level-one guide waits for more than 0.1 seconds of stability after the operation.");
                    report.level1_waited_for_stable_board = true;
                }
                if (scenario != 0 && guide == null && Time.time - began < 1f)
                {
                    Require(!game.IsUiTouching && first.IsTouchEnabled && second.IsTouchEnabled && outside.IsTouchEnabled,
                        "The delayed double-lightball guide leaves the original board playable.");
                    report.level28_initial_input_open = true;
                }
                if (scenario == 2)
                {
                    Require(guide == null && report.level28_initial_input_open, "Early swap happens before the prompt.");
                    Require(game.Moves.MadeMoves == 0 &&
                        first.Mediation.CurrentItem is OriginalLightballBoardItem &&
                        second.Mediation.CurrentItem is OriginalLightballBoardItem,
                        "The early-input case starts on a fresh attempt with both original lightballs.");
                    if (!game.CanUserMakeMove() || !game.Fall.IsAllItemsSteady || board.State.HasAnyOperation()) return;
                    StartGesture(first, second, 2);
                    return;
                }
                if (guide == null || !guide.IsGuiding)
                {
                    if (scenario == 1)
                        Require(Time.time - began < 6f, "The level 28 guide starts while an operation remains active.");
                    return;
                }
                if (scenario == 1 && !report.level28_guide_during_operation)
                {
                    Require(board.State.HasOperation(7), "The level 28 guide starts while an operation remains active.");
                    game.State.Operations.OperationFinish(7);
                    report.level28_guide_during_operation = true;
                }
                Require(!game.IsUiTouching && first.IsTouchEnabled && second.IsTouchEnabled && !outside.IsTouchEnabled,
                    "The original pair can be dragged while the other cells are locked.");
                if (presented < 0f)
                {
                    presented = Time.time;
                    if (scenario == 1)
                    {
                        report.level28_delay_seconds = presented - began;
                        Require(report.level28_delay_seconds > 3.75f, "The original delay precedes the double-lightball prompt.");
                    }
                }
                if (Time.time - presented < .5f) return;
                var animation = guide.GetComponentInChildren<OriginalBakedMeshAnimation>();
                var mask = guide.GetComponentInChildren<Camera>();
                Require(animation.RigBoneCount == 14 && animation.RigPathConstraintCount == 2 && mask.targetTexture != null &&
                    mask.targetTexture.format == RenderTextureFormat.R8 && !mask.enabled,
                    "The original hand evaluates its bones and paths while the cached isolated mask camera is disabled.");
                var animator = animation.PlaybackAnimator;
                Require(animator != null && animator.isActiveAndEnabled &&
                    animator.runtimeAnimatorController == AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/Resources/Recovered/AnimatorController/tutorial_hand_Controller.controller"),
                    "The hand uses its original Unity AnimatorController.");
                var state = animator.GetCurrentAnimatorStateInfo(0);
                float expectedTime = (state.normalizedTime * animation.Duration) % animation.Duration;
                Require(Mathf.Abs(animation.PlaybackTime - expectedTime) < .00001f,
                    "The visible hand samples the actual Mecanim state after the animation update.");
                if (scenario == 0) { report.level1_hand_mecanim_clock = true; report.level1_hand_native_rig = true; }
                else { report.level28_hand_mecanim_clock = true; report.level28_hand_native_rig = true; }
                int raised = 0;
                foreach (var group in UnityEngine.Object.FindObjectsOfType<SortingGroup>())
                    if (group.enabled && group.sortAtRoot && group.sortingOrder == 510) raised++;
                Require(raised == (scenario == 0 ? 37 : 1), "The original highlighted groups are raised.");
                if (scenario == 0)
                {
                    var masks = mask.transform.parent.Find("Cell masks");
                    report.level1_mask_cells = masks.childCount;
                    report.level1_raised_groups = raised;
                    Require(report.level1_mask_cells == 7, "The level-one guide uses the expected number of mask sprites.");
                }
                var text = guide.GetComponentInChildren<TextMeshPro>();
                Require(scenario == 0 ? text != null && text.text == "Make a match next to a BOX to break it!" : text == null,
                    "Only the original level-one guide shows an instruction.");
                GrassGameplayVerification.Capture(flow.UiCamera, $"core_directed_level{controller.CurrentLevelNumber}.png");
                StartGesture(outside, board.Grid.GetCell(1, outside.Y), 1);
                return;
            }
            if (game.BallBallCombo.IsComboActive) observedCombo = true;
            if (sample < 3)
            {
                Input.Set(sample == 0 ? from : to, sample);
                listener.ManualUpdate();
                sample++;
                return;
            }
            if (phase == 1)
            {
                Require(game.AcceptedSwaps == 0 && game.Moves.LeftMoves == moves && guide != null && guide.IsGuiding,
                    "An off-target gesture leaves the guide active and spends no move.");
                game.OnSwap += ObserveGuideRelease;
                StartGesture(first, second, 2);
                return;
            }
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch) return;
            Require(game.AcceptedSwaps == 1 && game.Moves.LeftMoves == moves - 1,
                "The authored pair performs one actual swap and spends one move.");
            Require((guide == null || !guide.IsGuiding) && outside.IsTouchEnabled && !game.IsUiTouching,
                "The guide releases its touch restrictions after the original gesture.");
            if (scenario != 2)
                Require((scenario == 0 ? report.level1_guide_released_at_swap : report.level28_guide_released_at_swap),
                    "The tutorial root and its mask retire in the accepted-swap callback.");
            if (scenario == 0)
            {
                report.level1_boxes_removed = 36 - board.Goals.GetGoalLeftCount(11);
                if (game.CreatedRefills == refills) return;
                report.level1_refills = game.CreatedRefills - refills;
                Require(report.level1_boxes_removed == 3 && board.Goals.GetGoalUiLeftCount(11) == 33 &&
                    report.level1_refills == 6 && game.CreatedSpecials == 0,
                    "The guided match clears exactly three boxes and refills six ordinary tiles.");
                RequireLevelOneTiles(LevelOneAfterGuidedSwap);
                int lateUpdates = contextHost.CompletedLateUpdateCount - initialLateUpdates;
                Require(lateUpdates > 0 && board.Clock.FrameCount - initialLevelFrame == lateUpdates &&
                    Mathf.Abs(lateUpdates - (Time.frameCount - initialUnityFrame)) <= 1,
                    "The game late update advances the level clock once per rendered frame.");
                report.level1_game_late_updates = lateUpdates;
                var cornerBox = (OriginalBoxItemSpriteView)board.Grid.GetCell(0, 0).Mediation.CurrentItem.SwapView;
                Require(cornerBox.sortingGroup.sortingLayerID == boxSortingLayer &&
                    cornerBox.sortingGroup.sortingOrder == boxSortingOrder,
                    "The remaining boxes return to gameplay sorting after the tutorial gesture.");
                report.level1_box_sorting_restored = true;
                report.level1_guided_swap_completed = true;
            }
            else
            {
                Require(observedCombo && game.Session.SpecialItemCounts.Combos.TryGetValue(1, out int count) && count == 1,
                    "The two original lightballs activate the actual board-wide combo.");
                if (scenario == 1) report.level28_guided_combo_completed = true;
                else
                {
                    if (Time.time - began < 4.25f) return;
                    Require(guide == null, "A valid early swap still suppresses the prompt after its original delay.");
                    report.level28_early_combo_skips_guide = true;
                    report.passed = true;
                    Finish();
                    return;
                }
            }
            if (scenario == 1)
            {
                // Reopening the scene can resume the just-played attempt.
                // Retry clears that checkpoint before the early-input case.
                restartingAttempt = game;
                restartingContext = contextHost.Context;
                scenario++;
                deadline = Time.realtimeSinceStartup + 30f;
                flow.RetryLevel();
                return;
            }
            string path = controller.gameObject.scene.path;
            if (scenario == 0) controller.Progress.CompleteLevel(27);
            scenario++;
            controller = null;
            deadline = Time.realtimeSinceStartup + 30f;
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private static void ObserveGuideRelease(OriginalGridCell from, OriginalGridCell to, bool valid)
        {
            if (!valid) return;
            controller.Gameplay.OnSwap -= ObserveGuideRelease;
            var guide = controller.CoreFlow.DirectedTutorial;
            var mask = guide.GetComponentInChildren<Camera>(true);
            bool released = !guide.IsGuiding && !guide.gameObject.activeSelf && mask.targetTexture == null;
            if (scenario == 0) report.level1_guide_released_at_swap = released;
            else report.level28_guide_released_at_swap = released;
        }

        private static void StartGesture(OriginalGridCell first, OriginalGridCell second, int nextPhase)
        {
            from = first;
            to = second;
            sample = 0;
            phase = nextPhase;
        }

        private static void RequireLevelOneTiles(int[] expected)
        {
            const int width = 9, height = 8;
            Require(board.Grid.Width == width && board.Grid.Height == height && expected.Length == width * height,
                "The first level uses the original 9 by 8 grid.");
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                int id = expected[x + y * width];
                Require(item != null && item.HasView && item.TiledId == id,
                    $"First level cell ({x}, {y}) must display tile {id}.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            report.status = "finished";
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable]
        private sealed class Report
        {
            public string status, failure;
            public bool passed, original_context_before_cell_updates, original_game_late_update_order, level1_opening_tiles_verified;
            public bool original_ui_input_order, level_flow_units_registered, level_flow_units_released;
            public bool level1_guided_swap_completed, level1_waited_for_stable_board, level28_initial_input_open;
            public bool level28_guided_combo_completed, level28_early_combo_skips_guide;
            public bool level28_guide_during_operation, level1_box_sorting_restored;
            public bool level1_guide_released_at_swap, level28_guide_released_at_swap;
            public bool level1_hand_mecanim_clock, level28_hand_mecanim_clock, level1_hand_native_rig, level28_hand_native_rig;
            public int level1_mask_cells, level1_raised_groups;
            public int level1_boxes_removed, level1_refills, level1_game_late_updates;
            public float level1_stable_wait_seconds, level28_delay_seconds;
        }
    }
}
