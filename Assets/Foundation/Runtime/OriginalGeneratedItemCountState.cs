using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public struct OriginalCreatedItemCountEntry
    {
        public readonly int Key, Value;
        public OriginalCreatedItemCountEntry(int key, int value) { Key = key; Value = value; }
    }
    public interface IOriginalSavedCreatedItemCounts
    {
        int Count { get; }
        OriginalCreatedItemCountEntry Get(int index);
    }
    public interface IOriginalCreatedCountElements
    {
        int Count { get; }
        int GetTiledId(int index);
        void Disable(int index);
    }
    public interface IOriginalCreatedCountSet
    {
        IOriginalCreatedCountElements Elements { get; }
        void UpdateFillAvailability();
    }
    public interface IOriginalCreatedItemCountContext
    {
        void ClearPrerequisiteItems();
        Dictionary<int, int> GetPredefinedInitialCounts();
        IOriginalSavedCreatedItemCounts GetCurrentRestoreLoader();
        IOriginalCreatedCountSet[] GetSets();
        bool IsFillingObstacle(int itemType);
        int GetLimitCount(int goalType);
        int GetGoalTargetCount(int goalType);
        void CalculateRandomFactors();
    }
    // The shared ItemCreator dictionary is supplied by the actual generation
    // owner. These methods neither replace the queue nor fabricate save state.
    public sealed class OriginalGeneratedItemCountState : IOriginalGameStartItemCreator
    {
        private readonly Dictionary<int, int> created;
        private readonly IOriginalCreatedItemCountContext context;
        public OriginalGeneratedItemCountState(Dictionary<int, int> created, IOriginalCreatedItemCountContext context)
        { this.created = created; this.context = context; }
        public void ClearCreatedItemsCountExceptPredefined() // 0x23ead94.
        {
            context.ClearPrerequisiteItems();
            created.Clear();
            var initial = context.GetPredefinedInitialCounts() ?? throw new NullReferenceException();
            foreach (var pair in initial) created.Add(pair.Key, pair.Value);
        }
        public void UpdateCreatedItemsCountExceptPredefined() // 0x23eaf58.
        {
            int length = (context.GetCurrentRestoreLoader() ?? throw new NullReferenceException()).Count;
            for (int i = 0; i < length; i++)
            {
                var pair = (context.GetCurrentRestoreLoader() ?? throw new NullReferenceException()).Get(i);
                int value = pair.Value;
                if (created.TryGetValue(pair.Key, out int previous))
                    created[pair.Key] = unchecked(unchecked(value - previous) + created[pair.Key]);
                else created.Add(pair.Key, value);
            }
            int setIndex = 0;
            while (true)
            {
                var sets = context.GetSets() ?? throw new NullReferenceException();
                if (setIndex >= sets.Length) break;
                var set = sets[setIndex] ?? throw new NullReferenceException();
                var elements = set.Elements ?? throw new NullReferenceException();
                for (int i = 0; i < elements.Count; i++)
                {
                    int type = OriginalTiledItemTypes.AsItemType(elements.GetTiledId(i));
                    if (!context.IsFillingObstacle(type)) continue;
                    int goal = OriginalGoalTypes.FromItemType(type);
                    int limit = context.GetLimitCount(goal);
                    bool exists = created.TryGetValue(type, out int count);
                    if ((exists && count == context.GetGoalTargetCount(goal)) || (limit != 0 && count == limit))
                        elements.Disable(i);
                }
                set.UpdateFillAvailability();
                setIndex++;
            }
            context.CalculateRandomFactors();
        }
    }
}
