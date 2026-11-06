using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class PropellerTargetVerification
    {
        public static void Run()
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            var reserved = new List<IOriginalExplodeTarget>();
            var payloads = new List<OriginalExplodeData>();
            int difficultyBefore = game.GetLevelRandom().DifficultySeedGenerationCount;
            int boxes = 0, moves = game.Moves.LeftMoves;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                if (board.Grid.GetCell(x, y)?.Mediation.CurrentItem is OriginalBoxBoardItem) boxes++;
            try
            {
                Require(boxes == 36, "Fresh original first board has 36 box targets.");
                for (int i = 0; i < boxes; i++)
                {
                    var data = OriginalExploder.Next(8, new OriginalCellPoint(4, 4));
                    var target = game.GuidedTargets.FindForPropeller(data, null);
                    Require(target is OriginalBoxBoardItem && !reserved.Contains(target), "Unreserved box receives the highest native score.");
                    reserved.Add(target); payloads.Add(data);
                    Require(target.IncomingExploderCount == 1 && target.RemainingExplodeCount() == 0, "Reservation consumes exactly one pending hit.");
                    target.AddIncomingExploder(data);
                    Require(target.IncomingExploderCount == 1, "Repeated explode id does not reserve twice.");
                }
                var extraData = OriginalExploder.Next(8, new OriginalCellPoint(4, 4));
                var extra = game.GuidedTargets.FindForPropeller(extraData, null);
                Require(extra is OriginalMatchBoardItem, "Exhausted positive-score targets are skipped before zero-score matches.");
                reserved.Add(extra); payloads.Add(extraData);
                var keepData = OriginalExploder.Next(30, new OriginalCellPoint(4, 4));
                Require(ReferenceEquals(extra, game.GuidedTargets.FindForPropeller(keepData, extra)), "Existing target is retained at the zero-score bucket.");
                Require(extra.IncomingExploderCount == 1, "Keeping a target does not add a second incoming exploder.");
                Require(game.GetLevelRandom().DifficultySeedGenerationCount == difficultyBefore, "Guided targeting does not consume the difficulty RNG.");
            }
            finally
            {
                for (int i = 0; i < reserved.Count; i++)
                {
                    reserved[i].RemoveIncomingExploder(payloads[i]);
                    reserved[i].RemoveIncomingExploder(payloads[i]);
                }
            }
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!cell.IsNormalCell) continue;
                var item = cell.Mediation.CurrentItem;
                Require(item != null && item.IncomingExploderCount == 0 && ReferenceEquals(item, cell.Mediation.NextItem)
                    && ReferenceEquals(item, cell.Mediation.TargetItem) && !cell.IsFallFrozen, "Releasing reservations leaves all 72 live cells intact.");
            }
            Require(game.Moves.LeftMoves == moves && !board.State.HasAnyOperation(), "Target queries do not spend a player move or leave an operation.");
            File.WriteAllText(Path.Combine(Application.dataPath, "../live_propeller_targets_verification.json"),
                JsonUtility.ToJson(new Report { passed = true, reserved_boxes = boxes, released_reservations = reserved.Count,
                    difficulty_rng_unchanged = true, stable_cells = 72 }, true) + "\n");
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Report
        {
            public bool passed, difficulty_rng_unchanged;
            public int reserved_boxes, released_reservations, stable_cells;
            public string scope = "Actual formal-scene target owner, native score ordering, pending-hit reservations/release, difficulty RNG independence. Does not verify propeller activation or flight.";
        }
    }
}
