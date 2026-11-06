using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Whitebox.Foundation.ConfigData;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit hole/seed fixture only. Native parser/containers/model/view/pool/
    // fill placement and actual formal-board motion run their runtime code.
    public static class FillingViewCreationVerification
    {
        [Serializable] private sealed class Report
        {
            public bool passed = true, formal_creation, predefined_fall, random_fall, pooled_reuse_and_reset, mask_transitions;
            public int frames;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Formal36 match views use restored model CreateView and configured pooled initialization. Actual90 predefined parser/models plus real filling backend/placement, pool and board FallComponent complete predefined and random three-row entry falls. Reused pooled view resets hidden/root/swap state and fill masks; random draws match actual first-level set. Diagnostic seed/holes/column placement only; automatic target planning, application/session factory ownership and player resolution remain pending. Scene restored afterward.";
        }
        private sealed class Inputs : IOriginalPredefinedPreparationContext, IOriginalInitialItemCreationContext, IOriginalFillSetGenerationContext, IOriginalFillingPlacementSpace
        {
            public OriginalLevelRandomSource Random;
            public OriginalInitialItemCreation Initial;
            public OriginalFillSetGeneration Generation;
            public OriginalMatchTileColors Colors;
            public OriginalLevelSetSelection[] Sets;
            public int[] FirstIds;
            public IOriginalItemViewCreationContext Views;
            public int[][] PotionColors => null;
            public bool ContainsDrillMasterPoint(OriginalGridCell c) { throw new InvalidOperationException("No cell in first-level predefined entries"); }
            public OriginalBoardItem CreateItem(OriginalItemSettings settings) => Views.CreateItem(settings);
            public void OnNewItemCreated(int type) => Generation.OnNewItemCreated(type);
            public OriginalLevelRandomSource GetLevelRandom() => Random;
            public int SelectMatchTileColor(int id, OriginalLevelSetSelection first, OriginalLevelRandomSource random) { Require(ReferenceEquals(first, Sets[0]) && ReferenceEquals(random, Random), "Shared palette owners"); return Colors.SelectMatchTileColor(id, FirstIds); }
            public IOriginalPredefinedItemCreator ItemCreator => Initial;
            public bool IsTuningEnabled => false;
            public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell) { throw new InvalidOperationException(); }
            public int GetLimitCount(int goal) { throw new InvalidOperationException(); }
            public int GetGoalTargetCount(int goal) { throw new InvalidOperationException(); }
            public void ReportRandomColorFallback(int total) { throw new InvalidOperationException(); }
            public float OffsetX => throw new InvalidOperationException("Top-board queued transport is outside this fixture");
            public float OffsetY => throw new InvalidOperationException("Top-board queued transport is outside this fixture");
            public void GetCellViewPosition(OriginalFillingCell c, out float x, out float y) { throw new InvalidOperationException("No top-board queued transport"); }
        }
        private static OriginalInitialBoardView board;
        private static OriginalGridCell[] cells;
        private static OriginalBoardItem[] saved;
        private static Vector3[] positions;
        private static OriginalFillingCell source;
        private static OriginalBoardItem savedCreated, created;
        private static OriginalMatchItemSpriteView view, previousView;
        private static Inputs inputs;
        private static OriginalItemViewCreation creation;
        private static OriginalFallRefillColumns columns;
        private static OriginalFillingItemFactory filling;
        private static Report report;
        private static int phase, lastFrame, poolBefore;
        private static float started;
        [MenuItem("Recovery/Verify Filling View Creation (Play Mode)")]
        public static void Run()
        {
            Require(EditorApplication.isPlaying && board == null, "Run once in Play mode");
            try
            {
                board = Object.FindObjectOfType<OriginalInitialBoardView>(); Require(board != null, "Formal board"); report = new Report();
                int matches = 0;
                for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                {
                    var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                    if (item is OriginalMatchBoardItem) { Require(item.HasView && item.SwapView is OriginalMatchItemSpriteView, "Formal restored creation"); matches++; }
                }
                Require(matches == 36, "Formal36 matches"); report.formal_creation = true;
                source = board.Grid.GetCell(4, 7) as OriginalFillingCell;
                Require(source != null && source.IsPredefined, "Original configured top filling cell"); savedCreated = source.LastCreatedItem;
                cells = new[] { board.Grid.GetCell(4, 7), board.Grid.GetCell(4, 6), board.Grid.GetCell(4, 5) };
                saved = new OriginalBoardItem[cells.Length]; positions = new Vector3[cells.Length];
                for (int i = 0; i < cells.Length; i++)
                {
                    saved[i] = cells[i].Mediation.CurrentItem; var v = (OriginalMatchItemSpriteView)saved[i].SwapView;
                    positions[i] = v.transform.position; cells[i].Mediation.ClearAllItems(); v.gameObject.SetActive(false);
                }
                var level = OriginalLevelReader.Load("Levels/level_1"); var parsed = OriginalParsedLevelSet.ParseSets(level, 0, 0);
                inputs = new Inputs { Random = new OriginalLevelRandomSource(371), Views = board, FirstIds = parsed[0].ElementIds };
                inputs.Sets = new[] { new OriginalLevelSetSelection(parsed[0], inputs.Random) }; inputs.Colors = new OriginalMatchTileColors(inputs.Random, 0, 0);
                inputs.Generation = new OriginalFillSetGeneration(inputs.Random, new OriginalTargetColumnSetContainer(), inputs); inputs.Generation.AddAvailableSets(inputs.Sets);
                var predefined = new OriginalPredefinedItemContainer(); inputs.Initial = new OriginalInitialItemCreation(inputs.Sets, inputs.Random, predefined, inputs);
                OriginalPredefinedPreparation.Parse(new OriginalDecodedPredefinedData(level), inputs.Sets, 0, 0, inputs);
                creation = new OriginalItemViewCreation(board); columns = new OriginalFallRefillColumns();
                filling = new OriginalFillingItemFactory(columns, new OriginalFillingCreationBackend(predefined, inputs.Generation, creation), inputs);
                poolBefore = board.Presentation.Pool.GetPoolLength(2); phase = 0; BeginFall(); lastFrame = Time.frameCount;
                EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
        }
        private static void BeginFall()
        {
            OriginalFillingItemPlacement.CreateItemFromFillingCell(columns, source, cells[2], filling);
            created = source.LastCreatedItem; Require(created != null && ReferenceEquals(created.Mediator.TargetCell, cells[2].Mediation), "Actual fill placement");
            view = created.SwapView as OriginalMatchItemSpriteView; Require(view != null && created.HasView && view.IsFillMaskEnabled, "Actual view and entry mask");
            Require(view.transform.localPosition == new Vector3(4, 8, 0) && ReferenceEquals(((OriginalFallComponentState)created.FallComponent).FillingCell, source), "Original spawn position/filling ownership");
            if (phase == 0) Require(inputs.Random.DifficultySeedGenerationCount == 0, "Predefined model consumes no random");
            else
            {
                var random = new System.Random(371); random.Next(0, 1); int id = inputs.FirstIds[random.Next(0, 4)];
                Require(ReferenceEquals(view, previousView) && inputs.Random.DifficultySeedGenerationCount == 2 && ((OriginalMatchBoardItem)created).MatchType == id, "Reused view and exact first-level random generation");
            }
            started = Time.realtimeSinceStartup;
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying) { Cleanup(); return; }
                if (lastFrame == Time.frameCount) return; lastFrame = Time.frameCount; report.frames++;
                Require(Time.realtimeSinceStartup - started < 5f, "Entry fall finishes within five seconds");
                var fall = (OriginalFallComponentState)created.FallComponent; fall.ManualUpdate();
                if (!fall.IsItemSteady || !ReferenceEquals(created.Mediator.CurrentCell, cells[2].Mediation)) return;
                Require(view.transform.position == cells[2].GetViewPosition() && !source.HasCreatedItem && !view.IsFillMaskEnabled, "Actual landing releases filling slot and entry mask"); report.mask_transitions = true;
                if (phase == 0)
                {
                    report.predefined_fall = true;
                    created.Mediator.ClearFromAllRegisteredCells(); created = null; previousView = view;
                    view.HideView(); view.SwapAnimationStarted(true, 0); view.EnableFillMask(); board.Presentation.RecycleView(view); view = null;
                    var fixedItem = creation.CreateItemAt(1, new OriginalSwapPoint(4, 8, 0)); view = fixedItem.SwapView as OriginalMatchItemSpriteView;
                    Require(ReferenceEquals(view, previousView) && fixedItem.HasView && !view.IsFillMaskEnabled && !view.IsSwapping, "CreateWithSettings resets pooled mask/swap state");
                    Require(!view.GetComponentInChildren<OriginalRootTransform>(true).IsHidden && view.transform.localPosition == new Vector3(4, 8, 0), "Reused root shown and positioned");
                    report.pooled_reuse_and_reset = true; board.Presentation.RecycleView(view); view = null;
                    filling = new OriginalFillingItemFactory(columns, new OriginalFillingCreationBackend(new OriginalPredefinedItemContainer(), inputs.Generation, creation), inputs);
                    phase = 1; BeginFall(); return;
                }
                report.random_fall = true;
                Cleanup();
                File.WriteAllText(Path.Combine(Application.dataPath, "../filling_view_creation_unity_verification.json"), JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("Filling view creation Unity verification passed.");
            }
            catch { Cleanup(); throw; }
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (created != null) created.Mediator.ClearFromAllRegisteredCells();
            if (view != null && board != null) { var recycled = view; view = null; board.Presentation.RecycleView(recycled); }
            if (source != null) source.LastCreatedItem = savedCreated;
            if (cells != null && saved != null)
            {
                for (int i = 0; i < cells.Length; i++) if (cells[i] != null) cells[i].Mediation.ClearAllItems();
                for (int i = 0; i < cells.Length; i++) if (saved[i] != null)
                {
                    cells[i].Mediation.SetAllItems(saved[i]); var v = saved[i].SwapView as OriginalMatchItemSpriteView;
                    if (v != null) { v.transform.position = positions[i]; v.gameObject.SetActive(true); v.ArrangeSelfSorting(); }
                }
            }
            bool poolRestored = board == null || poolBefore == 0 || board.Presentation.Pool.GetPoolLength(2) == poolBefore;
            board = null; cells = null; saved = null; positions = null; source = null; savedCreated = null; created = null; view = null; previousView = null; inputs = null; creation = null; columns = null; filling = null; poolBefore = 0;
            Require(poolRestored, "Pool count restored");
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
