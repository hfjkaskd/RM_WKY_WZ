using Bizza;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bizza.Loading
{
    [Obfuz.ObfuzIgnore]
    /// <summary>
    /// 加载游戏数据任务
    /// </summary>
    public class LoadGameDataTask : LoadingTaskBase
    {
        public override LoadingTaskName TaskName => LoadingTaskName.LoadGameData;
        public override float Weight => 0.3f;
        public override LoadingTaskName[] Dependencies => System.Array.Empty<LoadingTaskName>();
        
        public override async UniTask Execute()
        {
            LogLogger.LogInfo($"{TaskName} start");
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] LoadGameDataTask.Execute begin saveModule:{(SaveDataModule.Instance != null)} loadingFinish:{(SaveDataModule.Instance != null && SaveDataModule.Instance.LoadingFinish)}");
#endif
            SetProgress(0.94f);

            //游戏数据在SaveDataModule模块加载，这里等待就好
#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadGameDataTask wait SaveDataModule.LoadingFinish begin");
#endif
            if (SaveDataModule.Instance != null
                && (SaveDataUtils.GameData == null || SaveDataUtils.SettingData == null))
            {
                SaveDataModule.Instance.ManualLoadAllData();
            }

            await UniTask.WaitUntil(() => SaveDataModule.Instance != null
                                          && SaveDataModule.Instance.LoadingFinish
                                          && SaveDataUtils.GameData != null
                                          && SaveDataUtils.SettingData != null);
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] LoadGameDataTask wait SaveDataModule.LoadingFinish end loadingFinish:{SaveDataModule.Instance.LoadingFinish}");
#endif

            //设置语言
            SetProgress(1f);

#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] LoadGameDataTask.Execute end");
#endif
            LogLogger.LogInfo($"{TaskName} end");
        }
    }
}

