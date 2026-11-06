#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class WithdrawLevelItem : MonoBehaviour
{
    public TMP_Text levelTxt;
    public TMP_Text rateText;

    public TMP_Text balanceText;

    public GameObject shadowObj;
    public GameObject selectedObj;
    public GameObject selectObj;
    private bool isLock;

    public Material material;
    public Image[] images;

    private int startLevel; public int StartLevel => startLevel;
    private int endLevel; public int EndLevel => endLevel;
    public int gapLevel => startLevel - SaveDataUtils.GameData.playerSelectedLv;

    private RealWithdrawPanel page;
    private string Itembalance; public string ItemBalance => Itembalance;
    private float baseRate = 2.5f;
    private double rateValue;
    private int index; public int Index => index;

    [Header("按钮")]
    [SerializeField] private BizzaButton clickBtn;

    private void Awake()
    {
        clickBtn.onClick.AddListener(() => { OnClick(); });
    }

    /// <summary>
    /// index 当前的序列、balance 余额、data 提现的数据、ratioHint 点击的回调 
    /// </summary>
    public void Init(RealWithdrawPanel page, int index, int maxIndex, float balance, AccountModule.OceanShineWithdrawalPageResponse.WithdrawalRatio data, Action<float, WithdrawLevelItem> ratioHint)
    {
        this.page = page;
        this.index = index;
        selectedObj.gameObject.SetActive(false);
        shadowObj.gameObject.SetActive(false);
        int currentLevel = SaveDataUtils.GameData.playerSelectedLv;
        startLevel = data.Os_Ben;
        endLevel = data.Os_End;
        double rate = data.Os_Wro;

        bool isLevelInSection = currentLevel >= startLevel && currentLevel <= endLevel; // 玩家当前等级是否在区间内 --- 点亮
        bool isLevelExSection = currentLevel > endLevel; // 玩家当前等级是否超出区间 --- 灰色
        bool isLevelNoSection = currentLevel < startLevel; // 玩家当前等级是否在区间外 -- 不可触发

        if (index == maxIndex && isLevelInSection)
        {
            isLevelInSection = true;
        }
        if (AccountModule.CountryType == AccountModule.E_CountryType.ID)
        {
            rateValue = rate * 1000f / baseRate;
        }
        else if (AccountModule.CountryType == AccountModule.E_CountryType.US)
        {
            rateValue = rate * 10000f / baseRate;
        }
        else if (AccountModule.CountryType == AccountModule.E_CountryType.BR)
        {
            rateValue = rate * 100f / baseRate;
        }

        if (AccountModule.CountryType == AccountModule.E_CountryType.ID)
        {
            rateText.text = $"{rateValue/10:F1}X";
        }
        else
        {
            rateText.text = $"{rateValue:F1}X";
        }

        float itemBalance = (float)Math.Round((double)(balance * rate), 2, MidpointRounding.AwayFromZero);
        Itembalance = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(itemBalance)}";
        balanceText.text = Itembalance;

        isLock = isLevelNoSection;
        shadowObj.SetActive(isLock);
        SetSelect(isLevelInSection);
        Selected(isLevelExSection, isLevelInSection);

        if (index == 0)
        {
            levelTxt.text = $"{LanguageUtils.GetFormatText("RealPage_CurrentLevel", startLevel)}";
        }
        else
        {
            if (ChannelConfig.Instance.real_CustomConfig.realWithdrawPassMode)
            {
                levelTxt.text = $"{LanguageUtils.GetFormatText("RealPage_PassLevel", startLevel - 1)}";
            }
            else
            {
                levelTxt.text = $"{LanguageUtils.GetFormatText("RealPage_CurrentLevel", startLevel)}";
            }
        }

        if (isLevelInSection)
        {
            balanceText.text = $"{LanguageUtils.GetText("CurrencyToken")}{AccountModule.Instance.Get_S_Ewl()}";
            ratioHint?.Invoke((float)rate, this);
        }
    }

    public void OnClick()
    {
        page.OnClickWithdrawItem(this);
        float quota = (float)rateValue * baseRate;
        string quotaValue = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(quota)}";
        page.RefreshProgress(startLevel, endLevel, Itembalance, quotaValue, Index);
    }

    public void ResetContent()
    {
        selectObj.gameObject.SetActive(true);
        float quota = (float)rateValue * baseRate;
        if (AccountModule.CountryType == AccountModule.E_CountryType.ID)
        {
            quota = quota * 10;
        }
        else if (AccountModule.CountryType == AccountModule.E_CountryType.US)
        {
            quota = quota * 100;
        }
        string quotaValue = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(quota)}";
        page.RefreshProgress(startLevel, endLevel, Itembalance, quotaValue, Index);
    }

    public void SetSelect(bool isSelect)
    {
        selectObj.gameObject.SetActive(isSelect);
    }

    public void Selected(bool isSelected, bool isLevelInSection)
    {
        if (isLevelInSection) return;
        if (!isSelected)
        {
            shadowObj.SetActive(true);
            return;
        }

        foreach (var image in images)
        {
            image.material = material;
        }
        selectedObj.SetActive(true);
    }

    public bool OnIsLock()
    {
        return isLock;
    }
}

#if UNITY_EDITOR
[Serializable]
public class WithdrawLevelItemData
{
    public string minLevel;
    public string maxLevel;
    public string rate;
    public Sprite sprite;
}
#endif
#endif