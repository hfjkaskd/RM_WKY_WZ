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
                Exception failure = null;
                try
                {
                        if (await UIModule.Instance.OpenPage(UIPageIds.LoadingPanel) == null)
                        {
                                throw new InvalidOperationException("The framework loading page failed to open.");
                        }

                        await UniTask.NextFrame();
                        await _loadingProcedure.Run(OnLoadingFinish);
                }
                catch (Exception error)
                {
                        failure = error;
                }

                // Classify after leaving the catch region; only the ordinary
                // failure path awaits a prompt, matching the existing owners.
                if (failure == null) return;
                if (failure is WKYInitializationException blocked &&
                    (blocked.ErrorCode == "CountryRestricted" || blocked.ErrorCode == AccountModule.SuspendedCode))
                {
                        LogLogger.LogInfo($"账号或地区限制阻止进入游戏: {blocked.Message}");
                        return;
                }
                if (failure is Bizza.TokenClientSystem.TokenClientBlockedException tokenBlocked)
                {
                        LogLogger.LogError($"用户授权阻止继续加载: {tokenBlocked.Message}");
                        return;
                }
                if (released || AccountModule.Instance?.IsSuspended == true) return;
                LogLogger.LogError($"加载流程失败: {failure}");
                var args = new CommonConfirmTipsPanel.Args
                {
                        isLanguage = true,
                        des = "HTTPNetworkProblem",
                        onFinish = Application.Quit,
                };
                await SDKAssetHandler.OpenCommonConfirmTipsPanel(args);
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
