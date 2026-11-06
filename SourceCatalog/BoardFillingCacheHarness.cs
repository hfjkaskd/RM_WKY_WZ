using System;
using Whitebox.Foundation;
namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
namespace Whitebox.Foundation
{
    public interface IOriginalFallIceCubeHelper { }
    public interface IOriginalFallTargetServices { }
    public interface IOriginalFallTraversal { }
    public class OriginalInitialItemAmounts { public bool IsThereItemInLevel(int type) => false; }
    public partial class OriginalBoardItem
    {
        public bool IsAvailableForFall(OriginalGridCell c) => true;
        public bool CanFallCross => true;
        public bool IsSpecialItem => false;
        public bool IsCrossFallBlocking => false;
    }
    public partial class OriginalGridCell { public bool IsFallBlocked(bool a, bool b) => false; }
    public static class OriginalLevelRules
    {
        public static int GetIndexFromPoint(int x, int y, int width) => y * width + x;
        public static int AsCellType(int id) => 1;
    }
}
public static class BoardFillingCacheHarness
{
    private static string State(OriginalBoardFillingCells cache)
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var e in cache.Cells) list.Add(e.Key + "," + (e.Value.Y * 4 + e.Value.X));
        return string.Join("/", list);
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#'); var h = Array.ConvertAll(parts[0].Split(','), int.Parse);
            var data = Array.ConvertAll(parts[1].Split(','), int.Parse); var grid = new OriginalCellGrid(4, 5);
            for (int i = 0; i < data.Length; i++)
            {
                int kind = data[i] & 3;
                if (kind == 3) continue;
                OriginalGridCell cell = kind == 1 ? new OriginalFillingCell(i % 4, i / 4, 1, 0, 1, false, false, 0) : new OriginalGridCell(i % 4, i / 4, 1, 0, kind == 2 ? 1 : 0);
                grid.SetCell(i % 4, i / 4, cell);
                if (kind == 1 && (data[i] & 4) != 0) ((OriginalFillingCell)cell).AddItemToComingFromTopBoard(new OriginalBoardItem(i, 1));
            }
            var cache = new OriginalBoardFillingCells(); var region = new OriginalBoardRegion(h[0], h[1], h[2], h[3]);
            var output = new System.Collections.Generic.List<string>();
            foreach (var command in parts[2].Split(';'))
            {
                string value = "ok";
                try { if (command == "c") cache.Cache(region, grid); else value = cache.CanCrossFall(int.Parse(command.Substring(1))) ? "1" : "0"; }
                catch { value = "error"; }
                output.Add(value + ":" + State(cache));
            }
            Console.WriteLine(string.Join("|", output));
        }
    }
}
