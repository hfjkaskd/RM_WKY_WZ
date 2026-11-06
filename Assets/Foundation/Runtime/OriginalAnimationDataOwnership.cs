using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Context resolution remains late, at the original Init/load/score call.
    // Constructing AnimationData does not query managers or load event art.
    public interface IOriginalAnimationDataContext
    {
        IOriginalMadnessSessionManager GetMadness();
        IOriginalMadnessSessionPools GetMadnessPools();
        IOriginalPropellerRushProgress GetPropellerRush();
        IOriginalRoyalPassSessionManager GetRoyalPass();
        IOriginalPinataSessionManager GetPinataParty();
        IOriginalSessionAbValues GetAbValues();
        IOriginalDailyLightningRushSessionManager GetDailyLightningRush();
        IOriginalDailyLightningRushPools GetDailyLightningRushPools();
        IOriginalSessionBundleManager GetBundleManager();
        IOriginalClashSessionManager GetClashOfChampions();
        IOriginalClashSessionPools GetClashPools();
        IOriginalClashGameplayAsset LoadClashAsset(IOriginalSessionEventBundle bundle, string assetName);
        IOriginalLevelCollectState GetWeeklyPassCollectState();
        IOriginalLevelCollectState GetMagicCauldronCollectState();
        IOriginalLevelCollectState GetDukesFortuneCollectState();
        IOriginalLevelCollectState GetOceanOdysseyCollectState();
        IOriginalLevelCollectState GetPuzzleBreakCollectState();
        IOriginalLevelCollectState GetMergeSmithCollectState();
        IOriginalLevelCollectState GetShortExpeditionCollectState();
        IOriginalMissionSuperBooster GetMissionSuperBooster();
        OriginalActiveLevelData GetActiveLevel();
        bool IsTableGameLevel();
        int GetActiveAmountMultiplier();
    }
    public sealed partial class OriginalActiveLevelAnimationValues
    {
        public readonly Dictionary<int, int> Boosters, UnlimitedBoosters, EventItems, Multipliers;
        public bool BoosterShouldConsume;
        public OriginalActiveLevelAnimationValues(IOriginalAnimationDataContext context) // 0x338a648, original construction order.
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            LeagueCrown = new OriginalBeforeAfterData();
            KingsCup = new OriginalBeforeAfterData();
            TeamBattle = new OriginalBeforeAfterData();
            Coin = new OriginalBeforeAfterData();
            CompletedNThousandLevelCoin = new OriginalBeforeAfterData();
            SkyRace = new OriginalBeforeAfterData();
            Madness = new OriginalMadnessSessionData(context.GetMadness, context.GetPropellerRush, context.GetMadnessPools, context.IsTableGameLevel);
            RoyalPass = new OriginalRoyalPassLevelData(context.GetRoyalPass);
            TeamTreasure = new OriginalBeforeAfterData();
            SpaceMission = new OriginalBeforeAfterData();
            BalloonRise = new OriginalBeforeAfterData();
            DailyLightningRush = new OriginalDailyLightningRushLevelData(context.GetDailyLightningRush, context.GetBundleManager,
                context.GetDailyLightningRushPools, context.GetActiveLevel, context.IsTableGameLevel);
            PropellerRush = new OriginalPropellerRushLevelData(context.GetPropellerRush, context.IsTableGameLevel, context.GetActiveLevel);
            PinataParty = new OriginalPinataPartyLevelData(context.GetPinataParty, context.GetAbValues);
            HiddenTemple = new OriginalBeforeAfterData();
            DragonNest = new OriginalBeforeAfterData();
            MagicCauldron = new OriginalMagicCauldronLevelData(context.GetMagicCauldronCollectState);
            ArcheryArena = new OriginalArcheryLevelData();
            DukesFortune = new OriginalDukesFortuneLevelData(context.GetDukesFortuneCollectState);
            TeamTournament = new OriginalBeforeAfterData();
            TrainJourney = new OriginalBeforeAfterData();
            OceanOdyssey = new OriginalOceanOdysseyLevelData(context.GetOceanOdysseyCollectState);
            MissionEvents = new OriginalMissionEventsStartData(context.GetMissionSuperBooster);
            PuzzleBreak = new OriginalPuzzleBreakLevelData(context.GetPuzzleBreakCollectState);
            RobotRivals = new OriginalBeforeAfterData();
            MergeSmith = new OriginalMergeSmithLevelData(context.GetMergeSmithCollectState);
            ClashOfChampions = new OriginalClashOfChampionsLevelData(context.GetClashOfChampions, context.GetBundleManager,
                context.GetClashPools, context.LoadClashAsset, context.GetActiveAmountMultiplier, context.GetActiveLevel);
            RoyalWorldCup = new OriginalBeforeAfterData();
            WeeklyPass = new OriginalWeeklyPassLevelData(context.GetWeeklyPassCollectState);
            ShortExpeditionEvent = new OriginalShortExpeditionEventLevelData(context.GetShortExpeditionCollectState);
            InviteFriends = new OriginalInviteFriendsStartData();
            DukesTreats = new OriginalDukesTreatsLevelData();
            Boosters = new Dictionary<int, int>();
            UnlimitedBoosters = new Dictionary<int, int>();
            EventItems = new Dictionary<int, int>();
            Multipliers = new Dictionary<int, int>();
            StartSnapshots = this;
        }
    }
}
