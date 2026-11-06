using System;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;

public static class StorageKernelHarness
{
    static readonly List<string> trace=new List<string>();
    static int[] v; static long value,fallback; static OriginalDatabaseService db;
    static string raw; static string Encode(string s)=>s==null?"~":Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    sealed class Connection:IOriginalKeyValueConnection
    {
        readonly int id;
        public Connection(int id=0){this.id=id;}
        void Fail(){if(v[2]==3)throw new InvalidOperationException();}
        public OriginalKeyValue Find(string key){trace.Add("find");Fail();return new OriginalKeyValue{Key=v[8]==0?null:v[8]==1?"Coins":"",Value=raw,Synced=v[6],SyncBlocked=v[7]};}
        public int InsertOrReplace(OriginalKeyValue row){trace.Add("insert,"+Encode(row.Key)+","+Encode(row.Value)+","+row.Synced+","+row.SyncBlocked);Fail();return v[1];}
        public int Delete(string key){trace.Add("delete");Fail();return v[1];}
        public int UpdateSynced(string key,int amount){trace.Add("synced,"+amount);Fail();return v[1];}
        public int UpdateSyncBlocked(string key,int amount){trace.Add("blocked,"+amount);Fail();return v[1];}
        public void Commit(){trace.Add("commit,"+id);if(v[2]==4)throw new InvalidOperationException();if(v[2]==7)db.Connection=new Connection(1);}
        public void BeginTransaction(){trace.Add("begin,"+id);if(v[2]==5)throw new InvalidOperationException();}
        public void Dispose(){}
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            string[] p=line.Split('|');v=Array.ConvertAll(p[0].Split(','),int.Parse);value=long.Parse(p[1]);fallback=long.Parse(p[2]);
            raw=p[3]=="~"?null:Encoding.UTF8.GetString(Convert.FromBase64String(p[3]));
            db=new OriginalDatabaseService(v[2]==2?null:new Connection(),v[4],()=>{trace.Add("clean");if(v[2]==6)throw new InvalidOperationException();});
            if(v[3]!=0){try{db.InsertOrReplace(default);}catch{} }
            if(v[2]==1)db=null;
            trace.Clear();bool fault=false;string result="0";
            try
            {
                switch(v[0])
                {
                    case 0:result=OriginalKeyValueTable.SetString(db,"Coins",raw,v[9]!=0)?"1":"0";break;
                    case 1:result=OriginalKeyValueTable.SetInt(db,"Coins",unchecked((int)value),v[9]!=0)?"1":"0";break;
                    case 2:result=OriginalKeyValueTable.SetLong(db,"Coins",value,v[9]!=0)?"1":"0";break;
                    case 3:result=Encode(OriginalKeyValueTable.GetString(db,"Coins","fallback"));break;
                    case 4:result=OriginalKeyValueTable.GetInt(db,"Coins",unchecked((int)fallback)).ToString();break;
                    case 5:result=OriginalKeyValueTable.GetLong(db,"Coins",fallback).ToString();break;
                    case 6:result=OriginalKeyValueTable.HasKey(db,"Coins")?"1":"0";break;
                    case 7:result=OriginalKeyValueTable.GetSyncBlocked(db,"Coins")?"1":"0";break;
                    case 8:result=OriginalKeyValueTable.SetSyncBlocked(db,"Coins",v[9]!=0)?"1":"0";break;
                    case 9:result=OriginalKeyValueTable.SetSynced(db,"Coins",v[9]!=0)?"1":"0";break;
                    case 10:result=OriginalKeyValueTable.DeleteKey(db,"Coins")?"1":"0";break;
                    case 11:db.Save();break;
                    case 12:db.ManualUpdate(v[5]);break;
                    case 13:db.OnApplicationResume();break;
                    case 14:db.OnApplicationPause();break;
                    case 15:db.OnApplicationQuit();break;
                    case 16:result=new OriginalUserKeyValue(()=>db).IncrementInt("Coins",unchecked((int)value),unchecked((int)fallback)).ToString();break;
                }
            }
            catch{fault=true;result="0";}
            Console.WriteLine((fault?1:0)+"#"+(db!=null&&db.ShouldSave?1:0)+","+result+"#"+string.Join(";",trace));
        }
    }
}
