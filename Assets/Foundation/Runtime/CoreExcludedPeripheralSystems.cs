using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Explicit product policy requested by the user: all peripheral systems
    // are excluded, even if an original unlock level would make one eligible.
    // These are scope decisions, not missing production owners disguised as
    // inactive events. Board/session/remaining-move logic still runs natively.
    public sealed class CoreExcludedPeripheralSystems : IOriginalSessionWinContext, IOriginalSessionWinRewards,
        IOriginalWinInventory, IOriginalWinSuperBooster, IOriginalWinSuperDuke, IOriginalWinMagicDuke, IOriginalRemainingSessionEvents
    {
        private readonly OriginalActiveLevelData session;
        public int Coin { get; }
        public CoreExcludedPeripheralSystems(OriginalActiveLevelData session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            if (session.Number < 1 || session.Number > 100 || session.IsSideProgressionLevel || session.IsLeague)
                throw new InvalidOperationException("Core-only composition requires a numbered level 1–100.");
            Coin = session.AnimationValues.Coin.Before;
        }
        private static Exception Excluded() => new InvalidOperationException("Peripheral feature is excluded by the requested core-only scope.");
        public IOriginalWinInventory GetInventory() => this;
        public int PersistentLevelNumber => session.Number;
        public int GetNThousandLevelCoin(int level) => throw Excluded();
        public bool IsWaterLevel => false;
        public bool KingsCupIsInGroup => false;
        public bool TeamBattleIsInGroup => false;
        public bool WeeklyPassShouldCollect => false;
        public bool TeamTreasureShouldProgress => false;
        public bool SpaceMissionShouldProgress => false;
        public bool BalloonRiseShouldProgress => false;
        public bool SkyRaceIsContinue => false;
        public bool SkyRaceIsInGroup => false;
        public bool HiddenTempleShouldIncrease => false;
        public bool DragonNestShouldIncrease => false;
        public bool TeamTournamentShouldProgress => false;
        public bool MagicCauldronShouldIncrease => false;
        public bool ArcheryArenaShouldProgress => false;
        public int GetArcheryCurrentMultiplier() => throw Excluded();
        public bool DukesFortuneShouldIncrease => false;
        public bool TrainJourneyShouldIncrease => false;
        public bool OceanOdysseyShouldIncrease => false;
        public bool PuzzleBreakShouldCollect => false;
        public bool RobotRivalsShouldIncrease => false;
        public bool RoyalWorldCupShouldProgress => false;
        public int GetRoyalWorldCupQualifierMultiplier() => throw Excluded();
        public bool MergeSmithShouldCollect => false;
        public bool ShortExpeditionShouldCollect => false;
        public void UpdateCloche(OriginalBeforeAfterData snapshot) { } // Decoration progression removed.
        public IOriginalWinSuperBooster GetSuperBooster() => this;
        public IOriginalWinSuperDuke GetSuperDuke() => this;
        public IOriginalWinMagicDuke GetMagicDuke() => this;
        public bool HasShowedTutorial => false;
        public bool HasShowedTagTutorial => false;
        public bool HasShowedHeaderTutorial => false;
        public int GetUnlockLevel(bool shown) => int.MaxValue; // Product exclusion, not an original unlock claim.
        public bool IsFeatureActive() => false;
        public void IncrementWinCount() => throw Excluded();
        public bool CanUpdateProgress(bool win) => false;
        public bool IsFeatureReady() => false;
        public int MaxProgress => 0;
        public void UpdateProgress(int progress) => throw Excluded();
        public bool IsRoyalPassEventValid() => false;
        public bool IsPinataEventValid() => false;
        public void AddLeagueCrown(sbyte d) => throw Excluded();
        public void AddKingsCup(sbyte d) => throw Excluded();
        public void AddTeamBattle(sbyte d) => throw Excluded();
        public void AddRoyalPass(sbyte d) => throw Excluded();
        public void AddWeeklyPass(sbyte d) => throw Excluded();
        public void AddPinata(sbyte d) => throw Excluded();
        public void AddTeamTreasure(sbyte d) => throw Excluded();
        public void AddSpaceMission() => throw Excluded();
        public void AddBalloonRise(sbyte d) => throw Excluded();
        public void AddSkyRace() => throw Excluded();
        public void AddHiddenTemple(sbyte d) => throw Excluded();
        public void AddDragonNest(sbyte d) => throw Excluded();
        public void AddTeamTournament(sbyte d) => throw Excluded();
        public void AddMagicCauldron(sbyte d) => throw Excluded();
        public void AddArcheryArena(sbyte d,int m) => throw Excluded();
        public void AddDukesFortune(sbyte d) => throw Excluded();
        public void AddTrainJourney(sbyte d) => throw Excluded();
        public void AddOceanOdyssey(sbyte d) => throw Excluded();
        public void AddPuzzleBreak(sbyte d) => throw Excluded();
        public void AddRobotRivals() => throw Excluded();
        public void AddRoyalWorldCup(sbyte d,int m) => throw Excluded();
        public void AddMergeSmith(sbyte d) => throw Excluded();
        public void AddShortExpedition(sbyte d) => throw Excluded();
        // Native RemainingMovesEnd dispatches these unconditionally. They are
        // deliberately discarded by the same explicit no-events product policy.
        public void AddMadnessRemainingMoves(int n,int d) { }
        public void AddLightningRemainingMoves(int n) { }
        public void AddPropellerRush(int n) { }
        public void AddClashRemainingMoves(int n) { }
        public bool IsPlayingWithSuperLightball() => false;
        public void AddClashRemainingSpecials(Dictionary<int,int> items,bool super) { }
        public void AddLightningForLightballsOnBoard() { }
        public bool IsMadnessSpecialIcon() => false;
        public void AddMissionEvents(int type,sbyte d,int n) { }
    }
}
