using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Whitebox.Foundation.ConfigData;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit diagnostics only. Seed, quotas and tuning are supplied test inputs;
    // no alternate session or fill implementation is installed in the scene.
    public static class FillSetGenerationVerification
    {
        [Serializable] private sealed class Report
        {
            public bool passed = true, first_level_sequence, quota_identity_and_frame_cache, stop_first_element_only;
            public int generated_items;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Actual parsed first-level set, restored weighted generation/random, concrete grid iterator and item/cell mediation, first-frame cache and duplicate identity quotas, native limit stopping. Explicit diagnostic seed/frame/flying/goal/tuning services; formal scene unchanged. Application/session/predefined/factory ownership and automatic refill are still pending.";
        }
        private sealed class Inputs : IOriginalFillSetGenerationContext, IOriginalLevelSetAvailabilityContext, IOriginalGridAccess
        {
            public int Frame, Flying;
            public OriginalCellGrid Grid;
            public bool IsTuningEnabled => false;
            public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell) { throw new InvalidOperationException("No tuning expected in this fixture."); }
            public int GetLimitCount(int goal) => 2;
            public int GetGoalTargetCount(int goal) => 5;
            public void ReportRandomColorFallback(int total) { throw new InvalidOperationException("No fallback expected."); }
            public int FrameCount => Frame;
            public int GetFlyingThrowItemCount(int goal) { Require(goal == OriginalGoalTypes.FromItemType(7), "Actual goal conversion"); return Flying; }
            public OriginalGridCell CurrentBoardBottomLeftCell => Grid.GetCell(0, 0);
            public OriginalGridCell GetCell(int x, int y) => Grid.GetCell(x, y);
            public bool IsInCurrentBoard(OriginalGridCell cell) => cell.BoardId == 0;
            public OriginalGridIterator GetCurrentBoardHorizontalIterator() => new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(this));
        }
        [MenuItem("Recovery/Verify Fill Set Generation (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run in Play mode.");
            var board = Object.FindObjectOfType<OriginalInitialBoardView>();
            Require(board != null && board.CreatedViewCount == 72, "Formal board is running");
            var report = new Report();
            var level = OriginalLevelReader.Load("Levels/level_1");
            var parsed = OriginalParsedLevelSet.ParseSets(level, 0, 0);
            Require(parsed.Length == 1 && parsed[0].ElementIds.Length == 4, "Actual first-level set");
            var random = new OriginalLevelRandomSource(1839);
            var set = new OriginalLevelSetSelection(parsed[0], random);
            var inputs = new Inputs();
            var generation = new OriginalFillSetGeneration(random, new OriginalTargetColumnSetContainer(), inputs);
            generation.AddAvailableSets(new[] { set });
            var filling = new OriginalFillingCell(0, 7, 1, 0, 1, true, false, 0);
            var expected = new System.Random(1839);
            for (int i = 0; i < 128; i++)
            {
                expected.Next(0, 1);
                Require(generation.GetRandomFillItem(filling) == parsed[0].ElementIds[expected.Next(0, 4)], "First-level exact selection and draw consumption");
                report.generated_items++;
            }
            Require(random.DifficultySeedGenerationCount == 256, "Two original draws per first-level generated item");
            report.first_level_sequence = true;

            var quotaSource = new FTiledSet { CanFall = true, Elements = new[] { new FTiledSetElement { Id = 25, CreateRatio = 1 } }, MaxItemCounts = new[] { new FTiledSetMaxItemCount { Id = 7, MaxCount = 5 } } };
            var quotaRandom = new OriginalLevelRandomSource(32);
            var quota = new OriginalLevelSetSelection(new OriginalParsedLevelSet(quotaSource, 0, 0), quotaRandom);
            quota.InitFillAvailability();
            inputs.Grid = new OriginalCellGrid(3, 1);
            var first = new OriginalGridCell(0, 0, 1);
            var second = new OriginalGridCell(1, 0, 1);
            var last = new OriginalFillingCell(2, 0, 1, 0, 1, true, false, 0);
            inputs.Grid.SetCell(0, 0, first); inputs.Grid.SetCell(1, 0, second); inputs.Grid.SetCell(2, 0, last);
            inputs.Grid.CacheNeighbors();
            var item1 = new OriginalBoardItem(1, 7); var item2 = new OriginalBoardItem(2, 7); var item3 = new OriginalBoardItem(3, 7);
            first.Mediation.SetCurrentItem(item1); first.Mediation.SetTargetItem(item1);
            second.Mediation.SetCurrentItem(item2); last.AddCreatedItem(item3);
            quota.BindAvailabilityContext(inputs);
            Require(!quota.CanFillNewItemWithMaxCount(7) && quota.RemainingItemCount == 0 && quotaRandom.DifficultySeedGenerationCount == 1, "Native frame zero reuses zero state and still consumes random");
            inputs.Frame = 1; inputs.Flying = 1;
            quota.CanFillNewItemWithMaxCount(7);
            Require(quota.RemainingItemCount == 1, "Target/current item identity counted once plus current-only and created item");
            last.RemoveCreatedItem(); inputs.Flying = 99;
            quota.CanFillNewItemWithMaxCount(7);
            Require(quota.RemainingItemCount == 1, "Same-frame scan is cached");
            inputs.Frame = 2; inputs.Flying = 0;
            quota.CanFillNewItemWithMaxCount(7);
            Require(quota.RemainingItemCount == 3, "Next frame refreshes actual grid/created/flying state");
            report.quota_identity_and_frame_cache = true;

            var stopSet = new OriginalLevelSetSelection(new OriginalParsedLevelSet(new FTiledSet { CanFall = true, Elements = new[] { new FTiledSetElement { Id = 25, CreateRatio = 1 }, new FTiledSetElement { Id = 25, CreateRatio = 1 } } }, 0, 0), new OriginalLevelRandomSource(9));
            generation.AddAvailableSets(new[] { stopSet });
            generation.OnNewItemCreated(7); Require(stopSet.TotalCreateRatio == 2, "Below limit");
            generation.OnNewItemCreated(7); Require(stopSet.TotalCreateRatio == 1 && stopSet.IsAvailableForFill, "Only first matching element disabled at limit");
            for (int i = 0; i < 3; i++) generation.OnNewItemCreated(7);
            Require(stopSet.TotalCreateRatio == 1 && generation.CreatedCounts[7] == 5, "Goal hit disables the same first element again");
            report.stop_first_element_only = true;
            File.WriteAllText(Path.Combine(Application.dataPath, "../fill_set_generation_unity_verification.json"), JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("Fill set generation Unity verification passed.");
        }
        private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    }
}
