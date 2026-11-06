using System;

namespace Whitebox.Foundation
{
    public interface IOriginalClocheLevelStart
    {
        int GetCloche();
        void SaveProgressData(int value);
    }
    public interface IOriginalMultiplierLevelStart
    {
        bool CanPlayLevelWithMultiplier { get; }
        int GetSelectedMultiplierValue();
    }
    public interface IOriginalUnlimitedBoosterTimes { int RemainingTime(int boosterType); }
    public interface IOriginalBalloonLevelStart { int GetCollectibleCount(); }
    public sealed class OriginalArcheryStartStreak { public long PackedProgress; }
    public sealed class OriginalArcheryStartRank { public int RankIndex; }
    public interface IOriginalArcheryLevelStart
    {
        bool ShouldProgress { get; }
        OriginalArcheryStartStreak Streak { get; }
        OriginalArcheryStartRank Rank { get; }
        bool IsIconActive { get; }
    }
    public interface IOriginalRestartDifficultySnapshot { void Init(bool isRestart, int difficulty); }
    public interface IOriginalRestartSnapshot { void Init(bool isRestart); }
    public interface IOriginalClashLevelStart { void Init(sbyte difficulty, bool isMultiplied, bool isAfterRestore); }
    public interface IOriginalActiveLevelStartContext : IOriginalActiveLevelResetContext
    {
        IOriginalClocheLevelStart GetCloche();
        IOriginalSessionProgress GetMagicDuke();
        IOriginalMultiplierLevelStart GetMultiplierSelection();
        bool CanMultiply(bool checkSelectedMultiplier);
        IOriginalUnlimitedBoosterTimes GetUnlimitedBoosterInventory();
        float RealtimeSinceStartup { get; }
        IOriginalBalloonLevelStart GetBalloonRise();
        IOriginalArcheryLevelStart GetArcheryArena();
    }

    // Shared AnimationData entries. The owned constructor creates these on the
    // animation object itself; supplied snapshots remain available for diagnostics.
    public class OriginalLevelStartSnapshots
    {
        public IOriginalRestartDifficultySnapshot Madness;
        public IOriginalRestartSnapshot RoyalPass, PinataParty;
        public OriginalBeforeAfterData WeeklyPass, DailyLightningRush;
        public OriginalPropellerRushLevelData PropellerRush;
        public OriginalBeforeAfterData SkyRace, KingsCup, TeamTreasure, BalloonRise, SpaceMission, TeamBattle, LeagueCrown;
        public OriginalBeforeAfterData HiddenTemple, DragonNest, MagicCauldron, DukesFortune, TeamTournament, OceanOdyssey;
        public OriginalMissionEventsStartData MissionEvents;
        public OriginalBeforeAfterData TrainJourney, PuzzleBreak, RobotRivals, RoyalWorldCup, MergeSmith;
        public IOriginalClashLevelStart ClashOfChampions;
        public OriginalBeforeAfterData ShortExpeditionEvent;
        public OriginalInviteFriendsStartData InviteFriends;
        public OriginalBeforeAfterData DukesTreats;
        public OriginalArcheryLevelData ArcheryArena;
    }
    public sealed partial class OriginalActiveLevelAnimationValues : OriginalLevelStartSnapshots
    {
        public readonly OriginalLevelStartSnapshots StartSnapshots;
        public bool CanPlayLevelWithMultiplier;
        public OriginalActiveLevelAnimationValues(OriginalBeforeAfterData coin, OriginalBeforeAfterData completedNThousandLevelCoin,
            OriginalLevelStartSnapshots startSnapshots) : this(coin, completedNThousandLevelCoin)
        { StartSnapshots = startSnapshots; }
    }
    // These are the original startup field writes. Other event and save
    // operations are restored separately rather than replaced with no-ops.
    public sealed partial class OriginalMissionEventsStartData
    {
        public bool ShouldConsume;
        public int CreatedPropellerCount, CreatedRocketCount, CreatedTntCount, CreatedLightballCount, CreatedSuperLightballCount;
        public void Init(int start) // 0x32b4084: deliberately retains shouldConsume.
        { CreatedPropellerCount = start; CreatedRocketCount = start; CreatedTntCount = start; CreatedLightballCount = start; CreatedSuperLightballCount = start; }
    }
    public sealed partial class OriginalInviteFriendsStartData
    {
        public bool ShouldConsume;
        public int EgoPrice;
        public void Init() { ShouldConsume = false; EgoPrice = 0; } // 0x32b1180.
    }
    public class OriginalArcheryLevelData : OriginalBeforeAfterData
    {
        public int StreakIndex { get; private set; }
        public int RankIndex { get; private set; }
        public bool IconWasVisibleAtHome { get; private set; }
        public void Init(int start, int currentStreakAtLevelStart, int currentRankAtLevelStart, bool userDidSeeIconAtHome) // 0x338c4e8.
        {
            StreakIndex = currentStreakAtLevelStart; RankIndex = currentRankAtLevelStart; IconWasVisibleAtHome = userDidSeeIconAtHome;
            Init(start);
        }
        public override long GetSaveData() // 0x338c500.
        { return restoreConfig.PrepareData(new long[] { ShouldConsume ? 1 : 0, Before, After, StreakIndex, RankIndex, IconWasVisibleAtHome ? 1 : 0 }); }
        public override void LoadSaveData(long value) // 0x338c5c4.
        {
            restoreConfig.SetDataRaw(value);
            ShouldConsume = restoreConfig.GetData(0) == 1;
            Before = unchecked((int)restoreConfig.GetData(1)); After = unchecked((int)restoreConfig.GetData(2));
            StreakIndex = unchecked((int)restoreConfig.GetData(3)); RankIndex = unchecked((int)restoreConfig.GetData(4));
            IconWasVisibleAtHome = restoreConfig.GetData(5) == 1;
        }
        public override short GetDataLength(int index) // 0x338c660.
        { return index == 0 || index == 5 ? (short)1 : index == 1 || index == 2 ? (short)20 : index == 3 ? (short)3 : index == 4 ? (short)6 : (short)0; }
    }
    public interface IOriginalPropellerRushProgress { bool ShouldProgress { get; } }
    public sealed class OriginalPropellerRushLevelData : OriginalBeforeAfterData
    {
        private readonly Func<IOriginalPropellerRushProgress> manager;
        private readonly Func<bool> isTableGameLevel;
        private readonly Func<OriginalActiveLevelData> currentLevel;
        public int CollectedAmountAfterWin { get; private set; }
        public int Difficulty { get; private set; }
        public OriginalPropellerRushLevelData(Func<IOriginalPropellerRushProgress> manager, Func<bool> isTableGameLevel, Func<OriginalActiveLevelData> currentLevel)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
            this.isTableGameLevel = isTableGameLevel ?? throw new ArgumentNullException(nameof(isTableGameLevel));
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
        }
        public void Init(int start, int levelDifficulty) // 0x32b4ce4 and its inline use in SetStartValues.
        {
            Before = start; After = start; ShouldConsume = false; CollectedAmountAfterWin = 0; Difficulty = levelDifficulty;
            TryIncrementCount(10);
        }
        public void Reset() // 0x32b4d9c: retains difficulty.
        { ShouldConsume = false; Before = 0; After = 0; CollectedAmountAfterWin = 0; }
        public void TryIncrementCount(int amount) // 0x32b4cf8.
        {
            var current = manager(); if (current == null) throw new NullReferenceException();
            if (!current.ShouldProgress || isTableGameLevel()) return;
            ShouldConsume = true;
            AddExtra(unchecked(GetMultiplierValueForDifficulty(Difficulty) * amount));
        }
        public override void LoadSaveData(long value) // 0x32b4eac: base snapshot layout, then current level difficulty.
        {
            base.LoadSaveData(value);
            var level = currentLevel(); if (level == null) throw new NullReferenceException();
            Difficulty = level.Difficulty;
        }
    }

    public sealed partial class OriginalActiveLevelData
    {
        public int EgoPurchaseCount, NoPossibleMoveShuffleCount, NoPossibleMoveColoredShuffleCount;
        public int FalseSwipeCount, ShownHintCount, UsedHintCount;
        public float LevelStartTime;
        public bool IsMultipliedInLevel;
        public bool HasUnlimitedHammer, HasUnlimitedArrow, HasUnlimitedCannon, HasUnlimitedJester;
        public bool DidUseUnlimitedHammer, DidUseUnlimitedArrow, DidUseUnlimitedCannon, DidUseUnlimitedJester;

        public void Start(IOriginalActiveLevelStartContext context) // 0x32b6ed0; only diagnostic Log.Debug omitted.
        { ResetValues(context); SetStartValues(context, 1); }
        public void Restart(IOriginalActiveLevelStartContext context) // 0x32b7a6c.
        { ResetValues(context); SetStartValues(context, 2); }
        public void SetStartValues(IOriginalActiveLevelStartContext context, int origin) // 0x32b7244.
        {
            EgoPurchaseCount = 0;
            var cloche = context.GetCloche(); if (cloche == null) throw new NullReferenceException();
            Cloche.Init(cloche.GetCloche());
            cloche = context.GetCloche(); if (cloche == null) throw new NullReferenceException();
            cloche.SaveProgressData(Cloche.Before);
            var magic = context.GetMagicDuke(); if (magic == null) throw new NullReferenceException();
            MagicDuke.Init(magic.CurrentProgress);
            if (origin == 1) WonAnyLevel = false;
            var multiplier = context.GetMultiplierSelection();
            var animation = AnimationValues;
            bool canPlay = false;
            if (StoryLevel >= 0 && !IsTableGame && !IsBonus)
            {
                if (multiplier == null) throw new NullReferenceException();
                canPlay = multiplier.CanPlayLevelWithMultiplier;
            }
            if (animation == null) throw new NullReferenceException();
            animation.CanPlayLevelWithMultiplier = canPlay;
            bool multiplied = context.CanMultiply(false);
            IsMultipliedInLevel = multiplied;
            int selected = 1;
            if (multiplied)
            {
                if (multiplier == null) throw new NullReferenceException();
                selected = multiplier.CanPlayLevelWithMultiplier ? multiplier.GetSelectedMultiplierValue() : 2;
            }
            EventMultiplier = selected;
            HasUnlimitedHammer = RemainingTime(context, 4) > 0;
            HasUnlimitedArrow = RemainingTime(context, 5) > 0;
            HasUnlimitedCannon = RemainingTime(context, 6) > 0;
            int jesterTime = RemainingTime(context, 7);
            DidUseUnlimitedHammer = false; DidUseUnlimitedArrow = false; DidUseUnlimitedCannon = false; DidUseUnlimitedJester = false;
            HasUnlimitedJester = jesterTime > 0;
            NoPossibleMoveShuffleCount = 0; NoPossibleMoveColoredShuffleCount = 0;
            LevelStartTime = context.RealtimeSinceStartup;
            SpecialItemCounts.Reset(); MatchItemCounts.Reset();
            FalseSwipeCount = 0; ShownHintCount = 0; UsedHintCount = 0; IsUsefulMoveMade = true;
            UselessMoveCount = 0; MaxUselessMoveStreak = 0; CurrentUselessMoveStreak = 0; ShiftCount = 0;
            var balloon = context.GetBalloonRise();
            var archery = context.GetArcheryArena();
            var data = animation.StartSnapshots;
            if (data == null) throw new NullReferenceException();
            bool isRestart = origin == 2;
            data.Madness.Init(isRestart, Difficulty);
            data.RoyalPass.Init(isRestart);
            data.WeeklyPass.Init(0);
            data.PinataParty.Init(isRestart);
            data.DailyLightningRush.Init(Difficulty);
            data.PropellerRush.Init(0, Difficulty);
            data.SkyRace.Init(0); data.KingsCup.Init(0); data.TeamTreasure.Init(0);
            if (balloon == null) throw new NullReferenceException();
            var balloonData = data.BalloonRise;
            int collected = balloon.GetCollectibleCount();
            balloonData.Init(collected);
            data.SpaceMission.Init(0); data.TeamBattle.Init(0); data.LeagueCrown.Init(0);
            data.HiddenTemple.Init(0); data.DragonNest.Init(0); data.MagicCauldron.Init(0); data.DukesFortune.Init(0);
            data.TeamTournament.Init(0); data.OceanOdyssey.Init(0);
            data.MissionEvents.Init(0);
            data.TrainJourney.Init(0); data.PuzzleBreak.Init(0); data.RobotRivals.Init(0); data.RoyalWorldCup.Init(0); data.MergeSmith.Init(0);
            data.ClashOfChampions.Init(Difficulty, IsMultipliedInLevel, false);
            data.ShortExpeditionEvent.Init(0); data.InviteFriends.Init(); data.DukesTreats.Init(0);
            if (archery == null) throw new NullReferenceException();
            int streak = 0;
            if (archery.ShouldProgress)
            {
                var progress = archery.Streak; if (progress == null) throw new NullReferenceException();
                streak = unchecked((int)(progress.PackedProgress >> 20)) & 7;
            }
            var rank = archery.Rank; if (rank == null) throw new NullReferenceException();
            var archeryData = data.ArcheryArena;
            int rankIndex = rank.RankIndex;
            bool visible = archery.IsIconActive;
            archeryData.Init(0, streak, rankIndex, visible);
        }
        private static int RemainingTime(IOriginalActiveLevelStartContext context, int booster)
        {
            var inventory = context.GetUnlimitedBoosterInventory(); if (inventory == null) throw new NullReferenceException();
            return inventory.RemainingTime(booster);
        }
    }
}
