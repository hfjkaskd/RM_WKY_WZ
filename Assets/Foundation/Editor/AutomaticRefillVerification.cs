using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit removal/seed/level-status fixture. All target decisions, item
    // availability, blockers, creation, mediation, motion and views are runtime.
    public static class AutomaticRefillVerification
    {
        private sealed class Inputs : IOriginalFallLevelStatus, IOriginalPredefinedPreparationContext,
            IOriginalInitialItemCreationContext, IOriginalFillSetGenerationContext, IOriginalFillingPlacementSpace
        {
            public OriginalLevelRandomSource Random;
            public OriginalInitialItemCreation Initial;
            public OriginalFillSetGeneration Generation;
            public OriginalMatchTileColors Colors;
            public OriginalLevelSetSelection[] Sets;
            public int[] FirstIds;
            public IOriginalItemViewCreationContext Views;
            public bool IsPortalLevel => false;
            public bool IsMultipleBoardLevel => false;
            public bool IsLevelWon => false;
            public int[][] PotionColors => null;
            public bool ContainsDrillMasterPoint(OriginalGridCell cell) => throw new InvalidOperationException();
            public OriginalBoardItem CreateItem(OriginalItemSettings settings) => Views.CreateItem(settings);
            public void OnNewItemCreated(int type) => Generation.OnNewItemCreated(type);
            public OriginalLevelRandomSource GetLevelRandom() => Random;
            public int SelectMatchTileColor(int id, OriginalLevelSetSelection first, OriginalLevelRandomSource random)
            { Require(ReferenceEquals(first, Sets[0]) && ReferenceEquals(random, Random), "Shared palette owners"); return Colors.SelectMatchTileColor(id, FirstIds); }
            public IOriginalPredefinedItemCreator ItemCreator => Initial;
            public bool IsTuningEnabled => false;
            public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell) => throw new InvalidOperationException();
            public int GetLimitCount(int goal) => throw new InvalidOperationException();
            public int GetGoalTargetCount(int goal) => throw new InvalidOperationException();
            public void ReportRandomColorFallback(int total) => throw new InvalidOperationException();
            public float OffsetX => throw new InvalidOperationException();
            public float OffsetY => throw new InvalidOperationException();
            public void GetCellViewPosition(OriginalFillingCell cell, out float x, out float y) => throw new InvalidOperationException();
        }
        private sealed class RecordingCreation : IOriginalFillingItemCreation
        {
            public IOriginalFillingItemCreation Actual;
            public readonly List<OriginalBoardItem> Items = new List<OriginalBoardItem>();
            public OriginalBoardItem CreateItemForFillingCellAt(float x, float y, OriginalFillingCell cell)
            {
                var item = Actual.CreateItemForFillingCellAt(x, y, cell);
                Require(x == 4f && y == 8f && item != null && item.HasView, "Actual configured fill source");
                Require(((OriginalMatchItemSpriteView)item.SwapView).IsFillMaskEnabled, "Entry mask starts enabled");
                Items.Add(item); return item;
            }
        }
        [Serializable] private sealed class Report
        {
            public bool passed = true, automatic_predefined, automatic_random, existing_piece_fall, steady_operations, board_and_pool_restored;
            public int frames, created_views;
            public float max_landing_position_error;
            public bool real_match_explosion, frozen_cells_released;
            public int exploded_match_count;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Actual formal72 model grid, board filling cache and runtime context, native target planner/FallManager, availability/static queries, predefined/random backend, pooled views and real frame motion. Three diagnostic holes in each phase require one existing piece and three created pieces to settle automatically, without assigned targets or direct per-item updates. Seed, removal and level status are explicit fixtures; production session startup, swap/damage/collection/player resolution and Ice helper composition remain pending. Grid identities/positions and pool counts restored afterward; level time and global model IDs advance normally.";
        }
        private static OriginalInitialBoardView board;
        private static OriginalGridCell[] cells;
        private static OriginalBoardItem[] saved;
        private static Vector3[] positions;
        private static OriginalFillingCell source;
        private static OriginalBoardItem savedCreated;
        private static OriginalFallManager manager;
        private static OriginalFallRefillColumns columns;
        private static RecordingCreation recording;
        private static Inputs inputs;
        private static Report report;
        private static int phase, lastFrame, poolBefore;
        private static float started, settledAt;
        private static bool explodeMatches;
        private static OriginalMatchItemCounts matchCounts;
        private static OriginalBoardMatchItemExplosionServices explosions;

        [MenuItem("Recovery/Verify Automatic Refill (Play Mode)")]
        public static void Run() => Run(false);
        [MenuItem("Recovery/Verify Match Destruction Refill (Play Mode)")]
        public static void RunMatchDestructionRefill() => Run(true);
        private static void Run(bool actualExplosion)
        {
            Require(EditorApplication.isPlaying && board == null, "Run once in Play mode");
            try
            {
                board = Object.FindObjectOfType<OriginalInitialBoardView>(); Require(board != null, "Formal board");
                report = new Report(); poolBefore = board.Presentation.Pool.GetPoolLength(2);
                explodeMatches = actualExplosion;
                if (explodeMatches)
                {
                    matchCounts = new OriginalMatchItemCounts();
                    explosions = new OriginalBoardMatchItemExplosionServices(board, () => matchCounts, null, null);
                    report.real_match_explosion = true;
                    report.scope = "Formal72 model grid and actual MatchItemModel.Explode/TryExplode/FinalExplodeCompleted, concrete goal/static/view/session-count dispatch, original coroutine animation and particles, timed cell freeze, and native automatic planner/refill/motion. Three existing normal pieces receive an explicit trigger2 in each phase, without manual cell removal; one existing piece and three predefined or random creations settle automatically. Diagnostic direct individual triggers, seed and level status remain inputs: match-group neighbor damage, player swaps, active session/collection/auto-match cascades remain pending. Original model identities/view bindings/positions, freeze and operation counts and view-pool count are restored after successful execution; clock, exploder/model IDs advance normally. Full optional mechanic/collection owners are required when used, never defaulted.";
                }
                cells = new OriginalGridCell[board.Grid.Width * board.Grid.Height]; saved = new OriginalBoardItem[cells.Length]; positions = new Vector3[cells.Length];
                for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                {
                    int i = x * board.Grid.Height + y; cells[i] = board.Grid.GetCell(x, y); saved[i] = cells[i].Mediation.CurrentItem;
                    var view = saved[i].SwapView as OriginalMatchItemSpriteView; if (view != null) positions[i] = view.transform.position;
                }
                source = board.Grid.GetCell(4, 7) as OriginalFillingCell;
                if (source != null) savedCreated = source.LastCreatedItem;
                Require(source != null && !source.HasCreatedItem, "Configured unoccupied filling slot");
                var level = OriginalLevelReader.Load("Levels/level_1"); var parsed = OriginalParsedLevelSet.ParseSets(level, 0, 0);
                inputs = new Inputs { Random = new OriginalLevelRandomSource(371), Views = board, FirstIds = parsed[0].ElementIds };
                inputs.Sets = new[] { new OriginalLevelSetSelection(parsed[0], inputs.Random) }; inputs.Colors = new OriginalMatchTileColors(inputs.Random, 0, 0);
                inputs.Generation = new OriginalFillSetGeneration(inputs.Random, new OriginalTargetColumnSetContainer(), inputs); inputs.Generation.AddAvailableSets(inputs.Sets);
                var predefined = new OriginalPredefinedItemContainer(); inputs.Initial = new OriginalInitialItemCreation(inputs.Sets, inputs.Random, predefined, inputs);
                OriginalPredefinedPreparation.Parse(new OriginalDecodedPredefinedData(level), inputs.Sets, 0, 0, inputs);
                phase = 0; Begin(predefined); lastFrame = Time.frameCount; EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }
        private static void Begin(OriginalPredefinedItemContainer predefined)
        {
            var data = explodeMatches ? OriginalExploder.Next(2) : default(OriginalExplodeData);
            for (int y = 4; y <= 6; y++)
            {
                var cell = board.Grid.GetCell(4, y); var item = cell.Mediation.CurrentItem;
                Require(item is OriginalMatchBoardItem, "Remove configured normal pieces");
                if (explodeMatches)
                {
                    var match = (OriginalMatchBoardItem)item; match.BindExplosionServices(explosions); match.Explode(data);
                    Require(!match.HasView && cell.Mediation.CurrentItem == null && cell.Mediation.NextItem == null && cell.Mediation.TargetItem == null && cell.IsFallFrozen,
                        "Actual final explosion clears all cell registrations and starts native timed freeze");
                    report.exploded_match_count++;
                }
                else { cell.Mediation.ClearAllItems(); ((OriginalMatchItemSpriteView)item.SwapView).gameObject.SetActive(false); }
            }
            columns = new OriginalFallRefillColumns();
            recording = new RecordingCreation { Actual = new OriginalFillingItemFactory(columns,
                new OriginalFillingCreationBackend(predefined, inputs.Generation, new OriginalItemViewCreation(board)), inputs) };
            var context = new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, inputs, null);
            Require(context.CurrentBoardFillingCells.Count == 9 && ReferenceEquals(context.CurrentBoardFillingCells[4], source), "Native highest-filling cache");
            Require(!context.IsAvailableForFall(board.Grid.GetCell(4, 3).Mediation.CurrentItem, null), "Actual boxes block fall");
            var targets = new OriginalFallTargetPlanner(context, columns);
            manager = new OriginalFallManager(columns, targets, recording, board.GameOperations); manager.InitializeGrid(context);
            started = Time.realtimeSinceStartup; settledAt = -1f;
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying) { Cleanup(); return; }
                if (lastFrame == Time.frameCount) return; lastFrame = Time.frameCount; report.frames++;
                Require(Time.realtimeSinceStartup - started < 15f, "Automatic refill reaches steady state within fifteen seconds");
                manager.ManualUpdate(); // No hand-assigned targets or direct FallComponent calls.
                if (!manager.IsAllItemsSteady) { Require(board.GameOperations.Container.GetCount(0) == 1, "One falling operation"); settledAt = -1f; return; }
                if (recording.Items.Count != 3 || source.HasCreatedItem) return;
                Require(board.GameOperations.Container.GetCount(0) == 0, "All steady releases falling operation"); report.steady_operations = true;
                if (settledAt < 0f) { settledAt = Time.realtimeSinceStartup; return; }
                if (Time.realtimeSinceStartup - settledAt < .5f) return; // Let native landing squash finish.
                Require(ReferenceEquals(board.Grid.GetCell(4, 4).Mediation.CurrentItem, saved[4 * board.Grid.Height + 7]), "Existing top piece automatically falls three rows");
                for (int i = 0; i < cells.Length; i++)
                {
                    var cell = cells[i]; var item = cell.Mediation.CurrentItem;
                    Require(item != null && ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem), "All72 mediation slots steady");
                    var view = item.SwapView as OriginalMatchItemSpriteView;
                    if (view != null)
                    {
                        var delta = view.transform.position - cell.GetViewPosition();
                        float error = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
                        report.max_landing_position_error = Mathf.Max(report.max_landing_position_error, error);
                        // Native StopIfReached 0x2ee8cc8 accepts Y <= target+.001;
                        // FallStraight clamps below target-.001. Bitwise equality
                        // is not the original stop contract at varying frame deltas.
                        if (!(error <= .001f && !view.IsFillMaskEnabled)) throw new InvalidOperationException(
                            "Real landing positions and masks: cell=" + cell.X + "," + cell.Y + " actual=" + view.transform.position.ToString("R") + " expected=" + cell.GetViewPosition().ToString("R") + " mask=" + view.IsFillMaskEnabled);
                    }
                    if (cell.X != 4 || cell.Y < 4) Require(ReferenceEquals(item, saved[i]), "Other columns and boxes unchanged");
                }
                Require(board.Presentation.Pool.GetPoolLength(2) == poolBefore - (explodeMatches ? 0 : 3), "Recycled/borrowed match view accounting");
                if (explodeMatches)
                {
                    Require(board.GameOperations.Container.GetCount(1) == 0, "Timed freeze releases operation1");
                    for (int y = 4; y <= 6; y++) Require(!board.Grid.GetCell(4, y).IsFallFrozen, "Native cell freeze countdown ends");
                    int count = 0; for (int type = 1; type <= 5; type++) count += matchCounts.GetCount(type);
                    Require(count == report.exploded_match_count, "Actual session match statistics count each explosion once");
                    report.frozen_cells_released = true;
                }
                report.existing_piece_fall = true; report.created_views += recording.Items.Count;
                if (phase == 0)
                {
                    Require(inputs.Random.DifficultySeedGenerationCount == 0, "Predefined filling consumes no random"); report.automatic_predefined = true;
                    RestoreBoard(); phase = 1; Begin(new OriginalPredefinedItemContainer()); return;
                }
                Require(inputs.Random.DifficultySeedGenerationCount == 6, "Three actual random items consume six draws");
                var random = new System.Random(371);
                foreach (var item in recording.Items) { random.Next(0, 1); Require(((OriginalMatchBoardItem)item).MatchType == inputs.FirstIds[random.Next(0, 4)], "Exact first-level random draw sequence"); }
                report.automatic_random = true;
                Cleanup(); report.board_and_pool_restored = true;
                File.WriteAllText(Path.Combine(Application.dataPath, report.real_match_explosion ? "../match_destruction_refill_unity_verification.json" : "../automatic_refill_unity_verification.json"), JsonUtility.ToJson(report, true) + "\n");
                Debug.Log(report.real_match_explosion ? "Match destruction/refill Unity verification passed." : "Automatic refill Unity verification passed.");
            }
            catch { Cleanup(); throw; }
        }
        private static void RestoreBoard()
        {
            if (cells == null || saved == null) return;
            while (recording != null && recording.Items.Count > 0)
            {
                var item = recording.Items[0]; recording.Items.RemoveAt(0);
                item.Mediator.ClearFromAllRegisteredCells();
                var view = item.SwapView as OriginalMatchItemSpriteView;
                if (view != null) board.Presentation.RecycleView(view);
            }
            if (source != null) source.LastCreatedItem = savedCreated;
            foreach (var cell in cells) if (cell != null) cell.Mediation.ClearAllItems();
            for (int i = 0; i < cells.Length; i++) if (saved[i] != null)
            {
                cells[i].Mediation.SetAllItems(saved[i]);
                if (saved[i] is OriginalMatchBoardItem && !saved[i].HasView)
                    ((IOriginalItemViewCreationContext)board).CreateView(saved[i], new OriginalSwapPoint(cells[i].X, cells[i].Y, 0f));
                var view = saved[i].SwapView as OriginalMatchItemSpriteView;
                if (view != null) { view.transform.position = positions[i]; view.gameObject.SetActive(true); view.ArrangeSelfSorting(); }
            }
            board.GameOperations.OperationReset(0);
            Require(board.Presentation.Pool.GetPoolLength(2) == poolBefore, "Pool count restored");
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            try { if (board != null) RestoreBoard(); }
            finally { board = null; cells = null; saved = null; positions = null; source = null; savedCreated = null; manager = null; columns = null; recording = null; inputs = null; explosions = null; matchCounts = null; }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
