#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Channel;
using Bizza.Sdk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static AccountModule;

[Obfuz.ObfuzIgnore]
public class BadgeShow : MonoBehaviour
{
    public Image badgeImg;
    public List<Sprite> badges;

    public TMP_Text priceTxt;
    public List<WithDrawMissionSO> withdrawMissionsID;
    public List<WithDrawMissionSO> withdrawMissionsUS;

    public List<WithDrawMissionSO> withdrawMissions
    {
        get
        {
            if (AccountModule.CountryType == E_CountryType.ID)
            {
                return withdrawMissionsID;
            }
            else if (AccountModule.CountryType == E_CountryType.US || AccountModule.CountryType == E_CountryType.BR)
            {
                return withdrawMissionsUS;
            }
            else
            {
                return new List<WithDrawMissionSO>();
            }
        }
    }


    private void OnEnable()
    {
        OnRefresh();
        if (!isActiveAndEnabled) return;
        BizzaEventSystem.On(EventDefine.Game.RefreshDanItem, OnRefresh);
        BizzaEventSystem.On(EventDefine.Item.ItemChangedWithData, OnRefresh);
    }

    private void OnDisable()
    {
        BizzaEventSystem.Off(EventDefine.Game.RefreshDanItem, OnRefresh);
        BizzaEventSystem.Off(EventDefine.Item.ItemChangedWithData, OnRefresh);
    }

    private void OnRefresh()
    {
        if (!this) return;

        if (ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode && AccountModule.CountryType != E_CountryType.BR)
        {
            gameObject.SetActive(false);
            return;
        }

        float targetMoney = 0f;
        bool isFind = false;
        for (int i = 0; i < withdrawMissions.Count; i++)
        {
            var mission = withdrawMissions[i];
            if (SaveDataUtils.GameData.playerSelectedLv - 1 < mission.missions[0].conditionData.targetValue)
            {
                if (isFind)
                {
                    continue;
                }

                isFind = true;
            }
            else if (SaveDataUtils.WithDrawDanPanelData.GetClaimState(i) == E_RewardStateType.None)
            {
                SaveDataUtils.WithDrawDanPanelData.SetClaimState(i, E_RewardStateType.CanClaim);
            }
        }

        if (!isFind)
        {
            if (badgeImg != null)
            {
                if (withdrawMissions.Count <= 0)
                {
                    return;
                }
                badgeImg.sprite = badges[withdrawMissions.Count - 1];
            }
        }

        if (badgeImg != null)
        {
            badgeImg.sprite = badges[0];
            bool isClaimed = false;
            for (int i = 0; i < withdrawMissions.Count; i++)
            {
                var state = SaveDataUtils.WithDrawDanPanelData.GetClaimState(i);
                if (state == E_RewardStateType.Claimed)
                {
                    badgeImg.sprite = badges[i];
                    targetMoney = withdrawMissions[i].withdrawMoney;
                    isClaimed = true;
                }
            }
            
            priceTxt.text = $"{LanguageUtils.GetText("CurrencyToken")}{targetMoney}";
            if (!isClaimed)
            {
                priceTxt.text = $"{LanguageUtils.GetText("CurrencyToken")}{withdrawMissions[0].withdrawMoney}";
            }
        }

        
    }
}
#endif
