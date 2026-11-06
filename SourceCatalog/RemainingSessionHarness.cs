using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;

namespace Whitebox.Foundation
{
    // Typed protocol boundaries only. The actual runtime session/end/reward
    // source files and BeforeAfterData arithmetic are compiled unchanged.
    public interface IOriginalMadnessSessionManager{bool ShouldShowIcon();int EventType{get;}}
    public sealed partial class OriginalActiveLevelData
    {
        public OriginalActiveLevelAnimationValues AnimationValues;public sbyte Difficulty;
        private readonly List<int> specialItemIds=new List<int>{2,3,5};
        public int TestItemCount=>specialItemIds.Count;
        public OriginalActiveLevelData(){ExtraCoins=-77;}
    }
    public sealed partial class OriginalActiveLevelAnimationValues
    {public OriginalBeforeAfterData Coin;public OriginalLevelStartSnapshots StartSnapshots;}
    public class OriginalLevelStartSnapshots
    {public object Madness,DailyLightningRush,ClashOfChampions;public OriginalPropellerRushLevelData PropellerRush;public OriginalMissionEventsStartData MissionEvents;}
    public partial class OriginalMadnessSessionData:OriginalBeforeAfterData
    {public bool Valid;public bool IsEventValid(){Harness.Trace.Add("valid");return Valid;}}
    public partial class OriginalDailyLightningRushLevelData
    {protected void IncrementCount(int amount){Harness.Trace.Add("inc,"+amount);}}
    public partial class OriginalClashOfChampionsLevelData
    {
        protected int rocketItemScore,propellerItemScore,tntItemScore,lightballItemScore,remainingSpecialItemsMultiplier;
        public bool Valid;
        public void Configure(int[] values){rocketItemScore=values[0];propellerItemScore=values[1];tntItemScore=values[2];lightballItemScore=values[3];remainingSpecialItemsMultiplier=values[4];}
        protected bool CanIncreaseScore{get{Harness.Trace.Add("valid");return Valid;}}
        protected void IncrementScore(int score){Harness.Trace.Add("score,"+score);}
        public void IncrementScoreForRemainingMoves(int count){throw new InvalidOperationException("Protocol test does not use the production adapter.");}
    }
    public class OriginalPropellerRushLevelData{public void TryIncrementCount(int count){throw new InvalidOperationException("Protocol test does not use the production adapter.");}}
    public sealed class OriginalMissionEventsStartData
    {public void AddExtraForItem(int type,int count){Harness.Trace.Add("mission,"+type+","+count);}}
}
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    private sealed class Events:IOriginalRemainingSessionEvents
    {
        public int Missing;public bool Super,Icon;public Dictionary<int,int> Items;
        private void Check(int stage){if(Missing==stage)throw new NullReferenceException();}
        public void AddMadnessRemainingMoves(int n,int d){Check(1);Trace.Add("madness,"+n+","+d);}
        public void AddLightningRemainingMoves(int n){Check(2);Trace.Add("lightning,"+n);}
        public void AddPropellerRush(int n){Check(3);Trace.Add("propeller,"+n);}
        public void AddClashRemainingMoves(int n){Check(4);Trace.Add("clash,"+n);}
        public bool IsPlayingWithSuperLightball(){Trace.Add("super,"+(Super?1:0));return Super;}
        public void AddClashRemainingSpecials(Dictionary<int,int> items,bool value)
        {if(!ReferenceEquals(items,Items))throw new InvalidOperationException();Trace.Add("specials,"+(value?1:0));}
        public void AddLightningForLightballsOnBoard(){Trace.Add("lightballs");}
        public bool IsMadnessSpecialIcon(){Trace.Add("icon");return Icon;}
        public void AddMissionEvents(int type,sbyte difficulty,int count){Trace.Add("mission,"+type+","+difficulty+","+count);}
    }
    public static void Main()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        string line;while((line=Console.ReadLine())!=null)
        {
            Trace.Clear();var p=line.Split(',');var v=new int[p.Length-1];for(int i=1;i<p.Length;i++)v[i-1]=int.Parse(p[i]);
            bool fault=false;string state="-";
            if(p[0]=="s")
            {
                var coin=new OriginalBeforeAfterData{Before=v[6],After=v[7],ShouldConsume=v[8]!=0};
                var level=new OriginalActiveLevelData{Difficulty=(sbyte)v[2],AnimationValues=v[9]==8?null:new OriginalActiveLevelAnimationValues{Coin=v[9]==9?null:coin}};
                var items=new Dictionary<int,int>{{3,2},{2,1},{4,3},{5,1}};
                var events=new Events{Super=v[3]!=0,Icon=v[4]!=0&&v[5]==3,Missing=v[9],Items=items};
                try{level.RemainingMovesEnd(v[0],v[1],items,events);}catch(NullReferenceException){fault=true;}
                state=coin.After+","+(coin.ShouldConsume?1:0)+","+level.ExtraCoins+","+level.TestItemCount;
            }
            else if(p[0]=="m")
            {
                var data=new OriginalMadnessSessionData{After=v[3],ShouldConsume=v[4]!=0,Valid=v[2]!=0};
                data.TryAddMadnessForRemainingMoves(v[0],v[1]);state=data.After+","+(data.ShouldConsume?1:0);
            }
            else if(p[0]=="d"||p[0]=="l")
            {
                var data=new OriginalDailyLightningRushLevelData();
                if(p[0]=="d")data.AddLightningForRushRemainingMoves(v[0],v[1]!=0);else data.AddLightningForRushLightballsOnBoard(v[0],v[1]!=0);
            }
            else if(p[0]=="a")
            {
                var data=new OriginalActiveLevelAnimationValues{StartSnapshots=new OriginalLevelStartSnapshots{MissionEvents=new OriginalMissionEventsStartData()}};
                data.AddMissionEvents(v[0],(sbyte)v[1],v[2]);
            }
            else if(p[0]=="c")
            {
                var data=new OriginalClashOfChampionsLevelData{Valid=v[0]!=0};data.Configure(new[]{v[2],v[3],v[4],v[5],v[6]});
                Dictionary<int,int> items=v[1]==2?null:new Dictionary<int,int>{{3,v[7]},{2,v[8]},{4,v[9]},{5,v[10]},{99,v[11]}};
                try{data.IncrementScoreForRemainingSpecialItems(items,v[1]==1);}catch(NullReferenceException){fault=true;}
            }
            else throw new InvalidOperationException();
            Console.WriteLine((fault?1:0)+"#"+state+"#"+string.Join(";",Trace));
        }
    }
}
