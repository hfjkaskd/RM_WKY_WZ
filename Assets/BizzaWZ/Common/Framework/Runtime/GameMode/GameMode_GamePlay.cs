using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameMode_GamePlay : GameModeBase<GameMode_GamePlay>
{
    private bool releaseRequested;

    public override E_GameModeType GameModeType => E_GameModeType.GamePlay;

    /// <summary>
    /// 关卡开始，关卡结束，关卡结算。
    /// </summary>
    protected override void OnInit()
    {
        releaseRequested = false;
        FlowModule.SetAutomaticPopupFlowBlocked(true);
        FlowModule.NewPlayerEnter();

        AssetUtils.LoadSceneWithBlockAsync("GamePlay", async () =>
        {
            try
            {
                if (!CanContinueAsyncInit())
                {
                    return;
                }
                await UIModule.Instance.OpenPage(UIPageIds.RealGamePanel);
                if (!CanContinueAsyncInit())
                {
                    return;
                }

                await UniTask.Yield();
                if (!CanContinueAsyncInit())
                {
                    return;
                }

                FlowModule.SetAutomaticPopupFlowBlocked(false);
                BridgingUtil.CanShowGuide();
            }
            catch (System.Exception)
            {
                if (CanContinueAsyncInit())
                {
                    throw;
                }
            }
        }).Forget();
    }

    protected override void LateInit()
    {

    }

    protected override void OnPreRelease()
    {
        releaseRequested = true;
        FlowModule.SetAutomaticPopupFlowBlocked(true);
        int layer = UIModule.Instance.GetLayerByName(nameof(E_UILayer.BaseLayer));
        UIModule.Instance.CloseAndDestroyLayerAllPage(layer);
#if BIZZA_REAL_WITHDRAW
        UnloadUnusedAssetsAfterUiCleanup().Forget();
#endif
    }

    private bool CanContinueAsyncInit()
    {
        return !releaseRequested && this != null && Instance == this;
    }

#if BIZZA_REAL_WITHDRAW
    private static async UniTaskVoid UnloadUnusedAssetsAfterUiCleanup()
    {
        await UniTask.DelayFrame(1);
        await Resources.UnloadUnusedAssets();
    }
#endif

    /// <summary>
    /// 是否开始自定义的教程
    /// </summary>
    /// <param name="startState"></param>
    public void OnSetCustomTutorialState(bool startState)
    {
        if (SaveDataUtils.GameData.customTutorialEnd)
        {
            return;
        }
        SaveDataUtils.GameData.customTutorialEnd = startState;
        SaveDataUtils.Save();
    }


}
