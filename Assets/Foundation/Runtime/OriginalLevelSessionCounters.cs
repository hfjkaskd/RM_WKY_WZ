using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalUsefulMoveCounters
    {
        bool IsUsefulMoveMade { get; set; }
        int UselessMoveCount { get; set; }
        int MaxUselessMoveStreak { get; set; }
        int CurrentUselessMoveStreak { get; set; }
    }

    // MatchItemCounts constructor/reset 0x21a4194/0x21a3f58 replace the array.
    // Native statistics intentionally support 1..5 only, including when the
    // MatchType enum also contains Orange (6).
    public sealed class OriginalMatchItemCounts
    {
        private int[] explodeCounts = new int[5];
        public void Reset() { explodeCounts = new int[5]; }
        public void IncrementCount(int matchType, int count) // 0x21a4028.
        { if (matchType >= 1 && matchType <= 5) explodeCounts[matchType - 1] = unchecked(explodeCounts[matchType - 1] + count); }
        public int GetCount(int matchType) // 0x21a4094.
        { return matchType >= 1 && matchType <= 5 ? explodeCounts[matchType - 1] : -1; }
    }

    // Count creation/serialization and game-context static forwarding are restored
    // separately. Reset clears entries; it does not zero and retain old entries.
    public sealed class OriginalSpecialItemCounts
    {
        public readonly Dictionary<int, OriginalSpecialItemCount> Items = new Dictionary<int, OriginalSpecialItemCount>();
        public readonly Dictionary<int, int> Combos = new Dictionary<int, int>();
        public int TappedLightballCount, SwappedItemCount, AutoTriggeredItemCount, SpecialItemCountCreatedByCrystal;
        public void Reset() // 0x27b2874.
        {
            Items.Clear(); Combos.Clear();
            TappedLightballCount = 0; SwappedItemCount = 0; AutoTriggeredItemCount = 0; SpecialItemCountCreatedByCrystal = 0;
        }
    }
    public sealed class OriginalSpecialItemCount : IOriginalRestorable
    {
        private OriginalRestorableData restoreConfig;
        public int CreationCount, AutoCreationCount, UseCount, AutoTriggerCount, LightballCreationCount;
        public OriginalSpecialItemCount() { restoreConfig = new OriginalRestorableData(0, this); } // 0x27b310c.
        private int Clamp(int count, int index) // ClampByBits 0x2d7bd18 has no lower clamp.
        { int max = ~(-1 << GetDataLength(index)); return count > max ? max : count; }
        public long GetSaveData() // 0x27b3b74.
        { return restoreConfig.PrepareData(new long[] { Clamp(CreationCount, 0), Clamp(AutoCreationCount, 1), Clamp(UseCount, 2), Clamp(AutoTriggerCount, 3), Clamp(LightballCreationCount, 4) }); }
        public void LoadSaveData(long value) // 0x27b3ebc.
        {
            restoreConfig.SetDataRaw(value);
            CreationCount = unchecked((short)restoreConfig.GetData(0)); AutoCreationCount = unchecked((short)restoreConfig.GetData(1));
            UseCount = unchecked((short)restoreConfig.GetData(2)); AutoTriggerCount = unchecked((short)restoreConfig.GetData(3));
            LightballCreationCount = unchecked((short)restoreConfig.GetData(4));
        }
        public short GetDataLength(int index) => index == 0 || index == 1 ? (short)13 : index >= 2 && index <= 4 ? (short)12 : (short)0;
        public bool GetIsDataSigned(int index) => false;
    }

    // LevelRestartData constructor/reset and unlimited/animation flags. Refund,
    // purchase/save and restore operations require their real inventory/save
    // context and are not substituted here.
    public sealed class OriginalLevelRestartData
    {
        public readonly Dictionary<int, int> Purchases = new Dictionary<int, int>();
        public readonly Dictionary<int, int> Refunds = new Dictionary<int, int>();
        private bool hadUnlimitedRocket, hadUnlimitedTnt, hadUnlimitedLightball;
        private bool hammerAnimation, arrowAnimation, cannonAnimation, jesterAnimation;
        public void Reset() // 0x32b27e0.
        {
            Purchases.Clear(); Refunds.Clear();
            hammerAnimation = false; arrowAnimation = false; cannonAnimation = false; jesterAnimation = false;
            hadUnlimitedRocket = false; hadUnlimitedTnt = false; hadUnlimitedLightball = false;
        }
        public void OnStartedWithUnlimitedPrelevel(int booster) // 0x32b1428.
        {
            if (booster == 1) hadUnlimitedRocket = true;
            else if (booster == 2) hadUnlimitedTnt = true;
            else if (booster == 3) hadUnlimitedLightball = true;
        }
        public bool HadUnlimitedPrelevel(int booster) // 0x32b1464.
        { return booster == 1 ? hadUnlimitedRocket : booster == 2 ? hadUnlimitedTnt : booster == 3 && hadUnlimitedLightball; }
        public void SetIsInGameBoosterAnimationActive(int booster, bool active) // 0x32b17c8.
        {
            if (booster == 4) hammerAnimation = active;
            else if (booster == 5) arrowAnimation = active;
            else if (booster == 6) cannonAnimation = active;
            else if (booster == 7) jesterAnimation = active;
        }
        public bool IsInGameBoosterAnimationActive(int booster) // 0x32b2da8.
        { return booster == 4 ? hammerAnimation : booster == 5 ? arrowAnimation : booster == 6 ? cannonAnimation : booster == 7 && jesterAnimation; }
    }

    public sealed class OriginalRemainingItemSnapshot
    {
        public string Name;
        public int Type, Remaining, State;
    }

    public sealed class OriginalLevelStartInventory
    {
        public int Coin, Star;
    }
    public interface IOriginalSessionProgress { int CurrentProgress { get; } }
    public interface IOriginalSessionIntPreferences
    {
        int GetInt(string key);
        void SetInt(string key, int value);
    }
    public interface IOriginalActiveLevelResetContext
    {
        OriginalLevelStartInventory GetInventory();
        IOriginalSessionProgress GetSuperBooster();
        IOriginalSessionProgress GetSuperDuke();
        IOriginalSessionIntPreferences Preferences { get; }
    }

    // The part of AnimationData read by ResetValues. The application must supply
    // its actual shared snapshots; this class creates no fallback values.
    public sealed partial class OriginalActiveLevelAnimationValues
    {
        public readonly OriginalBeforeAfterData Coin, CompletedNThousandLevelCoin;
        public OriginalActiveLevelAnimationValues(OriginalBeforeAfterData coin, OriginalBeforeAfterData completedNThousandLevelCoin)
        { Coin = coin; CompletedNThousandLevelCoin = completedNThousandLevelCoin; }
    }
}
