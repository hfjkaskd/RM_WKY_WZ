#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
public static partial class SaveDataUtils
{
    public static readonly DataStrategy<WithDrawDanPanelSaveData> WithDrawDanPanelStrategy = new();
    public static WithDrawDanPanelSaveData WithDrawDanPanelData => WithDrawDanPanelStrategy.Data;
}
   [Obfuz.ObfuzIgnore]
public enum E_RewardStateType
{
    None,
    CanClaim,
    Claimed,
}
 
public class WithDrawDanPanelSaveData : ISaveData
{
    public List<E_RewardStateType> taskClaimStates = new List<E_RewardStateType>();
    public int curStageIndex;
    public int LoginTodayMathCount = 0; // 累计登录天数且每天进行n次合成
    public bool IsTodayAdd = false;
    public int TodayMathCount = 0;

    public bool IsClaimed(int index)
    {
        if (index < 0 || index >= taskClaimStates.Count)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Game,$"WithDrawDanPanelSaveData IsClaimed index out of range {index}");
            return true;
        }
        return taskClaimStates[index] == E_RewardStateType.Claimed;
    }
    public E_RewardStateType GetClaimState(int index)
    {
        if (index < 0 || index >= taskClaimStates.Count)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Game,$"WithDrawDanPanelSaveData IsClaimed index out of range {index}");
            return E_RewardStateType.None;
        }
        return taskClaimStates[index];
    }
    public void SetClaimState(int index, E_RewardStateType state)
    {
        if (index < 0 || index >= taskClaimStates.Count)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Game,$"WithDrawDanPanelSaveData SetClaimed index out of range {index}");
            return;
        }
        taskClaimStates[index] = state;
    }
    public void SetStageIndex(int index)
    {
        curStageIndex = index;
        SaveDataUtils.WithDrawDanPanelStrategy.SaveData();
    }

    public void AfterLoadData()
    {
        BizzaEventSystem.On(EventDefine.RealWithdraw.OnGameWin, () =>
        {
            TodayMathCount++;
            if (!IsTodayAdd && TodayMathCount >= WithdrawConditionData.TodayDayMathCount)
            {
                IsTodayAdd = true;
                LoginTodayMathCount++;
            }
        });
        BizzaEventSystem.On(EventDefine.Time.SystemNewDay, () =>
        {
            IsTodayAdd = false;
            SaveDataUtils.WithDrawDanPanelData.TodayMathCount = 0;
        });
    }

    public void BeforeSave()
    {
    }

    public void InitData()
    {
        taskClaimStates.Clear();
        taskClaimStates.CompleteList(7, E_RewardStateType.None);
    }
}
#endif