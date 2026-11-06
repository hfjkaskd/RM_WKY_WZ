using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public sealed class OriginalBoardItem { public int Id, Saved = 999; }
    public sealed class OriginalCellMediation
    {
        private readonly OriginalGridCell cell;
        public OriginalBoardItem CurrentItem, NextItem, TargetItem;
        public OriginalCellMediation(OriginalGridCell cell) { this.cell = cell; }
        public void ClearAllItems() { GridItemPopulationHarness.Step("clear," + cell.X); CurrentItem = NextItem = TargetItem = null; }
        public void SetAllItems(OriginalBoardItem item) { GridItemPopulationHarness.Step("set," + cell.X + "," + (item == null ? -1 : item.Id)); CurrentItem = NextItem = TargetItem = item; }
    }
}
public static class GridItemPopulationHarness
{
    public static readonly List<string> Trace = new List<string>();
    private static int[] v;
    private static int calls, nextId;
    private static bool setup;
    private static OriginalGridCell[] cells;
    private static readonly List<OriginalBoardItem> created = new List<OriginalBoardItem>();
    private static OriginalGridItemPopulation owner;
    public static void Step(string label) { if (setup) return; Trace.Add(label); if (++calls == v[3]) throw new InvalidOperationException(); }
    private sealed class Strategy : IOriginalGridIteratorStrategy
    {
        public OriginalGridCell FirstCell() { Step("first"); return v[1] == 0 ? null : cells[0]; }
        public OriginalGridCell GetNext(OriginalGridCell cell) { Step("next," + cell.X); return cell.X + 1 < v[1] ? cells[cell.X + 1] : null; }
    }
    private sealed class Data : IOriginalGridItemPopulationData
    {
        public int GetItemAtCell(int x, int y) { Step("tile," + x); return v[8 + x]; }
        public int GetItemOriginalIdAtCell(int x, int y) { Step("original," + x); return v[12 + x]; }
    }
    private sealed class Context : IOriginalGridItemPopulationContext
    {
        public OriginalBoardItem CreateItemModelForStart(int tiled, int original, OriginalGridCell cell)
        {
            if (v[2] == 2 || v[2] == 3) throw new NullReferenceException();
            Step("create," + cell.X + "," + tiled + "," + original);
            int id = nextId++;
            if (cell.X == v[4]) return null;
            var item = new OriginalBoardItem { Id = id }; created.Add(item); return item;
        }
        public short GetRestoredItemData(int x, int y)
        {
            if (v[2] >= 5) throw new InvalidOperationException();
            Step("saved," + x); return unchecked((short)v[16 + x]);
        }
        public void LoadItemSaveData(OriginalBoardItem item, short saved) { Step("load," + item.Id + "," + saved); item.Saved = saved; }
    }
    private static void Initialized()
    {
        Step("event");
        if (v[5] == 2) throw new InvalidOperationException();
        if (v[5] == 3) owner.OnItemModelsInitialized -= Initialized;
    }
    private static string Item(OriginalBoardItem item) => item == null ? "-1" : item.Id.ToString();
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            v = Array.ConvertAll(line.Split(','), int.Parse); cells = new OriginalGridCell[4]; created.Clear(); nextId = 100;
            for (int i = 0; i < 4; i++)
            {
                cells[i] = new OriginalGridCell(i, i + 10, v[20 + i]);
                var item = new OriginalBoardItem { Id = i }; cells[i].Mediation.CurrentItem = cells[i].Mediation.NextItem = cells[i].Mediation.TargetItem = item;
            }
            setup = true; owner = new OriginalGridItemPopulation(new Strategy(), v[2] == 1 ? null : new Context()); setup = false;
            Trace.Clear(); calls = 0; if (v[5] != 0) owner.OnItemModelsInitialized += Initialized;
            bool fault = false;
            try { for (int i = 0; i < v[6]; i++) owner.CreateItems(v[2] == 4 ? null : new Data(), v[0]); } catch (Exception) { fault = true; }
            var state = new List<string>();
            foreach (var cell in cells) state.Add(Item(cell.Mediation.CurrentItem) + "," + Item(cell.Mediation.NextItem) + "," + Item(cell.Mediation.TargetItem));
            var saved = new List<string>(); foreach (var item in created) saved.Add(item.Id + "," + item.Saved);
            Console.WriteLine((fault ? 1 : 0) + "#" + string.Join("/", state) + "#" + string.Join("/", saved) + "#" + string.Join(";", Trace));
        }
    }
}
