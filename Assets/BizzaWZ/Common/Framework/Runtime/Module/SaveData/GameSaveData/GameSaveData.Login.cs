using System;
using Bizza;

public partial class GameSaveData
{
    public string playerGuid;
    public DateTime registerTime;
    public long lastLoginTimeStamp = 0;// 上次登录时间戳
    public int totalLoginDay = 0;// 总登录天数
    public int GuideRecord;
    public bool easyMode;
    public bool freeLuckyWheel = true;
    public bool freeAutoClick = true;
    public long onlineRewardLastLoginTime = int.MaxValue;

    public int GM_CarnivalDay;  //TODO 仅嘉年华GM跨天使用
    public int CollectSlot;

    public bool isNewDay = false;
    partial void OnNewDay();

    public void Login()
    {
        if (string.IsNullOrEmpty(playerGuid))
        {
            playerGuid = Guid.NewGuid().ToString();
        }

        if (registerTime == default)
        {
            registerTime = DateTime.Today;
        }
        isNewDay = false;
        var today = DateTime.Today;
        var prev = lastLoginTimeStamp.ToDateTime();
        if (today > prev)
        {
            totalLoginDay++;
            OnNewDay();
            isNewDay = true;

            itemCarCount = 0;  // 道具每日刷新 - 刷新小车 
            itemCharacterCount = 0; // 道具每日刷新 - 刷新角色 
        }
        lastLoginTimeStamp = DataTimeUtil.GetTimeStamp();

        GM_CarnivalDay = 0;
    }

    /// <summary>
    /// 获取实际物理的登录天数
    /// </summary>
    /// <returns></returns>
    public int GetLoginDayPhysical()
    {
        var day = (DateTime.Today - registerTime).Days + 1 + GM_CarnivalDay;

        if (day < 0)
            day = 0;

        return day;
    }
}
