using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public interface IOriginalGameStartShuffle { bool ShouldTryAgain(); }
    public sealed class OriginalBoardItem
    {
        public int Id;
        public int ItemType { get { PossibleMoveHarness.Step("type," + Id); return PossibleMoveHarness.Rows[Id][8]; } }
    }
    public sealed class OriginalCellMediation
    {
        private readonly OriginalGridCell cell;
        public OriginalCellMediation(OriginalGridCell cell) { this.cell = cell; }
        public bool HasCurrentItem() { PossibleMoveHarness.Step("has," + cell.X); return PossibleMoveHarness.Rows[cell.X][0] != 0; }
        public OriginalBoardItem CurrentItem { get { PossibleMoveHarness.Step("item," + cell.X); return PossibleMoveHarness.Rows[cell.X][1] == 0 ? null : PossibleMoveHarness.Items[cell.X]; } }
    }
}
public static class PossibleMoveHarness
{
    private static int[] v;
    public static int[][] Rows;
    public static OriginalBoardItem[] Items;
    private static OriginalGridCell[] cells;
    public static readonly List<string> Trace = new List<string>();
    private static int calls;
    private static bool setup;
    public static void Step(string label)
    {
        if (setup) return;
        Trace.Add(label); calls++;
        if (calls == v[5]) throw new InvalidOperationException();
        if (calls == v[6])
        {
            Rows[v[7]][v[8]] = v[9];
        }
    }
    private sealed class Services : IOriginalPossibleMoveServices, IOriginalInitialMatchSearch
    {
        private bool Value(string label, int id, int field) { Step(label + "," + id); return Rows[id][field] != 0; }
        public bool CanEnterPossibleMatch(OriginalGridCell cell) => Value("enter", cell.X, 2);
        public bool CanEnterPossibleMatchAcceptingChain(OriginalGridCell cell) => Value("accept", cell.X, 3);
        public bool HasSpecialItem(OriginalGridCell cell) => Value("special-cell", cell.X, 4);
        public bool HasTouchBlockingItem(OriginalGridCell cell) => Value("blocking", cell.X, 5);
        public bool HasChain(OriginalGridCell cell) => Value("chain", cell.X, 6);
        public bool IsMatchItem(OriginalBoardItem item) => Value("match", item.Id, 10);
        public bool IsSpecialItem(OriginalBoardItem item) => Value("special", item.Id, 11);
        public bool IsSwappable(OriginalBoardItem item) => Value("swap", item.Id, 12);
        public int GetMatchType(OriginalBoardItem item) => Rows[item.Id][7];
        public bool IsThereAnyMatch(bool exclude) { Step("any," + (exclude ? 1 : 0)); return v[4] != 0; }
    }
    private sealed class Strategy : IOriginalGridIteratorStrategy
    {
        public OriginalGridCell FirstCell() { Step("first"); return v[10] == 0 ? null : cells[0]; }
        public OriginalGridCell GetNext(OriginalGridCell cell) { Step("next," + cell.X); return cell.X + 1 < v[10] ? cells[cell.X + 1] : null; }
    }
    private static string Id(OriginalGridCell cell) => cell == null ? "-1" : cell.X.ToString();
    private static string Match(OriginalSwappableMatch match) => Id(match.RequiredCell) + "," + match.MinNeighborCount + "," + (match.ContainsSpecialItem ? 1 : 0) + "," + Id(match.Top) + "," + Id(match.Right) + "," + Id(match.Bottom) + "," + Id(match.Left);
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var p = line.Split('|'); v = Array.ConvertAll(p[0].Split(','), int.Parse); var raw = Array.ConvertAll(p[1].Split(','), int.Parse);
            Rows = new int[9][]; Items = new OriginalBoardItem[9]; cells = new OriginalGridCell[9];
            for (int i = 0; i < 9; i++) { Rows[i] = new int[13]; Array.Copy(raw, i * 13, Rows[i], 0, 13); Items[i] = new OriginalBoardItem { Id = i }; cells[i] = new OriginalGridCell(i, 0, raw[117 + i]); }
            for (int i = 0; i < 9; i++) for (int d = 0; d < 8; d++)
            { int dx = new[] { -1, 0, 1, 1, 1, 0, -1, -1 }[d], dy = new[] { 1, 1, 1, 0, -1, -1, -1, 0 }[d], x = i % 3 + dx, y = i / 3 + dy; cells[i].Neighbors.Set(d, x < 0 || x >= 3 || y < 0 || y >= 3 ? null : cells[x + y * 3]); }
            var services = new Services(); var finder = new OriginalPossibleMoveFinder(services);
            setup = true; var iterator = new OriginalGridIterator(new Strategy()); setup = false; Trace.Clear(); calls = 0;
            var check = new OriginalInitialShuffleCheck(finder, iterator) { MatchManager = v[11] != 0 ? null : services };
            int result = -99; var match = new OriginalSwappableMatch(); bool fault = false;
            try
            {
                var cell = v[1] < 0 ? null : cells[v[1]];
                switch (v[0])
                {
                    case 0: result = finder.CheckBoard(iterator); break;
                    case 1: result = finder.HasAnySpecialItemAround(cell) ? 1 : 0; break;
                    case 2: match = finder.CheckNeighborForPossibleMatches(cell, v[2], v[3]); break;
                    case 3: match = finder.CheckNeighborsForMatchType(cell, v[2], v[4], v[3]); break;
                    case 4: result = check.ShouldTryAgain() ? 1 : 0; break;
                    case 5:
                        match.MinNeighborCount = v[4]; match.ContainsSpecialItem = v[11] != 0;
                        for (int i = 0; i < 4; i++) if ((v[10] & (1 << i)) != 0) match.SetSwappableNeighbor(cells[i], new[] { 1, 3, 5, 7 }[i]);
                        match.SetSwappableNeighbor(cell, v[3]); result = match.GetSwappableNeighborsCount() * 10 + (match.IsValid(v[2] != 0) ? 1 : 0); break;
                }
            }
            catch (Exception) { fault = true; }
            Console.WriteLine((fault ? 1 : 0) + "#" + result + "#" + (check.OnlyLightballExists ? 1 : 0) + "#" + Match(match) + "#" + string.Join(";", Trace));
        }
    }
}
