#if BIZZA_REAL_WITHDRAW
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Real_AdWatchProgress : MonoBehaviour
{
    [Header("UI")]
    public Image progressImg;
    public Image paymentImg;
    public TMP_Text hintTxt;

    [Header("Config")]
    public PaymentConfig payCfg;

    private void OnEnable()
    {
        RefreshWithdrawProgressState();
    }

    private void RefreshWithdrawProgressState() // 刷新提现进度状态
    {
        if (SaveDataUtils.GameData.userTodayLookAdCount >= Real_AdWatchCfg.maxRequestAdCount)
        {
            gameObject.SetActive(false);
            return;
        }
        // icon
        paymentImg.sprite = AccountModule.CountryType switch
        {
            AccountModule.E_CountryType.ID => payCfg.GetSmallSpriteByType(E_PayeeAccountType.Dana),
            AccountModule.E_CountryType.US => payCfg.GetSmallSpriteByType(E_PayeeAccountType.Paypal),
            AccountModule.E_CountryType.BR => payCfg.GetSmallSpriteByType(E_PayeeAccountType.PIX),
            _ => paymentImg.sprite
        };

        int beAdCount = Real_AdWatchCfg.GetRequiredAdCount();
        if (beAdCount == 0)
        {
            beAdCount = Real_AdWatchCfg.maxRequestAdCount;
        }
        float progressNum = (float)SaveDataUtils.GameData.userTodayLookAdCount / beAdCount;

        if (progressNum >= 1)
        {
            SaveDataUtils.GameData.dailyWithdrawProgressState = Mathf.Clamp(
                SaveDataUtils.GameData.dailyWithdrawProgressState + 1,
                1,
                Real_AdWatchCfg.maxAdWatchState
            );
            SaveDataUtils.gameStrategy.SaveData();
            beAdCount = Real_AdWatchCfg.GetRequiredAdCount();
            if (beAdCount == 0)
            {
                beAdCount = Real_AdWatchCfg.maxRequestAdCount;
            }
            progressNum = (float)SaveDataUtils.GameData.userTodayLookAdCount / beAdCount;
        }
        if (progressNum >= 1)
        {
            gameObject.SetActive(false);
            return;
        }

        progressImg.fillAmount = Mathf.Clamp01(progressNum);
        int gapAdCount = Mathf.Max(0, beAdCount - SaveDataUtils.GameData.userTodayLookAdCount);
        if (gapAdCount == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        float targetValue = Real_AdWatchCfg.GetTargetValue();
        hintTxt.text = LanguageUtils.GetFormatText("Real_WithdrawProgressHint", gapAdCount, WithdrawalUtil.GetCustomizedValueByCountryType(targetValue));
    }

}

public class Real_AdWatchCfg
{
    public const int maxAdWatchState = 2;
    public const int maxRequestAdCount = 51;
    private static AccountModule.E_CountryType e_CountryType => AccountModule.CountryType;
    public static float GetTargetValue()
    {
        int currentState = SaveDataUtils.GameData.dailyWithdrawProgressState;
        float targetValue = 0;
        switch (e_CountryType)
        {
            case AccountModule.E_CountryType.BR:
                if (currentState == 1)
                {
                    targetValue = 5.1f;
                }
                else if (currentState == 2)
                {
                    targetValue = 51f;
                }
                break;
            case AccountModule.E_CountryType.US:
                if (currentState == 1)
                {
                    targetValue = 7.7f;
                }
                else if (currentState == 2)
                {
                    targetValue = 77f;
                }
                break;
            case AccountModule.E_CountryType.ID:
                if (currentState == 1)
                {
                    targetValue = 1688f;
                }
                else if (currentState == 2)
                {
                    targetValue = 16888f;
                }
                break;
        }
        return targetValue;
    }

    public static int GetRequiredAdCount()
    {
        double maxECPM = AccountModule.m_Temp_ShowECPM;
        maxECPM = Math.Round(maxECPM, 2, MidpointRounding.AwayFromZero);
        if (maxECPM <= 0)
        {
            return maxRequestAdCount;
        }
        float USExchangeRate = 1;
        float IDExchangeRate = 17000;
        float BRExchangeRate = 5;

        float RatioOfDivision = 0.5f;
        float targetValue = GetTargetValue();

        float targetExchangeRate = e_CountryType switch
        {
            AccountModule.E_CountryType.US => USExchangeRate,
            AccountModule.E_CountryType.ID => IDExchangeRate,
            AccountModule.E_CountryType.BR => BRExchangeRate,
        };

        int adRequestCount = (int)Math.Round(
            targetValue / (((maxECPM / 1000) * targetExchangeRate) * RatioOfDivision)
        );
        LogLogger.LogAdInfo("玩家计算出的广告次数: " + adRequestCount);
        adRequestCount = Mathf.Clamp(adRequestCount, 0, maxRequestAdCount);
        LogLogger.LogAdInfo("玩家还需要看的广告次数: " + adRequestCount);
        return adRequestCount;
    }
}
#endif