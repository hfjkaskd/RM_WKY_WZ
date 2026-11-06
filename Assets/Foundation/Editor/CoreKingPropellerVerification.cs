using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreKingPropellerVerification
    {
        private const string Happy = "k-1sevinme_6";

        public static void Run(FoundationController controller, string test) =>
            controller.StartCoroutine(Verify(controller, test));

        private static IEnumerator Verify(FoundationController controller, string test)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var king = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual;
            var game = controller.Gameplay;
            Require(controller.CurrentLevelNumber == 2 && board.Goals.GetGoalLeftCount(11) == 36,
                "Use the original level 2 box board.");
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            var samples = new CoreModalTutorialVerification.GestureSamples();
            var listener = new OriginalGameTouchListener(samples, game);
            var from = board.Grid.GetCell(0, 8);
            int comboType = test switch
            {
                "core-king-propeller-single" => 0,
                "core-king-propeller-pair" => 10,
                "core-king-propeller-tnt" => 8,
                "core-king-propeller-rocket" => 9,
                _ => throw new ArgumentException("Unknown King propeller fixture.", nameof(test))
            };
            var report = new Report { combo_type = comboType };
            var propeller = (OriginalPropellerBoardItem)CoreKingComboVerification.Place(game, board, from, 10);
            int initialMoves = game.Moves.LeftMoves;
            float deadline = Time.realtimeSinceStartup + 25f;
            if (comboType == 0)
            {
                VerifyRecycledDuringTakeoff(game, board, from);
                report.recycled_takeoff_particles = true;
                var secondCell = board.Grid.GetCell(8, 8);
                var second = (OriginalPropellerBoardItem)CoreKingComboVerification.Place(game, board, secondCell, 10);
                CoreKingComboVerification.KeepTwoBoxes(board, 5);

                // Prime the real scorer with one target without letting a
                // projectile arrive. The subsequent player tap must clear it.
                var probe = OriginalExploder.Next(8, new OriginalCellPoint(from.X, from.Y));
                var primed = game.GuidedTargets.FindForPropeller(probe, null);
                Require(primed is OriginalBoxBoardItem && king.CurrentAnimation != Happy,
                    "A single reserved hit does not finish two outstanding boxes.");
                primed.RemoveIncomingExploder(probe);
                yield return CoreKingComboVerification.Gesture(listener, samples, from, from);
                Require(propeller.IsInFlight && game.Moves.LeftMoves == initialMoves - 1 &&
                    board.Goals.GetGoalLeftCount(11) == 2 && king.CurrentAnimation != Happy,
                    "A new player tap clears the previous single-target forecast.");
                propeller.FindNewTarget();
                Require(propeller.IsInFlight && board.Goals.GetGoalLeftCount(11) == 2 && king.CurrentAnimation != Happy,
                    "Retargeting clears the old forecast before reserving the new hit.");
                report.tap_and_retarget_reset = true;

                while (board.Goals.GetGoalLeftCount(11) == 2)
                {
                    Require(Time.realtimeSinceStartup < deadline, "The retargeted propeller reaches its box.");
                    yield return null;
                }
                Require(board.Goals.GetGoalLeftCount(11) == 1 && game.State.IsPlaying(),
                    "The first actual hit leaves one box.");
                yield return CoreKingComboVerification.Gesture(listener, samples, secondCell, secondCell);
                Require(second.IsInFlight && king.CurrentAnimation == Happy &&
                    board.Goals.GetGoalLeftCount(11) == 1 && game.Moves.LeftMoves == initialMoves - 2,
                    "One actual propeller predicts the remaining box before impact.");
                report.goals_when_king_celebrates = 1;
            }
            else
            {
                var to = board.Grid.GetCell(1, 8);
                int partnerTile = comboType switch { 8 => 40, 9 => 20, _ => 10 };
                var partner = CoreKingComboVerification.Place(game, board, to, partnerTile);
                CoreKingComboVerification.KeepTwoBoxes(board, 5);
                samples.Set(from, 0);
                listener.ManualUpdate();
                yield return null;

                // A newly created stationary item may still lock its sorting.
                var partnerView = (OriginalItemSwapView)partner.SwapView;
                if (partnerView is OriginalTntItemSpriteView tnt) tnt.PlayCreationAnimation(0f, false);
                if (partnerView is OriginalRocketItemSpriteView rocket) rocket.PlayCreationAnimation(0f, false);
                var moving = propeller.ItemView.transform;
                var start = moving.localPosition;
                var stationary = partnerView.transform.localPosition;
                var target = to.GetViewLocalPosition();
                int moveType = -1;
                Action<int> onCompleted = type => moveType = type;
                game.Moves.OnMoveCompleted += onCompleted;
                samples.Set(to, 1);
                listener.ManualUpdate();
                Require(game.Moves.LeftMoves == initialMoves - 1 && game.State.Operations.Container.GetCount(2) == 1,
                    "The accepted swipe spends one move and starts its swap hold.");
                var sorting = partnerView switch
                {
                    OriginalTntItemSpriteView tntView => tntView.GetSorting(),
                    OriginalRocketItemSpriteView rocketView => rocketView.GetSorting(),
                    OriginalPropellerItemSpriteView propellerView => propellerView.GetSorting(),
                    _ => throw new InvalidOperationException("Unexpected propeller combo partner.")
                };
                Require(sorting.Order == 8905 && sorting.Layer == game.ItemSortingLayer,
                    "The propeller combo forces its stationary partner behind the moving item during creation.");
                Require(moving.localPosition == start && partnerView.transform.localPosition == stationary,
                    "The accepted swipe queues the slide without advancing either view in the input callback.");

                const float nativeDuration = .135f;
                float elapsed = 0f;
                do
                {
                    yield return null;
                    elapsed += board.Clock.DeltaTime;
                    if (report.slide_frames == 0)
                    {
                        samples.Set(to, 2);
                        listener.ManualUpdate();
                    }
                    report.slide_frames++;
                    Require(Time.realtimeSinceStartup < deadline, "The propeller slide reaches its target.");
                    if (elapsed < nativeDuration)
                    {
                        var expected = Vector3.LerpUnclamped(start, target, elapsed / nativeDuration);
                        Require((moving.localPosition - expected).sqrMagnitude < 1e-8f &&
                            partnerView.transform.localPosition == stationary,
                            "Only the moving view follows the linear level-clock slide.");
                        Require(!propeller.IsInFlight && moveType == -1 &&
                            game.State.Operations.Container.GetCount(2) == 1,
                            "The flight and move completion wait for the slide endpoint.");
                    }
                } while (elapsed < nativeDuration);
                game.Moves.OnMoveCompleted -= onCompleted;
                Require(game.Session.SpecialItemCounts.Combos.TryGetValue(comboType, out int comboCount) &&
                    comboCount == 1 && moveType == 1 && game.State.Operations.Container.GetCount(2) == 0,
                    "The endpoint tick dispatches the combo and completes the swap once.");
                report.slide_seconds = elapsed;
                report.stationary_sorting_verified = true;
                Require(propeller.IsInFlight && king.CurrentAnimation == Happy &&
                    board.Goals.GetGoalLeftCount(11) == 2 && game.State.IsPlaying() &&
                    game.Moves.LeftMoves == initialMoves - 1,
                    "Separate reservations or a carried blast predict both boxes before damage.");
                report.goals_when_king_celebrates = 2;
            }

            while (!game.State.IsWin())
            {
                Require(Time.realtimeSinceStartup < deadline, "The actual guided hits complete the level.");
                yield return null;
            }
            Require(board.Goals.GetGoalLeftCount(11) == 0, "Actual victory follows the forecast.");
            report.passed = true;
            File.WriteAllText("live_" + test.Replace('-', '_') + "_verification.json", JsonUtility.ToJson(report, true) + "\n");
        }

        private static void VerifyRecycledDuringTakeoff(OriginalPlayableBoard game, OriginalInitialBoardView board,
            OriginalGridCell cell)
        {
            int views = game.Pool.GetSpawnedCount(10);
            int takeoffEffects = game.Pool.GetSpawnedCount(11);
            int targetEffects = game.Pool.GetSpawnedCount(12);
            var probe = new OriginalPropellerBoardItem(-1, () => board.Clock.Time, game);
            probe.SetStrategy(new RecycleDuringStart());
            probe.CreateView(board.Presentation, board.BoardRoot, new Vector3(cell.X, cell.Y, 0f), game);

            probe.ItemView.MoveToTarget(true, null);

            Require(!probe.ItemView.gameObject.activeSelf && game.Pool.GetSpawnedCount(10) == views &&
                game.Pool.GetSpawnedCount(11) == takeoffEffects + 1 && game.Pool.GetSpawnedCount(12) == targetEffects + 1,
                "Reentrant recycling completes and both original particle effects spawn exactly once.");
        }

        // Reproduces the synchronous recycle inside StartFlying without depending
        // on which targets are available during a particular TNT cascade.
        private sealed class RecycleDuringStart : IOriginalGuidedExploderStrategy, IOriginalGuidedExploderPilot
        {
            public bool IsTntOrRocketCombo => false;
            public bool ShouldPlaySfxInLoop => false;
            public OriginalGuidedExploderFlyState FlyState => OriginalGuidedExploderFlyState.Stable;
            public IOriginalGuidedExploderPilot GetNewPilot() => this;
            public void StartFlying(IOriginalPropellerPilotView view, IOriginalGuidedExploderViewDelegate viewDelegate)
                => view.ExplodeSelf(false);
            public void TargetChanged() => throw new NotSupportedException();
            public void Move() => throw new NotSupportedException();
            public bool ExplodeNeighbors(OriginalGridCell cell, int trigger, OriginalGuidedExploderBoardItem model)
                => throw new NotSupportedException();
            public OriginalExplodeData NextExploder(OriginalGridCell cell) => throw new NotSupportedException();
            public void OnTargetFound() => throw new NotSupportedException();
            public void OnTargetCancelled() => throw new NotSupportedException();
            public void OnTargetReached(OriginalGridCell cell, OriginalExplodeData data) => throw new NotSupportedException();
            public void ForceExplode() => throw new NotSupportedException();
            public bool IsTargetAmountReached() => throw new NotSupportedException();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable] private sealed class Report
        {
            public bool passed, tap_and_retarget_reset, recycled_takeoff_particles, stationary_sorting_verified;
            public int combo_type, goals_when_king_celebrates, slide_frames;
            public float slide_seconds;
        }
    }
}
