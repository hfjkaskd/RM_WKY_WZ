using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreKingComboVerification
    {
        private const string Happy = "k-1sevinme_6";
        private const string Excited = "Heyecan_1";

        public static void Run(FoundationController controller, bool doubleLightball) =>
            controller.StartCoroutine(Verify(controller, doubleLightball));

        public static void RunMixedLightball(FoundationController controller, string test) =>
            controller.StartCoroutine(VerifyMixedLightball(controller, test));

        private static IEnumerator VerifyMixedLightball(FoundationController controller, string test)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var king = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual;
            var game = controller.Gameplay;
            Require(controller.CurrentLevelNumber == 83 && board.Goals.GetGoalLeftCount(14) == 45,
                "Use the original level 83 honey board.");
            int comboType = test switch
            {
                "core-king-lightball-tnt" => 2,
                "core-king-lightball-propeller" => 3,
                "core-king-lightball-rocket" => 4,
                _ => throw new ArgumentException("Unknown mixed Lightball fixture.", nameof(test))
            };
            int partnerTile = comboType switch { 2 => 40, 3 => 10, _ => 20 };
            bool shouldExcite = comboType != 2;
            int thresholdCount = shouldExcite ? 7 : 6;
            var from = board.Grid.GetCell(6, 0);
            var to = board.Grid.GetCell(7, 0);
            Place(game, board, from, 50);
            Place(game, board, to, partnerTile);
            var revealedMatch = (OriginalMatchBoardItem)board.Grid.GetCell(6, 1).Mediation.CurrentItem;
            int color = revealedMatch.MatchType;
            int revealed = 0, total = 0;
            for (int x = 0; x < board.Grid.Width; x++)
                for (int y = 0; y < board.Grid.Height; y++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    if (cell?.Mediation.CurrentItem is not OriginalMatchBoardItem match || match.MatchType != color) continue;
                    total++;
                    if (!cell.StaticTouch.HasTouchBlockingItemExceptChain()) revealed++;
                }
            Require(total > 7 && revealed < thresholdCount, "Honey masks some of the authored same-color pieces.");
            for (int x = 6; x <= 8 && revealed < thresholdCount; x++)
                for (int y = board.Grid.Height - 1; y >= 3 && revealed < thresholdCount; y--)
                {
                    var cell = board.Grid.GetCell(x, y);
                    if (cell?.Mediation.CurrentItem is not OriginalMatchBoardItem match || match.MatchType != color ||
                        !cell.StaticTouch.HasStaticItem(OriginalHoneyBoardItem.StaticType)) continue;
                    cell.ExplodeCurrentItem(OriginalExploder.Next(6));
                    revealed++;
                }
            Require(revealed == thresholdCount, "Real honey hits expose the exact threshold count.");

            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            var samples = new CoreModalTutorialVerification.GestureSamples();
            var listener = new OriginalGameTouchListener(samples, game);
            int initialMoves = game.Moves.LeftMoves;
            float deadline = Time.realtimeSinceStartup + 20f;
            yield return Gesture(listener, samples, from, to);
            while (!game.Session.SpecialItemCounts.Combos.ContainsKey(comboType))
            {
                Require(Time.realtimeSinceStartup < deadline, "The legal mixed Lightball swap activates.");
                yield return null;
            }
            Require((king.CurrentAnimation == Excited) == shouldExcite && king.CurrentAnimation != Happy &&
                game.Moves.LeftMoves == initialMoves - 1 && game.State.IsPlaying(),
                "The King uses the reachable same-color count at the native mixed combo boundary.");
            if (shouldExcite)
            {
                while (king.CurrentAnimation == Excited && king.AnimationTime < .25f)
                {
                    Require(Time.realtimeSinceStartup < deadline, "The excited pose advances after the mix.");
                    yield return null;
                }
                Require(king.CurrentAnimation == Excited, "The mixed combo reaches the authored excited pose.");
                GrassGameplayVerification.Capture(Camera.main, test.Replace('-', '_') + ".png");
            }
            var report = new MixedLightballReport { passed = true, combo_type = comboType, eligible_color_count = revealed,
                same_color_including_honey = total, expected_excited = shouldExcite, observed_clip = king.CurrentAnimation };
            File.WriteAllText("live_" + test.Replace('-', '_') + "_verification.json", JsonUtility.ToJson(report, true) + "\n");
        }

        private static IEnumerator Verify(FoundationController controller, bool doubleLightball)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var king = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual;
            var game = controller.Gameplay;
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            var samples = new CoreModalTutorialVerification.GestureSamples();
            var listener = new OriginalGameTouchListener(samples, game);
            float deadline = Time.realtimeSinceStartup + 30f;
            int initialMoves = game.Moves.LeftMoves;
            var report = new Report();
            if (doubleLightball)
            {
                Require(controller.CurrentLevelNumber == 3 && board.Goals.GetGoalLeftCount(11) == 25,
                    "The real level 3 starts with 25 double-layer boxes.");
                var from = board.Grid.GetCell(0, 8);
                var to = board.Grid.GetCell(1, 8);
                Place(game, board, from, 50);
                Place(game, board, to, 50);
                yield return Gesture(listener, samples, from, to);
                while (!game.BallBallCombo.IsComboActive)
                {
                    Require(Time.realtimeSinceStartup < deadline, "The legal double Lightball swap activates.");
                    yield return null;
                }
                Require(king.CurrentAnimation == Excited && !game.BallBallCombo.IsExplosionActive &&
                    board.Goals.GetGoalLeftCount(11) == 25 && game.Moves.LeftMoves == initialMoves - 1,
                    "A non-winning double Lightball predicts excitement before its radial hit.");
                GrassGameplayVerification.Capture(Camera.main, "core_king_double_lightball.png");
                while (game.BallBallCombo.IsComboActive)
                {
                    Require(Time.realtimeSinceStartup < deadline, "The double Lightball completes its native hit wave.");
                    yield return null;
                }
                Require(board.Goals.GetGoalLeftCount(11) > 0 && game.State.IsPlaying(),
                    "One hit across the double-layer board leaves an unfinished goal.");
                report.double_lightball_excited = true;
            }
            else
            {
                Require(controller.CurrentLevelNumber == 2 && board.Goals.GetGoalLeftCount(11) == 36,
                    "The real level 2 starts with 36 boxes.");
                var from = board.Grid.GetCell(0, 8);
                var to = board.Grid.GetCell(1, 8);
                Place(game, board, from, 20);
                Place(game, board, to, 30);
                yield return Gesture(listener, samples, from, to);
                do
                {
                    Require(king.CurrentAnimation != Happy && king.CurrentAnimation != Excited,
                        "A double rocket that leaves goals does not replace the idle animation.");
                    Require(Time.realtimeSinceStartup < deadline, "The non-winning double rocket settles.");
                    yield return null;
                } while (!game.Session.SpecialItemCounts.Combos.ContainsKey(7) ||
                    !game.Fall.IsAllItemsSteady || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch);
                Require(board.Goals.GetGoalLeftCount(11) > 2 && game.Moves.LeftMoves == initialMoves - 1,
                    "The non-winning combo spends one move and leaves real targets.");
                report.nonwinning_rocket_kept_idle = true;

                from = board.Grid.GetCell(5, 8);
                to = board.Grid.GetCell(6, 8);
                Place(game, board, from, 40);
                Place(game, board, to, 30);
                // Both targets lie outside a single rocket cross through (6,8).
                KeepTwoBoxes(board, 5);
                yield return Gesture(listener, samples, from, to);
                while (!game.Session.SpecialItemCounts.Combos.ContainsKey(6))
                {
                    Require(Time.realtimeSinceStartup < deadline, "The TNT and rocket swap activates.");
                    yield return null;
                }
                Require(king.CurrentAnimation == Happy && board.Goals.GetGoalLeftCount(11) == 2 &&
                    game.State.IsPlaying() && game.Moves.LeftMoves == initialMoves - 2,
                    "The wide cross predicts both off-center hits and celebrates before damage.");
                GrassGameplayVerification.Capture(Camera.main, "core_king_combo_win_prediction.png");
                while (!game.State.IsWin())
                {
                    Require(Time.realtimeSinceStartup < deadline, "The real TNT and rocket damage wins.");
                    yield return null;
                }
                Require(board.Goals.GetGoalLeftCount(11) == 0,
                    "Actual victory follows both predicted hits.");
                report.winning_wide_cross_celebrated_before_damage = true;
            }
            report.passed = true;
            string path = doubleLightball ? "live_core_king_ballball_verification.json" : "live_core_king_combo_verification.json";
            File.WriteAllText(path, JsonUtility.ToJson(report, true) + "\n");
        }

        internal static void KeepTwoBoxes(OriginalInitialBoardView board, int column)
        {
            for (int x = 0; x < board.Grid.Width; x++)
                for (int y = 0; y < board.Grid.Height; y++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    if ((x != column || y > 1) && cell.Mediation.CurrentItem is OriginalBoxBoardItem)
                        cell.ExplodeCurrentItem(OriginalExploder.Next(6));
                }
            Require(board.Goals.GetGoalLeftCount(11) == 2, "Fixture leaves two original boxes in the side column.");
        }

        internal static OriginalSpecialBoardItem Place(OriginalPlayableBoard game, OriginalInitialBoardView board, OriginalGridCell cell, int tiledId)
        {
            Require(cell.Mediation.CurrentItem is OriginalMatchBoardItem, "The fixture replaces an ordinary color piece.");
            var match = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            match.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)match.SwapView);
            return (OriginalSpecialBoardItem)game.CreateItemAtCell(tiledId, cell);
        }

        internal static IEnumerator Gesture(OriginalGameTouchListener listener,
            CoreModalTutorialVerification.GestureSamples samples, OriginalGridCell from, OriginalGridCell to)
        {
            for (int sample = 0; sample < 3; sample++)
            {
                samples.Set(sample == 0 ? from : to, sample);
                listener.ManualUpdate();
                yield return null;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable] private sealed class Report
        {
            public bool passed, nonwinning_rocket_kept_idle, winning_wide_cross_celebrated_before_damage, double_lightball_excited;
        }
        [Serializable] private sealed class MixedLightballReport
        {
            public bool passed, expected_excited;
            public int combo_type, eligible_color_count, same_color_including_honey;
            public string observed_clip;
        }
    }
}
