using System;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;
class Services : IOriginalLevelRandomLifecycleServices, IOriginalLevelSeedRandom, IOriginalRandomUserEvents, IOriginalSavedLevelRandom
{
    public int Flags,Level,Device,Attempt,AppSeed,SavedSeed,SavedCount,State;
    public long UserId;
    public readonly List<Action> Handlers=new List<Action>();
    static void Need(bool yes) { if(!yes)throw new NullReferenceException(); }
    bool Has(int mask)=>(Flags&mask)==mask;
    public IOriginalLevelSeedRandom GetApplicationRandom() { Verify.Record("app");return Has(1)?this:null; }
    public int Next(int min,int max) { Verify.Record("appnext,"+min+","+max);return AppSeed; }
    public void RequireCurrentUserSession() { Verify.Record("session");Need(Has(6)); }
    public int GetLevelNumber() { Verify.Record("level");Need(Has(14));return Level; }
    public long GetUserId() { Verify.Record("userid");Need(Has(18));return UserId; }
    public int GetDeviceIdKey() { Verify.Record("device");return Device; }
    public int GetAttemptIndex() { Verify.Record("attempt");return Attempt; }
    public IOriginalRandomUserEvents GetUserEvents() { Verify.Record("user");return Has(32)?this:null; }
    public bool IsCurrentLevelWon { get { Verify.Record("win");Need(Has(14));return State==3; } }
    public event Action OnUserChanged
    {
        add { Verify.Record("add");Handlers.Add(value); }
        remove { Verify.Record("remove");int i=Handlers.LastIndexOf(value);if(i>=0)Handlers.RemoveAt(i); }
    }
    public void Emit() { foreach(var action in Handlers.ToArray())action(); }
    public IOriginalSavedLevelRandom GetSavedRandomData() { Verify.Record("saved");return Has(448)?this:null; }
    public int GetDifficultySeed() { Verify.Record("savedseed");return SavedSeed; }
    public int GetDifficultySeedGenerationCount() { Verify.Record("savedcount");return SavedCount; }
    public void Set(int key,int value)
    {
        switch(key)
        {
            case 0:Flags=value;break;case 1:Level=value;break;case 2:UserId=value;break;case 3:Device=value;break;
            case 4:Attempt=value;break;case 5:AppSeed=value;break;case 6:SavedSeed=value;break;case 7:SavedCount=value;break;case 8:State=value;break;
        }
    }
}
class Verify
{
    static int calls,fail;
    static readonly List<string> trace=new List<string>();
    public static void Record(string value) { trace.Add(value);if(++calls==fail)throw new InvalidOperationException(); }
    static string Snapshot(OriginalLevelRandomManager manager,Services s)
    {
        var a=manager.AttemptState;int hash=0;
        for(int i=0;i<a.Seeds.Count;i++)hash=unchecked(hash+(i+1)*a.Seeds[i]);
        return string.Join(",",new int[]{manager.DifficultySource.DifficultySeed,manager.DifficultySource.DifficultySeedGenerationCount,
            a.IsGenerated?1:0,a.CachedDeviceIdKey,a.Seeds.Count,hash,manager.IsRemoteLevel?1:0,manager.HasSwitchedLevel?1:0,
            manager.IsSetFromAdminPanel?1:0,manager.AdminSeed,s.Handlers.Count});
    }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('#');var h=p[0].Split(',');var s=new Services {Flags=int.Parse(h[0]),Level=int.Parse(h[1]),UserId=long.Parse(h[2]),
                Device=int.Parse(h[3]),Attempt=int.Parse(h[4]),AppSeed=int.Parse(h[5]),SavedSeed=int.Parse(h[6]),SavedCount=int.Parse(h[7]),State=int.Parse(h[8])};
            var manager=new OriginalLevelRandomManager(s);var output=new List<string>();
            foreach(var command in p[1].Split(';'))
            {
                var r=command.Split(',');int mode=int.Parse(r[0]),a=int.Parse(r[1]);int b=mode==13?0:int.Parse(r[2]);fail=int.Parse(r[3]);calls=0;trace.Clear();string result="-";
                try
                {
                    switch(mode)
                    {
                        case 0:manager.ResetSeed();break;case 1:manager.Bind();break;case 2:s.Emit();break;
                        case 3:manager.Clear(a!=0);break;case 4:manager.RestoreFromSaveData();break;
                        case 5:manager.IsRemoteLevel=(a&1)!=0;manager.HasSwitchedLevel=(a&2)!=0;manager.IsSetFromAdminPanel=(a&4)!=0;manager.AdminSeed=b;break;
                        case 6:result=manager.DifficultyNext(a,b).ToString();break;case 7:result=manager.Next(a,b).ToString();break;
                        case 8:result=manager.NextBool()?"1":"0";break;case 9:result=manager.DeviceIdKey.ToString();break;
                        case 10:s.Set(a,b);break;case 11:manager.DifficultySource.SetDifficultySeed(a);break;
                        case 12:manager.AttemptState.ResetDifficultySeed(s);break;
                        case 13:result=OriginalLevelRandomManager.ParseDeviceIdKey(r[2]=="~"?null:Encoding.UTF8.GetString(Convert.FromBase64String(r[2]))).ToString();break;
                        case 14:result=manager.Id.ToString();break;
                    }
                }
                catch(Exception){result="error";}
                output.Add(result+":"+Snapshot(manager,s)+":"+string.Join("/",trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
