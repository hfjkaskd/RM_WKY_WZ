#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId ExchangeRatePanel = "ExchangeRatePanel";
}


public class ExchangeRatePanel : UIPageBase<ExchangeRateInfo>
{
    public TMP_Text beforeBlanceText;
    public TMP_Text beforeClashText;
    public TMP_Text nowBlanceText;
    public TMP_Text nowClashText;

    public TMP_Text beforeDefiniteText;
    public TMP_Text nowDefiniteText;

    public RectTransform root;

    [Header("按钮")]
    [SerializeField] private BizzaButton withdrawBtn;
    [SerializeField] private BizzaButton clickBtn;
    protected override void OnAwake()
    {

        withdrawBtn.onClick.AddListener(OnClickBtn);
        clickBtn.onClick.AddListener(CloseSelf);
    }

    protected override void OnOpen(ExchangeRateInfo info)
    {
        beforeBlanceText.text = $"{WithdrawalUtil.GetCustomizedValueByCountryType((float)info.beforeBlance)}";
        beforeClashText.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType((float)info.beforeClash)}";
        nowBlanceText.text = $"{WithdrawalUtil.GetCustomizedValueByCountryType((float)info.nowBlance)}";
        nowClashText.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType((float)info.nowClash)}";


        // var definiteInfo = info.ExchangeDefiniteRate();
        // beforeDefiniteText.text = definiteInfo.Item1;
        // nowDefiniteText.text = definiteInfo.Item2;
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    public void OnClickBtn()
    {
        CloseSelf();
        UIModule.Instance.OpenPage(UIPageIds.RealWithdrawPanel).Forget();
    }

    protected override void OnClose()
    {

    }
}

[Serializable]
public struct ExchangeRateInfo
{
    public double beforeBlance;
    public double beforeClash;
    public double nowBlance;
    public double nowClash;

    public double beforeRate;
    public double nowRate;

    public (string, string) ExchangeDefiniteRate()
    {
        string beforeInfo = "";
        string nowInfo = "";
        if (AccountModule.CountryType == AccountModule.E_CountryType.BR)
        {
            beforeInfo = $"100≈{LanguageUtils.GetText("CurrencyToken")}{beforeRate * 100}";
        }
        else if (AccountModule.CountryType == AccountModule.E_CountryType.ID)
        {
            beforeInfo = $"1000≈{LanguageUtils.GetText("CurrencyToken")}{beforeRate * 1000}";
        }
        else if (AccountModule.CountryType == AccountModule.E_CountryType.US)
        {
            beforeInfo = $"10000≈{LanguageUtils.GetText("CurrencyToken")}{beforeRate * 10000}";
        }

        if (AccountModule.CountryType == AccountModule.E_CountryType.BR)
        {
            nowInfo = $"100≈{LanguageUtils.GetText("CurrencyToken")}{nowRate * 100}";
        }
        else if (AccountModule.CountryType == AccountModule.E_CountryType.ID)
        {
            nowInfo = $"1000≈{LanguageUtils.GetText("CurrencyToken")}{nowRate * 1000}";
        }
        else if (AccountModule.CountryType == AccountModule.E_CountryType.US)
        {
            nowInfo = $"10000≈{LanguageUtils.GetText("CurrencyToken")}{nowRate * 10000}";
        }

        return (beforeInfo, nowInfo);
    }
}
#endif