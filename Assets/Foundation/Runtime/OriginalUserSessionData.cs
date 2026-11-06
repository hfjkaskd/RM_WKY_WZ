using System;

namespace Whitebox.Foundation
{
    public interface IOriginalUserSessionEnvironment
    {
        event Action OnApplicationPause;
        // Resolve the global current user/session/active level, which may differ
        // from the session whose scene callback is running. Missing owners fail.
        OriginalActiveLevelData GetCurrentActiveLevel();
        int GetCurrentScene();
    }
    // UserSessionData ownership and reset lifecycle. Active/prelevel save
    // restoration remains separate until their complete native readers exist.
    public sealed class OriginalUserSessionData : IOriginalLevelLoadSession
    {
        public OriginalPrelevelBoosterSelectionData PrelevelSelection { get; }
        public OriginalActiveLevelData ActiveLevel { get; }
        public OriginalActiveLevelAnimationValues AnimationData { get; }
        public OriginalSessionTagData SessionTagData { get; }
        public float RoyalPassScrollPosition;
        public bool IsGameSceneLoadedOnce { get; private set; }
        public bool IsHomeSceneLoadedMoreThanOnce { get; private set; }
        public int GameSceneLoadCount { get; private set; }
        private readonly IOriginalUserSessionEnvironment environment;
        public OriginalUserSessionData(IOriginalAnimationDataContext animationContext, OriginalSlbMoveConfig slb,
            IOriginalThreeColorLevelNames threeColor, IOriginalUserSessionEnvironment environment) // 0x32bb070.
        {
            this.environment = environment;
            AnimationData = new OriginalActiveLevelAnimationValues(animationContext);
            ActiveLevel = new OriginalActiveLevelData(slb, threeColor, AnimationData);
            PrelevelSelection = new OriginalPrelevelBoosterSelectionData();
            SessionTagData = new OriginalSessionTagData();
            RoyalPassScrollPosition = -1f;
            environment.OnApplicationPause += ResetOnPause; // Native retains this subscription.
        }
        public void Reset() // 0x32bb22c: scene history and tag/source values survive.
        {
            ActiveLevel.Reset();
            PrelevelSelection.Reset();
            AnimationData.Reset();
            ActiveLevel.ResetCumulativeProgress();
            RoyalPassScrollPosition = -1f;
        }
        public void ResetOnSceneChange(int scene) // 0x32bb280.
        {
            RoyalPassScrollPosition = -1f;
            var active = environment.GetCurrentActiveLevel();
            if (active == null) throw new NullReferenceException();
            // Native SceneType.Home is2. The names of the history fields are
            // retained even though both flags are set on this Home transition.
            if (scene != 2 || active.Number == 1) return;
            IsGameSceneLoadedOnce = true;
            IsHomeSceneLoadedMoreThanOnce = true;
            if (GameSceneLoadCount <= 4) GameSceneLoadCount = unchecked(GameSceneLoadCount + 1);
        }
        private void ResetOnPause() // 0x32bb320; ApplicationContext scene-manager unit2 is required.
        { if (environment.GetCurrentScene() != 2) IsGameSceneLoadedOnce = false; }
    }

    // Native SessionTagData constructor0x32b6114 only initializes zero fields.
    // Formatting/telemetry dispatch are outside the core session lifecycle.
    public sealed class OriginalSessionTagData
    {
        public int SentKingsCupTag, SentTeamBattleTag, SentPropellerMadnessTag, SentLadderOfferTag;
        public int SentRoyalPassTag, SentSkyRaceTag, SentSpaceMissionTag, SentPinataPartyTag;
        public long SentTeamTreasureIdTag;
        public int SentTeamOfferTag, SentBalloonRiseTag, SentWeeklyLeaderboardTag, SentMagicCauldronTag;
        public int SentDragonNestTag, SentTrainJourneyTag, SentDailyLightningRushTag, SentArcheryArenaTag;
        public int SentTeamTournamentTag, SentHiddenTempleTag, SentDukesFortuneTag, SentMissionPursuitTag;
        public int SentMissionControlTag, SentLavaQuestIdTag, SentOceanOdysseyIdTag, SentPuzzleBreakIdTag;
        public int SentPropellerRushTag, SentAncientAdventureIdTag, SentRobotRivalsTag, SentMergeSmithIdTag;
        public int SentClashOfChampionsTag, SentWeeklyPassTag, SentRoyalWorldCupTag, SentShortExpeditionEventIdTag, SentWeeklyMissionTag;
    }
}
