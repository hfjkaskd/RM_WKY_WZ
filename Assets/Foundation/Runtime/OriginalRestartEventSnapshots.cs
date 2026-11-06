using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalSessionAbValues { int GetVariant(int id); }
    public static class OriginalRemainingCollectablesConversion
    {
        public static bool IsEnabled(IOriginalSessionAbValues values) // 0x32b46f0.
        {
            if (values == null) throw new NullReferenceException();
            return values.GetVariant(224) == 2 || values.GetVariant(225) == 2;
        }
    }
    public interface IOriginalMadnessSessionManager
    {
        int EventId { get; }
        int EventType { get; }
        bool ShouldShowIcon();
        int GetCount();
    }
    public interface IOriginalMadnessSessionPools { void CreateMadnessPools(int eventType); }
    public interface IOriginalRoyalPassSessionManager
    {
        int EventId { get; }
        bool IsEventActive { get; }
        bool ShouldShowFirstTimeTutorial();
        bool IsSafeFull();
        int GetCount();
    }
    public interface IOriginalPinataSessionManager
    {
        int EventId { get; }
        bool IsEventPlayable { get; }
        bool CanClaimFinalStep();
        bool ShouldShowFirstTimeTutorial();
        int GetCount();
    }

    // The three native snapshots share signed before/after/event-id packing.
    // Madness uses 22-bit counts; Royal Pass and Pinata use 24-bit counts.
    public abstract class OriginalRestartEventData : OriginalBeforeAfterData
    {
        protected int eventId = -1;
        public int EventId => eventId;
        protected abstract short CountBits { get; }
        public override long GetSaveData()
        { return restoreConfig.PrepareData(new long[] { ShouldConsume ? 1 : 0, Before, After, eventId }); }
        public override void LoadSaveData(long value)
        {
            restoreConfig.SetDataRaw(value);
            ShouldConsume = restoreConfig.GetData(0) == 1;
            Before = unchecked((int)restoreConfig.GetData(1));
            After = unchecked((int)restoreConfig.GetData(2));
            eventId = unchecked((int)restoreConfig.GetData(3));
        }
        public override short GetDataLength(int index)
        { return index == 0 ? (short)1 : index == 1 || index == 2 ? CountBits : index == 3 ? (short)14 : (short)0; }
        public override bool GetIsDataSigned(int index) => index >= 1 && index <= 3;
    }

    public partial class OriginalMadnessSessionData : OriginalRestartEventData, IOriginalRestartDifficultySnapshot
    {
        private readonly Func<IOriginalMadnessSessionManager> manager;
        private readonly Func<IOriginalPropellerRushProgress> propeller;
        private readonly Func<IOriginalMadnessSessionPools> pools;
        private readonly Func<bool> isTableGameLevel;
        private Dictionary<int, int> exploderIdMatchItemCount;
        public Dictionary<int, int> ExploderIdMatchItemCount => exploderIdMatchItemCount;
        protected override short CountBits => 22;
        public OriginalMadnessSessionData(Func<IOriginalMadnessSessionManager> manager,
            Func<IOriginalPropellerRushProgress> propeller, Func<IOriginalMadnessSessionPools> pools, Func<bool> isTableGameLevel)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.propeller = propeller ?? throw new ArgumentNullException(nameof(propeller));
            this.pools = pools ?? throw new ArgumentNullException(nameof(pools));
            this.isTableGameLevel = isTableGameLevel ?? throw new ArgumentNullException(nameof(isTableGameLevel));
        }
        private IOriginalMadnessSessionManager Manager()
        { var value = manager(); if (value == null) throw new NullReferenceException(); return value; }
        public bool IsEventValid() // 0x32b2f8c.
        {
            if (Before == -1) return false;
            int previous = eventId;
            if (previous != Manager().EventId) return false;
            return Manager().ShouldShowIcon();
        }
        public void Init(bool isRestart, int difficulty) // 0x32b2fd8.
        {
            bool useEvent = false;
            if (Manager().ShouldShowIcon())
            {
                int previous = eventId;
                useEvent = previous == Manager().EventId || !isRestart;
            }
            var currentPropeller = propeller(); if (currentPropeller == null) throw new NullReferenceException();
            bool shouldProgress = currentPropeller.ShouldProgress;
            exploderIdMatchItemCount = null;
            eventId = -1;
            bool addInitialCount = false;
            int start = -1;
            if (useEvent)
            {
                start = Manager().GetCount();
                eventId = Manager().EventId;
                addInitialCount = Manager().EventType == 3;
                CreateCurrentEventPools();
                if (Manager().EventType == 1) exploderIdMatchItemCount = new Dictionary<int, int>();
            }
            else if (shouldProgress)
            {
                var factory = pools(); if (factory == null) throw new NullReferenceException();
                factory.CreateMadnessPools(3);
            }
            Init(start); // Native virtual Init(int), including derived overrides.
            if (addInitialCount && !isTableGameLevel()) AddExtra(unchecked(GetMultiplierValueForDifficulty(difficulty) * 10));
        }
        private void CreateCurrentEventPools()
        {
            // Native resolves both dependencies before testing either for null.
            var factory = pools(); var current = Manager();
            if (factory == null) throw new NullReferenceException();
            factory.CreateMadnessPools(current.EventType);
        }
        public void InitAfterRestore() // 0x32b322c.
        {
            exploderIdMatchItemCount = null;
            bool useEvent = false;
            if (Manager().ShouldShowIcon())
            {
                int previous = eventId;
                useEvent = previous == Manager().EventId;
            }
            var currentPropeller = propeller(); if (currentPropeller == null) throw new NullReferenceException();
            bool shouldProgress = currentPropeller.ShouldProgress;
            if (useEvent)
            {
                CreateCurrentEventPools();
                if (Manager().EventType == 1) exploderIdMatchItemCount = new Dictionary<int, int>();
            }
            else if (shouldProgress) CreateCurrentEventPools();
        }
        public override void LoadSaveData(long value) // 0x32b3bbc.
        { base.LoadSaveData(value); InitAfterRestore(); }
    }

    public class OriginalRoyalPassLevelData : OriginalRestartEventData, IOriginalRestartSnapshot
    {
        private readonly Func<IOriginalRoyalPassSessionManager> manager;
        protected override short CountBits => 24;
        public OriginalRoyalPassLevelData(Func<IOriginalRoyalPassSessionManager> manager)
        { this.manager = manager ?? throw new ArgumentNullException(nameof(manager)); }
        private IOriginalRoyalPassSessionManager Manager()
        { var value = manager(); if (value == null) throw new NullReferenceException(); return value; }
        public bool ShouldSetData(bool isRestart) // 0x32b5264.
        {
            if (!Manager().IsEventActive) return false;
            if (Manager().ShouldShowFirstTimeTutorial()) return false;
            if (Manager().IsSafeFull()) return false;
            int previous = eventId;
            return previous == Manager().EventId || !isRestart;
        }
        public void Init(bool isRestart) // 0x32b51fc.
        {
            int start = -1, id = -1;
            if (ShouldSetData(isRestart)) { start = Manager().GetCount(); id = Manager().EventId; }
            eventId = id;
            Init(start);
        }
        public bool IsEventValid() // 0x32b5448.
        {
            if (Before == -1) return false;
            int previous = eventId;
            if (previous != Manager().EventId) return false;
            if (!Manager().IsEventActive) return false;
            return !Manager().IsSafeFull();
        }
    }

    public class OriginalPinataPartyLevelData : OriginalRestartEventData, IOriginalRestartSnapshot
    {
        private readonly Func<IOriginalPinataSessionManager> manager;
        private readonly Func<IOriginalSessionAbValues> abValues;
        protected override short CountBits => 24;
        public OriginalPinataPartyLevelData(Func<IOriginalPinataSessionManager> manager, Func<IOriginalSessionAbValues> abValues)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.abValues = abValues ?? throw new ArgumentNullException(nameof(abValues));
        }
        private IOriginalPinataSessionManager Manager()
        { var value = manager(); if (value == null) throw new NullReferenceException(); return value; }
        public void Init(bool isRestart) // 0x32b45c8.
        {
            bool conversion = OriginalRemainingCollectablesConversion.IsEnabled(abValues());
            int start = -1, id = -1;
            if (Manager().IsEventPlayable)
            {
                bool finalClaim = Manager().CanClaimFinalStep();
                if (conversion || !finalClaim)
                {
                    bool tutorial = Manager().ShouldShowFirstTimeTutorial();
                    if (!tutorial || (conversion && Manager().GetCount() > 0))
                    {
                        int previous = eventId;
                        int currentId = Manager().EventId;
                        if (previous == currentId || !isRestart)
                        { start = Manager().GetCount(); id = Manager().EventId; }
                    }
                }
            }
            eventId = id;
            Init(start);
        }
        public bool IsEventValid() // 0x32b4ac0.
        {
            if (Before == -1) return false;
            int previous = eventId;
            if (previous != Manager().EventId) return false;
            return Manager().IsEventPlayable;
        }
    }
    public sealed class OriginalDukesTreatsLevelData : OriginalBeforeAfterData
    {
        public bool IsCooldownReduction;
        public override void Init(int start) // 0x32b1104.
        { base.Init(start); IsCooldownReduction = false; }
        public void CollectTreat(int count) // 0x32b1120.
        { IsCooldownReduction = false; Before = Math.Max(unchecked(count - 1), 0); After = count; ShouldConsume = true; }
        public void ReduceCooldown(int seconds) // 0x32b113c.
        { Before = 0; After = seconds; IsCooldownReduction = true; ShouldConsume = true; }
        public int GetTreatCount() => IsCooldownReduction ? 0 : unchecked(After - Before); // 0x32b1150.
    }
}
