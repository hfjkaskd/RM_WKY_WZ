#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using cfg;
using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore]
public class WzIconAmend : MonoBehaviour
{
    public E_WzIconType iconType;
    public Image image;
    public bool isNativeSize = false;

    private bool isInit = false;

    private void Awake()
    {
        BizzaEventSystem.On(EventDefine.Login.InitContentByCountry, UpdateContent);
    }

    private void OnEnable()
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        if (!this || isInit)
        {
            return;
        }
        if (Tables.Instance == null || Tables.Instance.TblCommonWzTexture == null 
            || Tables.Instance.TblCommonWzTexture.DataMap == null)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Asset, "WzIconAmend - 未初始化");
            return;
        }
        if (image == null && TryGetComponent(out image) == false)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Asset, "未设置图片");
            return;
        }

        ResetIconType();
        // isInit = true;
        UIUtils.SetWzSprite(image, iconType.ToString(), isNativeSize);
    }

    private void ResetIconType()
    {
        if (!ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
        {
            return;
        }

        if (iconType == E_WzIconType.GoldCoin)
        {
            iconType = E_WzIconType.StackMoney;
            return;
        }

        if (iconType == E_WzIconType.PileGold)
        {
            iconType = E_WzIconType.HundredMoney;
            return;
        }

        if (iconType == E_WzIconType.PileWealth)
        {
            iconType = E_WzIconType.HundredMoney;
            return;
        }

        if (iconType == E_WzIconType.MoneyEnhancement)
        {
            iconType = E_WzIconType.StackMoney;
            return;
        }

        // if (iconType == E_WzIconType.BubbleCoin)
        // {
        //     // iconType = E_WzIconType.BubbleMoney;
        //     return;
        // }
    }

}

public static partial class EventDefine
{
    public static class Login
    {
        public static GameEvent InitContentByCountry = new();
    }
}
#endif
