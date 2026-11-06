#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu(
    fileName = "WithdrawMissionConfig",
    menuName = "Withdraw/Mission Config"
)]
public class WithDrawMissionSO : ScriptableObject
{
    [LabelText("提现金额")]
    public float withdrawMoney;
    [LabelText("提现任务列表（按阶段）")]
    public List<WithdrawMissionData> missions = new();

    /// <summary>
    /// 根据阶段获取任务
    /// </summary>
    public WithdrawMissionData GetMissionByStateSafe(int state)
    {
        if (state < 0 || state >= missions.Count) return null;
        return missions[state];
    }
 

    /// <summary>
    /// 获取当前可提现的最小阶段
    /// </summary>
    public WithdrawMissionData GetNextAvailableMission()
    {
        return missions
            .FirstOrDefault(m => !m.IsCanWithdraw());
    }
    // public int GetNextAvailableMissionState(int curState)
    // {
    //     for (int i = curState + 1; i < missions.Count; i++)
    //     {
    //         if (!missions[i].IsCanWithdraw())
    //             return i;
    //     }
    //     return -1; // 没有
    // }

}

[Serializable]
public class WithdrawMissionData
{


    [LabelText("提现条件")]
    public WithdrawConditionData conditionData = new();

    #region Logic
    public string GetWithdrawDesc(float value)
    {
        if (conditionData == null)
            return LanguageUtils.GetText("WithdrawMission_AllMet");

        List<string> descList = new();

        string desc = GetSingleConditionDesc(conditionData, value);
        if (!string.IsNullOrEmpty(desc))
            descList.Add(desc);

        return descList.Count == 0
            ? LanguageUtils.GetText("WithdrawMission_AllMet")
            : string.Join("\n", descList);
    }
    
    public float GetProgress(float value)
    {
        switch (conditionData.condition)
        {
            case E_WithdrawCondition.None:
                return 0;
            case E_WithdrawCondition.Level:
                return value / (float)conditionData.targetValue;
            case E_WithdrawCondition.Money:
                return value / (float)conditionData.targetValue;
            case E_WithdrawCondition.Video:
                return value / (float)conditionData.targetValue;
            case E_WithdrawCondition.LoginTodayDayMath:
                return value / (float)conditionData.targetValue;
            case E_WithdrawCondition.WithDrawDanDollar:
                return value / (float)conditionData.targetValue;
            default:
                return 0;
        }
    }

    public float GetValueOfCondition()
    {
        switch (conditionData.condition)
        {
            case E_WithdrawCondition.None:
                return 0;
            case E_WithdrawCondition.Level:
                return SaveDataUtils.GameData.playerSelectedLv;
            case E_WithdrawCondition.Money:
                var itemEntryMoney = new ItemEntry()
                {
                    Type = E_ItemType.Dollar,
                    Count = ItemUtils.GetItemCount(E_ItemType.Dollar),
                };
                return ItemUtils.FormatCountFloat(itemEntryMoney);
            case E_WithdrawCondition.Video:
                return SaveDataUtils.GameData.totalFinishAdTimes;
            case E_WithdrawCondition.LoginTodayDayMath:
                return SaveDataUtils.WithDrawDanPanelData.LoginTodayMathCount;
            case E_WithdrawCondition.WithDrawDanDollar:
                var itemEntry = new ItemEntry()
                {
                    Type = E_ItemType.WithDrawDanDollar,
                    Count = ItemUtils.GetItemCount(E_ItemType.WithDrawDanDollar),
                };
                return ItemUtils.FormatCountFloat(itemEntry);
            default:
                return 0;
        }
    }

    public bool IsCanWithdraw()
    {
        float value = GetValueOfCondition();
        var data = conditionData;

        switch (data.condition)
        {
            case E_WithdrawCondition.None:
                return true;
            case E_WithdrawCondition.Level:
                return value >= data.targetValue;
            case E_WithdrawCondition.Money:
                {
                    var itemEntry = new ItemEntry()
                    {
                        Type = E_ItemType.Dollar,
                        Count = data.targetValue,
                    };
                    float targetValue = ItemUtils.FormatCountFloat(itemEntry);
                    return value >= targetValue;
                }
            case E_WithdrawCondition.Video:
                return value >= data.targetValue;
            case E_WithdrawCondition.LoginTodayDayMath:
                return value >= data.targetValue;
            case E_WithdrawCondition.WithDrawDanDollar:
                {
                    var itemEntry = new ItemEntry()
                    {
                        Type = E_ItemType.WithDrawDanDollar,
                        Count = data.targetValue,
                    };
                    float targetValue = ItemUtils.FormatCountFloat(itemEntry);
                    return value >= targetValue;
                }

            default:
                return false;
        }
    }

    private string GetSingleConditionDesc(WithdrawConditionData data, float value)
    {
        switch (data.condition)
        {
            case E_WithdrawCondition.Money:
                {
                    var itemEntryTarget = new ItemEntry()
                    {
                        Type = E_ItemType.Dollar,
                        Count = data.targetValue,
                    };
                    float targetValue = ItemUtils.FormatCountFloat(itemEntryTarget);
                    float lack = Mathf.Max(0, targetValue - value);
                    if (lack <= 0)
                    {
                        return "";
                    }

                    return LanguageUtils.GetFormatText("WithdrawMission_Desc_Money", $"{LanguageUtils.GetText("CurrencyToken") + targetValue}", $"{LanguageUtils.GetText("CurrencyToken") + lack}");
                }

            case E_WithdrawCondition.Level:
                {
                    float lack = Mathf.Max(0, data.targetValue - value);
                    return LanguageUtils.GetFormatText("WithdrawMission_Desc_Level", data.targetValue, lack);
                }

            case E_WithdrawCondition.Video:
                {
                    float lack = Mathf.Max(0, data.targetValue - value);
                    return LanguageUtils.GetFormatText("WithdrawMission_Desc_Video", data.targetValue, lack);
                }
            case E_WithdrawCondition.LoginTodayDayMath:
                {
                    float lack = Mathf.Max(0, data.targetValue - value);
                    return LanguageUtils.GetFormatText("WithdrawMission_Desc_TotalDayLevelPass", lack, WithdrawConditionData.TodayDayMathCount);
                }
            case E_WithdrawCondition.WithDrawDanDollar:
                {
                    var itemEntryTarget = new ItemEntry()
                    {
                        Type = E_ItemType.WithDrawDanDollar,
                        Count = data.targetValue,
                    };
                    float targetValue = ItemUtils.FormatCountFloat(itemEntryTarget);
                    float lack = Mathf.Max(0, targetValue - value);

                    return LanguageUtils.GetFormatText("WithdrawMission_Desc_WithDrawDanDollar", targetValue, lack);
                }

            default:
                return null;
        }
    }

    #endregion
}

  [Obfuz.ObfuzIgnore]
public enum E_WithdrawCondition
{
    None, // 无条件
    Level, // 关卡
    Money, // 金钱
    Video, // 视频
    LoginTodayDayMath, // 累计登录{0}天且每天通过{1}关
    WithDrawDanDollar, // 段位提现货币
}

[Serializable]
public class WithdrawConditionData
{
    [LabelText("条件类型")]
    public E_WithdrawCondition condition;

    [LabelText("目标值")]
    public float targetValue;
    /// 累计登录天数每天的通关数
    public const int TodayDayMathCount = 10;
}
#endif