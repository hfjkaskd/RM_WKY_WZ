using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation.Editor
{
    public static class GeneratedCountVerification
    {
        private sealed class Inputs : IOriginalFillSetGenerationContext
        {
            public OriginalGoalManager Goals;
            public bool IsTuningEnabled => false;
            public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell) => throw new InvalidOperationException();
            public int GetLimitCount(int goal) => Goals.GetLimitCount(goal);
            public int GetGoalTargetCount(int goal) => Goals.GetGoalTargetCount(goal);
            public void ReportRandomColorFallback(int ratio) => throw new InvalidOperationException();
        }
        private static OriginalLevelSetSelection Set(OriginalLevelRandomSource random, int ratio, params int[] ids)
        {
            var entries = new FTiledSetElement[ids.Length];
            for (int i = 0; i < ids.Length; i++) entries[i] = new FTiledSetElement { Id = ids[i], CreateRatio = 2 + i };
            return new OriginalLevelSetSelection(new OriginalParsedLevelSet(new FTiledSet {
                CanFall = true, CreateRatio = ratio, Elements = entries }, 0, 0), random);
        }
        public static void Run()
        {
            var report = new Report();
            try
            {
                var goals = new OriginalGoalManager(() => 0, new OriginalGameOperations(() => 0), new OriginalGoalClaims());
                goals.InitWithGoalsAndLimits(new[] { new Dictionary<int, OriginalGoalTuple> {
                    { 12, new OriginalGoalTuple(12, 3, false, goals) }, { 16, new OriginalGoalTuple(16, 9, false, goals) } } },
                    new[] { new Dictionary<int, OriginalLimitTuple> { { 16, new OriginalLimitTuple(16, 1) } } }, true);
                var random = new OriginalLevelRandomSource(17);
                var inputs = new Inputs { Goals = goals };
                var generation = new OriginalFillSetGeneration(random, new OriginalTargetColumnSetContainer(), inputs);
                Require(OriginalFillSetGeneration.IsFillingObstacle(7) && !OriginalFillSetGeneration.IsFillingObstacle(8) &&
                    OriginalFillSetGeneration.IsFillingObstacle(9) && OriginalTiledItemTypes.AsItemType(26) == 9 && OriginalGoalTypes.FromItemType(9) == 16,
                    "Actual filling classification and tiled/goal identities select genuine capped obstacles");
                var obstacle = Set(random, 7, 25); var mixed = Set(random, 3, 1, 26);
                generation.AddAvailableSets(new[] { obstacle, mixed });
                Require(generation.FillRatios[1] == 10, "Initial actual set availability and global fill ratios");
                var predefined = new OriginalPredefinedItemContainer();
                var first = OriginalCoreItemModels.Create(OriginalItemSettings.ForTiledId(1));
                predefined.AddPredefinedItem(0, first); predefined.AddPredefinedItem(0, OriginalCoreItemModels.Create(OriginalItemSettings.ForTiledId(2)));
                generation.OnNewItemCreated(7); goals.AddToPrerequisite(12);
                FLevelSaveData save = new FLevelSaveData { BoardSaveData = new FBoardSaveData { ItemModelCreatorSaveData = new FItemModelCreatorSaveData {
                    CreatedSetItemCountExceptMatchItem = new[] { new FIntIntPair { Key = 7, Value = 3 }, new FIntIntPair { Key = 9, Value = 1 }, new FIntIntPair { Key = 7, Value = 3 } } } } };
                var loader = new OriginalDecodedCreatedItemCounts(() => save);
                var owner = generation.CreateInitialCountState(goals, predefined, () => loader);
                owner.ClearCreatedItemsCountExceptPredefined();
                Require(!goals.HasAnyPrerequisite() && generation.CreatedCounts.Count == 1 && generation.CreatedCounts[1] == 2 &&
                    ReferenceEquals(predefined.GetPredefinedItem(0), first) && predefined.InitialItemCounts[1] == 2,
                    "Clear uses actual goal prerequisites, shared generation dictionary and immutable initial queue counts without dequeuing");
                owner.ClearCreatedItemsCountExceptPredefined();
                Require(generation.CreatedCounts[1] == 2, "Dequeuing does not change original initial counts used by retry");
                report.actual_clear_and_queue_verified = true;
                owner.UpdateCreatedItemsCountExceptPredefined();
                Require(generation.CreatedCounts[7] == 3 && generation.CreatedCounts[9] == 1 && generation.CreatedCounts[1] == 2,
                    "Restore replaces saved keys, retains unrelated predefined counts and handles duplicate saved keys");
                Require(!obstacle.IsAvailableForFill && obstacle.TotalCreateRatio == 0 && mixed.IsAvailableForFill && mixed.TotalCreateRatio == 2 && generation.FillRatios[1] == 3,
                    "Goal/limit exhaustion disables actual elements, updates each set availability and recalculates native global ratios");
                var cell = new OriginalFillingCell(0, 0, 1, 0, 1, false, false, 0);
                for (int i = 0; i < 32; i++) Require(generation.GetRandomFillItem(cell) == 1, "Subsequent actual fill selection cannot choose restored exhausted obstacles");
                report.actual_restore_limits_ratios_verified = true;
                save.BoardSaveData.ItemModelCreatorSaveData.CreatedSetItemCountExceptMatchItem[0].Value = 1;
                save.BoardSaveData.ItemModelCreatorSaveData.CreatedSetItemCountExceptMatchItem[2].Value = 1;
                owner.UpdateCreatedItemsCountExceptPredefined();
                Require(!obstacle.IsAvailableForFill && obstacle.TotalCreateRatio == 0 && generation.CreatedCounts[7] == 1,
                    "Repeated restore updates values without reenabling previously disabled elements");
                var captured = ((IOriginalCreatedCountSet)obstacle).Elements;
                obstacle.FillFourColors();
                Require(ReferenceEquals(captured, ((IOriginalCreatedCountSet)obstacle).Elements) && captured.Count == 5,
                    "Captured element-list view retains identity and reads live appended elements");
                report.retained_element_view_verified = true;
                save.BoardSaveData.ItemModelCreatorSaveData.CreatedSetItemCountExceptMatchItem = null;
                Require(loader.Count == 0, "Absent decoded count vector has original zero length");
                bool failed = false; try { loader.Get(0); } catch (InvalidOperationException) { failed = true; }
                Require(failed, "Missing pair value fails instead of becoming a synthetic zero count");
                save.BoardSaveData.ItemModelCreatorSaveData.CreatedSetItemCountExceptMatchItem = new FIntIntPair[] { null };
                failed = false; try { loader.Get(0); } catch (InvalidOperationException) { failed = true; }
                Require(failed, "Explicit null saved pair retains required-value failure");
                save = null; failed = false; try { int unused = loader.Count; } catch (InvalidOperationException) { failed = true; }
                Require(failed, "Missing level save value is required");
                report.decoded_required_values_verified = true;
                report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally { File.WriteAllText(Path.Combine(Application.dataPath, "../live_generated_counts_verification.json"), JsonUtility.ToJson(report, true) + "\n"); }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Report
        {
            public bool passed, actual_clear_and_queue_verified, actual_restore_limits_ratios_verified, retained_element_view_verified, decoded_required_values_verified;
            public string failure;
            public string scope = "Actual shared generation dictionary, real goal prerequisites/limits, native set element availability and fill ratios, real predefined queue/initial counts and decoded save getters. Actual post-restore selection rejects exhausted obstacles. Full binary save parsing, all concrete item models and production startup/mobile composition remain unverified.";
        }
    }
}
