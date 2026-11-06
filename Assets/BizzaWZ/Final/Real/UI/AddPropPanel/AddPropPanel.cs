#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId AddPropPanel = "AddPropPanel";
}
public class AddPropPanel : UIPageBase<E_ItemType>
{
    public BizzaButton adBuyBtn;
    public BizzaButton closeBtn;
    public Image propIcon;
    public TMP_Text propName;
    public TMP_Text limitTxt;
    public Sprite busAwaySortPropIcon;
    public Sprite busAwayShufflePropIcon;

    private E_ItemType _itemType;
    private PropConfigSO propConfigSO;

    void Awake()
    {
        propConfigSO = PropConfigSO.Instance;
        adBuyBtn.onClick.AddListener(() =>
        {
            var value = WithdrawalUtil.GetDollarCountByReward();
            BizzaSdk.Ad.ShowRewardAd(_itemType.ToString(), WithdrawalUtil.GetCustomizedFloatByCountryType(value), OnAdBuyFinish);
            UIModule.Instance.m_curadvertistics--;
        });

        closeBtn.onClick.AddListener(() => { CloseSelf(); });
    }

    private void OnAdBuyFinish(Bizza.Sdk.ShowAdResult showAdResult)
    {
        bool success = showAdResult.success;
        if (success)
        {
            //NumbericalStatistics.AddPropTimesByEnum(_itemType);
            AddPropAndClose();
        }
    }

    private void AddPropAndClose()
    {
        ItemUtils.AddItem(_itemType, 1, true, true, propIcon.transform.position, false);
        CloseSelf();
    }

    protected override void OnOpen(E_ItemType itemType)
    {
        _itemType = itemType;
        var config = propConfigSO.GetPropConfigInfo(itemType);
        if (config != null)
        {
            propIcon.sprite = config.propIcon;
        }
        propName.text = LanguageUtils.GetText("ItemName_" + itemType.ToString());
        var _propUseTimes = NumbericalStatistics._propUseTimes;
        var maxTimes = config.preLimitNum;
        var curTimes = _propUseTimes[itemType];
        limitTxt.text = LanguageUtils.GetFormatText("Limit_Tip", curTimes, maxTimes);
    }

    protected override void OnClose()
    {
    }
}
#endif
