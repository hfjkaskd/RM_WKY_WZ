using System;
using Bizza;
using Bizza.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 游戏加载阶段
/// 加载数据表=>加载主场景
/// </summary>
public class GameMode_Loading : GameModeBase
{
        public override E_GameModeType GameModeType => E_GameModeType.Loading;

        #region Loading UI

        private bool handingOffToGameplay;
        private int lastProgressPercent = -1;
        private LoadingProcedure _loadingProcedure;

        protected override void OnInit()
        {
                base.OnInit();
                _loadingProcedure = new LoadingProcedure();

                GameExtensionRegistry.RunLoadingProcedureHooks(_loadingProcedure);

#if WKY_SDK || USER_VERIFICATION_SDK
                _loadingProcedure.AddTask(new BootsFlowTask()); // SDK + 用户授权统一流程
#endif
#if BIZZA_REMOTEGROUPDATA
                _loadingProcedure.AddTask(new InitRemoteGroupDataTask()); // 远程配置拉取
#endif
                // _loadingProcedure.AddTask(new LoadGameResTask()); // 加载游戏资源 表格资源
                _loadingProcedure.AddTask(new PreLoadAssetsTask()); // 预加载资源
                _loadingProcedure.AddTask(new LoadGameDataTask()); // 加载游戏数据
                RunLoadingSafe().Forget();
        }

        protected override void OnRelease()
        {
                base.OnRelease();
                if (!handingOffToGameplay)
                {
                        UIModule.Instance.ClosePage(UIPageIds.LoadingPanel);
                }
        }

        private void OnLoadingFinish()
        {
                LogLogger.LogInfo("OnLoadingFinish");
                StartGame();
        }

        private async UniTaskVoid RunLoadingSafe()
        {
                try
                {
                        if (await UIModule.Instance.OpenPage(UIPageIds.LoadingPanel) == null)
                        {
                                throw new InvalidOperationException("The framework loading page failed to open.");
                        }

                        await UniTask.NextFrame();
                        await _loadingProcedure.Run(OnLoadingFinish);
                }
#if USER_VERIFICATION_SDK
                catch (Bizza.TokenClientSystem.TokenClientBlockedException e)
                {
                        // AuthFlow 已经完成对应的失败提示，这里只阻止进入游戏，避免重复弹出网络错误。
                        LogLogger.LogError($"用户授权阻止继续加载: {e.Message}");
                }
#endif
                catch (Exception e)
                {
                        LogLogger.LogError($"加载流程失败: {e}");
                        var args = new CommonConfirmTipsPanel.Args
                        {
                                isLanguage = true,
                                des = "HTTPNetworkProblem",
                                onFinish = Application.Quit,
                        };
                        await SDKAssetHandler.OpenCommonConfirmTipsPanel(args);
                }
        }

        public void StartGame()
        {
                if (handingOffToGameplay)
                {
                        return;
                }

                WkyGameplayBridge.Instance.PrepareInitialAttempt();
                bool teachEnable = GameExtensionRegistry.ShouldEnableTeach();
                LogLogger.LogInfo($"ReadyChangeScene {teachEnable}");

                if (SoundManager.Instance != null && SaveDataUtils.SettingData != null)
                {
                        SoundManager.Instance.MuteBGM(!SaveDataUtils.SettingData.enableMusic);
                }

                WkyGameplayBridge.ReportProgress(WkyGameplayBridge.BootProgressWeight);
                handingOffToGameplay = true;
                TransitionBlock.ToGamePlay();
        }

        private void Update()
        {
                if (!handingOffToGameplay && _loadingProcedure != null)
                {
                        float progress = _loadingProcedure.progress * WkyGameplayBridge.BootProgressWeight;
                        int percent = Mathf.FloorToInt(progress * 100f);
                        if (percent != lastProgressPercent)
                        {
                                lastProgressPercent = percent;
                                WkyGameplayBridge.ReportProgress(progress);
                        }
                }
        }

        #endregion
}
