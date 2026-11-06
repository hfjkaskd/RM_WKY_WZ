#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bizza.Sdk;

//安卓平台展示广告时提示玩家广告后可以获得收益
public class AdShowHint : MonoBehaviour
{
    void OnEnable()
    {
        BizzaEventSystem.Set(EventDefine.AdEvent.InterAdStart, OnAdStart, true);
        BizzaEventSystem.Set(EventDefine.AdEvent.RewardAdStart, OnAdStart, true);
    }

    void OnDisable()
    {
        BizzaEventSystem.Set(EventDefine.AdEvent.InterAdStart, OnAdStart, false);
        BizzaEventSystem.Set(EventDefine.AdEvent.RewardAdStart, OnAdStart, false);
    }

    private void OnAdStart()
    {

#if BIZZA_REAL_WITHDRAW
        LogLogger.LogAdInfo($"开始提示看完广告有奖励 ： ");
        TipNativeBridge.ShowTips(LanguageUtils.GetText("ADShow_Hint"));
#endif
    }
}
#endif