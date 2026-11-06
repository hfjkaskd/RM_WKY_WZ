using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreKingWorryVerification
    {
        public static void Run(FoundationController controller, int level = 2) =>
            controller.StartCoroutine(Verify(controller, level));

        private static IEnumerator Verify(FoundationController controller, int level)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var king = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual;
            var game = controller.Gameplay;
            bool mail = level == 21, safe = level == 91;
            int goal = mail ? 18 : safe ? 20 : 11;
            Require(controller.CurrentLevelNumber == level &&
                board.Goals.GetGoalLeftCount(goal) == (mail ? 50 : safe ? 4 : 36),
                "Start on the original single-goal board.");
            float deadline = Time.realtimeSinceStartup + 15f;
            while (controller.CoreFlow.BlocksBoardInput)
            {
                Require(Time.realtimeSinceStartup < deadline, "The original board introduction must finish.");
                var tutorial = controller.CoreFlow.ModalTutorial;
                if (tutorial != null && tutorial.DismissButton.interactable)
                    CoreFlowVerification.Click(tutorial.DismissButton, controller.CoreFlow.UiCamera);
                yield return null;
            }

            game.Moves.IncreaseMove(4 - game.Moves.LeftMoves, 0);
            Require(CoreCupboardVerification.FindNaturalSwap(board, out var from, out var to),
                "Find a legal swap on the unmodified board.");
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            var samples = new CoreModalTutorialVerification.GestureSamples();
            var listener = new OriginalGameTouchListener(samples, game);
            int completedAt = -1;
            void Completed(int type) { if (type == 1) completedAt = Time.frameCount; }
            game.Moves.OnMoveCompleted += Completed;
            for (int sample = 0; sample < 3; sample++)
            {
                samples.Set(sample == 0 ? from : to, sample);
                listener.ManualUpdate();
                yield return null;
            }

            deadline = Time.realtimeSinceStartup + 15f;
            while (king.CurrentAnimation != "k-1_kaygi")
            {
                Require(Time.realtimeSinceStartup < deadline, "A legal move with three steps and more than five layers starts worry.");
                yield return null;
            }
            game.Moves.OnMoveCompleted -= Completed;
            int delayFrames = Time.frameCount - completedAt;
            Require(completedAt >= 0 && delayFrames is 4 or 5 && !king.IsAnimationLooping &&
                game.AcceptedSwaps == 1 && game.Moves.MadeMoves == 1 && game.Moves.LeftMoves == 3,
                "Worry starts four coroutine frames after the real swap completion.");
            GrassGameplayVerification.Capture(Camera.main,
                mail ? "core_king_mail_worry.png" : safe ? "core_king_safe_worry.png" : "core_king_worry.png");
            while (king.CurrentAnimation != "k-1_kaygi_loop" || !game.Fall.IsAllItemsSteady ||
                board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            {
                Require(Time.realtimeSinceStartup < deadline, "Worry loops while the actual board settles.");
                yield return null;
            }
            Require(king.IsAnimationLooping, "The worry follow-up loops.");
            if (mail)
            {
                int letters = board.Goals.GetGoalLeftCount(goal);
                Require(letters > 5 && letters == board.Goals.GetGoalUiLeftCount(goal),
                    "Only the shared letter goal remains above the three-step cap.");
                var mailReport = new Report { passed = true, worry_observed_after_frames = delayFrames, letters_left = letters };
                File.WriteAllText("live_core_king_mail_worry_verification.json", JsonUtility.ToJson(mailReport, true) + "\n");
                yield break;
            }

            var report = new Report { worry_observed_after_frames = delayFrames };
            OriginalBoxBoardItem reservedBox = null;
            OriginalExplodeData pending = default;
            if (safe)
            {
                report.safe_footprint_on_relief = LeaveOneSafe(board);
            }
            else
            {
                RemoveBoxes(board, 7);
                yield return null;
                yield return null;
                Require(board.Goals.GetGoalLeftCount(11) == 7 && king.CurrentAnimation == "k-1_kaygi_loop",
                    "Seven outstanding hits are still above the three-step cap.");
                pending = OriginalExploder.Next(8);
                pending.Point = new OriginalCellPoint(4, 7);
                reservedBox = game.GuidedTargets.FindForPropeller(pending, null) as OriginalBoxBoardItem;
                Require(reservedBox != null && reservedBox.IncomingExploderCount == 1 && reservedBox.LayerCount == 1,
                    "The real propeller target finder reserves one hit on a still-present box.");
                RemoveBoxes(board, 6, reservedBox);
                report.boxes_left_on_relief = board.Goals.GetGoalLeftCount(11);
            }
            while (king.CurrentAnimation != "Relax_1")
            {
                Require(Time.realtimeSinceStartup < deadline, safe
                    ? "A five-layer safe must count once across its four cells."
                    : "Six boxes with one reserved propeller hit must reach the five-hit relief threshold.");
                yield return null;
            }
            Require(game.Moves.LeftMoves == 3 && !king.IsAnimationLooping && king.IsAnimationMixing,
                "Relief blends from worry without spending a move or changing level state.");
            if (reservedBox != null)
            {
                Require(board.Goals.GetGoalLeftCount(11) == 6 && reservedBox.HasView &&
                    reservedBox.LayerCount == 1 && reservedBox.IncomingExploderCount == 1,
                    "The relief starts before the pending box takes damage.");
                reservedBox.RemoveIncomingExploder(pending);
                reservedBox.Mediator.CurrentCell.Owner.ExplodeCurrentItem(pending);
                Require(board.Goals.GetGoalLeftCount(11) == 5, "Applying the reserved hit then removes its box.");
            }
            else Require(board.Goals.GetGoalLeftCount(20) == 1 && report.safe_footprint_on_relief == 4,
                "One intact safe with three proxies remains when the relief begins.");
            GrassGameplayVerification.Capture(Camera.main, safe ? "core_king_safe_relief.png" : "core_king_relief.png");

            while (!king.CurrentAnimation.StartsWith("idle_connect_", StringComparison.Ordinal))
            {
                Require(Time.realtimeSinceStartup < deadline, "Relief queues the original connector.");
                yield return null;
            }
            string connector = king.CurrentAnimation;
            while (!king.CurrentAnimation.StartsWith("idle_loop_", StringComparison.Ordinal))
            {
                Require(Time.realtimeSinceStartup < deadline, "The idle cycle resumes after the relief connector.");
                yield return null;
            }
            report.passed = true;
            report.relief_connector = connector;
            report.resumed_idle = king.CurrentAnimation;
            File.WriteAllText(safe ? "live_core_king_safe_worry_verification.json" : "live_core_king_worry_verification.json",
                JsonUtility.ToJson(report, true) + "\n");
        }

        private static void RemoveBoxes(OriginalInitialBoardView board, int leave, OriginalBoxBoardItem except = null)
        {
            for (int y = board.Grid.Height - 1; y >= 0 && board.Goals.GetGoalLeftCount(11) > leave; y--)
                for (int x = 0; x < board.Grid.Width && board.Goals.GetGoalLeftCount(11) > leave; x++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    if (cell.Mediation.CurrentItem is OriginalBoxBoardItem box && !ReferenceEquals(box, except))
                        cell.ExplodeCurrentItem(OriginalExploder.Next(6));
                }
            Require(board.Goals.GetGoalLeftCount(11) == leave, "Controlled real box hits leave the expected goal count.");
        }

        private static int LeaveOneSafe(OriginalInitialBoardView board)
        {
            OriginalSafeBoardItem kept = null;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.Mediation.CurrentItem is not OriginalSafeBoardItem item) continue;
                if (kept == null) { kept = item; continue; }
                int layers = item.LayerCount;
                for (int hit = 0; hit < layers; hit++) cell.ExplodeCurrentItem(OriginalExploder.Next(6));
            }
            Require(kept != null && kept.LayerCount == 5 && board.Goals.GetGoalLeftCount(20) == 1,
                "Real hits clear three safes and leave one untouched.");
            int footprint = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                if (ReferenceEquals(item, kept) || item is OriginalSafeFakeBoardItem proxy &&
                    ReferenceEquals(proxy.Master, kept)) footprint++;
            }
            return footprint;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable] private sealed class Report
        {
            public bool passed;
            public int worry_observed_after_frames, letters_left, boxes_left_on_relief, safe_footprint_on_relief;
            public string relief_connector, resumed_idle;
        }
    }
}
