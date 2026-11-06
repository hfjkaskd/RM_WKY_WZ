using System;

namespace Whitebox.Foundation
{
    public interface IOriginalWelcomeBackMoveProvider { int GetExtraMovesOfWelcomeBackGift(); }
    public interface IOriginalWelcomeBackGiftProgress
    {
        int Tier { get; }
        int StartLevel { get; }
        void SetStartLevel(int level);
        void SetTier(int tier);
        void SetOfflineGiftDeservedDate(int date);
        void SetShouldGiveReward(bool value);
    }
    public sealed class OriginalWelcomeBackUserProgress { public int Level; }
    [Serializable]
    public sealed class OriginalWelcomeBackMoveTiers { public int[] tier1, tier2, tier3; }
    // The move-count portion of WelcomeBackGiftManager. Gift award, date/flow
    // handling and packed progress persistence remain their own required services.
    public sealed class OriginalWelcomeBackMoves : IOriginalWelcomeBackMoveProvider
    {
        private readonly Func<OriginalWelcomeBackUserProgress> currentUserProgress;
        private readonly Func<OriginalActiveLevelData> currentLevel;
        private readonly Action<int> writeChurnPeriod;
        private readonly OriginalWelcomeBackMoveTiers tiers;
        public IOriginalWelcomeBackGiftProgress Progress;
        public object Rewards;
        public int LastChurnPeriod;
        public OriginalWelcomeBackMoves(Func<OriginalWelcomeBackUserProgress> currentUserProgress,
            Func<OriginalActiveLevelData> currentLevel, Action<int> writeChurnPeriod, OriginalWelcomeBackMoveTiers tiers)
        {
            this.currentUserProgress = currentUserProgress ?? throw new ArgumentNullException(nameof(currentUserProgress));
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
            this.writeChurnPeriod = writeChurnPeriod ?? throw new ArgumentNullException(nameof(writeChurnPeriod));
            this.tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
        }
        public void SetChurnPeriod(int days) // 0x283b0e4 writes memory before storage.
        { LastChurnPeriod = days; writeChurnPeriod(days); }
        public void ResetGiftData(bool resetAfterExtraLevels) // 0x283bcb0: retain partial writes when a service fails.
        {
            if (resetAfterExtraLevels)
            {
                if (Progress == null) throw new NullReferenceException(); Progress.SetStartLevel(0);
                if (Progress == null) throw new NullReferenceException(); Progress.SetTier(0);
            }
            if (Progress == null) throw new NullReferenceException(); Progress.SetOfflineGiftDeservedDate(0);
            if (Progress == null) throw new NullReferenceException(); Progress.SetShouldGiveReward(false);
            SetChurnPeriod(0); Rewards = null;
        }
        public bool IsMoveGiftActive() // 0x283be08: the expiry reset occurs only at exactly 25.
        {
            var user = currentUserProgress();
            if (user == null || Progress == null) throw new NullReferenceException();
            int difference = unchecked(user.Level - Progress.StartLevel);
            if (difference < 25) return difference >= 0;
            if (difference == 25) ResetGiftData(true);
            return false;
        }
        public int GetExtraMovesOfWelcomeBackGift() // 0x283bea4 and inlined GetExtraMoveCount 0x283bedc.
        {
            if (!IsMoveGiftActive()) return 0;
            if (Progress == null) throw new NullReferenceException();
            int tier = Progress.Tier;
            var user = currentUserProgress();
            if (user == null || Progress == null) throw new NullReferenceException();
            var active = currentLevel(); if (active == null) throw new NullReferenceException();
            if (active.IsBonus) return 0;
            int difference = unchecked(user.Level - Progress.StartLevel);
            int hardMinimum = 0;
            if (difference == 0 && active.Difficulty > 0)
                hardMinimum = active.Difficulty == 1 ? 10 : active.Difficulty == 2 ? 15 : 20;
            int[] values;
            switch (tier)
            {
                case 1: values = tiers.tier1; break;
                case 2: values = tiers.tier2; break;
                case 3: values = tiers.tier3; break;
                default: values = Array.Empty<int>(); break; // Native Array.Empty<int> static field.
            }
            if (values == null) throw new NullReferenceException();
            if (difference < 0 || difference >= values.Length) return 0;
            return Math.Max(hardMinimum, values[difference]);
        }
    }
}
