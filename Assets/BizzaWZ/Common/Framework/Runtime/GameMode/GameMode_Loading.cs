using System;
using System.Collections.Generic;
using Bizza;
using Bizza.GameAnalytics;
using Bizza.Loading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 游戏加载阶段
/// 加载数据表=>加载主场景
/// </summary>
public class GameMode_Loading : GameModeBase
{
        public override E_GameModeType GameModeType => E_GameModeType.Loading;

        #region Loading UI

        private float progress;
        [LabelText("加载速度")]
        [Range(0f, 1f)]
        public float progressSpeed = 0.2f;

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
                UIModule.Instance.OpenPage(UIPageIds.LoadingPanel).Forget();
        }

        protected override void OnRelease()
        {
                base.OnRelease();
                UIModule.Instance.ClosePage(UIPageIds.LoadingPanel);
        }


        private void OnLoadingFinish()
        {
                LogLogger.LogInfo("OnLoadingFinish");
#if BIZZA_REAL_WITHDRAW
                var account = AccountModule.Instance;
                if (account?.Os_Current_Uso != null)
                {
                SaveDataUtils.GameData.playerSelectedLv =
                        account.Os_Current_Uso.Os_Rts;
                }
#endif
                StartGame();
        }

        private async UniTaskVoid RunLoadingSafe()
        {
                try
                {
                        await UniTask.DelayFrame(1);
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
                bool teachEnable = GameExtensionRegistry.ShouldEnableTeach();

                BizzaGameAnalytics.TrackGameLoadComplete();
                LogLogger.LogInfo($"ReadyChangeScene {teachEnable}");
		TransitionBlock.ToGamePlay();
                BizzaEventSystem.Emit(EventDefine.Frame.LoadingStageComplete);

                if (SoundManager.Instance != null && SaveDataUtils.SettingData != null)
                {
                        SoundManager.Instance.MuteBGM(!SaveDataUtils.SettingData.enableMusic);
                }
                GameUtils.DelayDo(() =>
                {
                        SoundManager.Instance?.PlayBGM("SFX_BGM");
                }, 1f).Forget();
        }

        private void Update()
        {
                progress += Time.deltaTime * progressSpeed;
                float min = 0;
                float max = _loadingProcedure != null ? _loadingProcedure.progress : 0;
                progress = Mathf.Clamp(progress, min, max);
                BizzaEventSystem.Emit(EventDefine.Frame.LoadingProgress, progress);
        }

        #endregion
}
