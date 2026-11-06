using System;
using Bizza;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bizza.Loading
{
    public class PreLoadAssetsTask : LoadingTaskBase
    {
        public override LoadingTaskName TaskName => LoadingTaskName.PreLoadAsset;
        public override float Weight => 0.3f;
        public override LoadingTaskName[] Dependencies => Array.Empty<LoadingTaskName>();
        
        public override async UniTask Execute()
        {
            LogLogger.LogInfo($"加载任务：{TaskName} start");
#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] PreLoadAssetsTask.Execute begin");
#endif
            SetProgress(0.9f);

            GameExtensionRegistry.RunPreloadHooks();

#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] PreLoadAssetsTask preload RealGamePanel request");
#endif
            AssetUtils.LoadAssetAsync<GameObject>("UIPanel/RealGamePanel").Forget();

#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] PreLoadAssetsTask BridgingUtil.LoadGamePlayAsync begin");
#endif
            await BridgingUtil.LoadGamePlayAsync();
#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] PreLoadAssetsTask BridgingUtil.LoadGamePlayAsync end");
#endif
#if BIZZA_REAL_WITHDRAW
            await FakeWithdrawalAnalytics.PreloadAsync();
#endif
            SetProgress(1f);

#if UNITY_EDITOR
            Debug.Log("[WhiteBootstrap] PreLoadAssetsTask.Execute end");
#endif
            LogLogger.LogInfo($"加载任务：{TaskName} end");
        }
    }
}
