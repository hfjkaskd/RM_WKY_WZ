using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Whitebox.Foundation.ConfigData;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Diagnostic model construction only; no seed, fake creator or alternate
    // initialization is installed into the actual scene.
    public static class PredefinedPreparationVerification
    {
        [Serializable] private sealed class Report
        {
            public bool passed = true, first_level_column_order, no_random_for_fixed_ids, no_views_created, shared_placeholder_cache;
            public int first_level_models;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Actual decoded first-level90 predefinitions, original parser ordering/color replacement, actual OriginalMatchBoardItem/PredefinedItemContainer and shared placeholder-color cache. Explicit diagnostic model factory/identity and seed; original application/session model factory and automatic refill remain pending. Formal scene unchanged.";
        }
        private sealed class Context : IOriginalPredefinedPreparationContext, IOriginalPredefinedItemCreator
        {
            public readonly OriginalLevelRandomSource Random = new OriginalLevelRandomSource(87);
            public readonly OriginalPredefinedItemContainer Container = new OriginalPredefinedItemContainer();
            public readonly OriginalMatchTileColors Colors;
            public OriginalLevelSetSelection Set;
            public int[] FirstSetIds;
            public int Created, RandomLookups;
            public Context() { Colors = new OriginalMatchTileColors(Random, 0, 0); }
            public OriginalLevelRandomSource GetLevelRandom() { RandomLookups++; return Random; }
            public int SelectMatchTileColor(int id, OriginalLevelSetSelection set, OriginalLevelRandomSource random)
            {
                Require(ReferenceEquals(set, Set) && ReferenceEquals(random, Random), "Shared set/random identity");
                return Colors.SelectMatchTileColor(id, FirstSetIds);
            }
            public IOriginalPredefinedItemCreator ItemCreator => this;
            public OriginalBoardItem CreateItemModelForStart(int id, int original, OriginalGridCell cell)
            {
                Require(id == original && cell == null && OriginalLevelRules.ToMatchType(id) != 0, "Original parser passes converted ID twice and no cell");
                return new OriginalMatchBoardItem(++Created, OriginalTiledItemTypes.AsItemType(id), OriginalLevelRules.ToMatchType(id));
            }
            public void AddPredefinedItem(int column, OriginalBoardItem item) { Container.AddPredefinedItem(column, item); }
        }
        [MenuItem("Recovery/Verify Predefined Preparation (Play Mode)")]
        public static void Run()
        {
            Require(EditorApplication.isPlaying && Object.FindObjectOfType<OriginalInitialBoardView>() != null, "Run with formal scene in Play mode");
            var report = new Report();
            var level = OriginalLevelReader.Load("Levels/level_1");
            var parsed = OriginalParsedLevelSet.ParseSets(level, 0, 0);
            var context = new Context { FirstSetIds = parsed[0].ElementIds };
            context.Set = new OriginalLevelSetSelection(parsed[0], context.Random);
            // The original ignores Y, including values unrelated to source order.
            for (int i = 0; i < level.Predefined.Length; i++) level.Predefined[i].Y = 900 - i;
            OriginalPredefinedPreparation.Parse(new OriginalDecodedPredefinedData(level), new[] { context.Set }, 0, 0, context);
            Require(context.Created == 90 && context.Container.InitialItemCounts[1] == 90 && context.RandomLookups == 1 && context.Random.DifficultySeedGenerationCount == 0, "All90 fixed models, type counts and no random draws");
            for (int i = 0; i < level.Predefined.Length; i++)
            {
                var source = level.Predefined[i];
                foreach (int id in source.Ids)
                {
                    var item = context.Container.GetPredefinedItem(source.X) as OriginalMatchBoardItem;
                    Require(item != null && item.MatchType == id && !item.HasView && item.Mediator.CurrentCell == null && item.Mediator.TargetCell == null, "Original source/Ids column order and view-free queued models");
                }
            }
            for (int column = 0; column < 9; column++) Require(context.Container.GetPredefinedItem(column) == null, "Queue exhausted");
            Require(context.Container.InitialItemCounts[1] == 90, "Dequeue preserves initial quantity");
            report.first_level_models = context.Created; report.first_level_column_order = report.no_random_for_fixed_ids = report.no_views_created = true;
            var special = new FTiledLevel { Predefined = new[] { new FTiledPredefined { X = 0, Y = -99, Ids = new[] { 211, 212, 213, 211 } } } };
            var cache = new Context { FirstSetIds = parsed[0].ElementIds }; cache.Set = new OriginalLevelSetSelection(parsed[0], cache.Random);
            OriginalPredefinedPreparation.Parse(new OriginalDecodedPredefinedData(special), new[] { cache.Set }, 0, 0, cache);
            int first = ((OriginalMatchBoardItem)cache.Container.GetPredefinedItem(0)).MatchType;
            int second = ((OriginalMatchBoardItem)cache.Container.GetPredefinedItem(0)).MatchType;
            int third = ((OriginalMatchBoardItem)cache.Container.GetPredefinedItem(0)).MatchType;
            int repeated = ((OriginalMatchBoardItem)cache.Container.GetPredefinedItem(0)).MatchType;
            Require(first != second && first != third && second != third && repeated == first && cache.Random.DifficultySeedGenerationCount == 9, "Shared placeholder cache selects unique colors and cached repeats consume no draw");
            report.shared_placeholder_cache = true;
            File.WriteAllText(Path.Combine(Application.dataPath, "../predefined_preparation_unity_verification.json"), JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("Predefined preparation Unity verification passed.");
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    }
}
