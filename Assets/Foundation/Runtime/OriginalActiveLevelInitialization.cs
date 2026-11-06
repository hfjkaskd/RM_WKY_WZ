using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalActiveLevelData : IOriginalUsefulMoveCounters
    {
        public readonly OriginalBeforeAfterData Star, Cloche, SuperBooster, SuperDuke, MagicDuke;
        public readonly OriginalActiveLevelAnimationValues AnimationValues;
        public readonly OriginalSpecialItemCounts SpecialItemCounts;
        public readonly OriginalMatchItemCounts MatchItemCounts;
        public readonly OriginalLevelRestartData RestartData;
        private readonly List<int> specialItemIds;
        public readonly List<OriginalRemainingItemSnapshot> RemainingItemsSnapshots;
        public int EventMultiplier;
        public bool WonAnyLevel;
        public sbyte EnteredLevelDifficulty;
        public int ShiftCount;
        public bool IsUsefulMoveMade { get; set; }
        public int UselessMoveCount { get; set; }
        public int MaxUselessMoveStreak { get; set; }
        public int CurrentUselessMoveStreak { get; set; }

        public OriginalActiveLevelData(OriginalSlbMoveConfig slb, IOriginalThreeColorLevelNames threeColor,
            OriginalActiveLevelAnimationValues animationValues) // Native constructor 0x32b6324.
        {
            this.slb = slb ?? throw new ArgumentNullException(nameof(slb));
            this.threeColor = threeColor ?? throw new ArgumentNullException(nameof(threeColor));
            EventMultiplier = 1;
            specialItemIds = new List<int>();
            RemainingItemsSnapshots = new List<OriginalRemainingItemSnapshot>();
            AnimationValues = animationValues;
            Star = new OriginalBeforeAfterData(); Cloche = new OriginalBeforeAfterData();
            SuperBooster = new OriginalBeforeAfterData(); SuperDuke = new OriginalBeforeAfterData(); MagicDuke = new OriginalBeforeAfterData();
            SpecialItemCounts = new OriginalSpecialItemCounts();
            MatchItemCounts = new OriginalMatchItemCounts();
            RestartData = new OriginalLevelRestartData();
            IsUsefulMoveMade = true;
        }
        public void ResetValues(IOriginalActiveLevelResetContext context) // 0x32b7060.
        {
            var inventory = context.GetInventory();
            if (inventory == null) throw new NullReferenceException();
            Star.Init(inventory.Star);
            if (AnimationValues == null || AnimationValues.Coin == null) throw new NullReferenceException();
            AnimationValues.Coin.Init(inventory.Coin);
            if (AnimationValues.CompletedNThousandLevelCoin == null) throw new NullReferenceException();
            AnimationValues.CompletedNThousandLevelCoin.Init(0);
            var booster = context.GetSuperBooster();
            if (booster == null) throw new NullReferenceException();
            SuperBooster.Init(booster.CurrentProgress);
            var duke = context.GetSuperDuke();
            if (duke == null) throw new NullReferenceException();
            SuperDuke.Init(duke.CurrentProgress);
            State = 2;
            RestartData.Reset();
            IncrementStoryLevelTryCount(context);
            ShiftCount = 0;
            EnteredLevelDifficulty = Difficulty;
            RemainingItemsSnapshots.Clear();
        }
        public void IncrementStoryLevelTryCount(IOriginalActiveLevelResetContext context) // 0x32b8894.
        {
            if (StoryLevel >= 0) return;
            var prefs = context.Preferences;
            int count = prefs.GetInt("SLTC");
            prefs.SetInt("SLTC", unchecked(count + 1));
        }
        public void ResetCumulativeProgress() { WonAnyLevel = false; } // 0x32b8904.
    }
}
