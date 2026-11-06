using System;

namespace Whitebox.Foundation
{
    public interface IOriginalWinInventory{int Coin { get; }}
    public interface IOriginalWinSuperBooster
    {
        bool HasShowedTutorial { get; }
        bool HasShowedTagTutorial { get; }
        bool HasShowedHeaderTutorial { get; }
        int GetUnlockLevel(bool hasShowedTutorial);
        bool IsFeatureActive();
        void IncrementWinCount();
    }
    public interface IOriginalWinSuperDuke
    {bool CanUpdateProgress(bool isWin);void IncrementWinCount();}
    public interface IOriginalWinMagicDuke
    {bool IsFeatureReady();int MaxProgress { get; }void UpdateProgress(int progress);}
    public interface IOriginalSessionWinContext
    {
        IOriginalWinInventory GetInventory();
        int PersistentLevelNumber { get; }
        int GetNThousandLevelCoin(int level);
        bool IsWaterLevel { get; }
        bool KingsCupIsInGroup { get; }
        bool TeamBattleIsInGroup { get; }
        bool WeeklyPassShouldCollect { get; }
        bool TeamTreasureShouldProgress { get; }
        bool SpaceMissionShouldProgress { get; }
        bool BalloonRiseShouldProgress { get; }
        bool SkyRaceIsContinue { get; }
        bool SkyRaceIsInGroup { get; }
        bool HiddenTempleShouldIncrease { get; }
        bool DragonNestShouldIncrease { get; }
        bool TeamTournamentShouldProgress { get; }
        bool MagicCauldronShouldIncrease { get; }
        bool ArcheryArenaShouldProgress { get; }
        int GetArcheryCurrentMultiplier();
        bool DukesFortuneShouldIncrease { get; }
        bool TrainJourneyShouldIncrease { get; }
        bool OceanOdysseyShouldIncrease { get; }
        bool PuzzleBreakShouldCollect { get; }
        bool RobotRivalsShouldIncrease { get; }
        bool RoyalWorldCupShouldProgress { get; }
        int GetRoyalWorldCupQualifierMultiplier();
        bool MergeSmithShouldCollect { get; }
        bool ShortExpeditionShouldCollect { get; }
        void UpdateCloche(OriginalBeforeAfterData snapshot);
        IOriginalWinSuperBooster GetSuperBooster();
        IOriginalWinSuperDuke GetSuperDuke();
        IOriginalWinMagicDuke GetMagicDuke();
    }
    public interface IOriginalSessionWinRewards
    {
        bool IsRoyalPassEventValid();bool IsPinataEventValid();
        void AddLeagueCrown(sbyte difficulty);void AddKingsCup(sbyte difficulty);void AddTeamBattle(sbyte difficulty);
        void AddRoyalPass(sbyte difficulty);void AddWeeklyPass(sbyte difficulty);void AddPinata(sbyte difficulty);
        void AddTeamTreasure(sbyte difficulty);void AddSpaceMission();void AddBalloonRise(sbyte difficulty);void AddSkyRace();
        void AddHiddenTemple(sbyte difficulty);void AddDragonNest(sbyte difficulty);void AddTeamTournament(sbyte difficulty);
        void AddMagicCauldron(sbyte difficulty);void AddArcheryArena(sbyte difficulty,int multiplier);void AddDukesFortune(sbyte difficulty);
        void AddTrainJourney(sbyte difficulty);void AddOceanOdyssey(sbyte difficulty);void AddPuzzleBreak(sbyte difficulty);
        void AddRobotRivals();void AddRoyalWorldCup(sbyte difficulty,int multiplier);void AddMergeSmith(sbyte difficulty);void AddShortExpedition(sbyte difficulty);
    }
    public sealed partial class OriginalActiveLevelData
    {
        // UserActiveLevelData.Win0x32b8920. Required account/event/feature
        // owners stay typed dependencies; only native diagnostic formatting
        // and Log.Debug are omitted. No fabricated inactive runtime owners.
        public void Win(OriginalMoveManager moves,int coinGoal,IOriginalSessionWinContext context,IOriginalSessionWinRewards rewards)
        {
            if(moves==null)throw new NullReferenceException();
            State=3;LeftMoves=moves.LeftMoves;MadeMoves=moves.MadeMoves;WonAnyLevel=true;
            if(IsAdmin)return;
            var animation=AnimationValues??throw new NullReferenceException();
            var coin=animation.Coin;
            var inventory=context.GetInventory()??throw new NullReferenceException();
            if(coin==null)throw new NullReferenceException();
            coin.Init(inventory.Coin);coin.AddExtra(coinGoal);
            if(StoryLevel>=0&&!IsTableGame&&LeagueIndex<=0&&Number>=1&&Number%1000==0)
            {
                int amount=context.GetNThousandLevelCoin(Number);
                var milestone=animation.CompletedNThousandLevelCoin??throw new NullReferenceException();milestone.AddExtra(amount);
            }
            // Native story/non-water branch precedes the table-game branch.
            if(StoryLevel<0&&!context.IsWaterLevel){LeftMoves=moves.Seconds;ExtraCoins=0;}
            else if(IsTableGame){LeftMoves=0;ExtraCoins=0;}
            if(LeagueIndex>=1)rewards.AddLeagueCrown(Difficulty);
            else if(StoryLevel>=0&&!IsTableGame)
            {var star=Star??throw new NullReferenceException();star.AddExtra(1);}
            if(context.KingsCupIsInGroup)rewards.AddKingsCup(Difficulty);
            if(context.TeamBattleIsInGroup)rewards.AddTeamBattle(Difficulty);
            if(rewards.IsRoyalPassEventValid())rewards.AddRoyalPass(Difficulty);
            if(context.WeeklyPassShouldCollect)rewards.AddWeeklyPass(Difficulty);
            if(rewards.IsPinataEventValid())rewards.AddPinata(Difficulty);
            if(context.TeamTreasureShouldProgress)rewards.AddTeamTreasure(Difficulty);
            if(context.SpaceMissionShouldProgress)rewards.AddSpaceMission();
            if(context.BalloonRiseShouldProgress)rewards.AddBalloonRise(Difficulty);
            if(context.SkyRaceIsContinue&&context.SkyRaceIsInGroup)rewards.AddSkyRace();
            if(context.HiddenTempleShouldIncrease)rewards.AddHiddenTemple(Difficulty);
            if(context.DragonNestShouldIncrease)rewards.AddDragonNest(Difficulty);
            if(context.TeamTournamentShouldProgress)rewards.AddTeamTournament(Difficulty);
            if(context.MagicCauldronShouldIncrease)rewards.AddMagicCauldron(Difficulty);
            if(context.ArcheryArenaShouldProgress)
            {int multiplier=context.GetArcheryCurrentMultiplier();rewards.AddArcheryArena(Difficulty,multiplier);}
            if(context.DukesFortuneShouldIncrease)rewards.AddDukesFortune(Difficulty);
            if(context.TrainJourneyShouldIncrease)rewards.AddTrainJourney(Difficulty);
            if(context.OceanOdysseyShouldIncrease)rewards.AddOceanOdyssey(Difficulty);
            if(context.PuzzleBreakShouldCollect)rewards.AddPuzzleBreak(Difficulty);
            if(context.RobotRivalsShouldIncrease)rewards.AddRobotRivals();
            if(context.RoyalWorldCupShouldProgress)
            {int multiplier=context.GetRoyalWorldCupQualifierMultiplier();rewards.AddRoyalWorldCup(Difficulty,multiplier);}
            if(context.MergeSmithShouldCollect)rewards.AddMergeSmith(Difficulty);
            if(context.ShortExpeditionShouldCollect)rewards.AddShortExpedition(Difficulty);
            context.UpdateCloche(Cloche);
            var booster=context.GetSuperBooster()??throw new NullReferenceException();
            bool boosterEligible=LeagueIndex>0;
            if(!boosterEligible)
            {
                int number=Number;
                bool shown=booster.HasShowedTutorial;
                boosterEligible=number>=booster.GetUnlockLevel(shown);
                if(!boosterEligible&&StoryLevel<0)
                {
                    number=context.PersistentLevelNumber;shown=booster.HasShowedTutorial;
                    boosterEligible=number>=booster.GetUnlockLevel(shown);
                }
            }
            if(boosterEligible&&booster.IsFeatureActive()&&
                (booster.HasShowedTutorial||booster.HasShowedTagTutorial||booster.HasShowedHeaderTutorial))
            {
                var data=SuperBooster??throw new NullReferenceException();data.AddExtra(1);booster.IncrementWinCount();
            }
            var duke=context.GetSuperDuke();
            if(LeagueIndex>0||Number>=402) // Native literal0x192, independent of story/table flags.
            {
                if(duke==null)throw new NullReferenceException();
                if(duke.CanUpdateProgress(true)){var data=SuperDuke??throw new NullReferenceException();data.AddExtra(1);duke.IncrementWinCount();}
            }
            var magic=context.GetMagicDuke()??throw new NullReferenceException();
            if(magic.IsFeatureReady())
            {
                var data=MagicDuke??throw new NullReferenceException();
                int before=data.Before;
                if(before<magic.MaxProgress)data.AddExtra(1);
                magic.UpdateProgress(data.After);
            }
        }
    }
}
