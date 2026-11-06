using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Read-only observation of the scene's real player path. It installs no
    // gameplay services, changes no cell and issues no swap/explosion commands.
    public static class PlayableBoardObservation
    {
        [MenuItem("Recovery/Observe Live Player Board")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run while playing.");
            var controller = UnityEngine.Object.FindObjectOfType<FoundationController>();
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var input = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            var game = controller.Gameplay ?? throw new InvalidOperationException("Scene gameplay is not bound.");
            var report = new Report { state = board.State.GetGameState(), left_moves = board.Moves.LeftMoves,
                made_moves = board.Moves.MadeMoves, goals_left = board.Goals.GetGoalLeftCount(11),
                accepted_swaps = game.AcceptedSwaps, rejected_swaps = game.RejectedSwaps,
                created_refills = game.CreatedRefills, created_specials = game.CreatedSpecials,
                input_bound = input.Listener != null, can_user_move = game.CanUserMakeMove(),
                all_items_steady = game.Fall.IsAllItemsSteady, has_operations = board.State.HasAnyOperation() };
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!cell.IsNormalCell) continue;
                var item = cell.Mediation.CurrentItem;
                if (item == null) { report.empty_cells++; continue; }
                if (cell.IsFallFrozen) report.frozen_cells++;
                if (!ReferenceEquals(item, cell.Mediation.NextItem) || !ReferenceEquals(item, cell.Mediation.TargetItem)) report.unsettled_slots++;
                if (item is OriginalRocketBoardItem) report.rockets++;
                if (item is OriginalTntBoardItem) report.tnt++;
                if (item is OriginalLightballBoardItem) report.lightballs++;
                if (item is OriginalPropellerBoardItem) report.propellers++;
                var view = board.FallMotion.GetView(item);
                if (view == null) continue;
                var p = view.Position; var target = cell.GetViewPosition();
                report.max_position_error = Mathf.Max(report.max_position_error, Vector3.Distance(new Vector3(p.X, p.Y, p.Z), target));
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../live_player_board_observation.json"), JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("Live player board observed: swaps=" + report.accepted_swaps + ", moves=" + report.left_moves
                + ", empty=" + report.empty_cells + ", frozen=" + report.frozen_cells + ", specials=" + report.created_specials);
        }
        [Serializable] private sealed class Report
        {
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public bool input_bound, can_user_move, all_items_steady, has_operations;
            public int state, left_moves, made_moves, goals_left, accepted_swaps, rejected_swaps, created_refills, created_specials;
            public int empty_cells, frozen_cells, unsettled_slots, rockets, tnt, lightballs, propellers;
            public float max_position_error;
            public string scope = "Read-only snapshot of live formal scene composition after actual player input; no diagnostic services, seed preflight, cell mutation or automatic test commands installed. Does not establish full session, special activation/combo, settlement or original rendered parity.";
        }
    }
}
