using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    // Drives one opening move on each requested, unmodified board through the live touch listener.
    internal static class CoreAuthoredOpeningVerification
    {
        internal const string ReportPath = "live_core_authored_openings_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Input = new();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGameplayHud hud;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell from, to;
        private static Report report;
        private static Observation current;
        private static int index, frame, sample, previousOwner, initialRefills;
        private static float deadline, started, settledAt;

        internal static int[] ReadRequestedLevels(int firstLevel = 22, bool allowRetries = false)
        {
            var args = Environment.GetCommandLineArgs();
            int argument = Array.IndexOf(args, "-recoveryOpeningLevels");
            if (argument < 0 || argument + 1 >= args.Length)
                throw new ArgumentException("Pass the ordered levels to verify with -recoveryOpeningLevels.");
            var parts = args[argument + 1].Split(',');
            var levels = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out levels[i]) || levels[i] < firstLevel ||
                    levels[i] > CoreLevelCatalog.SupportedLevelCount ||
                    (i > 0 && (levels[i] < levels[i - 1] || (!allowRetries && levels[i] == levels[i - 1]))))
                {
                    string order = allowRetries ? "ascending (repeats are retry slots)" : "unique, ascending";
                    throw new ArgumentException($"Supply {order} level numbers between {firstLevel} and {CoreLevelCatalog.SupportedLevelCount}.");
                }
            }
            return levels;
        }

        internal static void Run(FoundationController owner)
        {
            report = new Report { requested_levels = ReadRequestedLevels() };
            index = 0;
            frame = -1;
            previousOwner = 0;
            Begin(owner);
            Save();
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            hud = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>();
            var game = owner.Gameplay;
            int level = report.requested_levels[index];
            var config = OriginalLevelReader.Load("Levels/level_" + level);
            Require(owner.CurrentLevelNumber == level && game.Session.Name == config.Name && game.State.IsPlaying() &&
                owner.CoreFlow.IsInitialized && game.Moves.LeftMoves == config.Move && game.Moves.MadeMoves == 0,
                "Formal entry loads the original session and move budget.");
            Require(board.Grid.Width == config.Grid.Width && board.Grid.Height == config.Grid.Height,
                "The active grid has the original dimensions.");
            Require(hud.moveText.text == config.Move.ToString(), "The HUD displays the authored starting budget.");
            var views = game.GoalPanel.Views;
            Require(config.Goals.Length == views.Count, "All authored goals have a HUD view.");
            foreach (var goal in config.Goals)
            {
                int slot = board.Goals.GetGoalIndex(goal.Goal);
                Require(slot >= 0 && views[slot].goalImage.sprite != null &&
                    board.Goals.GetGoalLeftCount(goal.Goal) == goal.Count &&
                    views[slot].countText.text == board.Goals.GetGoalDisplayCount(goal.Goal).ToString(),
                    "Each starting goal uses the original count, icon and HUD slot.");
            }

            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, game);
            current = new Observation { level = level, session_name = config.Name, move_budget = config.Move };
            report.levels.Add(current);
            from = to = null;
            sample = 0;
            started = Time.realtimeSinceStartup;
            deadline = started + 25f;
            settledAt = -1f;
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
            Require(Time.realtimeSinceStartup < deadline, "Timed out waiting for the original opening to complete.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.GetInstanceID() == previousOwner || owner.Gameplay == null) return;
                Begin(owner);
            }

            var game = controller.Gameplay;
            bool steady = game.Fall.IsAllItemsSteady && !board.State.HasAnyOperation() && !game.Auto.IsThereAWaitingMatch;
            if (from == null)
            {
                var flow = controller.CoreFlow;
                var modal = flow.ModalTutorial;
                if (modal != null && modal.BlocksBoardInput)
                {
                    if (modal.DismissButton.interactable) CoreFlowVerification.Click(modal.DismissButton, flow.UiCamera);
                    return;
                }
                var directed = flow.DirectedTutorial;
                if (directed != null && directed.IsGuiding && directed.DismissButton != null)
                {
                    if (directed.DismissButton.interactable) CoreFlowVerification.Click(directed.DismissButton, flow.UiCamera);
                    return;
                }
                if (!steady || game.IsUiTouching || !game.CanUserMakeMove()) return;
                from = flow.MatchHints.ActiveSource;
                to = flow.MatchHints.ActiveDestination;
                if (from == null && Time.realtimeSinceStartup - started >= 5f)
                    from = FindTappablePower();
                if (from == null) return;
                current.from_game_hint = flow.MatchHints.ActiveSource == from;
                current.from = CheckCameraCell(game, flow.UiCamera, from);
                current.to = CheckCameraCell(game, flow.UiCamera, to ?? from);
                current.empty_before = CountEmptyCells();
                initialRefills = game.CreatedRefills;
            }

            if (sample < 3)
            {
                Input.Set(sample == 0 ? from : to ?? from, sample);
                listener.ManualUpdate();
                sample++;
                return;
            }
            if (!steady)
            {
                settledAt = -1f;
                return;
            }
            if (settledAt < 0f) settledAt = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - settledAt < .30f) return;
            Require(game.AcceptedSwaps == (to != null ? 1 : 0) && game.RejectedSwaps == 0 &&
                game.Moves.MadeMoves == 1 && game.Moves.LeftMoves == current.move_budget - 1,
                "The live gesture spends one move and accepts the expected swap or tap.");
            Require(hud.moveText.text == game.Moves.LeftMoves.ToString(), "The HUD tracks the spent move.");
            current.refills = game.CreatedRefills - initialRefills;
            for (int color = 1; color <= 5; color++)
                current.colors_cleared += game.Session.MatchItemCounts.GetCount(color);
            Require(current.refills > 0 || current.colors_cleared > 0, "The move clears or refills the authored board.");
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!cell.IsNormalCell) continue;
                var slots = cell.Mediation;
                Require(ReferenceEquals(slots.CurrentItem, slots.NextItem) && ReferenceEquals(slots.CurrentItem, slots.TargetItem),
                    $"The settled cell ({x}, {y}) has inconsistent current, next or target slots.");
                if (slots.CurrentItem != null) continue;
                current.empty_after++;
                Require(!(cell is OriginalFillingCell fill) || !fill.CanRefill || cell.IsFallBlocked() || cell.IsFallFrozen,
                    $"The refill source ({x}, {y}) stayed empty after settling.");
            }
            if (current.empty_after > current.empty_before)
                current.fall_snapshot = CaptureFallBoard(game);
            current.move_spent = true;
            if (++index == report.requested_levels.Length) { report.passed = true; Finish(); return; }
            Save();

            previousOwner = controller.GetInstanceID();
            string path = controller.gameObject.scene.path;
            // Select the next starting board via the isolated progress database.
            controller.Progress.CompleteLevel(report.requested_levels[index] - 1);
            controller = null;
            deadline = Time.realtimeSinceStartup + 25f;
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private static OriginalGridCell FindTappablePower()
        {
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.Mediation.CurrentItem is OriginalSpecialBoardItem &&
                    OriginalCellTap.CanTapCurrentItem(cell, out _)) return cell;
            }
            return null;
        }

        private static int CountEmptyCells()
        {
            int count = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.IsNormalCell && cell.Mediation.CurrentItem == null) count++;
            }
            return count;
        }

        private static FallSnapshot CaptureFallBoard(OriginalPlayableBoard game)
        {
            Require(board.Layout.Count == 1 && !board.InitialItemAmounts.IsThereItemInLevel(51),
                "The settled fall replay uses a single ordinary board without ice.");
            int width = board.Grid.Width;
            int height = board.Grid.Height;
            var cells = new FallCell[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                var filling = cell as OriginalFillingCell;
                cells[x + y * width] = new FallCell
                {
                    cell_type = cell.CellType,
                    board_id = cell.BoardId,
                    fill_type = cell.FillType,
                    tiled_id = item?.TiledId ?? 0,
                    frozen = cell.IsFallFrozen,
                    blocked = cell.IsFallBlocked(),
                    should_find_cross = cell.ShouldFindCross,
                    refill_ready = filling != null && filling.CanRefill && filling.IncomingItemCountFromTopBoard == 0,
                    item_available = item != null && item.IsAvailableForFall(null),
                    item_can_cross = item != null && item.CanFallCross,
                    item_is_special = item != null && item.IsSpecialItem,
                };
            }
            return new FallSnapshot
            {
                width = width, height = height, active_board = board.Layout.CurrentBoardIndex,
                won = game.IsLevelWon, cells = cells
            };
        }

        private static Vector2Int CheckCameraCell(OriginalPlayableBoard game, Camera camera, OriginalGridCell cell)
        {
            var screen = camera.WorldToScreenPoint(cell.GetViewPosition());
            var world = camera.ScreenToWorldPoint(screen);
            Require(camera.pixelRect.Contains(screen) && ReferenceEquals(game.GetCellFromWorldPosition(world), cell),
                "The camera maps the visible gesture point back to the same live cell.");
            return new Vector2Int(cell.X, cell.Y);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException($"Level {report.requested_levels[index]}. {message}");
        }

        private static void Save() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        private static void Finish()
        {
            EditorApplication.update -= Tick;
            report.status = "finished";
            Save();
        }

        [Serializable] private sealed class Observation
        {
            public int level, move_budget, refills, colors_cleared, empty_before, empty_after;
            public string session_name;
            public Vector2Int from, to;
            public bool from_game_hint, move_spent;
            public FallSnapshot fall_snapshot;
        }
        [Serializable] private sealed class FallSnapshot
        {
            public int width, height, active_board;
            public bool won;
            public FallCell[] cells;
        }
        [Serializable] private sealed class FallCell
        {
            public int cell_type, board_id, fill_type, tiled_id;
            public bool frozen, blocked, should_find_cross, refill_ready;
            public bool item_available, item_can_cross, item_is_special;
        }
        [Serializable] private sealed class Report
        {
            public string status = "running", failure;
            public bool passed;
            public int[] requested_levels;
            public List<Observation> levels = new();
        }
    }
}
