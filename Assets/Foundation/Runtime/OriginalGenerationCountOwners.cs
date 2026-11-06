using System;
using System.Collections.Generic;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalLevelSetSelection : IOriginalCreatedCountSet
    {
        private CreatedCountElements createdCountElements;
        IOriginalCreatedCountElements IOriginalCreatedCountSet.Elements =>
            createdCountElements ?? (createdCountElements = new CreatedCountElements(this));
        // Retain the element-list identity while this set grows. Count and
        // indexed reads stay live, including changes during goal callbacks.
        private sealed class CreatedCountElements : IOriginalCreatedCountElements
        {
            private readonly OriginalLevelSetSelection owner;
            public CreatedCountElements(OriginalLevelSetSelection owner) { this.owner = owner; }
            public int Count => owner.ids.Length;
            public int GetTiledId(int index) => owner.ids[index];
            public void Disable(int index) { owner.available[index] = false; }
        }
    }
    public sealed partial class OriginalFillSetGeneration
    {
        public OriginalGeneratedItemCountState CreateInitialCountState(OriginalGoalManager goals,
            OriginalPredefinedItemContainer predefined, Func<IOriginalSavedCreatedItemCounts> currentRestoreLoader) =>
            new OriginalGeneratedItemCountState(createdCounts, new CountOwners(this, goals, predefined, currentRestoreLoader));
        private sealed class CountOwners : IOriginalCreatedItemCountContext
        {
            private readonly OriginalFillSetGeneration owner;
            private readonly OriginalGoalManager goals;
            private readonly OriginalPredefinedItemContainer predefined;
            private readonly Func<IOriginalSavedCreatedItemCounts> currentRestoreLoader;
            public CountOwners(OriginalFillSetGeneration owner, OriginalGoalManager goals,
                OriginalPredefinedItemContainer predefined, Func<IOriginalSavedCreatedItemCounts> currentRestoreLoader)
            { this.owner = owner; this.goals = goals; this.predefined = predefined; this.currentRestoreLoader = currentRestoreLoader; }
            public void ClearPrerequisiteItems() => (goals ?? throw new NullReferenceException()).ClearPrerequisiteItems();
            public Dictionary<int, int> GetPredefinedInitialCounts() => (predefined ?? throw new NullReferenceException()).InitialItemCounts;
            public IOriginalSavedCreatedItemCounts GetCurrentRestoreLoader() => (currentRestoreLoader ?? throw new NullReferenceException())();
            public IOriginalCreatedCountSet[] GetSets() => owner.sets;
            public bool IsFillingObstacle(int itemType) => OriginalFillSetGeneration.IsFillingObstacle(itemType);
            public int GetLimitCount(int goalType) => (goals ?? throw new NullReferenceException()).GetLimitCount(goalType);
            public int GetGoalTargetCount(int goalType) => (goals ?? throw new NullReferenceException()).GetGoalTargetCount(goalType);
            public void CalculateRandomFactors() => owner.CalculateRandomFactors();
        }
    }
    // Decoded data projection of the two saved-count getters. Full binary save
    // parsing and LevelRestoreManager ownership remain separate required work.
    public sealed class OriginalDecodedCreatedItemCounts : IOriginalSavedCreatedItemCounts
    {
        private readonly Func<FLevelSaveData> current;
        public OriginalDecodedCreatedItemCounts(Func<FLevelSaveData> current) { this.current = current; }
        private FIntIntPair[] Entries
        {
            get
            {
                var level = (current ?? throw new NullReferenceException())() ?? throw new InvalidOperationException("Missing level save value.");
                var board = level.BoardSaveData ?? throw new InvalidOperationException("Missing board save value.");
                var creator = board.ItemModelCreatorSaveData ?? throw new InvalidOperationException("Missing item creator save value.");
                return creator.CreatedSetItemCountExceptMatchItem;
            }
        }
        public int Count => Entries?.Length ?? 0; // 0x23e3a6c: absent vector length is zero.
        public OriginalCreatedItemCountEntry Get(int index) // 0x23e3b70: nullable pair must contain a value.
        {
            var entries = Entries ?? throw new InvalidOperationException("Missing count vector.");
            var pair = entries[index] ?? throw new InvalidOperationException("Missing count pair value.");
            return new OriginalCreatedItemCountEntry(pair.Key, pair.Value);
        }
    }
}
