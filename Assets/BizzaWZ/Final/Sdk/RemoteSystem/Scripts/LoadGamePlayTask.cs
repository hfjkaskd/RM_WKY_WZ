using Bizza.Loading;
using Bizza.GameAnalytics;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LoadGamePlayTask : LoadingTaskBase
{
    public override LoadingTaskName TaskName => LoadingTaskName.LoadGamePlayTask;
    public override float Weight => 10f;

#if BIZZA_REAL_WITHDRAW
    public override LoadingTaskName[] Dependencies => new[]
    {
        LoadingTaskName.LoadGameRes,
        LoadingTaskName.PreLoadAsset,
        LoadingTaskName.LoadGameData,
        LoadingTaskName.LoadTables,
        LoadingTaskName.ReadLanguage,
        LoadingTaskName.PrewarmFinalFeatures,
        LoadingTaskName.InitRemoteGroupData,
    };
#else
    public override LoadingTaskName[] Dependencies => new[]
    {
        LoadingTaskName.LoadGameRes,
        LoadingTaskName.PreLoadAsset,
        LoadingTaskName.LoadGameData,
        LoadingTaskName.InitRemoteGroupData,
    };
#endif

    public override async UniTask Execute()
    {
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] LoadGamePlayTask SortDash preload begin");
#endif
        SetProgress(0.1f);
        await BridgingUtil.LoadGamePlayAsync();
        SetProgress(1f);
#if UNITY_EDITOR
        Debug.Log("[WhiteBootstrap] LoadGamePlayTask SortDash preload end");
#endif
    }
}

public class InitRemoteGroupDataTask : LoadingTaskBase
{
    public override LoadingTaskName TaskName => LoadingTaskName.InitRemoteGroupData;
    public override float Weight => 0.3f;
#if WKY_SDK
    // 国家由 Boots 中的 WKY 登录确定；加载器必须先完成该批次再运行远端任务。
    public override LoadingTaskName[] Dependencies => new[] { LoadingTaskName.Boots };
#else
    public override LoadingTaskName[] Dependencies => System.Array.Empty<LoadingTaskName>();
#endif

    public override async UniTask Execute()
    {
        LogLogger.LogGameConfigLoad("开始执行远端分组配置加载任务。");
        SetProgress(0.1f);

        try
        {
            var shouldInitializeRemoteGroup = true;
#if BIZZA_REAL_WITHDRAW && WKY_SDK
            if (RemoteGroupDataSystem.current.UsesCountryConfig)
            {
                var accountModule = AccountModule.Instance;
                if (accountModule == null || !accountModule.LoadingFinish)
                {
                    LogLogger.LogGameConfigLoad("Boots 已结束但账号数据尚未就绪，跳过本次国家远端配置初始化。");
                    shouldInitializeRemoteGroup = false;
                }
                else
                {
                    LogLogger.LogGameConfigLoad($"Boots 已完成账号初始化，最终国家={AccountModule.CountryType}，开始初始化远端分组配置。");
                }
            }
            else
            {
                LogLogger.LogGameConfigLoad("未开启国家相关配置，按公共目录初始化远端分组配置。");
            }
#endif

            if (shouldInitializeRemoteGroup)
            {
                await RemoteGroupDataSystem.current.InitGameData();

                BizzaAnalyticsAgent.Instance.BUserProp(new System.Collections.Generic.Dictionary<string, object>()
                {
                    {"UserGroup" , RemoteGroupDataSystem.current.GetUserGroupName()}
                });
                BizzaGameAnalytics.SetUserGroup(RemoteGroupDataSystem.current.GetUserGroupName());
            }
        }
        catch (System.Exception exception)
        {
            LogLogger.LogGameConfigLoad($"远端分组配置加载任务异常：{exception.Message}。");
        }

        SetProgress(1f);
        LogLogger.LogGameConfigLoad("远端分组配置加载任务结束。");
    }
}
