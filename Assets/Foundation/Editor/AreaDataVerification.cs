using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Explicit storage/config integration checks. Download availability and the
    // published-area limit are fixture inputs; no Home UI/backend is supplied.
    public static class AreaDataVerification
    {
        private sealed class Context:IOriginalAreaDataContext,IOriginalAreaAssetAvailability
        {
            public OriginalUserAreaData Area;
            public int Limit=166;
            public readonly HashSet<int> Available=new HashSet<int>();
            private readonly OriginalAreaConfigContainer configs=new OriginalAreaConfigContainer();
            public OriginalUserAreaData GetCurrentAreaData()=>Area;
            public IOriginalAreaConfigContainer CreateConfigContainer()=>new OriginalAreaConfigContainer();
            public IOriginalAreaConfigContainer GetHomeConfigContainer()=>configs;
            public int PlayableAreaCount=>Limit;
            public IOriginalAreaAssetAvailability GetAssetBundleHelper()=>this;
            public bool HasAreaAssets(int areaId)=>Available.Contains(areaId);
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public static void Run()
        {
            var report=new Report();
            try
            {
                int textureCount=Resources.FindObjectsOfTypeAll<Texture>().Length;
                var configs=new OriginalAreaConfigContainer();report.task_counts=new int[167];report.config_data=new string[167];
                for(int id=0;id<167;id++)
                {
                    var config=configs.LoadAreaConfig(id);
                    Require(config!=null&&config.areaId==id&&config.tasks!=null,"Original area config loads by native numbered resource identity: "+id);
                    report.task_counts[id]=config.tasks.Length;
                    report.config_data[id]=JsonUtility.ToJson(config);
                    Require(ReferenceEquals(config,configs.LoadAreaConfig(id)),"Config cache preserves identity");
                }
                report.configs_loaded=report.task_counts.Length;
                report.new_textures_from_config_load=Resources.FindObjectsOfTypeAll<Texture>().Length-textureCount;
                Require(report.new_textures_from_config_load==0,"Area progress/config loading must not pull large textures into memory");
                string path=Path.Combine(Path.GetTempPath(),"royal-area-"+Guid.NewGuid().ToString("N")+".sqlite");
                using(var connection=OriginalRuntimeStorage.Open(path))
                {
                    var db=new OriginalDatabaseService(connection,61,()=>{});db.OnApplicationResume();
                    var values=new OriginalUserKeyValue(()=>db);var area=new OriginalUserAreaData(values);
                    var context=new Context{Area=area};var parser=new OriginalAreaDataParser(values,context);
                    parser.ParseAndSetFromLocalStorage();
                    Require(area.AreaId==1&&area.RealAreaId==1&&area.ChestStatus==0&&area.Tasks.Length==6&&!db.ShouldSave&&!values.HasKey("Area"),"Native fresh account initializes original six tasks without writing defaults");
                    report.fresh_area_verified=true;
                    values.SetString("Area",Convert.ToBase64String(new byte[]{1,0,0,0,3,0,0,0,5,0,0,0}));db.Save();
                    parser.ParseAndSetFromLocalStorage();
                    Require(area.AreaId==1&&area.RealAreaId==1&&area.ChestStatus==3&&area.GetTaskValue()==5&&!db.ShouldSave,"Legacy12-byte area restore preserves task bits/status without a write");
                    report.legacy_restore_verified=true;
                    values.SetString("NextArea","2\u00a75\u00a71");context.Available.Add(2);parser.ParseAndSetFromLocalStorage();
                    Require(area.AreaId==2&&area.RealAreaId==2&&area.ChestStatus==1&&area.GetTaskValue()==5&&!values.HasKey("NextArea"),"Downloaded real-area restore consumes NextArea only after updating the model/save");
                    report.next_area_restore_verified=true;
                    context.Limit=2;parser.ParseAndSetFromServer(area,8,7,4);
                    Require(area.AreaId==2&&area.RealAreaId==8&&area.ChestStatus==1&&values.GetString("NextArea",null)=="8\u00a77\u00a74","Newer server area keeps its real progress while displaying the published area");
                    for(int i=0;i<area.Tasks.Length;i++)Require(area.Tasks[i].IsCompleted,"Fallback area's native task mask is complete");
                    report.server_limit_verified=true;
                    area.UpdateAreaData(3,2,OriginalAreaDataParser.ConvertAreaTaskDataToArray(5,configs.LoadAreaConfig(3).tasks.Length));
                    context.Available.Clear();context.Available.Add(1);parser.ParseAndSetAvailableAreaFromLocalStorage();
                    var row=connection.Find("Area");
                    Require(area.AreaId==1&&area.RealAreaId==3&&area.ChestStatus==1&&row.Synced==1&&row.SyncBlocked==0&&values.GetString("NextArea",null)=="3\u00a75\u00a72","Missing bundle chooses the latest available area, preserves real task data and marks display save synced");
                    report.unavailable_bundle_verified=true;
                    context.Limit=166;context.Available.Add(3);parser.ParseAndSetFromLocalStorage();
                    Require(area.AreaId==3&&area.RealAreaId==3&&area.ChestStatus==2&&area.GetTaskValue()==5&&!values.HasKey("NextArea"),"Newly available real-area assets restore its saved task progress");
                    db.Save();
                    using(var reopened=OriginalRuntimeStorage.Open(path))
                    {
                        var reloadDb=new OriginalDatabaseService(reopened,61,()=>{});var kv=new OriginalUserKeyValue(()=>reloadDb);
                        var reload=new OriginalUserAreaData(kv);var reloadContext=new Context{Area=reload};
                        new OriginalAreaDataParser(kv,reloadContext).ParseAndSetFromLocalStorage();
                        Require(reload.AreaId==3&&reload.RealAreaId==3&&reload.ChestStatus==2&&reload.GetTaskValue()==5&&!reloadDb.ShouldSave,"Actual SQLite reopen restores area state without saving");
                    }
                    report.database_reopen_verified=true;
                }
                report.passed=true;
            }
            catch(Exception error){report.failure=error.ToString();}
            File.WriteAllText(Path.Combine(Application.dataPath,"../live_area_data_verification.json"),JsonUtility.ToJson(report,true));
        }
        [Serializable] private sealed class Report
        {
            public bool passed,fresh_area_verified,legacy_restore_verified,next_area_restore_verified,server_limit_verified,unavailable_bundle_verified,database_reopen_verified;
            public int configs_loaded,new_textures_from_config_load;public int[] task_counts;public string[] config_data;public string failure;
            public string scope="Actual167 recovered lightweight Resources configs and real private Windows SQLite storage execute runtime AreaDataParser/UserAreaData. Fresh/legacy/NextArea/server-limit/unavailable-bundle/available-again/reopen behavior is checked. Bundle availability/published limits are explicit inputs; this is not production account startup, actual download/Home navigation/rendered parity or mobile proof. Texture paths replace original heavy strong references; config restore loads zero additional Texture objects.";
        }
    }
}
