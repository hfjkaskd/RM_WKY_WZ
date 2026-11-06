#if BIZZA_REAL_WITHDRAW
using Obfuz;

public static partial class EventDefine
{
#if BIZZA_REAL_WITHDRAW
    public static class RealWithdraw
    {
        public static GameEvent OnGameWin = new();
        public static GameEvent RefreshDailyMissionPage = new();
        public static GameEvent RefreshDailyMissionPageFalse = new();
        public static GameEvent<FailHttpResponse<AccountModule.OceanShineLoginResponse>> Response_LoginResponse = new();
        public static readonly GameEvent<string, double> PlayCurrentFly = new();
        public static readonly GameEvent Response_UserReachReportResponse = new();
    }
#endif
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
