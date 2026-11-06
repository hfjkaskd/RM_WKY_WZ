#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using cfg;
using Newtonsoft.Json;
using UnityEngine;



public partial class GameSaveData : ISaveData
{
    public int playerPassLv = 0;
}

public static class SlotProgressUtil
{
    public const int RequiredPassedLevels = 5;

    public static int Current => SaveDataUtils.GameData == null
        ? 0
        : Mathf.Clamp(SaveDataUtils.GameData.playerPassLv, 0, RequiredPassedLevels);

    public static bool CanFreeSpin => Current >= RequiredPassedLevels;

    public static void AddPassedLevel()
    {
        SetProgress(Current + 1);
    }

    public static void SetProgress(int value)
    {
        if (SaveDataUtils.GameData == null) return;

        SaveDataUtils.GameData.playerPassLv = Mathf.Clamp(value, 0, RequiredPassedLevels);
        SaveDataUtils.Save();
        BizzaEventSystem.Emit(EventDefine.CustomGameEvent.SlotProgressChanged);
    }
}


// 主要涉及 引导和提现相关 的存档

public partial class GameSaveData : ISaveData
{
    #region 客服相关

    public List<ChatInfo> chatInfos = new List<ChatInfo>();

    public bool serviceAlter = false;
    public int serviceInfoCount = 0;

    #endregion

    #region 广告相关

    public int userTodayLookAdCount;
    public int dailyWithdrawProgressState;
    public DateTime lastGetDailyRewardTime;
    public int totalFinishAdTimes;

    #endregion

    #region 提现相关

    public bool fakeWithdrawPanelFirstWithdraw = false;
    #endregion

    

    public int userLastLoginLookAdCount;
    public int userLastLoginLookRewardAdCount;
    public int userLastLoginLookInsertAdCount;
    public int totalbeLookAdCount;
    public double totalAdEcpmValue;
    public int btnDailyTimeClick;
    public int btnWithdrawClick;
    public int btnDailyTaskClick;
    public int tutorialStep;
    public long levelStartTime;
    public long lastWithdrawTime;
    public int OperaStep;

    public int levelAttemptCount;
    public int levelReviveCount;

}
#endif


