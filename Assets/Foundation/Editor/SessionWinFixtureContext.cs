using System;

namespace Whitebox.Foundation.Editor
{
    // Explicit first-board account/event/feature inputs for integration checks.
    // Runtime Win/reward code is unchanged; this is not a production provider.
    internal sealed class SessionWinFixtureContext:IOriginalSessionWinContext,IOriginalWinAnimationContext,
        IOriginalWinSuperBooster,IOriginalWinMagicDuke,IOriginalWinInventory,
        IOriginalRoyalPassSessionManager,IOriginalPinataSessionManager,IOriginalSessionAbValues
    {
        private readonly OriginalPlayableBoard game;
        public IOriginalWinInventory Inventory { get; set; }
        public int ClocheCalls { get; private set; }
        public SessionWinFixtureContext(OriginalPlayableBoard game)
        {
            this.game=game;
            Inventory=this;
            var data=game.Session.AnimationValues.StartSnapshots;
            var royal=new OriginalRoyalPassLevelData(()=>this);royal.Init(false);data.RoyalPass=royal;
            var pinata=new OriginalPinataPartyLevelData(()=>this,()=>this);pinata.Init(false);data.PinataParty=pinata;
        }
        public IOriginalWinInventory GetInventory()=>Inventory;
        public int Coin=>((IOriginalActiveLevelResetContext)game).GetInventory().Coin;
        public int PersistentLevelNumber=>game.Session.Number;
        public int GetNThousandLevelCoin(int level)=>throw new InvalidOperationException("First-board fixture must not request a milestone package.");
        public bool IsWaterLevel=>false;
        public bool KingsCupIsInGroup=>false;public bool TeamBattleIsInGroup=>false;public bool WeeklyPassShouldCollect=>false;
        public bool TeamTreasureShouldProgress=>false;public bool SpaceMissionShouldProgress=>false;public bool BalloonRiseShouldProgress=>false;
        public bool SkyRaceIsContinue=>false;public bool SkyRaceIsInGroup=>throw new InvalidOperationException("Inactive SkyRace must short-circuit.");
        public bool HiddenTempleShouldIncrease=>false;public bool DragonNestShouldIncrease=>false;public bool TeamTournamentShouldProgress=>false;
        public bool MagicCauldronShouldIncrease=>false;public bool ArcheryArenaShouldProgress=>false;
        public int GetArcheryCurrentMultiplier()=>throw new InvalidOperationException("Locked fixture has no archery multiplier.");
        public bool DukesFortuneShouldIncrease=>false;public bool TrainJourneyShouldIncrease=>false;public bool OceanOdysseyShouldIncrease=>false;
        public bool PuzzleBreakShouldCollect=>false;public bool RobotRivalsShouldIncrease=>false;public bool RoyalWorldCupShouldProgress=>false;
        public int GetRoyalWorldCupQualifierMultiplier()=>throw new InvalidOperationException("Locked fixture has no world-cup multiplier.");
        public bool MergeSmithShouldCollect=>false;public bool ShortExpeditionShouldCollect=>false;
        public void UpdateCloche(OriginalBeforeAfterData data)
        {if(!ReferenceEquals(data,game.Session.Cloche))throw new InvalidOperationException("Cloche owner receives the actual shared snapshot.");ClocheCalls++;}
        public IOriginalWinSuperBooster GetSuperBooster()=>this;
        public bool HasShowedTutorial=>false;public bool HasShowedTagTutorial=>false;public bool HasShowedHeaderTutorial=>false;
        // Explicit default-AB inputs, native0x222d88c: shown92, default292.
        public int GetUnlockLevel(bool shown)=>shown?92:292;
        public bool IsFeatureActive()=>throw new InvalidOperationException("Level1 must not query locked super-booster activity.");
        public void IncrementWinCount()=>throw new InvalidOperationException("Locked fixture must not increment a feature win counter.");
        public IOriginalWinSuperDuke GetSuperDuke()=>null; // Native accepts an absent owner below402 outside league.
        public IOriginalWinMagicDuke GetMagicDuke()=>this;
        public bool IsFeatureReady()=>false;
        public int MaxProgress=>throw new InvalidOperationException("Locked MagicDuke must not query its maximum.");
        public void UpdateProgress(int progress)=>throw new InvalidOperationException("Locked fixture must not progress MagicDuke.");
        public IOriginalWinTeamTreasure GetTeamTreasure()=>throw new InvalidOperationException("Locked fixture must not collect a wheel.");
        public IOriginalWinDukesFortune GetDukesFortune()=>throw new InvalidOperationException("Locked fixture must not query a streak.");
        public int EventId=>-1;public bool IsEventActive=>false;public bool IsEventPlayable=>false;
        public int GetVariant(int id)=>0;
        public bool ShouldShowFirstTimeTutorial()=>throw new InvalidOperationException("Locked event must not query tutorial state.");
        public bool IsSafeFull()=>throw new InvalidOperationException("Locked event must not query capacity.");
        public bool CanClaimFinalStep()=>throw new InvalidOperationException("Locked event must not query a claim.");
        public int GetCount()=>throw new InvalidOperationException("Locked event must not query progress.");
    }
}
