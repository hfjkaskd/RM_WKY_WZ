using System;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;
namespace UnityEngine { public class ScriptableObject{} public static class Resources{public static T Load<T>(string p){throw new NotSupportedException();}} }
public static class AreaDataHarness
{
    static int[] v;static int calls;static bool dirty;static Context context;
    static readonly List<string> trace=new List<string>();
    static readonly Dictionary<string,OriginalKeyValue> rows=new Dictionary<string,OriginalKeyValue>();
    static void Step(string text){trace.Add(text);if(++calls==v[10])throw new InvalidOperationException();}
    sealed class Connection:IOriginalKeyValueConnection
    {
        public OriginalKeyValue Find(string key){Step("get:"+key);if(key=="Area"&&v[11]==1)context.Current=context.Second;rows.TryGetValue(key,out var r);return r;}
        public int InsertOrReplace(OriginalKeyValue r){dirty=true;Step("set:"+r.Key+":"+r.Value);rows[r.Key]=r;return 1;}
        public int Delete(string key){dirty=true;Step("delete:"+key);return rows.Remove(key)?1:0;}
        public int UpdateSynced(string key,int value){dirty=true;Step("synced:"+key+":"+value);if(!rows.TryGetValue(key,out var r))return 0;r.Synced=value;rows[key]=r;return 1;}
        public int UpdateSyncBlocked(string key,int value){dirty=true;Step("blocked:"+key+":"+value);if(!rows.TryGetValue(key,out var r))return 0;r.SyncBlocked=value;rows[key]=r;return 1;}
        public void BeginTransaction(){}public void Commit(){}public void Dispose(){}
    }
    sealed class Context:IOriginalAreaDataContext,IOriginalAreaConfigContainer,IOriginalAreaAssetAvailability
    {
        public OriginalUserAreaData First,Second,Current;
        public OriginalUserAreaData GetCurrentAreaData(){Step("user");return Current;}
        public IOriginalAreaConfigContainer CreateConfigContainer(){Step("new");return this;}
        public IOriginalAreaConfigContainer GetHomeConfigContainer(){Step("home");return v[9]==2?null:this;}
        public int PlayableAreaCount{get{Step("playable");return v[7];}}
        public IOriginalAreaAssetAvailability GetAssetBundleHelper(){Step("assets");return v[9]==5?null:this;}
        public bool HasAreaAssets(int id){Step("has:"+id);return v[8]==2||v[8]==1&&id<=1;}
        public OriginalAreaConfig LoadAreaConfig(int id,bool force=false){Step("load:"+id);return v[9]==3?null:new OriginalAreaConfig{tasks=v[9]==4?null:new OriginalAreaTaskConfig[v[4]]};}
    }
    static OriginalUserAreaData Area(OriginalUserKeyValue values,int id)=>new OriginalUserAreaData(values){AreaId=id,RealAreaId=9,ChestStatus=11,Tasks=new[]{new OriginalAreaTaskData(4,true),new OriginalAreaTaskData(2,false)}};
    static string Snap(OriginalUserAreaData area)
    {
        var tasks=new List<string>();if(area.Tasks!=null)foreach(var t in area.Tasks)tasks.Add(t.TaskId+":"+(t.IsCompleted?1:0));
        return area.AreaId+","+area.RealAreaId+","+area.ChestStatus+","+(area.Tasks==null?"-":string.Join("/",tasks));
    }
    static string Row(string key)=>rows.TryGetValue(key,out var r)?r.Value+","+r.Synced+","+r.SyncBlocked:"~";
    static string Payload(int id,int status,int mask,int bytes=12)
    {var data=new byte[bytes];if(bytes>=4)Buffer.BlockCopy(BitConverter.GetBytes(id),0,data,0,4);if(bytes>=8)Buffer.BlockCopy(BitConverter.GetBytes(status),0,data,4,4);if(bytes>=12)Buffer.BlockCopy(BitConverter.GetBytes(mask),0,data,8,4);return Convert.ToBase64String(data);}
    public static void Main()
    {
        Console.OutputEncoding=new UTF8Encoding(false);string line;
        while((line=Console.ReadLine())!=null)
        {
            v=Array.ConvertAll(line.Split(','),int.Parse);calls=0;dirty=false;trace.Clear();rows.Clear();
            var db=new OriginalDatabaseService(new Connection(),61,null);var values=new OriginalUserKeyValue(()=>db);
            context=new Context{First=Area(values,v[1]),Second=Area(values,12)};context.Current=v[9]==1?null:context.First;
            string stored=v[5]==0?null:v[5]==1?Payload(v[1],v[2],v[3]):v[5]==2?"!":v[5]==3?Payload(v[1],v[2],v[3],4):v[5]==4?Payload(v[1],v[2],v[3],16):string.Empty;
            string next=v[6]==0?null:v[6]==1?(v[1]+2)+"\u00a7"+v[3]+"\u00a7"+v[2]:v[6]==2?"bad\u00a74\u00a72":v[6]==3?"2\u00a73":"2\u00a73\u00a74\u00a799";
            if(stored!=null)rows["Area"]=new OriginalKeyValue{Key="Area",Value=stored,SyncBlocked=1};
            if(next!=null)rows["NextArea"]=new OriginalKeyValue{Key="NextArea",Value=next,SyncBlocked=1};
            var parser=new OriginalAreaDataParser(values,context);bool failed=false;
            try{switch(v[0]){case 0:parser.ParseAndSetFromLocalStorage();break;case 1:parser.ParseAndSetFromServer(context.Current,v[1],v[3],v[2]);break;case 2:parser.ParseAndSetAvailableAreaFromLocalStorage();break;case 3:parser.SetInitialValues(context.Current);break;}}
            catch(Exception){failed=true;}
            if(dirty!=db.ShouldSave)throw new InvalidOperationException("Database dirty publication differs from actual storage kernel");
            Console.WriteLine((failed?1:0)+"#"+Snap(context.First)+"#"+Snap(context.Second)+"#"+(dirty?1:0)+"#"+Row("Area")+"#"+Row("NextArea")+"#"+string.Join(";",trace));
        }
    }
}
