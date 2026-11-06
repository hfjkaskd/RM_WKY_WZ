using System;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class PossibleMoveVerification
    {
        private sealed class Statics : IOriginalSpecialItemStaticState
        {
            public bool IsUnderHoney(OriginalBoardItem item) => item.Mediator.CurrentCell.Owner.StaticTouch.HasStaticItem(4);
            public bool IsUnderRoof(OriginalBoardItem item) => item.Mediator.CurrentCell.Owner.StaticTouch.HasStaticItem(7);
            public bool IsAboveStaticSpreading(OriginalGridCell cell) => cell.IsAboveStaticSpreading;
        }
        private sealed class Chain : IOriginalStaticFallItem
        {
            public int StaticItemType => 6;
            public OriginalGridCell CurrentCell { get; set; }
            public bool DoesBlockFall => true;
            public bool IsTouchBlocking => true;
            public bool IsTouchBlockingExceptChain => false;
            public int RemainingExplodeCount() => 1;
        }
        private sealed class Grid : IOriginalGridAccess
        {
            public readonly OriginalCellGrid Cells = new OriginalCellGrid(3, 3);
            public readonly OriginalPossibleMoveFinder Finder;
            public readonly OriginalInitialShuffleCheck Check;
            public Grid(int[] colors)
            {
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                {
                    var cell = new OriginalGridCell(x, y, 1); Cells.SetCell(x, y, cell);
                    cell.Mediation.SetAllItems(OriginalCoreItemModels.Create(OriginalItemSettings.ForTiledId(colors[x + y * 3])));
                }
                Cells.CacheNeighbors();
                var blocking = new OriginalMatchBlockingState(false, null);
                var strategy = new OriginalBottomLeftHorizontalStrategy(this);
                Finder = new OriginalPossibleMoveFinder(new OriginalBoardPossibleMoveServices(blocking));
                Check = new OriginalInitialShuffleCheck(Finder, new OriginalGridIterator(strategy)) {
                    MatchManager = new Search(new OriginalMatchSearch(strategy, blocking)) };
            }
            public OriginalGridCell CurrentBoardBottomLeftCell => Cells.GetCell(0, 0);
            public OriginalGridCell GetCell(int x, int y) => Cells.GetCell(x, y);
            public bool IsInCurrentBoard(OriginalGridCell cell) => cell.BoardId == 0;
            public int Result() => Finder.CheckBoard(new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(this)));
        }
        private sealed class Search : IOriginalInitialMatchSearch
        {
            private readonly OriginalMatchSearch search;
            public Search(OriginalMatchSearch search) { this.search = search; }
            public bool IsThereAnyMatch(bool excludeFallingItems) => search.IsThereAnyMatch(excludeFallingItems);
        }
        public static void Run()
        {
            var report = new Report();
            try
            {
                var formal = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
                Require(formal.PossibleMoves != null && formal.InitialShuffle != null && !formal.InitialShuffle.ShouldTryAgain(), "Formal first board has a real move without an initial match");
                report.formal_board_verified = true;
                var dead = new Grid(new[] { 1, 2, 3, 3, 1, 2, 2, 3, 1 });
                Require(dead.Result() == 0 && dead.Check.ShouldTryAgain() && !dead.Check.OnlyLightballExists, "Dead fixed model layout requests a retry");
                report.dead_board_verified = true;
                var movable = new Grid(new[] { 1, 2, 1, 3, 1, 4, 2, 3, 2 });
                Require(movable.Result() == 2 && !movable.Check.ShouldTryAgain(), "Available exchange with no prematch accepts the model layout");
                report.movable_board_verified = true;
                var center = dead.GetCell(1, 1);
                center.Mediation.ClearAllItems();
                center.Mediation.SetAllItems(new OriginalLightballBoardItem(9000, () => 0f, new Statics()));
                Require(dead.Result() == 1 && dead.Check.ShouldTryAgain() && dead.Check.OnlyLightballExists, "Only-lightball layout requires a retry and retains its separate flag");
                dead.GetCell(1, 2).Mediation.ClearAllItems();
                dead.GetCell(1, 2).Mediation.SetAllItems(new OriginalLightballBoardItem(9001, () => 0f, new Statics()));
                Require(dead.Result() == 2 && !dead.Check.ShouldTryAgain() && !dead.Check.OnlyLightballExists, "Adjacent special combo is an ordinary move and clears the previous only-lightball flag");
                report.lightball_and_combo_verified = true;
                var chainGrid = new Grid(new[] { 2, 1, 3, 4, 2, 1, 3, 1, 2 });
                var empty = chainGrid.GetCell(1, 1); empty.Mediation.ClearAllItems();
                var side = chainGrid.GetCell(2, 1); side.AddStaticItem(new Chain());
                var found = chainGrid.Finder.CheckNeighborsForMatchType(empty, 1, 3, 1);
                Require(found.GetSwappableNeighborsCount() == 2 && !found.IsValid() && found.RequiredCell == empty && found.Right == null, "Lateral chain is excluded from a three-neighbor pattern while axial matches remain");
                side.StaticTouch.ClearStaticItem(6);
                found = chainGrid.Finder.CheckNeighborsForMatchType(empty, 1, 3, 1);
                Require(found.GetSwappableNeighborsCount() == 3 && found.IsValid() && found.Right == side, "Removing the real static-mediator chain restores the candidate");
                side.HasMostlyPlacedRoofTile = true;
                found = chainGrid.Finder.CheckNeighborsForMatchType(empty, 1, 3, 1);
                Require(found.GetSwappableNeighborsCount() == 2 && !found.IsValid(), "Incoming roof state prevents the neighbor from entering the pattern");
                report.chain_and_incoming_verified = true;
                report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally { File.WriteAllText(Path.Combine(Application.dataPath, "../live_possible_moves_verification.json"), JsonUtility.ToJson(report, true) + "\n"); }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Report
        {
            public bool passed, formal_board_verified, dead_board_verified, movable_board_verified, lightball_and_combo_verified, chain_and_incoming_verified;
            public string failure;
            public string scope = "Real formal first-board finder/match owners plus explicit small model layouts using actual core models, lightballs, cells, mediation, neighbors, static chain and incoming roof state. No item views are required for the model-only search. This does not verify full runtime shuffle animations/events, all static/item types, production account/factories or mobile execution.";
        }
    }
}
