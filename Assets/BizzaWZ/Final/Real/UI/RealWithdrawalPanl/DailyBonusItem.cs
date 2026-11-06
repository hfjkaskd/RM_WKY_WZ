#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

 
public class DailyBonusItem : MonoBehaviour
{
   public BonusRate bonusRate;

   public TMP_Text increaseText;
   public TMP_Text targetAdsCountText;
   public TMP_Text progressText;

   public Image progress;

   public GameObject goStateObj;
   public GameObject claimedStateObj;

   private Action refreshAction;
   /// <summary>
   /// 初始化DailyBonus
   /// </summary>
   /// <param name="rate">提升比例</param>
   /// <param name="isClaimed">是否已经领取</param>
   /// <param name="lookAds">观看广告数量</param>
   /// <param name="totalAds">该任务的目标广告数量</param>
   public void Init(Action refreshAction, int lookCount, int needCount, float ratio)
   {
      this.refreshAction = refreshAction;
      int lookAds = lookCount;
      int totalAds = needCount;
      bool isClaimed = lookAds >= totalAds;
      bonusRate.Init($"{ratio}%", false, false);
      increaseText.text = LanguageUtils.GetFormatText("RealWithdrawPanel_RewardIncrease", ratio); // LanguageUtils.GetText("DailyBonus_Increase");
      progressText.text = $"{lookAds}/{totalAds}"; // LanguageUtils.GetFormatText("DailyBonus_Looked", lookAds, totalAds);
      targetAdsCountText.text = LanguageUtils.GetFormatText("RealWithdrawPanel_WathADDesc", totalAds);

      progress.fillAmount = (float)lookAds / totalAds;

      goStateObj.SetActive(!isClaimed);
      claimedStateObj.SetActive(isClaimed);
   }

   public void OnClickGoStateBtn()
   {
      LogLogger.LogVerbose(BaseConst.LOG_Game,"OnClickGoStateBtn");
      // AdModule.OpenRewardAds(E_AdPos.DailyMission, OnRequest, true);
      //
      // void OnRequest(bool isSuccess, AccountModule.OceanShineAdRevenueResponse response)
      // {
      //    if (isSuccess)
      //    {
      //       refreshAction?.Invoke();
      //    }
      //    else
      //    {
      //       UIUtils.ShowTips("广告还未加载好，请过几分钟再试");
      //    }
      // }
      
      UIModule.Instance.ClosePage(UIPageIds.RealWithdrawPanel);
   }
}

#if UNITY_EDITOR
[Serializable]
public class DailyBonusItemData
{
   public string rate;
   public bool isClaimed;
   public int lookAds;
   public int totalAds;
}
#endif
#endif