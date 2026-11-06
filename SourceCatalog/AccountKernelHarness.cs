using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public interface IOriginalWinInventory { int Coin { get; } }
    public class OriginalActiveLevelMoveData { public int Number,MoveCount,OriginalMoveCount; }
    public class OriginalSlbMoveConfig {public bool TryGetLevelNameOverride(int level,out int version,out string ext){version=0;ext=null;return false;}}
    public sealed partial class OriginalActiveLevelData
    {public OriginalActiveLevelData(OriginalSlbMoveConfig s,IOriginalThreeColorLevelNames c,object animation){slb=s;threeColor=c;}}
}
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    public class Context:IOriginalBasicLevelProgressContext,IOriginalUserBalanceContext
    {
        public long[] V;public int KingsReads;public int Missing=>(int)V[15];
        private sealed class Timed:IOriginalProgressTimedEvent
        {
            public Context Owner;public bool Kings;
            public bool IsRemainingTimeFinished{get{int value=(int)Owner.V[Kings?(Owner.KingsReads++==0?10:11):12];Trace.Add((Kings?"kings,":"battle,")+value);return value!=0;}}
        }
        private sealed class Progress:IOriginalProgressEvent
        {public Context Owner;public bool Treasure;public bool ShouldProgress{get{int value=(int)Owner.V[Treasure?13:14];Trace.Add((Treasure?"treasure,":"weekly,")+value);return value!=0;}}}
        public IOriginalProgressTimedEvent GetKingsCup(){Trace.Add("get,7");return Missing==2?null:new Timed{Owner=this,Kings=true};}
        public IOriginalProgressTimedEvent GetTeamBattle(){Trace.Add("get,8");return Missing==3?null:new Timed{Owner=this};}
        public IOriginalProgressEvent GetTeamTreasure(){Trace.Add("get,16");return Missing==4?null:new Progress{Owner=this,Treasure=true};}
        public IOriginalProgressEvent GetWeeklyLeaderboard(){Trace.Add("get,28");return Missing==5?null:new Progress{Owner=this};}
        public int MaxLevelCount=>(int)V[3];
        public virtual void MarkCurrentSideProgressionLevelPlayed(){if(Missing==6||Missing==7)throw new NullReferenceException();Trace.Add("mark-side");}
        public bool CanMultiplyInLevel(bool after){Trace.Add("can-multiply");return V[8]!=0;}
        public int GetActiveAmountMultiplier(){Trace.Add("multiplier");return (int)V[9];}
        public void ClearWinProgressFlag(){Trace.Add("clear-win");}
        public void ClearOtherLevelProgressFlag(){Trace.Add("clear-other");}
        public void WriteLevel(int value){Trace.Add("level,"+value);if(Missing==9)throw new NullReferenceException();}
        public void WriteLeagueLevel(int value){Trace.Add("league,"+value);if(Missing==10)throw new NullReferenceException();}
        public void TriggerUpdateLevel(int value){Trace.Add("level-changed,"+value);}
        public void UpdateStoryLevel(){Trace.Add("story");}
        private void Leaderboard(string name,int amount,bool kings,bool battle,bool treasure,bool weekly){Trace.Add(name+","+amount+","+(kings?1:0)+","+(battle?1:0)+","+(treasure?1:0)+","+(weekly?1:0));}
        public void IncrementMyLevelWithEvents(int amount,bool a,bool b,bool c,bool d)=>Leaderboard("my-level",amount,a,b,c,d);
        public void IncrementMyLeagueScoreWithEvents(int amount,bool a,bool b,bool c,bool d)=>Leaderboard("league-score",amount,a,b,c,d);
        public void SetMissionProgress(int type,int amount){Trace.Add("mission,"+type+","+amount);}
        public virtual void OnCrownsWon(int amount){if(Missing==8)throw new NullReferenceException();Trace.Add("crowns,"+amount);}
        public void WriteCoin(long value){Trace.Add("coin,"+value);if(Missing==11)throw new NullReferenceException();}
        public void WriteStar(int value){Trace.Add("star,"+value);if(Missing==12)throw new NullReferenceException();}
        public void WriteEventInventory(long value){Trace.Add("event,"+value);if(Missing==13)throw new NullReferenceException();}
        public void TrackCoinTransaction(int amount){Trace.Add("transaction,"+amount);}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            long[] v=Array.ConvertAll(line.Split(','),long.Parse);Trace.Clear();var ctx=new Context{V=v};
            var basic=new OriginalBasicLevelProgress((int)v[1],(int)v[2],ctx);
            var balance=new OriginalUserInventoryBalances((int)v[16],(int)v[17],v[18],ctx);
            balance.OnCoinUpdated+=()=>Trace.Add("coin-updated");balance.OnStarsUpdated+=()=>Trace.Add("stars-updated");
            long packed=0;bool fault=false;
            try
            {
                if(v[0]==0)basic.IncrementLevel(v[15]==1?null:new OriginalActiveLevelData(new OriginalSlbMoveConfig(),null){StoryLevel=(int)v[4],IsTableGame=v[5]!=0,LeagueIndex=(int)v[6],Difficulty=(sbyte)v[7]});
                else if(v[0]==1)balance.UpdateCoins((int)v[19]);else if(v[0]==2)balance.UpdateStars((int)v[19]);
                else if(v[0]==3)balance.AddCoins((int)v[19],v[20]!=0);
                else if(v[0]==4)packed=new OriginalMadnessInventoryProgress{Step=(int)v[21],Count=(int)v[22],EventId=(int)v[23]}.UpdateLong(v[18]);
                else if(v[0]==5)balance.UpdateMadness(new OriginalMadnessInventoryProgress{Step=(int)v[21],Count=(int)v[22],EventId=(int)v[23]});
                else {var data=balance.GetMadness();Trace.Add("madness,"+data.Step+","+data.Count+","+data.EventId);}
            }catch(NullReferenceException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+basic.Level+","+basic.LeagueLevel+","+balance.Coin+","+balance.Star+","+balance.EventInventory+","+packed+"#"+string.Join(";",Trace));
        }
    }
}
