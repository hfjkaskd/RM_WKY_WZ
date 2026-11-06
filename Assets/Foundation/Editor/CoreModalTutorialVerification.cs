using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreModalTutorialVerification
    {
        private const string ReportPath = "live_core_modal_tutorials_verification.json";
        private static readonly int[] levels = { 21, 31, 41, 51, 71, 81, 91 };
        private static readonly int[] expectedRaised = { 4, 61, 5, 37, 5, 2, 5 };
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell from, to;
        private static readonly GestureSamples input = new GestureSamples();
        private static readonly List<SortingGroup> highlighted = new List<SortingGroup>();
        private static Report report;
        private static int index, frame, movesBefore, sample, previousOwner, dismissalRequestedAt;
        private static float deadline;
        private static bool triedEarly;

        public static void Run(FoundationController owner)
        {
            report = new Report();
            index = 0;
            frame = -1;
            Begin(owner);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            Require(owner.CurrentLevelNumber == levels[index], "Formal GameScene loaded the intended original level.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(input, owner.Gameplay);
            sample = -1;
            dismissalRequestedAt = -1;
            triedEarly = false;
            movesBefore = owner.Gameplay.Moves.LeftMoves;
            deadline = Time.realtimeSinceStartup + 25f;
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
            int level = levels[index];
            Require(Time.realtimeSinceStartup < deadline, "Timed out on original modal tutorial " + level);
            if (controller == null)
            {
                var next = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (next == null || next.GetInstanceID() == previousOwner || next.Gameplay == null) return;
                Begin(next);
            }

            var game = controller.Gameplay;
            if (sample < 0 && dismissalRequestedAt < 0)
            {
                var tutorial = controller.CoreFlow.ModalTutorial;
                if (tutorial == null) return;
                Require(game.IsUiTouching && tutorial.BlocksBoardInput, "Modal tutorial owns the real board input gate.");
                if (!triedEarly && !tutorial.DismissButton.interactable)
                {
                    tutorial.DismissButton.OnPointerClick(new PointerEventData(EventSystem.current)
                        { button = PointerEventData.InputButton.Left });
                    input.Set(board.Grid.GetCell(0, 0), 0);
                    listener.ManualUpdate();
                    Require(tutorial.BlocksBoardInput && listener.SelectedCell == null && game.Moves.LeftMoves == movesBefore,
                        "An early dismissal and board press are ignored.");
                    triedEarly = true;
                }
                if (!tutorial.DismissButton.interactable) return;
                Require(triedEarly, "The opening delay was observed before the tutorial became interactive.");
                var text = tutorial.GetComponentInChildren<TextMeshPro>();
                var canvas = tutorial.DismissButton.targetGraphic.canvas;
                Require(text != null && !string.IsNullOrEmpty(text.text) && Mathf.Approximately(tutorial.DismissButton.targetGraphic.color.a, .7f),
                    "Original tutorial text and full dim alpha are presented.");
                highlighted.Clear();
                foreach (var group in UnityEngine.Object.FindObjectsOfType<SortingGroup>())
                    if (group.enabled && group.sortAtRoot && group.sortingLayerID == canvas.sortingLayerID &&
                        (group.sortingOrder == canvas.sortingOrder + 1 || group.sortingOrder == canvas.sortingOrder + 10))
                        highlighted.Add(group);
                Require(highlighted.Count == expectedRaised[index], "Original highlight group count at level " + level + ": " + highlighted.Count);
                if (level == 41)
                    Require(Mathf.Approximately(text.rectTransform.sizeDelta.x, 7.5f), "Original potion instruction width.");
                string instruction = text.text;
                GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, "core_modal_level" + level + ".png");
                dismissalRequestedAt = Time.frameCount;
                CoreFlowVerification.Click(tutorial.DismissButton, controller.CoreFlow.UiCamera);
                input.Set(board.Grid.GetCell(0, 0), 2);
                listener.ManualUpdate();
                Require(tutorial.BlocksBoardInput && game.IsUiTouching && !tutorial.DismissButton.interactable &&
                    listener.SelectedCell == null && game.Moves.LeftMoves == movesBefore,
                    "The dismissal release stays gated and cannot spend a move.");
                report.steps.Add(new Observation { level = level, raisedGroups = highlighted.Count, instruction = instruction });
                return;
            }
            if (sample < 0)
            {
                var tutorial = controller.CoreFlow.ModalTutorial;
                if (tutorial != null && tutorial.BlocksBoardInput) return;
                int delay = Time.frameCount - dismissalRequestedAt;
                Require(delay >= 4 && !game.IsUiTouching, "A short Button click releases the board after four frames.");
                report.steps[index].dismissalObservedFrames = delay;
                foreach (var group in highlighted)
                    Require(!group.sortAtRoot, "Tutorial sorting is restored on dismissal.");
                if (level == 81)
                {
                    from = board.Grid.GetCell(5, 0);
                    to = board.Grid.GetCell(6, 0);
                }
                else
                    Require(CoreCupboardVerification.FindNaturalSwap(board, out from, out to, true), "A legal color swap is available.");
                sample = 0;
                return;
            }
            if (sample < 3)
            {
                input.Set(sample == 0 ? from : to, sample);
                listener.ManualUpdate();
                sample++;
                return;
            }
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch) return;
            Require(game.AcceptedSwaps == 1 && game.Moves.LeftMoves == movesBefore - 1, "One legal swap spends one move after dismissal.");
            report.steps[index].movesSpent = movesBefore - game.Moves.LeftMoves;
            if (++index == levels.Length) { report.passed = true; Finish(); return; }

            previousOwner = controller.GetInstanceID();
            string path = controller.gameObject.scene.path;
            // Select the next test level through the scene's isolated progress connection.
            controller.Progress.CompleteLevel(levels[index] - 1);
            controller = null;
            deadline = Time.realtimeSinceStartup + 25f;
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
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
            public bool passed;
            public List<Observation> steps = new List<Observation>();
        }

        [Serializable]
        private sealed class Observation
        {
            public int level, raisedGroups, movesSpent, dismissalObservedFrames;
            public string instruction;
        }

        internal sealed class GestureSamples : IOriginalGameTouchInput
        {
            private Vector2 position;
            private int phase;
            public void Set(OriginalGridCell cell, int sample)
            {
                position = cell.GetViewPosition();
                phase = sample;
            }
            public Vector2 GetMouseWorldPosition() => position;
            public bool GetMouseButtonDown() => phase == 0;
            public bool GetMouseButton() => phase < 2;
            public bool GetMouseButtonUp() => phase == 2;
        }
    }
}
