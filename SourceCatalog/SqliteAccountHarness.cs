using System;
using System.Globalization;
using System.IO;
using Whitebox.Foundation;

public static class SqliteAccountHarness
{
    static int checks;
    static void Require(bool value,string message){checks++;if(!value)throw new InvalidOperationException(message);}
    public static void Main(string[] args)
    {
        string file=args[0];int cleaned=0,resolves=0;
        using(var sql=new OriginalWindowsSqliteConnection(file))
        {
            var db=new OriginalDatabaseService(sql,61,()=>cleaned++);
            var kv=new OriginalUserKeyValue(()=>{resolves++;return db;});var account=new OriginalAccountStorage(kv);
            Require(!kv.HasKey("missing")&&kv.GetInt("missing",37)==37&&kv.GetLong("missing",long.MinValue)==long.MinValue,"Missing defaults");
            kv.SetString("nil",null);Require(kv.HasKey("nil")&&kv.GetString("nil","fallback")=="fallback","Existing null value is not missing key");
            kv.SetString("empty","");Require(kv.GetString("empty","fallback")=="","Empty value survives");
            kv.SetString("中文'🛡","王冠🚀");Require(kv.GetString("中文'🛡",null)=="王冠🚀","UTF16 and parameterized quote key");
            kv.SetString("nul","before\0after");Require(kv.GetString("nul",null)=="before","Native NUL-terminated binding");
            bool threw=false;try{kv.SetString(null,"invalid");}catch(InvalidOperationException){threw=true;}
            Require(threw&&db.ShouldSave,"Original NOT NULL key constraint and dirty-before-failure");
            Require(!kv.DeleteKey("absent")&&db.ShouldSave,"Missing delete still dirties service");
            var local=sql.Find("empty");Require(local.Synced==0&&local.SyncBlocked==1,"Local sync flags");
            Require(OriginalKeyValueTable.SetSynced(db,"empty",true),"Mark synced");
            local=sql.Find("empty");Require(local.Synced==1&&local.SyncBlocked==0,"Synced clears blocked first");
            OriginalKeyValueTable.SetSyncBlocked(db,"empty",true);OriginalKeyValueTable.SetSynced(db,"empty",false);
            local=sql.Find("empty");Require(local.Synced==0&&local.SyncBlocked==1,"Unsynced preserves blocked");
            kv.SetInt("int",int.MinValue);kv.SetLong("long",long.MaxValue);
            Require(kv.GetInt("int")==int.MinValue&&kv.GetLong("long")==long.MaxValue,"Number bounds");
            kv.SetString("overflow","9223372036854775808");Require(kv.GetInt("overflow",47)==47&&kv.GetLong("overflow",53)==53,"Overflow defaults");
            var previous=CultureInfo.CurrentCulture;var culture=(CultureInfo)CultureInfo.InvariantCulture.Clone();culture.NumberFormat.NegativeSign="~";
            try{CultureInfo.CurrentCulture=culture;kv.SetInt("culture",-19);Require(kv.GetString("culture",null)=="~19"&&kv.GetInt("culture")==-19,"Native current-culture overloads");}
            finally{CultureInfo.CurrentCulture=previous;}
            Require(kv.IncrementInt("new-counter",3,11)==14,"Increment honors missing default");
            kv.SetInt("int",int.MaxValue);Require(kv.IncrementInt("int")==int.MinValue,"Increment wraps int32");
            Require(resolves==1,"User context resolves once");kv.ResetConnection();kv.HasKey("empty");Require(resolves==2,"Reset connection re-resolves");
            account.WriteLevel(1);account.WriteLeagueLevel(0);account.WriteCoin(100);account.WriteStar(0);account.WriteEventInventory(16384);
            db.OnApplicationResume();db.OnApplicationResume(); // Native repeated begin is a no-op.
            account.WriteLevel(2);account.WriteCoin(171);account.WriteStar(1);account.ClearLevelTryCount();account.ClearHardLevelBackgroundIndex();
            using(var reader=new OriginalWindowsSqliteConnection(file))
                Require(reader.Find("Level").Value=="1"&&reader.Find("Coins").Value=="100","Uncommitted reward is isolated");
            db.ManualUpdate(60);Require(db.ShouldSave,"Only native frame interval saves");
            db.ManualUpdate(61);Require(!db.ShouldSave,"Interval save clears dirty");
            using(var reader=new OriginalWindowsSqliteConnection(file))
                Require(reader.Find("Level").Value=="2"&&reader.Find("Coins").Value=="171"&&reader.Find("Stars").Value=="1","Committed reward visible to reopened connection");
            account.WriteCoin(176);db.OnApplicationPause();Require(cleaned==1&&db.ShouldSave,"Pause commits and does not clear native dirty flag");
            db.OnApplicationResume();account.WriteCoin(178);db.OnApplicationQuit();Require(cleaned==2,"Quit dispatches corruption boundary after commit");
            db.OnApplicationResume();account.WriteCoin(999); // Must roll back on connection close.
        }
        using(var reopened=new OriginalWindowsSqliteConnection(file))
            Require(reopened.Find("Coins").Value=="178"&&reopened.Find("Level").Value=="2"&&reopened.Find("Stars").Value=="1","Reopen preserves committed reward and rolls back later writes");
        int retries=0;var missing=new OriginalUserKeyValue(()=>{retries++;return null;});
        for(int i=0;i<2;i++){try{missing.GetInt("missing");}catch(NullReferenceException){}}
        Require(retries==2,"Null services are retried, not cached as available");
        if(args.Length>1)
        {
            using(var captured=new OriginalWindowsSqliteConnection(args[1]))
            {
                var old=captured.Find("Level");Require(old.Key=="Level"&&old.Value!=null,"Read captured original schema without migration");
                captured.InsertOrReplace(new OriginalKeyValue{Key="RecoveryVerificationOnly",Value="typed",Synced=0,SyncBlocked=1});
                Require(captured.Delete("RecoveryVerificationOnly")==1,"Write/delete isolated copy of original schema");
            }
        }
        Console.WriteLine(checks);
    }
}
