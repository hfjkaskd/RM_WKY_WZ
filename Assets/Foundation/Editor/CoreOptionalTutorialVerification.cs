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
    internal static class CoreOptionalTutorialVerification
    {
        private const string ReportPath = "live_core_optional_tutorials_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Input =
            new CoreModalTutorialVerification.GestureSamples();
        private static readonly Vector2Int[] OwlPowerUps =
        {
            new Vector2Int(0, 9), new Vector2Int(1, 0), new Vector2Int(4, 7),
            new Vector2Int(7, 0), new Vector2Int(8, 9)
        };
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGameTouchListener listener;
        private static SortingGroup boardSorting;
        private static OriginalPlayableBoard restartingAttempt;
        private static Report report;
        private static int scenario, step, sample, frame, ownerId, moves;
        private static float deadline;

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
            Require(owner.CurrentLevelNumber == (scenario == 0 ? 4 : 61), "GameScene loads the intended original level.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            boardSorting = board.BoardRoot.GetComponent<SortingGroup>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, owner.Gameplay);
            moves = owner.Gameplay.Moves.LeftMoves;
            step = sample = 0;
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
            Require(Time.realtimeSinceStartup < deadline, $"Optional tutorial timed out, scenario {scenario}, step {step}.");
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
                restartingAttempt = null;
                Begin(controller);
            }
            var game = controller.Gameplay;
            var flow = controller.CoreFlow;
            var guide = flow.DirectedTutorial;

            if (step == 0)
            {
                if (guide == null || !guide.IsGuiding) return;
                Require(!guide.DismissButton.interactable && !game.IsUiTouching, "Board interaction opens before optional dismissal.");
                guide.DismissButton.OnPointerClick(new PointerEventData(EventSystem.current)
                    { button = PointerEventData.InputButton.Left });
                Require(guide.IsGuiding && game.Moves.LeftMoves == moves, "An early dismissal is ignored.");
                step = 1;
                return;
            }
            if (step == 1)
            {
                if (!guide.DismissButton.interactable) return;
                var text = guide.GetComponentInChildren<TextMeshPro>();
                var expected = scenario == 0 ? "Make a match on the GRASS to remove it!" : "Hit OWL STATUE with power-ups to break it!";
                Require(text != null && text.text == expected &&
                    Mathf.Approximately(guide.DismissButton.targetGraphic.color.a, .7f), "The recovered English instruction and shade are visible.");
                int enabled = 0;
                for (int y = 0; y < board.Grid.Height; y++)
                for (int x = 0; x < board.Grid.Width; x++)
                {
                    var cell = board.Grid.GetCell(x, y);
                    bool allowed = scenario == 0 ? x >= 1 && x <= 7 && y >= 1 && y <= 7 :
                        Array.IndexOf(OwlPowerUps, new Vector2Int(x, y)) >= 0;
                    Require(cell.IsTouchEnabled == allowed, "The original per-cell touch restriction is active.");
                    if (cell.IsTouchEnabled) enabled++;
                }
                if (scenario == 0)
                {
                    var mask = guide.GetComponentInChildren<Camera>();
                    Require(mask != null && !mask.enabled && mask.targetTexture.format == RenderTextureFormat.R8 &&
                        !boardSorting.sortAtRoot, "The level-four dim uses the isolated cell mask.");
                    report.level4_enabled_cells = enabled;
                }
                else
                {
                    Require(guide.GetComponentInChildren<Camera>() == null && boardSorting.enabled &&
                        boardSorting.sortAtRoot && boardSorting.sortingOrder == 501 &&
                        Mathf.Approximately(text.rectTransform.sizeDelta.x, 7.5f), "The full original owl board and widened instruction are highlighted.");
                    report.level61_enabled_cells = enabled;
                }
                if (scenario < 2)
                    GrassGameplayVerification.Capture(flow.UiCamera, $"core_optional_level{controller.CurrentLevelNumber}.png");
                step = 2;
            }
            if (step == 2)
            {
                if (scenario == 1)
                {
                    if (sample < 3)
                    {
                        var rocket = board.Grid.GetCell(0, 9);
                        if (sample == 2)
                        {
                            CoreFlowVerification.Click(guide.DismissButton, flow.UiCamera);
                            Require(guide.IsGuiding, "The standard Button defers dismissal until the board can process release.");
                        }
                        Input.Set(rocket, sample++);
                        listener.ManualUpdate();
                        return;
                    }
                }
                else
                {
                    CoreFlowVerification.Click(guide.DismissButton, flow.UiCamera);
                    Require(guide.IsGuiding && !guide.DismissButton.interactable,
                        "The standard Button queues one dismissal and ignores duplicate clicks.");
                }
                step = 3;
            }
            if (guide != null && guide.IsGuiding) return;
            Require(!game.IsUiTouching && !boardSorting.sortAtRoot && board.Grid.GetCell(0, 1).IsTouchEnabled,
                "The guide releases board input and restores sorting.");
            if (scenario == 1)
            {
                if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.CreatedRefills == 0) return;
                report.level61_rocket_owls_cleared = 85 - board.Goals.GetGoalLeftCount(17);
                Require(game.Moves.LeftMoves == moves - 1 && report.level61_rocket_owls_cleared == 9,
                    "The authored rocket spends one move, removes nine owls and refills.");
                report.level61_rocket_dismisses = true;
            }
            else
            {
                Require(game.Moves.LeftMoves == moves, "Optional dismissal spends no move.");
                if (scenario == 0) report.level4_button_dismisses = true;
                else
                {
                    report.level61_button_dismisses = true;
                    report.passed = true;
                    Finish();
                    return;
                }
            }
            if (scenario == 1)
            {
                // Reopening the scene would resume the played board without its opening guide.
                restartingAttempt = game;
                scenario++;
                deadline = Time.realtimeSinceStartup + 30f;
                flow.RetryLevel();
                return;
            }
            string path = controller.gameObject.scene.path;
            controller.Progress.CompleteLevel(60);
            scenario++;
            controller = null;
            deadline = Time.realtimeSinceStartup + 30f;
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
            public bool passed, level4_button_dismisses, level61_button_dismisses, level61_rocket_dismisses;
            public int level4_enabled_cells, level61_enabled_cells, level61_rocket_owls_cleared;
        }
    }
}
