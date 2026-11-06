using System;
using System.Collections.Generic;
using System.IO;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalInitialBoardView
    {
        // CellGridSaveData.set_Item 0x23df694 and LevelRestoreLoader.GetCell
        // 0x23e2df8 index both the save grid and the authored grid by x + y * width.
        private sealed class RestoredGrid : IOriginalGridItemPopulationData
        {
            private readonly FCellGridSaveData grid;
            internal RestoredGrid(FCellGridSaveData grid) { this.grid = grid; }
            internal FCellSaveData Cell(int x, int y) => grid.Grid[OriginalLevelRules.GetIndexFromPoint(x, y, grid.Width)];
            public int GetItemAtCell(int x, int y) => Cell(x, y).CurrentItem?.TiledId ?? 0;
            public int GetItemOriginalIdAtCell(int x, int y) => GetItemAtCell(x, y);
            internal FStaticItemSaveData GetStaticItem(int x, int y, int firstId, int lastId)
            {
                var items = Cell(x, y).StaticItems;
                if (items == null) return null;
                foreach (var item in items)
                    if (item.TiledId >= firstId && item.TiledId <= lastId) return item;
                return null;
            }
        }

        private RestoredGrid restoredGrid;
        internal void ReleaseRestoreGrid() => restoredGrid = null;

        internal FCellGridSaveData CaptureCellGrid()
        {
            var cells = new FCellSaveData[Grid.Width * Grid.Height];
            for (int y = 0; y < Grid.Height; y++)
            for (int x = 0; x < Grid.Width; x++)
            {
                var cell = Grid.GetCell(x, y);
                var grass = cell.StaticTouch.GetStaticItem(2) as OriginalGrassBoardItem;
                var honey = cell.StaticTouch.GetStaticItem(OriginalHoneyBoardItem.StaticType) as OriginalHoneyBoardItem;
                int count = (grass == null ? 0 : 1) + (honey == null ? 0 : 1);
                var statics = count == 0 ? Array.Empty<FStaticItemSaveData>() : new FStaticItemSaveData[count];
                if (grass != null) statics[0] = new FStaticItemSaveData {
                    TiledId = (short)grass.TiledId, Data = (short)grass.LayerCount
                };
                if (honey != null) statics[count - 1] = new FStaticItemSaveData {
                    TiledId = OriginalHoneyBoardItem.TiledId, Data = (short)honey.LayerCount
                };
                cells[OriginalLevelRules.GetIndexFromPoint(x, y, Grid.Width)] = new FCellSaveData {
                    CurrentItem = CaptureItem(cell.Mediation.CurrentItem), StaticItems = statics
                };
            }
            return new FCellGridSaveData {
                Grid = cells, Width = Grid.Width, Height = Grid.Height, ShifterData = Array.Empty<int>()
            };
        }

        internal static FItemSaveData CaptureItem(OriginalBoardItem item) => item == null
            ? new FItemSaveData()
            : new FItemSaveData { TiledId = (short)item.TiledId, Data = item.SaveRestorableData() };

        internal FBoardGoalSaveData[] CaptureGoals()
        {
            var all = Goals.GetAllGoals();
            var saved = new FBoardGoalSaveData[all.Count];
            for (int i = 0; i < saved.Length; i++)
            {
                var values = all[i];
                var types = new short[values.Length];
                var left = new short[values.Length];
                var targets = new short[values.Length];
                for (int j = 0; j < types.Length; j++)
                {
                    types[j] = (short)values[j].GoalType;
                    left[j] = (short)values[j].LeftCount;
                    targets[j] = (short)values[j].TargetCount;
                }
                saved[i] = new FBoardGoalSaveData { GoalType = types, LeftCount = left, TargetCount = targets };
            }
            return saved;
        }

        private static void RestoreGoals(List<Dictionary<int, OriginalGoalTuple>> goals, FBoardGoalSaveData[] saved)
        {
            if (saved == null || saved.Length != goals.Count) throw new InvalidDataException("Saved goal board count.");
            for (int i = 0; i < saved.Length; i++)
            {
                var values = saved[i];
                var types = values.GoalType ?? Array.Empty<short>();
                if (types.Length != goals[i].Count || types.Length != (values.LeftCount?.Length ?? 0) ||
                    types.Length != (values.TargetCount?.Length ?? 0))
                    throw new InvalidDataException("Saved goal count.");
                for (int j = 0; j < types.Length; j++)
                {
                    if (!goals[i].TryGetValue(types[j], out var goal) || values.LeftCount[j] < 0 || values.TargetCount[j] < 0)
                        throw new InvalidDataException("Saved goal does not match the level.");
                    goal.RestoreGoal(values.TargetCount[j], values.LeftCount[j]);
                }
            }
        }
    }
}
