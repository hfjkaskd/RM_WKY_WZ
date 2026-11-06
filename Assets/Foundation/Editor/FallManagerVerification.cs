using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Integration diagnostic only. Item availability, creation and motion are
    // explicit inputs; no fixture is installed into the production scene.
    public static class FallManagerVerification
    {
        private sealed class Fall : IOriginalFallUpdateComponent
        {
            public int Updates, FillingBindings;
            public bool Steady;
            public bool IsItemSteady => Steady;
            public void ManualUpdate() { Updates++; }
            public void SetFillingCell(OriginalFillingCell cell) { FillingBindings++; }
        }
        private sealed class Inputs : IOriginalFallTargetServices, IOriginalFallTraversal, IOriginalFillingItemCreation
        {
            public readonly OriginalCellGrid Grid = new OriginalCellGrid(2, 3);
            public readonly Dictionary<int, OriginalFillingCell> Filling = new Dictionary<int, OriginalFillingCell>();
            public readonly List<OriginalBoardItem> Items = new List<OriginalBoardItem>();
            public readonly List<Fall> Falls = new List<Fall>();
            public readonly OriginalBoardGridAccess Access;
            private readonly IOriginalGridIteratorStrategy vertical, horizontal;
            public int Creates;
            public Inputs()
            {
                var layout = OriginalBoardLayout.Create(2, 3, new int[6], false, false);
                Access = new OriginalBoardGridAccess(Grid, layout);
                for (int y = 0; y < 3; y++)
                for (int x = 0; x < 2; x++)
                {
                    OriginalGridCell cell = y == 2 ? new OriginalFillingCell(x, y, 2, 0, 1, false, false, 0) : new OriginalGridCell(x, y, 1);
                    Grid.SetCell(x, y, cell);
                    if (y == 2) Filling.Add(x, (OriginalFillingCell)cell);
                }
                Grid.CacheNeighbors();
                for (int x = 0; x < 2; x++) Grid.GetCell(x, 1).Mediation.SetAllItems(NewItem());
                vertical = new OriginalTotalBottomLeftVerticalStrategy(Access);
                horizontal = new OriginalTotalBottomLeftHorizontalStrategy(Access);
            }
            private OriginalBoardItem NewItem()
            {
                var item = new OriginalBoardItem(Items.Count + 1, 1);
                var fall = new Fall(); item.FallComponent = fall; Items.Add(item); Falls.Add(fall); return item;
            }
            public OriginalBoardItem CreateItemForFillingCellAt(float x, float y, OriginalFillingCell cell)
            {
                Require(x == cell.X && y == cell.Y + 1f, "Native filling spawn coordinates");
                Creates++; var item = NewItem(); cell.AddCreatedItem(item); return item;
            }
            public bool IsPortalLevel => false;
            public int BoardStartY => Access.BoardStartY;
            public IOriginalGridIteratorStrategy GetIteratorStrategy(int type) => type == 4 ? vertical : type == 5 ? horizontal : throw new InvalidOperationException();
            public int CurrentBoardIndex => 0;
            public bool IsMultipleBoardLevel => false;
            public bool CanCurrentBoardCrossFall(int x) => true;
            public bool IsLevelWon => false;
            public bool IsFallBlocked(OriginalGridCell cell) => false;
            public bool IsAvailableForFall(OriginalBoardItem item, OriginalGridCell toCell) => true;
            public bool CanFallCross(OriginalBoardItem item) => true;
            public bool IsSpecialItem(OriginalBoardItem item) => item.IsSpecialItem;
            public bool IsCrossFallBlocking(OriginalBoardItem item) => false;
            public bool IsThereIceCubeInLevel() => false;
            public IOriginalFallIceCubeHelper GetIceCubeHelper() => throw new InvalidOperationException();
            public bool HasCrossFallFreeze(OriginalGridCell cell) => false;
            public bool HasIncomingExploder(OriginalGridCell cell) => false;
            public IReadOnlyDictionary<int, OriginalFillingCell> CurrentBoardFillingCells => Filling;
        }
        [Serializable] private sealed class Report
        {
            public bool passed = true;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Concrete target planner, fall manager, full-grid strategies, board/grid adapter, bidirectional mediation, held filling-item placement, column disable/resume and operation accounting run together in Unity. Availability, creation and per-item motion are explicit diagnostic inputs. No production fixture is installed; full physics and player-driven gameplay remain incomplete.";
        }
        [MenuItem("Recovery/Verify Fall Manager")]
        public static void Run()
        {
            var input = new Inputs(); var columns = new OriginalFallRefillColumns();
            var targets = new OriginalFallTargetPlanner(input, columns);
            var operations = new OriginalGameOperations(() => Time.frameCount);
            var manager = new OriginalFallManager(columns, targets, input, operations);
            manager.InitializeGrid(input);
            manager.ManualUpdate();
            Require(input.Creates == 2 && input.Items.Count == 4, "Two real filling placement calls");
            for (int x = 0; x < 2; x++)
            {
                Require(ReferenceEquals(input.Grid.GetCell(x, 0).Mediation.TargetItem, input.Items[x]), "Original items reserve the lower row");
                Require(ReferenceEquals(input.Grid.GetCell(x, 1).Mediation.TargetItem, input.Items[x + 2]), "Created items reserve the vacated row");
                Require(input.Falls[x + 2].FillingBindings == 1, "New item receives its filling cell");
            }
            Require(!manager.IsAllItemsSteady && operations.Container.GetCount(0) == 1, "Unsteady items keep one falling operation");
            manager.ManualUpdate();
            Require(input.Creates == 2, "Held items are reused across target recalculation");
            foreach (var fall in input.Falls) Require(fall.Updates == 2, "All target items update on each frame");
            columns.DisableReFillForTopColumn(0); manager.ManualUpdate();
            Require(input.Falls[2].Updates == 2 && input.Falls[3].Updates == 3, "Disabled column suppresses only its held filling item");
            columns.EnableReFillForTopColumn(0); manager.ManualUpdate();
            Require(!columns.IsTopBoardCheckPending(0), "Pending column transfer completes after fall updates");
            foreach (var fall in input.Falls) fall.Steady = true;
            manager.ManualUpdate();
            Require(manager.IsAllItemsSteady && operations.Container.GetCount(0) == 0, "Steady aggregate releases falling operation");
            manager.Enable(false); operations.OperationStart(0); manager.ManualUpdate();
            Require(operations.Container.GetCount(0) == 0 && manager.IsAllItemsSteady, "Disabled update resets operation and retains steady flag");
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "fall_manager_unity_verification.json"), JsonUtility.ToJson(new Report(), true));
            Debug.Log("Fall manager integration verification passed.");
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
