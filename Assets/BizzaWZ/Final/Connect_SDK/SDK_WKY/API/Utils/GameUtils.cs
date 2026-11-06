using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if COMMONGAME
public static class GameUtils
{
    public static async UniTask DelayDo(Action action, float delayTime)
    {
        await UniTask.Delay((int)(delayTime * 1000));
        action?.Invoke();
    }
}

[Obfuz.ObfuzIgnore]
public enum E_PlatformType
{
    None,
    Editor,
    Oversea,
}

public enum BizzaLevelResultType
{
    Win,
    ReviveWin,
    ReviveFail,
    Fail
}

public struct LevelInfo
{
    public double levelProgress;
    public int levelTotalTarget;
    public int levelAchieveTarget;
    public int levelRemainingTarget; 
    public string levelPropUsedCount;
}

public static class BaseConst
{
    #region 日志

    public const string LOG_Info = "RunningTime";
    public const string LOG_Asset = "AssetPro_";
    public const string LOG_Game = "GAME_";
    public const string LOG_Withdrawal = "Withdrawal_";
    
    #endregion
}

  [Obfuz.ObfuzIgnore]
public enum E_AdPos
{
    None,
    Debug,
    SignIn,
    DailyTask,
    Flow,
    UITreasure,

    AddSlot,
    MatchAd,
    SettlementAd,
    GetItem1,
    GetItem2,
    GetItem3,
    AddCoin,
    Revive,
    DailyMission,
    TimePlay,
    Interstitial,
    GameWin,
    GameLose,
    GetReward,

    LocalTest,
    USSlot,
}
#endif
