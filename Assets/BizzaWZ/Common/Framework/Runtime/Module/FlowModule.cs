using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.GameAnalytics;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class FlowModule
{
    // Set before asynchronous settlement/level loading; resume only when gameplay is ready.
    internal static bool AutomaticPopupFlowBlocked { get; private set; }
    internal static void SetAutomaticPopupFlowBlocked(bool blocked) => AutomaticPopupFlowBlocked = blocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAutomaticPopupFlow() => AutomaticPopupFlowBlocked = false;

    private static string analyticsLevelId;
    private static string analyticsLevelAttemptId;

#if BIZZA_REAL_WITHDRAW
    private static bool winResultHandled;
    public static int WinLevelNumber { get; private set; }
#endif
    public static void NewPlayerEnter()
    {
        var gameData = SaveDataUtils.GameData;
        if (gameData == null || !gameData.newPlayerGiveProp || PropConfigSO.Instance == null)
        {
            return;
        }

        foreach (PropConfigInfo propConfigInfo in PropConfigSO.Instance.PropCfgInfos)
        {
            if (propConfigInfo == null || propConfigInfo.propType == E_ItemType.None)
            {
                continue;
            }

            ItemUtils.AddItem(propConfigInfo.propType, propConfigInfo.newPlayerPropCount);
        }

        gameData.newPlayerGiveProp = false;

        BridgingUtil.NewPlayerEnter();
        BizzaGameAnalytics.TrackActivation();

        SaveDataUtils.Save();
    }

    // 新手玩家进入，游戏关卡生成完毕，可以显示新手引导时触发
    public static void CanShowGuide()
    {
        if (SaveDataUtils.GameData == null)
        {
            return;
        }

        SaveDataUtils.GameData.customTutorialCanPlay = true;
        SaveDataUtils.Save();
        BridgingUtil.CanShowGuide();
    }

    // 新手玩家引导结束 触发
    public static void NewPlayerGuideEnd()
    {
        BridgingUtil.NewPlayerGuideEnd();
        GameMode_GamePlay.Instance.OnSetCustomTutorialState(true);
        BizzaGameAnalytics.TrackTutorialComplete();
    }

    // 每一关游戏开始时调用
    public static void GameStart()
    {
        SetAutomaticPopupFlowBlocked(false);
#if BIZZA_REAL_WITHDRAW
        winResultHandled = false;
        WinLevelNumber = 0;
        FakeWithdrawalAnalytics.BeginLevelAttempt();
        AccountModule.Instance.CheckPendingAutoPopups();
#endif
        BridgingUtil.GameStart();
        BeginAnalyticsLevel();
        NumbericalStatistics.InitProp();
        SaveDataUtils.GameData.currentReviveCount = 0;
#if BIZZA_REAL_WITHDRAW
        SaveDataUtils.GameData.levelAttemptCount++;
        SaveDataUtils.GameData.levelReviveCount = 0;
#endif
        SaveDataUtils.gameStrategy.SaveData();
        BizzaEventSystem.Emit(EventDefine.Item.GameStart);
    }

    // 打开胜利界面
    public static void OpenGameWinPanel(BizzaLevelResultType bizzaLevelResultType, LevelInfo levelInfo)
    {
#if BIZZA_REAL_WITHDRAW
        if (winResultHandled || UIModule.Instance == null)
        {
            return;
        }
        winResultHandled = true;
        SetAutomaticPopupFlowBlocked(true);
        // 异步加载结算界面前记录本关，避免读取到递增后的下一关进度。
        WinLevelNumber = SaveDataUtils.GameData.playerSelectedLv;
        FakeWithdrawalAnalytics.TrackLevelEnd(WinLevelNumber, success: true);
#endif
        CompleteAnalyticsLevel(success: true);
        BizzaEventSystem.Emit(EventDefine.Item.GameWin);
        BridgingUtil.OnOpenGameWinPanel();
#if BIZZA_REAL_WITHDRAW
        Real_GetRewardPanelUtil.OpenGetRewardPanel(DoubleGetRewardPanel.E_UseScene.WinPanel);
#else
        if (UIModule.Instance == null)
        {
            return;
        }
        UIModule.Instance.OpenPage<Action>(UIPageIds.WhiteWinPanel, null).Forget();
#endif
        SaveDataUtils.GameData.levelFailCount = 0;
#if BIZZA_REAL_WITHDRAW
        SaveDataUtils.GameData.levelAttemptCount = 0;
        SaveDataUtils.GameData.levelReviveCount = 0;
#endif
        if (WkyGameplayBridge.Instance == null)
        {
            SaveDataUtils.GameData.playerSelectedLv++;
        }
#if BIZZA_REAL_WITHDRAW
        // 只在实际胜利结算中计数，GM 打开奖励界面不会增加进度。
        SlotProgressUtil.AddPassedLevel();
#endif
    }

    // 打开失败界面
    public static void OpenGameLosePanel(BizzaLevelResultType bizzaLevelResultType, LoseReason loseReason, LevelInfo levelInfo)
    {
        SetAutomaticPopupFlowBlocked(true);
#if BIZZA_REAL_WITHDRAW
        FakeWithdrawalAnalytics.TrackLevelEnd(SaveDataUtils.GameData.playerSelectedLv, success: false);
#endif
        CompleteAnalyticsLevel(success: false, loseReason.ToString());
        SaveDataUtils.GameData.levelFailCount++;
        BizzaEventSystem.Emit(EventDefine.Item.GameLose);
        BridgingUtil.OnOpenGameLosePanel();
        UIModule.Instance.OpenPage(UIPageIds.LosePanel, loseReason, levelInfo).Forget();
    }

    public static void OpenGameRevivePanel(LoseReason loseReason, LevelInfo levelInfo)
    {
        SetAutomaticPopupFlowBlocked(true);
        BridgingUtil.OnOpenGameRevivePanel();
        UIModule.Instance.OpenPage(UIPageIds.LosePanel, loseReason, levelInfo).Forget();
    }

    // 复活结果
    public static void OnReviveResult(bool isRevive, LoseReason loseReason, LevelInfo levelInfo)
    {
        if (isRevive)
        {
            SetAutomaticPopupFlowBlocked(false);
#if BIZZA_REAL_WITHDRAW
            FakeWithdrawalAnalytics.BeginLevelAttempt();
#endif
            SaveDataUtils.GameData.currentReviveCount++;
#if BIZZA_REAL_WITHDRAW
            SaveDataUtils.GameData.levelReviveCount = SaveDataUtils.GameData.currentReviveCount;
#endif
            BizzaEventSystem.Emit(EventDefine.Item.GameRevive);
        }
        BridgingUtil.OnReviveResult(isRevive);
    }

    // 加载关卡,这里是直接读取生成关卡，关卡值直接使用存档数据，不需要重新传入
    public static void LoadGameLevel()
    {
        SetAutomaticPopupFlowBlocked(true);
#if BIZZA_REAL_WITHDRAW
        winResultHandled = false;
#endif
        BridgingUtil.LoadGameLevel();
    }

    // 游戏每次合成都会触发这个方法 传入剩余的合成次数和发生这次合成的位置
    public static void SynthesisLogic(int numRemaining, Vector3 synthesisPos)
    {
#if UNITY_EDITOR
        if (ChannelConfig.Instance.RecordMode)
        {
            return;
        }
#endif
#if BIZZA_REAL_WITHDRAW

        NumbericalStatistics.CheckGetDollar(synthesisPos);
        var uiModule = UIModule.Instance;
        if (uiModule == null || uiModule.GetPage(UIPageIds.GetRewardPanel) != null)
        {
            return;
        }

        NumbericalStatistics.CheckShowGetReward();
#endif
    }

    #region 道具使用

    // 道具使用 补充逻辑在 BridgingUtil 中补充
    public static bool UseProp_1()
    {
        bool userResult = BridgingUtil.PropUse_1();
        return userResult;
    }

    // 如果道具支持取消使用则调用这个方法， 补充逻辑在 BridgingUtil 中补充
    public static void PropUse_1_Over(bool breakFlow)
    {
        BridgingUtil.PropUse_1_Over(breakFlow);
        EmitPropUseOver(E_ItemType.GameProp_1, breakFlow);
    }

    public static bool UseProp_2()
    {
        bool userResult = BridgingUtil.PropUse_2();
        return userResult;
    }

    public static void PropUse_2_Over(bool breakFlow)
    {
        BridgingUtil.PropUse_2_Over(breakFlow);
        EmitPropUseOver(E_ItemType.GameProp_2, breakFlow);
    }

    public static bool UseProp_3()
    {
        bool userResult = BridgingUtil.PropUse_3();
        return userResult;
    }

    public static void PropUse_3_Over(bool breakFlow)
    {
        BridgingUtil.PropUse_3_Over(breakFlow);
        EmitPropUseOver(E_ItemType.GameProp_3, breakFlow);
    }

    public static bool UseProp_4()
    {
        bool userResult = BridgingUtil.PropUse_4();
        return userResult;
    }

    public static void PropUse_4_Over(bool breakFlow)
    {
        BridgingUtil.PropUse_4_Over(breakFlow);
        EmitPropUseOver(E_ItemType.GameProp_4, breakFlow);
    }

    public static bool UseProp_5()
    {
        bool userResult = BridgingUtil.PropUse_5();
        return userResult;
    }

    public static void PropUse_5_Over(bool breakFlow)
    {
        BridgingUtil.PropUse_5_Over(breakFlow);
        EmitPropUseOver(E_ItemType.GameProp_5, breakFlow);
    }

    private static void EmitPropUseOver(E_ItemType itemType, bool breakFlow)
    {
        if (!breakFlow)
        {
            BizzaGameAnalytics.TrackItemUse(
                itemType.ToString(),
                "prop",
                quantity: 1,
                source: "gameplay");
        }
        BizzaEventSystem.Emit(EventDefine.Item.PropUseOver, itemType, !breakFlow);
    }

    private static void BeginAnalyticsLevel()
    {
        if (SaveDataUtils.GameData == null)
        {
            return;
        }

        analyticsLevelId = SaveDataUtils.GameData.playerSelectedLv.ToString();
        int attempt = Mathf.Max(1, SaveDataUtils.GameData.levelFailCount + 1);
        analyticsLevelAttemptId = BizzaGameAnalytics.TrackLevelStart(analyticsLevelId, attempt);
    }

    private static void CompleteAnalyticsLevel(bool success, string failureReason = null)
    {
        if (string.IsNullOrEmpty(analyticsLevelId))
        {
            return;
        }

        if (success)
        {
            BizzaGameAnalytics.TrackLevelWin(analyticsLevelId, analyticsLevelAttemptId);
        }
        else
        {
            BizzaGameAnalytics.TrackLevelFail(
                analyticsLevelId,
                analyticsLevelAttemptId,
                failureReason);
        }

        analyticsLevelId = null;
        analyticsLevelAttemptId = null;
    }
    #endregion

}

public enum BizzaLevelResultType
{
    Win,
    ReviveWin,
    ReviveFail,
    Fail
}

