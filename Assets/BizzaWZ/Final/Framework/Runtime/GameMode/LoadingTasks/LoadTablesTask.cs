using Bizza;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bizza.Loading
{
    /// <summary>
    /// 加载表格任务
    /// </summary>
    public class LoadTablesTask : LoadingTaskBase
    {
        public override LoadingTaskName TaskName => LoadingTaskName.LoadTables;
        public override float Weight => 0.3f;
        public override LoadingTaskName[] Dependencies => new LoadingTaskName[]
        {
            // LoadingTaskName.LoadRemoteConfig
        }; // 依赖远程配置
        
        public override async UniTask Execute()
        {
            LogLogger.LogInfo($"{TaskName} start");
#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadTablesTask.Execute begin");
#endif
            SetProgress(0.9f);

#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadTablesTask.LoadTableConfigs begin");
#endif
            await TableUtils.LoadTableConfigs();
#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadTablesTask.LoadTableConfigs end");
#endif

            BizzaEventSystem.Emit(EventDefine.Frame.DataTableLoaded);
#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadTablesTask DataTableLoaded emitted");
#endif
            SetProgress(1f);

#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadTablesTask.Execute end");
#endif
            LogLogger.LogInfo($"{TaskName} end");
        }
    }
}

