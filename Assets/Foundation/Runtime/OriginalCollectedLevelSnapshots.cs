using System;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelCollectState { bool ShouldCollectFromLevel { get; set; } }

    // The six unsigned native variants share the same field/save/load operations.
    // Init and FullReset remain inherited: neither clears the extra collect flag.
    public abstract class OriginalCollectedLevelData : OriginalBeforeAfterData
    {
        private readonly Func<IOriginalLevelCollectState> manager;
        protected bool shouldCollectFromLevel;
        public bool ShouldCollectFromLevel => shouldCollectFromLevel;
        protected OriginalCollectedLevelData(Func<IOriginalLevelCollectState> manager)
        { this.manager = manager ?? throw new ArgumentNullException(nameof(manager)); }
        public void SetShouldCollect(bool value) { shouldCollectFromLevel = value; }
        public override long GetSaveData()
        { return restoreConfig.PrepareData(new long[] { ShouldConsume ? 1 : 0, Before, After, shouldCollectFromLevel ? 1 : 0 }); }
        public override void LoadSaveData(long value)
        {
            base.LoadSaveData(value);
            shouldCollectFromLevel = restoreConfig.GetData(3) == 1;
            var owner = manager(); if (owner == null) throw new NullReferenceException();
            owner.ShouldCollectFromLevel = shouldCollectFromLevel;
        }
        public override short GetDataLength(int index)
        { return index == 0 || index == 3 ? (short)1 : index == 1 || index == 2 ? (short)20 : (short)0; }
    }
    public sealed class OriginalWeeklyPassLevelData : OriginalCollectedLevelData
    {
        public OriginalWeeklyPassLevelData(Func<IOriginalLevelCollectState> manager) : base(manager) { } // 0x32bb7d8.
        public void SetShouldCollectKey(bool value) { SetShouldCollect(value); } // 0x32bb438.
        public override short GetDataLength(int index) // 0x32bb790.
        { return index == 0 || index == 3 ? (short)1 : index == 1 || index == 2 ? (short)24 : (short)0; }
        public override bool GetIsDataSigned(int index) => index == 1 || index == 2; // 0x32bb7bc.
    }
    public sealed class OriginalMagicCauldronLevelData : OriginalCollectedLevelData
    {
        public OriginalMagicCauldronLevelData(Func<IOriginalLevelCollectState> manager) : base(manager) { } // 0x32b3e58.
        public void SetShouldCollectElixir(bool value) { SetShouldCollect(value); } // 0x32b3c88.
    }
    public sealed class OriginalDukesFortuneLevelData : OriginalCollectedLevelData
    {
        public OriginalDukesFortuneLevelData(Func<IOriginalLevelCollectState> manager) : base(manager) { } // 0x338ae20.
        public void SetShouldCollectToken(bool value) { SetShouldCollect(value); } // 0x338e338.
    }
    public sealed class OriginalOceanOdysseyLevelData : OriginalCollectedLevelData
    {
        public OriginalOceanOdysseyLevelData(Func<IOriginalLevelCollectState> manager) : base(manager) { } // 0x32b4558.
        public void SetShouldCollectCannon(bool value) { SetShouldCollect(value); } // 0x32b4388.
    }
    public sealed class OriginalPuzzleBreakLevelData : OriginalCollectedLevelData
    { public OriginalPuzzleBreakLevelData(Func<IOriginalLevelCollectState> manager) : base(manager) { } } // 0x32b5184.
    public sealed class OriginalMergeSmithLevelData : OriginalCollectedLevelData
    { public OriginalMergeSmithLevelData(Func<IOriginalLevelCollectState> manager) : base(manager) { } } // 0x32b4030.
    public sealed class OriginalShortExpeditionEventLevelData : OriginalCollectedLevelData
    { public OriginalShortExpeditionEventLevelData(Func<IOriginalLevelCollectState> manager) : base(manager) { } } // 0x32b62ec.

    public interface IOriginalMissionSuperBooster { bool IsPlayingWithSuperBooster(); }
    public sealed partial class OriginalMissionEventsStartData : IOriginalRestorable
    {
        private OriginalRestorableData restoreConfig;
        private readonly Func<IOriginalMissionSuperBooster> superBooster;
        public OriginalMissionEventsStartData() // 0x32b4038. No manager is resolved during construction.
        { restoreConfig = new OriginalRestorableData(0, this); }
        public OriginalMissionEventsStartData(Func<IOriginalMissionSuperBooster> superBooster) : this()
        { this.superBooster = superBooster ?? throw new ArgumentNullException(nameof(superBooster)); }
        public void AddExtraForItem(int itemType, int add) // 0x32b4094.
        {
            if (add == 0) return;
            ShouldConsume = true;
            switch (itemType)
            {
                case 2: CreatedRocketCount = unchecked(CreatedRocketCount + add); break;
                case 3: CreatedPropellerCount = unchecked(CreatedPropellerCount + add); break;
                case 4: CreatedTntCount = unchecked(CreatedTntCount + add); break;
                case 5: AddLightball(add); break;
            }
        }
        private void AddLightball(int add) // 0x32b40fc: normal count changes before required manager query.
        {
            CreatedLightballCount = unchecked(CreatedLightballCount + add);
            var owner = superBooster == null ? null : superBooster();
            if (owner == null) throw new NullReferenceException();
            if (owner.IsPlayingWithSuperBooster()) CreatedSuperLightballCount = unchecked(CreatedSuperLightballCount + add);
        }
        public void AddExtraForTiledId(int tiledId, int add) // 0x32b41a4.
        {
            int item = tiledId == 10 ? 3 : tiledId == 20 || tiledId == 30 ? 2 : tiledId == 40 ? 4 : tiledId == 50 ? 5 : 0;
            AddExtraForItem(item, add);
        }
        public void AddExtraForSlotSelectionOrder(int order, int add) // 0x32b41fc, ELF 0x405384c.
        { AddExtraForItem(order == 0 ? 3 : order == 1 ? 4 : order == 2 ? 2 : order == 3 ? 5 : 0, add); }
        public void MultiplyValues(int multiplier) // 0x32b421c.
        {
            CreatedPropellerCount = unchecked(CreatedPropellerCount * multiplier);
            CreatedRocketCount = unchecked(CreatedRocketCount * multiplier);
            CreatedTntCount = unchecked(CreatedTntCount * multiplier);
            CreatedLightballCount = unchecked(CreatedLightballCount * multiplier);
            CreatedSuperLightballCount = unchecked(CreatedSuperLightballCount * multiplier);
        }
        public void Consume() { ShouldConsume = false; } // 0x32b423c.
        public long GetSaveData() // 0x32b4244: shouldConsume is deliberately absent from the packed data.
        { return restoreConfig.PrepareData(new long[] { CreatedPropellerCount, CreatedRocketCount, CreatedTntCount, CreatedLightballCount, CreatedSuperLightballCount }); }
        public void LoadSaveData(long value) // 0x32b42f8: retains shouldConsume.
        {
            restoreConfig.SetDataRaw(value);
            CreatedPropellerCount = unchecked((int)restoreConfig.GetData(0));
            CreatedRocketCount = unchecked((int)restoreConfig.GetData(1));
            CreatedTntCount = unchecked((int)restoreConfig.GetData(2));
            CreatedLightballCount = unchecked((int)restoreConfig.GetData(3));
            CreatedSuperLightballCount = unchecked((int)restoreConfig.GetData(4));
        }
        public short GetDataLength(int index) => (uint)index < 5u ? (short)10 : (short)0; // 0x32b4370.
        public bool GetIsDataSigned(int index) => false;
    }
    public sealed partial class OriginalInviteFriendsStartData
    {
        public void SetEgoPrice(int price) { EgoPrice = price; ShouldConsume = true; } // 0x32b118c.
        public void Consume() { ShouldConsume = false; EgoPrice = 0; } // 0x32b119c, diagnostic Log.Debug omitted.
    }
}
