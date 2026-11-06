using System;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public sealed class OriginalActiveLevelAnimationValues
    {
        public OriginalBeforeAfterData Coin,CompletedNThousandLevelCoin;
        public OriginalUserEndSnapshots StartSnapshots=new OriginalUserEndSnapshots();
    }
    public sealed class OriginalUserEndSnapshots { public object Madness; }
    public sealed class OriginalMadnessSessionData
    {public int After;public bool Valid;public bool IsEventValid(){Harness.Trace.Add("madness-valid");return Valid;}}
    public sealed partial class OriginalActiveLevelData
    {public OriginalBeforeAfterData Star,Cloche;public OriginalActiveLevelAnimationValues AnimationValues;public int ExtraCoins;public bool IsUsefulMoveMade;}
}
public static class UserEndHarness
{
    private sealed class Context:Harness.Context,IOriginalUserLevelEndContext,IOriginalClocheWriter,IOriginalStoryLevelProgress,IOriginalLevelRemainingEndContext
    {
        public OriginalUserEndState User;public OriginalBasicLevelProgress Basic;public OriginalUserInventoryBalances Balance;
        private bool currentMissing;
        public int M=>(int)V[35];
        private static void Add(string text)=>Harness.Trace.Add(text);
        public void Build()
        {
            Basic=new OriginalBasicLevelProgress((int)V[1],(int)V[2],this);
            Balance=new OriginalUserInventoryBalances((int)V[16],(int)V[17],V[18],this);
            Balance.OnCoinUpdated+=()=>Add("coin-updated");Balance.OnStarsUpdated+=()=>Add("stars-updated");
            var active=new OriginalActiveLevelData(new OriginalSlbMoveConfig(),null){Number=(int)V[1],StoryLevel=(int)V[4],IsTableGame=V[5]!=0,LeagueIndex=(int)V[6],Difficulty=(sbyte)V[7],State=(int)V[24],ExtraCoins=(int)V[30],IsAdmin=V[36]!=0,
                Star=M==11?null:new OriginalBeforeAfterData{Before=(int)V[17],After=(int)V[27]},Cloche=M==31?null:new OriginalBeforeAfterData{After=2}};
            if(M!=9)active.AnimationValues=new OriginalActiveLevelAnimationValues{
                Coin=M==10?null:new OriginalBeforeAfterData{Before=(int)V[25],After=(int)V[26]},
                CompletedNThousandLevelCoin=M==12?null:new OriginalBeforeAfterData{Before=(int)V[28],After=(int)V[29]},
                StartSnapshots=new OriginalUserEndSnapshots{Madness=M==20?null:new OriginalMadnessSessionData{Valid=V[31]!=0,After=(int)V[32]}}};
            User=new OriginalUserEndState{Basic=M==3?null:Basic,Inventory=M==4||Missing==7?null:Balance,ActiveLevel=M==2?null:active,StoryProgress=M==30?null:this};
        }
        public OriginalUserEndState GetCurrentUser(){if(M==32)throw new NullReferenceException();return currentMissing||M==1||Missing==6?null:User;}
        public void GetEventsInfoOnLevelEnd()=>Add("event-info");
        public void ClearLevelSaveFile(){if(M==5)throw new NullReferenceException();Add("clear-save");}
        public IOriginalClocheWriter GetClocheWriter()=>M==6?null:this;
        public void SetCloche(int value)=>Add("cloche,"+value);
        private sealed class Feature:IOriginalLevelEndFeature
        {public string Name;public void OnLevelEnd()=>Add(Name);}
        public IOriginalLevelEndFeature GetSuperDuke()=>M==7?null:new Feature{Name="duke"};
        public IOriginalLevelEndFeature GetMultiplierToken()=>M==8?null:new Feature{Name="token"};
        public void EpisodeOnLevelSuccess(){if(M==13)throw new NullReferenceException();Add("episode-win");}
        public void NewUserUxOnLevelSuccess(OriginalActiveLevelData active){if(M==14)throw new NullReferenceException();Add("new-ux");}
        public int GetWinMissionProgressFlag(){Add("win-flag");return (int)V[33];}
        public bool IsMagicLanternTutorialCompleted{get{if(M==28)throw new NullReferenceException();Add("lantern-check");return V[34]!=0;}}
        public void SetMagicLanternTutorialAsSeen(bool value)=>Add("lantern-seen,"+(value?1:0));
        public void OnNightmareLevelWin(){Add("nightmare-win");if(M==34)currentMissing=true;}
        public void AddLevelWin(int level)=>Add("story-win,"+level);
        public void OnSideProgressionLevelWin()=>Add("side-win");
        public void EpisodeOnLevelFail(){if(M==13)throw new NullReferenceException();Add("episode-fail");}
        public void FailedSideProgressionLevel(){if(M==21)throw new NullReferenceException();Add("side-fail");}
        public void QuitDetectionLevelEnded()=>Add("quit-detection");
        public void FailedLevel(){if(M==21)throw new NullReferenceException();Add("fail");}
        public void OnStatsLevelFail(){if(M==19||Missing==8)throw new NullReferenceException();Add("stats-fail");}
        public void OnStatsLevelWin(){if(M==19||Missing==8)throw new NullReferenceException();Add("stats-win");}
        private sealed class Package:IOriginalLevelMilestonePackage{}
        public IOriginalLevelMilestonePackage GetNThousandLevelPackage(int level){Add("milestone,"+level);return new Package();}
        public void AddInventoryPackage(IOriginalLevelMilestonePackage package,bool paid)=>Add("package,"+(paid?1:0));
        public void NThousandRewardClaim(IOriginalLevelMilestonePackage package)=>Add("milestone-claim");
        public void MultiplierTokenPurchaseOnLevelWin(){if(M==8||M==29)throw new NullReferenceException();Add("token-purchase");}
        public void UpdateEventsOnLevelWin()=>Add("event-win");
        public void SendDataToBackend(bool force)=>Add("backend,"+(force?1:0));
        public void TryMultiplyValues(bool after)=>Add("try-multiply,"+(after?1:0));
        public void EventsRemainingMovesEnd(OriginalActiveLevelData active){if(M==21)throw new NullReferenceException();Add("event-remaining");}
        public void MissionEventsRemainingMovesEnd(){if(M==22)throw new NullReferenceException();Add("mission-remaining");}
        public override void MarkCurrentSideProgressionLevelPlayed(){if(M==4)throw new NullReferenceException();base.MarkCurrentSideProgressionLevelPlayed();}
        public override void OnCrownsWon(int amount){if(M==19)throw new NullReferenceException();base.OnCrownsWon(amount);}
        public OriginalActiveLevelData GetCurrentSession(){var user=GetCurrentUser()??throw new NullReferenceException();return user.ActiveLevel;}
        public void UserRemainingMovesEnd()=>OriginalUserLevelEnd.RemainingMovesEnd(this);
        public void SendLevelEndRewardAnalytics(object data){if(M==33)throw new NullReferenceException();Add("reward,"+(data==null?0:1));}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            long[] v=Array.ConvertAll(line.Split(','),long.Parse);Harness.Trace.Clear();var ctx=new Context{V=v};ctx.Build();bool fault=false;
            try
            {
                if(v[37]==0)OriginalUserLevelEnd.LevelEnd(ctx);
                else if(v[37]==1)OriginalUserLevelEnd.RemainingMovesEnd(ctx);
                else OriginalLevelEnd.RemainingMovesEnd(ctx,v[38]==0?null:new object());
            }catch(NullReferenceException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+ctx.Basic.Level+","+ctx.Basic.LeagueLevel+","+ctx.Balance.Coin+","+ctx.Balance.Star+","+ctx.Balance.EventInventory+"#"+string.Join(";",Harness.Trace));
        }
    }
}
