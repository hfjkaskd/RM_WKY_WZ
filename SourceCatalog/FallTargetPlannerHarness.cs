using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
namespace Whitebox.Foundation
{
    // Test compilation boundary for unrelated operation accounting; freeze state
    // uses the actual OriginalCellFallState implementation.
    public sealed class OriginalGameOperations { public void OperationStart(int n) { } public void OperationFinish(int n) { } }
    public interface IOriginalMatchFreezeOperations { void FreezeFall(OriginalGridCell cell); }
}
static class FallTargetPlannerHarness
{
    class Item : OriginalBoardItem
    { public int[] V; public Item(int id, int[] values) : base(id, 1) { V = values; MarkViewCreated(); } }
    class Multiple : Item, IOriginalMultipleFallableItem
    {
        readonly Inputs f;
        public Multiple(int id, int[] v, Inputs f) : base(id, v) { this.f = f; }
        public void SetCellAsTarget(OriginalGridCell cell) { f.Record("multiple," + Id + "," + f.CellId(cell)); cell.Mediation.SetTargetItem(this); }
        public bool ShouldDisableCrossFallsForTargetCell(OriginalGridCell cell) { f.Record("disable," + Id + "," + f.CellId(cell)); return V[5] != 0; }
    }
    sealed class Fake : Multiple, IOriginalIceCubeFallFake { public Fake(int id, int[] v, Inputs f) : base(id, v, f) { } }
    sealed class Master : Multiple, IOriginalIceCubeFallMaster { public Master(int id, int[] v, Inputs f) : base(id, v, f) { } }
    sealed class Inputs : IOriginalFallTargetServices, IOriginalFallIceCubeHelper
    {
        public int[] H; public int[][] C;
        public OriginalGridCell[] Cells;
        public Item[] Items;
        public readonly List<string> Trace = new List<string>();
        public readonly Dictionary<int, OriginalFillingCell> Filling = new Dictionary<int, OriginalFillingCell>();
        public int Calls;
        public int CellId(OriginalGridCell cell) { if (cell == null) return -1; for (int i = 0; i < Cells.Length; i++) if (ReferenceEquals(Cells[i], cell)) return i; throw new Exception(); }
        public OriginalGridCell Cell(int id) => id < 0 ? null : Cells[id];
        public void Record(string s) { Trace.Add(s); if (++Calls == H[9]) throw new InvalidOperationException(); }
        public int CurrentBoardIndex => H[0];
        public bool IsMultipleBoardLevel { get { Record("multiple_board"); return H[1] != 0; } }
        public bool CanCurrentBoardCrossFall(int x) { Record("board_cross," + x); return H[2] != 0; }
        public bool IsLevelWon => H[3] != 0;
        public bool IsFallBlocked(OriginalGridCell cell) { int i = CellId(cell); Record("blocked," + i); return C[i][6] != 0; }
        public bool IsAvailableForFall(OriginalBoardItem item, OriginalGridCell cell) { Record("available," + item.Id + "," + CellId(cell)); return ((Item)item).V[1] != 0; }
        public bool CanFallCross(OriginalBoardItem item) { Record("can_cross," + item.Id); return ((Item)item).V[2] != 0; }
        public bool IsSpecialItem(OriginalBoardItem item) { Record("special," + item.Id); return ((Item)item).V[3] != 0; }
        public bool IsCrossFallBlocking(OriginalBoardItem item) { Record("cross_blocking," + item.Id); return ((Item)item).V[4] != 0; }
        public bool IsThereIceCubeInLevel() { Record("level"); Record("ice"); return H[4] != 0; }
        public IOriginalFallIceCubeHelper GetIceCubeHelper() { Record("ice_helper"); return H[8] != 0 ? null : this; }
        public bool HasCrossFallFreeze(OriginalGridCell cell) { int i = CellId(cell); Record("cross_freeze," + i); return C[i][7] != 0; }
        public bool HasIncomingExploder(OriginalGridCell cell) { int i = CellId(cell); Record("incoming," + i); return C[i][8] != 0; }
        public bool HasIceCubeAbove(int x, int y) { Record("above," + x + "," + y); return H[5] != 0; }
        public OriginalGridCell GetNeighborUnderIce(OriginalGridCell cell) { Record("under," + CellId(cell)); return Cell(H[6]); }
        public bool HasIceCubeAtBottomInColumn(int x) { Record("bottom," + x); return H[7] != 0; }
        public IReadOnlyDictionary<int, OriginalFillingCell> CurrentBoardFillingCells => H[10] != 0 ? null : Filling;
        public void Create(string cells, string items)
        {
            C = Array.ConvertAll(cells.Split('/'), s => Array.ConvertAll(s.Split(','), int.Parse));
            var values = Array.ConvertAll(items.Split('/'), s => Array.ConvertAll(s.Split(','), int.Parse));
            Cells = new OriginalGridCell[C.Length];
            Items = new Item[values.Length];
            int width = 0, height = 0;
            foreach (var cell in C)
            {
                width = Math.Max(width, cell[0] + 1);
                height = Math.Max(height, cell[1] + 1);
            }
            if (width * height != Cells.Length) throw new ArgumentException("Expected a row-major rectangle.");
            for (int i = 0; i < Items.Length; i++)
            { int k = values[i][0]; Items[i] = k == 1 ? new Multiple(i+1, values[i], this) : k == 2 ? new Fake(i+1, values[i], this) : k == 3 ? (Item)new Master(i+1, values[i], this) : new Item(i+1, values[i]); }
            for (int i = 0; i < Cells.Length; i++)
            {
                var v = C[i]; var c = v[4] != 0 ? new OriginalFillingCell(v[0],v[1],v[2],v[3],v[4],false,false,0) : new OriginalGridCell(v[0],v[1],v[2],v[3]);
                Cells[i] = c; c.BindGameOperations(new OriginalGameOperations());
                if (v[5] != 0) c.FreezeFall(); c.ShouldFindCross = v[14] != 0;
                var filling = c as OriginalFillingCell;
                if (filling != null) { filling.CanRefill = v[13] != 0; if (v[12] != 0) filling.LastCreatedItem = Items[v[12]-1]; if (v[3] == H[0]) Filling[v[0]] = filling; }
            }
            for (int i = 0; i < Cells.Length; i++)
            {
                var c = Cells[i]; var v = C[i];
                for (int d = 0; d < 8; d++)
                {
                    c.Neighbors.GetPoint(d, out int x, out int y);
                    c.Neighbors.Set(d, x < 0 || x >= width || y < 0 || y >= height ? null : Cells[y * width + x]);
                }
                if (v[10] != 0) c.Mediation.SetCurrentItem(Items[v[10]-1]);
                if (v[9] != 0) c.Mediation.SetNextItem(Items[v[9]-1]);
                if (v[11] != 0) c.Mediation.SetTargetItem(Items[v[11]-1]);
            }
        }
        static int Id(OriginalBoardItem item) => item == null ? 0 : item.Id;
        public string State(OriginalFallRefillColumns columns)
        {
            var result = new List<string>();
            for (int i = 0; i < Cells.Length; i++)
            {
                var c = Cells[i]; var f = c as OriginalFillingCell; int mask = 0;
                if (f != null) for (int j=0;j<Items.Length;j++) if (f.ContainsInTopQueue(Items[j])) mask |= 1<<j;
                result.Add(string.Join(",",new[]{Id(c.Mediation.CurrentItem),Id(c.Mediation.NextItem),Id(c.Mediation.TargetItem),c.ShouldFindCross?1:0,f==null?0:Id(f.LastCreatedItem),f!=null&&f.IsReadyToRefill(columns)?1:0,mask}));
            }
            foreach (var item in Items) result.Add(string.Join(",",new[]{CellId(item.Mediator.CurrentCell?.Owner),CellId(item.Mediator.NextCell?.Owner),CellId(item.Mediator.TargetCell?.Owner)}));
            return string.Join("/",result);
        }
    }
    static void Main()
    {
        string line;
        while ((line=Console.ReadLine())!=null)
        {
            var p=line.Split('#'); var f=new Inputs {H=Array.ConvertAll(p[0].Split(','),int.Parse)}; f.Create(p[1],p[2]);
            var columns=new OriginalFallRefillColumns(); var planner=new OriginalFallTargetPlanner(f,columns); var output=new List<string>();
            foreach (var command in p[3].Split(';'))
            {
                var v=command.Split(','); int n=v.Length>1?int.Parse(v[1]):0, m=v.Length>2?int.Parse(v[2]):0; string result="-"; f.Calls=0;f.Trace.Clear();
                try
                {
                    switch(v[0])
                    {
                        case "n":var item=planner.GetNextItemOfCell(f.Cell(n),f.Cell(m)); result=(item==null?0:item.Id).ToString();break;
                        case "p":result=planner.IsFallPossibleFromCellToCell(f.Cell(n),f.Cell(m))?"1":"0";break;
                        case "f":planner.FindTargetItemOrFillingCell(f.Cell(n),out var filling);result=f.CellId(filling).ToString();break;
                        case "s":result=planner.ShouldFindCross(f.Cell(n),f.Cell(m))?"1":"0";break;
                        case "c":planner.FindCrossItem(f.Cell(n));break;
                        case "x":var found=planner.FindTargetItemFromCrossCell(f.Cell(n),f.Cell(m),int.Parse(v[3]));result=(found==null?0:found.Id).ToString();break;
                        case "r":result=planner.AddTopBoardRemainingItemsToFillingCell(f.Cell(n) as OriginalFillingCell)?"1":"0";break;
                        case "q":result=planner.AddRemainingItemsToFillingCellQueue(n)?"1":"0";break;
                        case "d":columns.DisableReFillForTopColumn(n);break;
                        case "e":columns.EnableReFillForTopColumn(n);break;
                        case "z":f.Cell(n).Mediation.ClearTargetItem();break;
                        case "t":f.Cell(n).Mediation.SetTargetItem(f.Items[m-1]);break;
                        case "h":f.H[n]=m;break;
                    }
                }
                catch {result="error";}
                output.Add(result+":"+f.State(columns)+":"+string.Join(";",f.Trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
