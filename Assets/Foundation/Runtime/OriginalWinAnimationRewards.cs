using System;

namespace Whitebox.Foundation
{
    public interface IOriginalWinTeamTreasure{void WheelCollected();}
    public interface IOriginalWinDukesFortune{int StreakMultiplier { get; }}
    public interface IOriginalWinAnimationContext
    {IOriginalWinTeamTreasure GetTeamTreasure();IOriginalWinDukesFortune GetDukesFortune();}
    // Native AnimationData win rewards0x338b348..0x338bc58. The supplied
    // snapshots are the same level-owned objects used by start/save/collect.
    public sealed class OriginalSessionWinRewards:IOriginalSessionWinRewards
    {
        private readonly OriginalActiveLevelAnimationValues animation;
        private readonly IOriginalWinAnimationContext context;
        public OriginalSessionWinRewards(OriginalActiveLevelAnimationValues animation,IOriginalWinAnimationContext context)
        {this.animation=animation??throw new ArgumentNullException(nameof(animation));this.context=context??throw new ArgumentNullException(nameof(context));}
        private OriginalLevelStartSnapshots Data=>animation.StartSnapshots??throw new NullReferenceException();
        private static void Add(OriginalBeforeAfterData data,int amount)
        {if(data==null)throw new NullReferenceException();data.AddExtra(amount);}
        private static int Multiplier(sbyte difficulty)=>OriginalBeforeAfterData.GetMultiplierValueForDifficulty(difficulty);
        public bool IsRoyalPassEventValid()
        {var data=Data.RoyalPass as OriginalRoyalPassLevelData??throw new NullReferenceException();return data.IsEventValid();}
        public bool IsPinataEventValid()
        {var data=Data.PinataParty as OriginalPinataPartyLevelData??throw new NullReferenceException();return data.IsEventValid();}
        public void AddLeagueCrown(sbyte difficulty)=>Add(Data.LeagueCrown,Multiplier(difficulty));
        public void AddKingsCup(sbyte difficulty)=>Add(Data.KingsCup,Multiplier(difficulty));
        public void AddTeamBattle(sbyte difficulty)=>Add(Data.TeamBattle,Multiplier(difficulty));
        public void AddRoyalPass(sbyte difficulty)=>Add(Data.RoyalPass as OriginalBeforeAfterData,Multiplier(difficulty));
        public void AddWeeklyPass(sbyte difficulty)=>Add(Data.WeeklyPass,Multiplier(difficulty));
        public void AddPinata(sbyte difficulty)=>Add(Data.PinataParty as OriginalBeforeAfterData,Multiplier(difficulty));
        public void AddTeamTreasure(sbyte difficulty)
        {Add(Data.TeamTreasure,Multiplier(difficulty));var owner=context.GetTeamTreasure()??throw new NullReferenceException();owner.WheelCollected();}
        public void AddSpaceMission()=>Add(Data.SpaceMission,1);
        public void AddBalloonRise(sbyte difficulty)=>Add(Data.BalloonRise,Multiplier(difficulty));
        public void AddSkyRace()=>Add(Data.SkyRace,1);
        public void AddHiddenTemple(sbyte difficulty)=>Add(Data.HiddenTemple,Multiplier(difficulty));
        public void AddDragonNest(sbyte difficulty)=>Add(Data.DragonNest,Multiplier(difficulty));
        public void AddTeamTournament(sbyte difficulty)=>Add(Data.TeamTournament,Multiplier(difficulty));
        public void AddMagicCauldron(sbyte difficulty)=>Add(Data.MagicCauldron,Multiplier(difficulty));
        public void AddArcheryArena(sbyte difficulty,int multiplier)=>Add(Data.ArcheryArena,unchecked(Multiplier(difficulty)*multiplier));
        public void AddDukesFortune(sbyte difficulty)
        {
            var owner=context.GetDukesFortune()??throw new NullReferenceException();
            var data=Data.DukesFortune;int multiplier=owner.StreakMultiplier;Add(data,unchecked(Multiplier(difficulty)*multiplier));
        }
        public void AddTrainJourney(sbyte difficulty)=>Add(Data.TrainJourney,Multiplier(difficulty));
        public void AddOceanOdyssey(sbyte difficulty)=>Add(Data.OceanOdyssey,Multiplier(difficulty));
        public void AddPuzzleBreak(sbyte difficulty)=>Add(Data.PuzzleBreak,Multiplier(difficulty));
        public void AddRobotRivals()=>Add(Data.RobotRivals,1);
        public void AddRoyalWorldCup(sbyte difficulty,int multiplier)=>Add(Data.RoyalWorldCup,unchecked(Multiplier(difficulty)*multiplier));
        public void AddMergeSmith(sbyte difficulty)=>Add(Data.MergeSmith,Multiplier(difficulty));
        public void AddShortExpedition(sbyte difficulty)=>Add(Data.ShortExpeditionEvent,Multiplier(difficulty));
    }
}
