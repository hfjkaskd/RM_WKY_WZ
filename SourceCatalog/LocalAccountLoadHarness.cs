using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;

public static class LocalAccountLoadHarness
{
    static readonly string[] keys={"SyncId","Level","LeagueLevel","FullLivesTimeInMs","UnlimitedLivesEndTimeInMs","UserData","UserOfferSegment","Coins","Stars","Inbox","Chest","Feature1","Feature2","RET","TET","LET","EventInventory","InGameInventory","PreLevelInventory","RemainingBoosterTimes","RoyalPassProgress","RoyalPassFree","RoyalPassGold","MultiplierEndTimes","LWM","TeamOffer","DynaDiff","Dialogs","KNLWM","KNLW","UserName","TeamId","FacebookId","AppleId","GoogleId","ByteArrayKeyValue12"};
    static readonly List<string> trace=new List<string>();
    static string[] data;static int[] v;static int reads;static long now;
    static OriginalLocalUserData user,other;
    static OriginalBasicLevelProgress basic,replacementBasic;
    static OriginalUserInventoryBalances inventory,replacementInventory;
    static string E(string s)=>s==null?"~":Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    static int B(bool value)=>value?1:0;
    static string Bits(BitArray bits){if(bits==null)return "~";byte[] bytes=new byte[(bits.Length+7)/8];bits.CopyTo(bytes,0);return bits.Length+":"+Convert.ToBase64String(bytes);}
    static string Basic(OriginalBasicLevelProgress b)=>string.Join(",",b.SyncId,b.Level,b.LeagueLevel,b.FullLivesTimeInMs,b.UnlimitedLivesEndTimeInMs,b.UserData,b.UserOfferSegment);
    static string Inventory(OriginalUserInventoryBalances b)
    {
        var p=b.RoyalPass;var t=b.TeamOffer;
        return string.Join(",",b.Coin,b.Star,b.Inbox,b.Chest,b.EventInventory,b.InGameInventory,b.PreLevelInventory,b.RocketEndTime,b.TntEndTime,b.LightBallEndTime,
            b.RemainingBoosterTimes,b.Feature1,b.Feature2,b.MultiplierEndTimes,b.DynamicDifficultyInventory,b.Dialogs,
            p.PassProgress,p.FreeProgress,p.GoldProgress,B(p.IsGold),p.EventId,p.Step,p.Count,p.TutorialState,B(p.NewThemedTutorialSeen),p.PreviousCount,B(p.IsCardCollectionDisabled),B(p.HasNicknameStyle),
            t.ProgressValue,t.EventId,t.AbGroupId,B(t.HasGold),B(t.HasSilver),B(t.HasBronze),E(b.UnlimitedBoosterInventoryString),Bits(b.UnlimitedBoosterInventory));
    }
    sealed class Context:IOriginalLocalAccountLoadContext
    {
        public OriginalLocalUserData Current;
        public OriginalLocalUserData GetCurrentUser()=>Current;
        public long CurrentTimeInMilliseconds{get{trace.Add("now");if(v[0]==8)throw new InvalidOperationException();return now;}}
        public void ParseAndSetAreaFromLocalStorage(){trace.Add("area");if(v[0]==7)throw new InvalidOperationException();}
    }
    sealed class Connection:IOriginalKeyValueConnection
    {
        readonly Context context;public Connection(Context context){this.context=context;}
        public OriginalKeyValue Find(string key)
        {
            trace.Add("get,"+key);reads++;
            if(reads==v[1])throw new InvalidOperationException();
            if(reads==v[2])switch(v[3])
            {
                case 1:user.Basic=replacementBasic;break;case 2:user.Inventory=replacementInventory;break;
                case 3:user.Basic=null;break;case 4:user.Inventory=null;break;case 5:context.Current=other;break;
            }
            int index=Array.IndexOf(keys,key);if(index<0)throw new InvalidOperationException("Unexpected key: "+key);
            return new OriginalKeyValue{Key=key,Value=data[index]};
        }
        public int InsertOrReplace(OriginalKeyValue row)=>throw new InvalidOperationException("Load must not write");
        public int Delete(string key)=>throw new InvalidOperationException("Load must not delete");
        public int UpdateSynced(string key,int value)=>throw new InvalidOperationException("Load must not sync");
        public int UpdateSyncBlocked(string key,int value)=>throw new InvalidOperationException("Load must not sync");
        public void BeginTransaction()=>throw new InvalidOperationException("No load transaction call");
        public void Commit()=>throw new InvalidOperationException("No load commit call");
        public void Dispose(){}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            string[] p=line.Split('|');v=Array.ConvertAll(p[0].Split(','),int.Parse);now=long.Parse(p[1]);
            data=Array.ConvertAll(p[2].Split(','),s=>s=="~"?null:Encoding.UTF8.GetString(Convert.FromBase64String(s)));
            basic=new OriginalBasicLevelProgress(22,33,null){SyncId=11,FullLivesTimeInMs=44,UnlimitedLivesEndTimeInMs=55,UserData=66,UserOfferSegment=77};
            inventory=new OriginalUserInventoryBalances(44,55,66,null);replacementBasic=new OriginalBasicLevelProgress(72,73,null);replacementInventory=new OriginalUserInventoryBalances(84,85,86,null);
            var identity=new OriginalLocalUserIdentity{Name="old",Team=13,FacebookId=14,AppleId="old",GoogleId="old"};
            var log=new OriginalLocalLogData{WinMoves="old"};var story=new OriginalLocalStoryData{NightmareWinMoves="old",TableGameWinMoves="old"};
            user=new OriginalLocalUserData{UserId=identity,Basic=basic,Inventory=inventory,Log=log,Story=story};
            other=new OriginalLocalUserData();
            if(v[0]==2)user.UserId=null;if(v[0]==3)user.Basic=null;if(v[0]==4)user.Inventory=null;if(v[0]==5)user.Log=null;if(v[0]==6)user.Story=null;
            var context=new Context{Current=v[0]==1?null:user};
            var db=new OriginalDatabaseService(new Connection(context),61,()=>{});var values=new OriginalUserKeyValue(()=>db);
            trace.Clear();reads=0;bool fault=false;
            try{OriginalLocalAccountLoad.UpdateDataFromLocalStorage(context,values);}catch{fault=true;}
            Console.WriteLine(B(fault)+"#"+(context.Current==null?0:context.Current==user?1:2)+","+(user.Basic==null?0:user.Basic==basic?1:2)+","+(user.Inventory==null?0:user.Inventory==inventory?1:2)+","+B(db.ShouldSave)+"#"+
                Basic(basic)+"#"+Basic(replacementBasic)+"#"+Inventory(inventory)+"#"+Inventory(replacementInventory)+"#"+
                string.Join(",",E(identity.Name),identity.Team,identity.FacebookId,E(identity.AppleId),E(identity.GoogleId),E(log.WinMoves),E(story.NightmareWinMoves),E(story.TableGameWinMoves))+"#"+string.Join(";",trace));
        }
    }
}
