#if COMMONGAME
using System.Collections.Generic;

#if BIZZA_REAL_WITHDRAW
using Bizza.Sdk;
#endif

/// <summary>
/// 当前项目业务代码使用到的事件定义。
/// </summary>
public static partial class EventDefine
{
    public static partial class Item
    {
        public static readonly GameEvent ItemChanged = new();
    }

    public static partial class RealWithdraw
    {
        public static readonly GameEvent RefreshDailyMissionPageFalse = new();
        public static readonly GameEvent<string, double> PlayCurrentFly = new();
    }

    public static partial class Login
    {
        public static readonly GameEvent InitContentByCountry = new();
    }

    public static partial class AdEvent
    {
        public static readonly GameEvent RewardAdStart = new();
        public static readonly GameEvent<bool> RewardAdFinish = new();
        public static readonly GameEvent InterAdStart = new();
        public static readonly GameEvent InterAdFinish = new();
        public static readonly GameEvent SplashAdStart = new();
        public static readonly GameEvent SplashAdFinish = new();
        public static readonly GameEvent<bool> BannerShowOrHide = new();
    }

#if BIZZA_REAL_WITHDRAW
    public static partial class BizzaPlayerAction
    {
        public static readonly GameEvent PlayerEventInit = new();
        public static readonly GameEvent<string> IdentifierConfirm = new();
        public static readonly GameEvent PlayerProperty = new();
        public static readonly GameEvent<LevelInfo> LevelRevive = new();
        public static readonly GameEvent<BizzaLevelResultType, LevelInfo> LevelWin = new();
        public static readonly GameEvent<BizzaLevelResultType, LevelInfo> LevelFail = new();
        public static readonly GameEvent<string, double, string> GameCoinRevenueAdd = new();
        public static readonly GameEvent<string, double, string> GameCoinRevenueToPlayer = new();
        public static readonly GameEvent<BizzaLogModule.CoinRevenueReportData> GameCoinRevenueToController = new();
        public static readonly GameEvent<string> PlayerBuyItem = new();
        public static readonly GameEvent<string> PlayerOpenUI = new();
        public static readonly GameEvent<string> PlayerCloseUI = new();
        public static readonly GameEvent<string, string, double, bool> PlayerBuyItemEvent = new();
        public static readonly GameEvent<string, string, string, bool> PlayerBuyItemResult = new();
        public static readonly GameEvent<string, Dictionary<string, object>> PlayerRefresh = new();
        public static readonly GameEvent PlayEnterGame = new();
        public static readonly GameEvent<bool> GameRegister = new();
        public static readonly GameEvent PlayerOpera = new();
        public static readonly GameEvent<float, float, int, string> SelfGamePlayerOpera = new();
    }

#endif
}
#endif
