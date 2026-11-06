#if BIZZA_REAL_WITHDRAW
public partial class GameSaveData
{
    public int gameTimeSecondsWithoutAd;
    public int userLookAdCount;
    public int userLookDailyAdCount;
    public int userLookDailyAdCountMax;

    partial void OnNewDay()
    {
        dailyWithdrawProgressState = 1;
        userTodayLookAdCount = 0;
        userLookAdCount = 0;
        userLookDailyAdCount = 0;
        userLookDailyAdCountMax = 100;
    }
}
#endif
