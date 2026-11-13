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
        private bool loadingCompleted, released;
        private int lastProgressPercent = -1;
        private LoadingProcedure _loadingProcedure;

        protected override void OnInit()
        {
                base.OnInit();
                handingOffToGameplay = loadingCompleted = released = false;
                lastProgressPercent = -1;
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
                released = true;
                base.OnRelease();
                if (!handingOffToGameplay)
                {
                        UIModule.Instance.ClosePage(UIPageIds.LoadingPanel);
                }
        }

        private void OnLoadingFinish()
        {
                if (released || loadingCompleted) return;
                loadingCompleted = true;
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
                catch (WKYInitializationException e) when (e.ErrorCode == "CountryRestricted" ||
                                                           e.ErrorCode == AccountModule.SuspendedCode)
                {
                        // The account/country gate owns its existing blocking prompt.
                        LogLogger.LogInfo($"账号或地区限制阻止进入游戏: {e.Message}");
                }
                catch (Bizza.TokenClientSystem.TokenClientBlockedException e)
                {
                        // AuthFlow 已经完成对应的失败提示，这里只阻止进入游戏，避免重复弹出网络错误。
                        LogLogger.LogError($"用户授权阻止继续加载: {e.Message}");
                }
                catch (Exception e)
                {
                        if (released || AccountModule.Instance?.IsSuspended == true) return;
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
                if (released || handingOffToGameplay || !loadingCompleted ||
                    AccountModule.Instance?.IsSuspended == true)
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
