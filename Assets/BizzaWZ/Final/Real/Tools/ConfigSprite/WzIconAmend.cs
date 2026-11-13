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

    private void Awake()
    {
        BizzaEventSystem.On(EventDefine.Login.InitContentByCountry, UpdateContent);
    }

    private void OnDestroy()
    {
        BizzaEventSystem.Off(EventDefine.Login.InitContentByCountry, UpdateContent);
    }

    private void OnEnable()
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        if (image == null && !TryGetComponent(out image)) return;
        image.enabled = image.sprite != null;
        if (ChannelConfig.Instance == null) return;
        ResetIconType();
        UIUtils.SetWzSprite(image, IconKey(iconType), isNativeSize);
    }

    private static string IconKey(E_WzIconType type) => type switch
    {
        E_WzIconType.MoneyIcon => "MoneyIcon",
        E_WzIconType.PieceMoney => "PieceMoney",
        E_WzIconType.StackMoney => "StackMoney",
        E_WzIconType.PileMoney => "PileMoney",
        E_WzIconType.PileWealth => "PileWealth",
        E_WzIconType.PileGold => "PileGold",
        E_WzIconType.HundredMoney => "HundredMoney",
        E_WzIconType.AbundanceWealth => "AbundanceWealth",
        E_WzIconType.MoneyEnhancement => "MoneyEnhancement",
        E_WzIconType.GoldCoin => "GoldCoin",
        E_WzIconType.BubbleCoin => "BubbleCoin",
        E_WzIconType.BubbleMoney => "BubbleMoney",
        _ => null
    };

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
