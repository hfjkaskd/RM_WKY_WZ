using System;
using System.Collections.Generic;
using Whitebox.Foundation;
// Child constructors and global environment are supplied boundaries. Compile the
// actual session, animation reset and prelevel state implementations below.
namespace Whitebox.Foundation
{
    public interface IOriginalAnimationDataContext { }
    public interface IOriginalThreeColorLevelNames { }
    public sealed class OriginalSlbMoveConfig { }
    public interface IOriginalLevelLoadSession { OriginalActiveLevelData ActiveLevel { get; } }
    public class OriginalBeforeAfterData
    {
        public bool ShouldConsume; public int Before, After;
        public void Consume() { ShouldConsume = false; }
    }
    public sealed class OriginalMissionEventsStartData { public bool ShouldConsume; public int CreatedPropellerCount, CreatedRocketCount; }
    public sealed class OriginalInviteFriendsStartData { public bool ShouldConsume; public int EgoPrice; }
    public sealed class OriginalActiveLevelData
    {
        public int State, Number; public bool WonAnyLevel;
        public readonly OriginalActiveLevelAnimationValues AnimationValues;
        public OriginalActiveLevelData() { }
        public OriginalActiveLevelData(OriginalSlbMoveConfig slb,IOriginalThreeColorLevelNames names,OriginalActiveLevelAnimationValues animation)
        { AnimationValues = animation; Verify.Record("active,1"); }
        public void Reset() { State = 0; }
        public void ResetCumulativeProgress() { WonAnyLevel = false; }
    }
    public sealed class ObservedDictionary : Dictionary<int,int>
    {
        private readonly int slot;
        public ObservedDictionary(int slot) { this.slot = slot; }
        public new void Clear() { Verify.Record("clear,"+slot); base.Clear(); }
    }
    public sealed partial class OriginalActiveLevelAnimationValues
    {
        public OriginalBeforeAfterData LeagueCrown = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData KingsCup = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData TeamBattle = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData Coin = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData CompletedNThousandLevelCoin = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData SkyRace = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData TeamTreasure = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData SpaceMission = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData BalloonRise = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData HiddenTemple = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData DragonNest = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData MagicCauldron = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData DailyLightningRush = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData PropellerRush = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData PinataParty = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData Madness = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData ArcheryArena = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData DukesFortune = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData TeamTournament = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData TrainJourney = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData OceanOdyssey = new OriginalBeforeAfterData();
        public OriginalMissionEventsStartData MissionEvents = new OriginalMissionEventsStartData();
        public OriginalBeforeAfterData PuzzleBreak = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData RobotRivals = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData MergeSmith = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData ClashOfChampions = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData RoyalWorldCup = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData ShortExpeditionEvent = new OriginalBeforeAfterData();
        public OriginalInviteFriendsStartData InviteFriends = new OriginalInviteFriendsStartData();
        public OriginalBeforeAfterData DukesTreats = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData RoyalPass = new OriginalBeforeAfterData();
        public OriginalBeforeAfterData WeeklyPass = new OriginalBeforeAfterData();
        public readonly ObservedDictionary Boosters=new ObservedDictionary(0), UnlimitedBoosters=new ObservedDictionary(1), EventItems=new ObservedDictionary(2), Multipliers=new ObservedDictionary(3);
        public bool BoosterShouldConsume, CanPlayLevelWithMultiplier;
        public OriginalActiveLevelAnimationValues(IOriginalAnimationDataContext context) { Verify.Record("animation"); }
    }
}
class Env : IOriginalUserSessionEnvironment
{
    public int Flags = 15, Scene = 2;
    public OriginalActiveLevelData Current = new OriginalActiveLevelData();
    public Action Pause;
    public event Action OnApplicationPause { add { Verify.Record("subscribe"); Pause += value; } remove { Pause -= value; } }
    public OriginalActiveLevelData GetCurrentActiveLevel()
    { Verify.Record("current"); if((Flags&7)!=7) throw new NullReferenceException(); return Current; }
    public int GetCurrentScene()
    { Verify.Record("scene"); if((Flags&8)==0)throw new NullReferenceException(); return Scene; }
}
class Verify
{
    static readonly List<string> trace = new List<string>();
    static int calls, failure;
    public static void Record(string value) { trace.Add(value); if(++calls==failure)throw new InvalidOperationException(); }
    static OriginalUserSessionData session;
    static OriginalActiveLevelAnimationValues a;
    static OriginalPrelevelBoosterSelectionData b;
    static object[] slots;
    static void SetSlot(int i, bool missing)
    {
        object value = missing ? null : slots[i];
        switch(i)
        {
            case 0:a.LeagueCrown=(OriginalBeforeAfterData)value;break;
            case 1:a.KingsCup=(OriginalBeforeAfterData)value;break;
            case 2:a.TeamBattle=(OriginalBeforeAfterData)value;break;
            case 3:a.Coin=(OriginalBeforeAfterData)value;break;
            case 4:a.CompletedNThousandLevelCoin=(OriginalBeforeAfterData)value;break;
            case 5:a.SkyRace=(OriginalBeforeAfterData)value;break;
            case 6:a.TeamTreasure=(OriginalBeforeAfterData)value;break;
            case 7:a.SpaceMission=(OriginalBeforeAfterData)value;break;
            case 8:a.BalloonRise=(OriginalBeforeAfterData)value;break;
            case 9:a.HiddenTemple=(OriginalBeforeAfterData)value;break;
            case 10:a.DragonNest=(OriginalBeforeAfterData)value;break;
            case 11:a.MagicCauldron=(OriginalBeforeAfterData)value;break;
            case 12:a.DailyLightningRush=(OriginalBeforeAfterData)value;break;
            case 13:a.PropellerRush=(OriginalBeforeAfterData)value;break;
            case 14:a.PinataParty=(OriginalBeforeAfterData)value;break;
            case 15:a.Madness=(OriginalBeforeAfterData)value;break;
            case 16:a.ArcheryArena=(OriginalBeforeAfterData)value;break;
            case 17:a.DukesFortune=(OriginalBeforeAfterData)value;break;
            case 18:a.TeamTournament=(OriginalBeforeAfterData)value;break;
            case 19:a.TrainJourney=(OriginalBeforeAfterData)value;break;
            case 20:a.OceanOdyssey=(OriginalBeforeAfterData)value;break;
            case 21:a.MissionEvents=(OriginalMissionEventsStartData)value;break;
            case 22:a.PuzzleBreak=(OriginalBeforeAfterData)value;break;
            case 23:a.RobotRivals=(OriginalBeforeAfterData)value;break;
            case 24:a.MergeSmith=(OriginalBeforeAfterData)value;break;
            case 25:a.ClashOfChampions=(OriginalBeforeAfterData)value;break;
            case 26:a.RoyalWorldCup=(OriginalBeforeAfterData)value;break;
            case 27:a.ShortExpeditionEvent=(OriginalBeforeAfterData)value;break;
            case 28:a.InviteFriends=(OriginalInviteFriendsStartData)value;break;
            case 29:a.DukesTreats=(OriginalBeforeAfterData)value;break;
            case 30:a.RoyalPass=(OriginalBeforeAfterData)value;break;
            case 31:a.WeeklyPass=(OriginalBeforeAfterData)value;break;
        }
    }
    static void Fill()
    {
        for(int i=0;i<slots.Length;i++)
        {
            if(slots[i] is OriginalBeforeAfterData value) { value.ShouldConsume=true;value.Before=-10-i;value.After=3*i; }
            else if(slots[i] is OriginalMissionEventsStartData m) { m.ShouldConsume=true;m.CreatedPropellerCount=-10-i;m.CreatedRocketCount=3*i; }
            else { var invite=(OriginalInviteFriendsStartData)slots[i];invite.ShouldConsume=true;invite.EgoPrice=-10-i; }
        }
        a.Boosters[1]=2;a.UnlimitedBoosters[2]=3;a.EventItems[3]=4;a.Multipliers[4]=5;
        a.BoosterShouldConsume=true;a.CanPlayLevelWithMultiplier=true;
    }
    static int B(bool v)=>v?1:0;
    static string Snapshot()
    {
        ulong consume=0;int hash=0;
        for(int i=0;i<slots.Length;i++)
        {
            bool flag;int left,right;
            if(slots[i] is OriginalBeforeAfterData value) { flag=value.ShouldConsume;left=value.Before;right=value.After; }
            else if(slots[i] is OriginalMissionEventsStartData m) { flag=m.ShouldConsume;left=m.CreatedPropellerCount;right=m.CreatedRocketCount; }
            else { var invite=(OriginalInviteFriendsStartData)slots[i];flag=invite.ShouldConsume;left=invite.EgoPrice;right=0; }
            if(flag)consume|=1UL<<i;hash=unchecked(hash+(i+1)*(left+31*right));
        }
        int bits=B(b.DidTouchRocket)|(B(b.DidTouchTnt)<<1)|(B(b.DidTouchLightball)<<2);
        for(int i=1;i<=3;i++) { bits|=B(b.IsSelected(i))<<(i+2);bits|=B(b.HasTime(i))<<(i+5); }
        // No-possible state is exposed by ShouldUse once selection/time are absent;
        // native and managed comparisons below use all observable getters as well.
        var active=session.ActiveLevel;
        return string.Join(",",new int[]{active.State,B(active.WonAnyLevel),BitConverter.ToInt32(BitConverter.GetBytes(session.RoyalPassScrollPosition),0),B(session.IsGameSceneLoadedOnce),B(session.IsHomeSceneLoadedMoreThanOnce),session.GameSceneLoadCount,active.Number,session.SessionTagData.SentKingsCupTag})+":"+
            string.Join(",",new int[]{bits,B(b.ShouldUseRocket),B(b.ShouldUseTnt),B(b.ShouldUseLightBall),b.LevelRocketBoosterSource,b.LevelTntBoosterSource,b.LevelLightballBoosterSource})+":"+
            consume+","+hash+","+a.Boosters.Count+","+a.UnlimitedBoosters.Count+","+a.EventItems.Count+","+a.Multipliers.Count+","+B(a.BoosterShouldConsume)+","+B(a.CanPlayLevelWithMultiplier);
    }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            trace.Clear();calls=0;failure=0;var env=new Env();session=new OriginalUserSessionData(null,null,null,env);a=session.AnimationData;b=session.PrelevelSelection;
            string constructed=string.Join("/",trace);
            slots=new object[]{a.LeagueCrown,a.KingsCup,a.TeamBattle,a.Coin,a.CompletedNThousandLevelCoin,a.SkyRace,a.TeamTreasure,a.SpaceMission,a.BalloonRise,a.HiddenTemple,a.DragonNest,a.MagicCauldron,a.DailyLightningRush,a.PropellerRush,a.PinataParty,a.Madness,a.ArcheryArena,a.DukesFortune,a.TeamTournament,a.TrainJourney,a.OceanOdyssey,a.MissionEvents,a.PuzzleBreak,a.RobotRivals,a.MergeSmith,a.ClashOfChampions,a.RoyalWorldCup,a.ShortExpeditionEvent,a.InviteFriends,a.DukesTreats,a.RoyalPass,a.WeeklyPass};
            var output=new List<string>();
            foreach(var command in line.Split(';'))
            {
                var f=command.Split(',');string result="-";calls=0;trace.Clear();int i=f.Length>1?int.Parse(f[1]):0;
                try
                {
                    switch(f[0])
                    {
                        case "s":session.ResetOnSceneChange(i);break;case "p":env.Pause();break;case "r":session.Reset();break;
                        case "h":a.Reset();break;case "a":Fill();break;case "m":SetSlot(i,true);break;case "u":SetSlot(i,false);break;
                        case "n":session.ActiveLevel.Number=i;break;case "j":env.Current.Number=i;break;case "c":env.Scene=i;break;case "f":env.Flags=i;break;case "x":failure=i;break;
                        case "d":session.ActiveLevel.State=i;session.ActiveLevel.WonAnyLevel=true;session.RoyalPassScrollPosition=17.25f;session.SessionTagData.SentKingsCupTag=99;break;
                        case "t":b.Toggle(i);break;case "b":b.ToggleForTime(i,int.Parse(f[2])!=0);break;case "v":b.SetNoPossibleMoveFailed(i);break;
                        case "z":b.ResetBooster(i);break;case "e":b.SetLevelBoosterUsages();break;case "l":b.Reset();break;
                        case "k":b.DidTouchRocket=(i&1)!=0;b.DidTouchTnt=(i&2)!=0;b.DidTouchLightball=(i&4)!=0;break;
                        case "o":result=B(b.IsSelected(i)).ToString();break;case "q":result=B(b.HasTime(i)).ToString();break;
                    }
                }
                catch(Exception){result="error";}
                output.Add(result+":"+Snapshot()+":"+string.Join("/",trace));
            }
            Console.WriteLine(constructed+"#"+string.Join("|",output));
        }
    }
}
