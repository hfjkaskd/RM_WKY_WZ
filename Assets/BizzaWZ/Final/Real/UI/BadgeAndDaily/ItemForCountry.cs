#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Obfuz;
using Sirenix.OdinInspector;
using UnityEngine;
using static AccountModule;


public class ItemForCountry : MonoBehaviour
{
    public List<ShowUIType> pageOpenPangels = new List<ShowUIType>()
    {
        new ShowUIType()
        {
            pageType = UIPageIds.WithdrawDanPanel,
            countryList = new List<E_CountryType>(){E_CountryType.US, E_CountryType.ID},
            showObject = null
        },
        new ShowUIType()
        {
            pageType = UIPageIds.DailyMissionPanel,
            countryList = new List<E_CountryType>(){E_CountryType.BR},
            showObject = null
        }

    };

    [Header("按钮")]
    [SerializeField] private BizzaButton DailyMissonBtn;
    [SerializeField] private BizzaButton DanBtn;

    private void Awake()
    {
        BizzaEventSystem.On(EventDefine.Game.RefreshDanItem, OnRefresh);
        DailyMissonBtn.onClick.AddListener(() => { OnClickItem(); });
        DanBtn.onClick.AddListener(() => { OnClickItem(); });
    }


    public void OnRefresh()
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        if (!this) return;
        LogLogger.LogVerbose(BaseConst.LOG_Asset, "胸章Itemn 出现 " + AccountModule.CountryType);
        AccountModule.E_CountryType type = AccountModule.CountryType;

        foreach (var condition in pageOpenPangels)
        {
            condition.showObject?.SetActive(false);
        }

        foreach (var condition in pageOpenPangels)
        {
            foreach (var country in condition.countryList)
            {
                if (condition.showObject == null)
                {
                    LogLogger.LogVerbose(BaseConst.LOG_Asset, "胸章Itemn 为null 跳过 " + condition.pageType);
                    continue;
                }

                if (country == type)
                {
                    LogLogger.LogVerbose(BaseConst.LOG_Asset, "胸章Itemn 显示 跳过 " + country + " " + condition.showObject.name);
                    condition.showObject?.SetActive(true);
                    break;
                }
            }
        }
    }

    [Serializable]

    public struct ShowUIType
    {
        // 要打开的页面
        [LabelText("要打开的页面")] public string pageType;

        // 要显示的UI
        [LabelText("要显示的UI物体")] public GameObject showObject;

        // 在那些国家显示
        [LabelText("那些国家显示这个物体")] public List<AccountModule.E_CountryType> countryList;
    }

    [ObfuzIgnore(ObfuzScope.MethodName)]
    public void OnClickItem()
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        LogLogger.LogVerbose(BaseConst.LOG_Asset, "OnClickItem 正式模式");
        AccountModule.E_CountryType type = AccountModule.CountryType;
        foreach (var condition in pageOpenPangels)
        {
            foreach (var eType in condition.countryList)
            {
                if (eType == AccountModule.CountryType)
                {
                    UIModule.Instance.OpenPage(condition.pageType).Forget();
                    LogLogger.LogInfo($"打开 {condition.pageType} + {type}");
                    return;
                }
            }
        }
    }

    [Button("OnClickDan")]
    public void OnClickDan()
    {
        AccountModule.E_CountryType type = AccountModule.E_CountryType.ID;
        foreach (var condition in pageOpenPangels)
        {
            foreach (var eType in condition.countryList)
            {
                if (eType == type)
                {
                    UIModule.Instance.OpenPage(condition.pageType).Forget();
                    LogLogger.LogInfo($"打开 {condition.pageType} + {type}");
                    return;
                }
            }
        }
    }

    [Button("OnClickDaily")]

    public void OnClickDaily()
    {
        AccountModule.E_CountryType type = AccountModule.E_CountryType.BR;
        foreach (var condition in pageOpenPangels)
        {
            foreach (var eType in condition.countryList)
            {
                if (eType == type)
                {
                    UIModule.Instance.OpenPage(condition.pageType).Forget();
                    LogLogger.LogInfo($"打开 {condition.pageType} + {type}");
                    return;
                }
            }
        }
    }
}

public static partial class EventDefine
{
    public static partial class Game
    {
        public static GameEvent RefreshDanItem = new();
    }
}
#endif
