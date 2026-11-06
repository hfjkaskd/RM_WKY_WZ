#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class Real_GetRewardPanelUtil
{
    /// <summary>
    /// 打开恭喜获得界面
    /// </summary>
    /// <param name="e_UseScene"></param>
    /// <param name="action"></param>
    public static void OpenGetRewardPanel(DoubleGetRewardPanel.E_UseScene e_UseScene, Action<bool> action = null)
    {
        float moneyValue = WithdrawalUtil.GetDollarCountByReward();
        var money = new ItemEntry()
        {
            Type = E_ItemType.Dollar,
            Count = WithdrawalUtil.GetCustomizedFloatByCountryType(moneyValue)
        };

        var coin = new ItemEntry()
        {
            Type = E_ItemType.Gold,
            Count = 1000
        };

        UIModule.Instance.OpenPage<ItemEntry, ItemEntry, DoubleGetRewardPanel.E_UseScene, Action<bool>>
            (UIPageIds.GetRewardPanel,
            coin, money,
            e_UseScene,
            action).Forget();
    }

    public static void ShowRewardAd(E_AdPos pos, Action<bool> action)
    {
        float moneyValue = WithdrawalUtil.GetDollarCountByReward();
        var money = new ItemEntry()
        {
            Type = E_ItemType.Dollar,
            Count = WithdrawalUtil.GetCustomizedFloatByCountryType(moneyValue)
        };
        BizzaSdk.Ad.ShowRewardAd(pos.ToString(), money.Count, (Bizza.Sdk.ShowAdResult showAdResult) =>
        {
            bool success = showAdResult.success;
            LogLogger.LogAdInfo("激励广告成功");
            action?.Invoke(success);
        }
        );
    }
}
#endif