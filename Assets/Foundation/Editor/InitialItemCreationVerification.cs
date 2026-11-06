using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Whitebox.Foundation.ConfigData;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit integration diagnostics. Seed/potion/drill and factory context
    // are fixture inputs; the formal scene's production owners remain intact.
    public static class InitialItemCreationVerification
    {
        [Serializable] private sealed class Report
        {
            public bool passed = true, formal_item_identity_and_settings, formal_box_fall_components, predefined90_unique_models, raw_set_selection, creation_flags;
            public int formal_models, queued_models;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Formal72 initial match/box models now use native global post-increment IDs, tiled/layer/goal settings and normal FallComponents. Actual90 predefined models run parser->initial creator->core match/box construction->generation notification->queue, without views. Raw set selection and cell/potion/drill/hand-given flags verified. Diagnostic seed/factory/tuning context only; full factory other item branches/SuperBooster query, production application/session/fill composition and automatic fall/refill remain pending.";
        }
        private sealed class Context : IOriginalInitialItemCreationContext, IOriginalPredefinedPreparationContext, IOriginalFillSetGenerationContext
        {
            public OriginalLevelRandomSource Random;
            public OriginalInitialItemCreation Creator;
            public OriginalFillSetGeneration Generation;
            public OriginalItemSettings Last;
            public OriginalMatchTileColors Colors;
            public OriginalLevelSetSelection[] Sets;
            public int[] FirstIds;
            public int DrillQueries;
            public readonly int[][] PotionColorsValue = { new[] { 1, 3 } };
            public int[][] PotionColors => PotionColorsValue;
            public bool ContainsDrillMasterPoint(OriginalGridCell cell) { DrillQueries++; return cell.X == 0; }
            public OriginalBoardItem CreateItem(OriginalItemSettings settings) { Last = settings; return OriginalCoreItemModels.Create(settings); }
            public void OnNewItemCreated(int type) { Generation.OnNewItemCreated(type); }
            public OriginalLevelRandomSource GetLevelRandom() => Random;
            public int SelectMatchTileColor(int id, OriginalLevelSetSelection first, OriginalLevelRandomSource random) { Require(ReferenceEquals(first, Sets[0]) && ReferenceEquals(random, Random), "Shared parser owners"); return Colors.SelectMatchTileColor(id, FirstIds); }
            public IOriginalPredefinedItemCreator ItemCreator => Creator;
            public bool IsTuningEnabled => false;
            public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell) { throw new InvalidOperationException(); }
            public int GetLimitCount(int goal) { throw new InvalidOperationException(); }
            public int GetGoalTargetCount(int goal) { throw new InvalidOperationException(); }
            public void ReportRandomColorFallback(int total) { throw new InvalidOperationException(); }
        }
        [MenuItem("Recovery/Verify Initial Item Creation (Play Mode)")]
        public static void Run()
        {
            Require(EditorApplication.isPlaying, "Run in Play mode");
            var board = Object.FindObjectOfType<OriginalInitialBoardView>(); Require(board != null, "Actual board");
            var level = OriginalLevelReader.Load("Levels/level_1"); var report = new Report(); var identities = new HashSet<int>();
            int first = board.Grid.GetCell(0, 0).Mediation.CurrentItem.Id, index = 0, boxes = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem; int tiled = level.Grid.Items[x + y * board.Grid.Width]; var settings = OriginalItemSettings.ForTiledId(tiled);
                Require(item.Id == unchecked(first + index++) && identities.Add(item.Id), "Global IDs in actual creation order");
                Require(item.TiledId == tiled && item.ItemType == settings.ItemType && item.LayerCount == settings.LayerCount && item.GoalType == settings.GoalType, "Actual model settings");
                var fall = item.FallComponent as OriginalFallComponentState; Require(fall != null && ReferenceEquals(fall.Item, item) && fall.IsItemSteady, "Every formal item owns a normal native fall component");
                fall.ManualUpdate(); Require(fall.IsItemSteady, "Stable uniform update including preview boxes");
                if (item is OriginalBoxBoardItem) boxes++;
            }
            Require(index == 72 && boxes == 36, "Actual first-level quantities"); report.formal_models = index; report.formal_item_identity_and_settings = report.formal_box_fall_components = true;
            var parsed = OriginalParsedLevelSet.ParseSets(level, 0, 0); var context = new Context { Random = new OriginalLevelRandomSource(71), FirstIds = parsed[0].ElementIds };
            context.Sets = new[] { new OriginalLevelSetSelection(parsed[0], context.Random) }; context.Colors = new OriginalMatchTileColors(context.Random, 0, 0);
            context.Generation = new OriginalFillSetGeneration(context.Random, new OriginalTargetColumnSetContainer(), context); context.Generation.AddAvailableSets(context.Sets);
            var container = new OriginalPredefinedItemContainer(); context.Creator = new OriginalInitialItemCreation(context.Sets, context.Random, container, context);
            OriginalPredefinedPreparation.Parse(new OriginalDecodedPredefinedData(level), context.Sets, 0, 0, context);
            foreach (var source in level.Predefined) foreach (int tiled in source.Ids)
            {
                var item = container.GetPredefinedItem(source.X) as OriginalMatchBoardItem;
                Require(item != null && item.TiledId == tiled && item.MatchType == tiled && item.IsHandGiven && identities.Add(item.Id) && !item.HasView, "View-free predefined models with globally unique identities");
                var fall = item.FallComponent as OriginalFallComponentState; Require(fall != null && fall.IsItemSteady && item.Mediator.CurrentCell == null && item.Mediator.TargetCell == null, "Queued model owns a new standard fall component");
                fall.ManualUpdate(); report.queued_models++;
            }
            Require(report.queued_models == 90 && context.Random.DifficultySeedGenerationCount == 0 && context.Generation.CreatedCounts.Count == 0, "Fixed IDs consume no random; non-filling types do not create counts");
            report.predefined90_unique_models = true;
            var raw = new OriginalLevelSetSelection(new OriginalParsedLevelSet(new FTiledSet { Elements = new[] { new FTiledSetElement { Id = 1, CreateRatio = 999 }, new FTiledSetElement { Id = 4, CreateRatio = 1 } } }, 0, 0), context.Random);
            raw.InitializeAvailability(false); context.Creator = new OriginalInitialItemCreation(new[] { raw }, context.Random, container, context);
            var expected = new System.Random(71);
            for (int i = 0; i < 64; i++) Require(context.Creator.GetRandomItemTypeFromSet(14) == (expected.Next(0, 2) == 0 ? 1 : 4), "Raw unweighted set elements, including disabled elements");
            report.raw_set_selection = true;
            var cell = board.Grid.GetCell(0, 0);
            var setItem = context.Creator.CreateItemModelForStart(14, 14, cell) as OriginalMatchBoardItem;
            Require(setItem != null && !setItem.IsHandGiven && context.Last.CellModel == null && context.Last.PotionColors == null && !context.Last.IsDrillMaster && context.DrillQueries == 0, "Set branch skips non-set cell/potion/drill settings");
            var directItem = context.Creator.CreateItemModelForStart(1, 14, cell) as OriginalMatchBoardItem;
            Require(directItem != null && !directItem.IsHandGiven && ReferenceEquals(context.Last.CellModel, cell) && ReferenceEquals(context.Last.PotionColors, context.PotionColorsValue) && context.Last.IsDrillMaster && context.DrillQueries == 1, "Direct branch uses actual cell/potion/drill fields and original ID hand-given test");
            var box = context.Creator.CreateItemModelForStart(24, 24, null) as OriginalBoxBoardItem; Require(box != null && box.LayerCount == 4 && box.TiledId == 24 && box.FallComponent != null, "Native box constructor receives parsed layers");
            report.creation_flags = true;
            File.WriteAllText(Path.Combine(Application.dataPath, "../initial_item_creation_unity_verification.json"), JsonUtility.ToJson(report, true) + "\n"); Debug.Log("Initial item creation Unity verification passed.");
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    }
}
