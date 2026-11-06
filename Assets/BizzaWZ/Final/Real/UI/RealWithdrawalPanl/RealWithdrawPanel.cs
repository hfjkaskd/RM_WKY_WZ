#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Obfuz;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId RealWithdrawPanel = "RealWithdrawPanel";
}


public class RealWithdrawPanel : UIPageBase
{
    [Header("颜色样式")]
    public Color _color;
    public List<ColorReplace> _colorReplaceList = new List<ColorReplace>()
    {
        new ColorReplace
        {
            replaceKey = "progressLevelColor",
            replaceValue = "#006ED6FF"
        },
        new ColorReplace
        {
            replaceKey = "progressClashColor",
            replaceValue = "#009A74FF"
        },
        new ColorReplace
        {
            replaceKey = "progressRatioColor",
            replaceValue = "#EC6F00FF"
        },
    };

    [Header("提现按钮样式")]
    public Sprite normalSprite;
    public Sprite canWithdrawSprite;
    public Image btn_Image;
    [Space(5)]
    public string withdrawValueKeyColor = "#009870FF";
    public string withdrawChannelKeyColor = "#007789FF";
    [Header("提现栏")]
    public TMP_Text balanceTxt;
    public TMP_Text rateTxt; // 提现比例
    public TMP_Text clashTxt; // 可提现金额
    public TMP_Text hintTxt;
    public TMP_Text passLevelText;
    public WithdrawWay withdrawWayItem;
    public Transform withdrawWayRoot;
    public GameObject fingerObj;
    public GameObject canWithdrawFingerHint;

    public int currentLevel
    {
        get
        {
            if (ChannelConfig.Instance.real_CustomConfig.realWithdrawPassMode)
            {
                return SaveDataUtils.GameData.playerpassLevel;
            }
            else
            {
                return SaveDataUtils.GameData.playerSelectedLv;
            }
        }
    }


    [Space(5)]
    [Header("提现档次栏")]

    public GameObject levelObj;
    public WithdrawLevelItem withdrawLevelItem;
    public Transform withdrawLevelRoot;

    private WithdrawWay currentWay;
    private List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform> plats = new();

    public WithdrawHintPanel hintPanel;
    private const float UsProgressInfoHeight = 470f;
    private const float DefaultProgressInfoHeight = 320;
    [SerializeField] private RectTransform progressInfoRect;

    private bool isSelectPlatform = false;
    private WithdrawWay CurrentWay
    {
        set
        {
            if (currentWay != null)
            {
                currentWay.OnDeselect();
            }

            currentWay = value;
        }
        get => currentWay;
    }
    private List<WithdrawWay> _withdrawWayItems = new();
    private List<WithdrawLevelItem> _withdrawLevelItems = new();

    public WithdrawLevelItem CurrentWithdrawLevelItem;
    private WithdrawLevelItem NowWithdrawLevelItem;

    public void OnClickWithdrawItem(WithdrawLevelItem selectItem)
    {
        foreach (var item in _withdrawLevelItems)
        {
            item.SetSelect(false);
        }
        selectItem.SetSelect(true);
        CurrentWithdrawLevelItem = selectItem;
    }

    protected override void OnOpen()
    {
        isSelectPlatform = false;
        fingerObj.SetActive(false);
        RefreshProgressInfoHeight();
        OriginalCanWithdrawHandleHint();
        if (ChannelConfig.Instance.real_CustomConfig.realWithdrawPassMode)
        {
            passLevelText.text = LanguageUtils.GetFormatText("RealPage_PassLevel", $":{currentLevel}");
        }
        else
        {
            passLevelText.text = $"{LanguageUtils.GetFormatText("RealPage_CurrentLevel", currentLevel)}";
        }

        OnRefresh();
        SetLinster(true);
    }

    private void RefreshProgressInfoHeight()
    {
        float targetHeight = AccountModule.CountryType == AccountModule.E_CountryType.US
            ? UsProgressInfoHeight
            : DefaultProgressInfoHeight;

        progressInfoRect.anchoredPosition = new Vector2(progressInfoRect.anchoredPosition.x, targetHeight);
    }

    private void SetLinster(bool enable)
    {
        #if BIZZA_ENABLE_MAX && BIZZA_REAL_WITHDRAW
        BizzaEventSystem.Set(EventDefine.WithDraw.FingerShow, SetFinger, enable);
        BizzaEventSystem.Set(EventDefine.Item.ItemChanged, OnItemChanged, enable);
        BizzaEventSystem.Set(EventDefine.WithDraw.RefreshDailyRealPage, OnSeleFirstWay, enable);
        BizzaEventSystem.Set(EventDefine.WithDraw.WithdrawOver, OnRefreshMinWithdrawValue, enable);
        #endif
    }

    private void SetFinger(bool isShow)
    {
        if (this == null) return;
        fingerObj?.SetActive(isShow);
        OriginalCanWithdrawHandleHint();
    }

    private bool isUpdateUserInfo = false;
    private void OnRefreshMinWithdrawValue()
    {
        if (!this) return;
        isUpdateUserInfo = true;
        AccountModule.Instance.Request_WithdrawalPageRequest(RefreshForPlatform, true, false);
    }


    private void OnRefresh()
    {
        hintPanel.OnClickClose();
        AccountModule.Instance.Request_WithdrawalPageRequest(RefreshForPlatform);
    }

    private void OnItemChanged()
    {
        balanceTxt.text = WithdrawalUtil.GetCustomizedValueByCountryType((float)AccountModule.Instance.Os_Current_Uso.GetBalance());
        clashTxt.text = "≈" + LanguageUtils.GetText("CurrencyToken") + AccountModule.Instance.Get_S_Ewl();
        AccountModule.Instance.Request_WithdrawalPageRequest(RefreshForPlatform);
    }

    // private float timer = 0;
    // private void Update()
    // {
    //     // timer += Time.deltaTime;
    //     // if (timer >= 1f) // 每秒更新一次
    //     // {
    //     //     timer = 0f;
    //     //     UpdateRemainingTime();
    //     // }
    //     // UpdateRemainingTime();
    // }

    private void UpdateRemainingTime()
    {
        DateTime now = DateTime.Now;
        DateTime tomorrow = now.Date.AddDays(1);
        TimeSpan remaining = tomorrow - now;
        string time = remaining.ToString(@"hh\:mm\:ss");
    }

    private void ShowCanWithdrawHandle()
    {
        if (CurrentWay != null && AccountModule.Instance.Os_Current_Uso.Os_Ewl >= CurrentWay.data.Os_Mlt)
        {
            SetCanWithdrawHandleHint();
        }
        else
        {
            OriginalCanWithdrawHandleHint();
        }
    }

    private void SetCanWithdrawHandleHint()
    {
        canWithdrawFingerHint.SetActive(false);
        if (_withdrawWayItems == null) return;
        foreach (var way in _withdrawWayItems)
        {
            if (AccountModule.Instance.Os_Current_Uso.Os_Ewl < way.data.Os_Mlt)
            {
                continue;
            }
            canWithdrawFingerHint.SetActive(true);
            btn_Image.sprite = canWithdrawSprite;
            return;
        }
        btn_Image.sprite = normalSprite;
    }

    private void OriginalCanWithdrawHandleHint()
    {
        canWithdrawFingerHint.SetActive(false);
        btn_Image.sprite = normalSprite;
    }

    private void RefreshForPlatform(FailHttpResponse<AccountModule.OceanShineWithdrawalPageResponse> response)
    {
        if (!this)
        {
            return;
        }

        plats.Clear();
        if (response.success)
        {
            balanceTxt.text = WithdrawalUtil.GetCustomizedValueByCountryType((float)AccountModule.Instance.Os_Current_Uso.GetBalance());
            clashTxt.text = "≈" + LanguageUtils.GetText("CurrencyToken") + AccountModule.Instance.Get_S_Ewl();

            // 提现选择
            int withdrawCount = response.data.Os_Wwf.Count;
            _withdrawWayItems.SetCmptListCount(withdrawWayItem, withdrawWayRoot, withdrawCount);
            for (int i = 0; i < withdrawCount; i++)
            {
                plats.Add(response.data.Os_Wwf[i]);
                _withdrawWayItems[i].Init(OnSelectWithdrawWay, response.data.Os_Wwf[i]);
            }

            SetCanWithdrawHandleHint();
            // 提现等级
            int withdrawLevelCount = response.data.Os_Wr.Count;
            _withdrawLevelItems.SetCmptListCount(withdrawLevelItem, withdrawLevelRoot, withdrawLevelCount);
            for (int i = 0; i < withdrawLevelCount; i++)
            {
                _withdrawLevelItems[i].Init(this, i, withdrawLevelCount - 1, (float)AccountModule.Instance.Os_Current_Uso.GetBalance(), response.data.Os_Wr[i], SetRatio);
            }

            switch (AccountModule.CountryType)
            {
                case AccountModule.E_CountryType.ID:
                    isSelectPlatform = false;
                    var plat1 = _withdrawWayItems.Find(w => string.Equals(w.data.Os_Cn, UIWithdrawalPanel.danaInfo));
                    plat1?.OnSelect();
                    OnSelectWithdrawWay(plat1);
                    break;
                case AccountModule.E_CountryType.BR:
                    var plat = _withdrawWayItems.Find(w =>
                        string.Equals(w.data.Os_Cn, UIWithdrawalPanel.pagBankInfo));
                    plat?.OnSelect();
                    OnSelectWithdrawWay(plat);
                    break;
                case AccountModule.E_CountryType.US:
                    var plat2 = _withdrawWayItems.Find(w => string.Equals(w.data.Os_Cn, UIWithdrawalPanel.paypalInfo));
                    plat2?.OnSelect();
                    OnSelectWithdrawWay(plat2);
                    break;
            }
            AccountModule.Instance.Request_UserInfoRequest(isUpdateUserInfo, true, RefreshForWithdraw);
        }
        else
        {
            LogLogger.LogVerbose(BaseConst.LOG_Info, "平台刷新失败");
            UIModule.Instance.ClosePage(UIPageIds.RealWithdrawPanel);
        }

        void SetRatio(float rate, WithdrawLevelItem item)
        {
            NowWithdrawLevelItem = item;
            if (AccountModule.CountryType == AccountModule.E_CountryType.BR)
            {
                rateTxt.text = $"100≈{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(rate * 100)}";
            }
            else if (AccountModule.CountryType == AccountModule.E_CountryType.ID)
            {
                rateTxt.text = $"1000≈{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(rate * 1000)}";
            }
            else if (AccountModule.CountryType == AccountModule.E_CountryType.US)
            {
                rateTxt.text = $"10000≈{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(rate * 10000)}";
            }

            CurrentWithdrawLevelItem = item;
            string _clash = LanguageUtils.GetText("CurrencyToken") + AccountModule.Instance.Get_S_Ewl();
            RefreshProgress(item.StartLevel, item.EndLevel, item.ItemBalance, "", item.Index);
        }
    }




    private void RefreshForWithdraw(FailHttpResponse<AccountModule.OceanShineUserInfoResponse> response)
    {
        isUpdateUserInfo = false;
        if (response.success && response.data != null && response.data.Os_Uso != null)
        {
            if (AccountModule.Instance.Os_Current_Uso.Os_Ewl < currentWay.data.Os_Mlt)
            {
                float balance = (float)(currentWay.data.Os_Mlt - AccountModule.Instance.Os_Current_Uso.Os_Ewl);
                string minblance = LanguageUtils.GetText("CurrencyToken") + WithdrawalUtil.GetCustomizedValueByCountryType((float)currentWay.data.Os_Mlt);
                minblance = $"<color={withdrawValueKeyColor}>{minblance}</color>";
                string targetblance = LanguageUtils.GetText("CurrencyToken") + WithdrawalUtil.GetCustomizedValueByCountryType(balance); // balance.ToString("F2")
                targetblance = $"<color={withdrawValueKeyColor}>{targetblance}</color>";
                string payName = $"<color={withdrawChannelKeyColor}>{CurrentWay.data.Os_Me}</color>";
                hintTxt.text = $"{payName}: " + LanguageUtils.GetFormatText("RealWithdrawPage_Hint_NotBlance", minblance, targetblance);
            }
            else
            {
                string haveblance = LanguageUtils.GetText("CurrencyToken") + AccountModule.Instance.Get_S_Ewl();
                haveblance = $"<color={withdrawValueKeyColor}>{haveblance}</color>";
                string payName = $"<color={withdrawChannelKeyColor}>{CurrentWay.data.Os_Me}</color>";
                hintTxt.text = $"{payName}: " + LanguageUtils.GetFormatText("RealWithdrawPage_Hint_HaveBlance", haveblance);
            }
        }
        else
        {
            LogLogger.LogVerbose(BaseConst.LOG_Info, "体现平台提示错误");
            CloseSelf();
        }
    }

    protected override void OnClose()
    {
        fingerObj.SetActive(false);
        SetLinster(false);
    }

    public void OnSeleFirstWay()
    {
        if (!this) return;
        OnSelectWithdrawWay(currentWay);
    }

    public void OnSelectWithdrawWay(WithdrawWay way)
    {
        if (way == null) return;
        LogLogger.LogVerbose(BaseConst.LOG_Info, $"way ::: " + way.data.Os_Cn);
        CurrentWay = way;
        if (CurrentWay != null)
        {
            CurrentWay.OnSelect();
        }
        ShowCanWithdrawHandle();
        AccountModule.Instance.Request_UserInfoRequest(false, true, RefreshForWithdraw);
    }

    [ObfuzIgnore(ObfuzScope.MethodName)]
    public void OnClickWithdraw()
    {
        if (CurrentWay == null)
        {
            UIUtils.ShowLanguageTips("RealWithdrawPanel_ChoosePaymentMode");
            return;
        }
        SaveDataUtils.GameData.btnWithdrawClick++;

        AccountModule.Instance.Request_UserInfoRequest(false, true, OpenWithdrawPage);
        void OpenWithdrawPage(FailHttpResponse<AccountModule.OceanShineUserInfoResponse> response)
        {
            if (!response.success || response.data == null)
                return;
            if (response.success)
            {

                bool withdrawDoorsill = AccountModule.Instance.Os_Current_Uso.Os_Ewl < currentWay.data.Os_Mlt;
                if (withdrawDoorsill)
                {
                    float balance = (float)(currentWay.data.Os_Mlt - AccountModule.Instance.Os_Current_Uso.Os_Ewl);
                    hintPanel.Init(balance, (float)currentWay.data.Os_Mlt);
                    return;
                }

                if (IsSelectLockLevelItem())
                {
                    int targetLevel = ChannelConfig.Instance.real_CustomConfig.realWithdrawPassMode ? CurrentWithdrawLevelItem.StartLevel - 1 : CurrentWithdrawLevelItem.StartLevel;
                    hintPanel.Init(targetLevel, currentLevel);
                    return;
                }

                CurrentWithdrawLevelItem.SetSelect(false);
                CurrentWithdrawLevelItem = NowWithdrawLevelItem;
                NowWithdrawLevelItem.ResetContent();

                isSelectPlatform = AccountModule.CountryType == AccountModule.E_CountryType.ID;
                UIModule.Instance.OpenPage
                <AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform, List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>, E_WithdrawType, Action, bool>
                    (UIPageIds.UIWithdrawalPanel, CurrentWay.data, plats, E_WithdrawType.Real, null, isSelectPlatform).Forget();
            }
            else
            {

            }
        }
    }

    private bool IsSelectLockLevelItem()
    {
        if (CurrentWithdrawLevelItem.OnIsLock())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [Space(5)]
    [Header("提现进度")]
    public Image progressBar;
    public TMP_Text progressTxt;
    public GameObject progressObj;
    public TMP_Text progressHintTxt;
    public TMP_Text completeHintTxt;
    public SpriteAsset BSprite;
    public SpriteAsset ISprite;
    private string iconName = AccountModule.CountryType switch
    { // 0 是金币
        AccountModule.E_CountryType.BR => ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode ? "3" : "2",
        AccountModule.E_CountryType.ID => ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode ? "1" : "0",
        AccountModule.E_CountryType.US => ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode ? "5" : "4",
        _ => "0"
    };


    // 当用户主动点击时候
    // 初始化界面的时候
    // 点击提现按钮后

    //  开始值、结束值、可提现金额、汇率
    public void RefreshProgress(int _startLevel, int _endLevel, string _clash, string rate, int index) // 传入的就是 服务器返回的  开始值、结束值、可提现金额、汇率
    {
        int fillEndLevel = ChannelConfig.Instance.real_CustomConfig.realWithdrawPassMode ? _startLevel - 1 : _startLevel;
        int gapLevel = _startLevel - SaveDataUtils.GameData.playerSelectedLv;
        bool isCompleted = gapLevel <= 0;
        if (_startLevel <= 1)
        {
            isCompleted = true;
        }
        progressObj.gameObject.SetActive(!isCompleted);
        progressHintTxt.gameObject.SetActive(!isCompleted);
        completeHintTxt.gameObject.SetActive(isCompleted);
        if (isCompleted)
        {
            completeHintTxt.text = LanguageUtils.GetFormatText("WithdrawHintPanel_Hint3", AccountModule.Instance.Get_S_Ewl()).GetReplaceDesc(_colorReplaceList[1]);
        }
        else
        {
            progressBar.fillAmount = (float)currentLevel / fillEndLevel;
            progressTxt.text = $"{currentLevel}/{fillEndLevel}";
            progressHintTxt.text = LanguageUtils.GetFormatText("WithdrawHintPanel_Hint2",
                fillEndLevel,
                _clash,
                iconName,
                rate,
                gapLevel).GetReplaceDesc(_colorReplaceList[0]).GetReplaceDesc(_colorReplaceList[1]).GetReplaceDesc(_colorReplaceList[2]);

        }
    }


    [Button("OpenHintPanel")]
    private void OpenHintPanel()
    {
        float balance = (float)currentWay.data.Os_Mlt - 0;
        hintPanel.Init(balance, (float)currentWay.data.Os_Mlt);
    }

    public void OnClickQFA()
    {
        UIModule.Instance.OpenPage(UIPageIds.QFA).Forget();
    }

    public void OnClickHistory()
    {
        fingerObj?.SetActive(false);
        UIModule.Instance.OpenPage(UIPageIds.WithdrawHistory).Forget();
    }

    public void OnClickClose()
    {
        UIModule.Instance.ClosePage(UIPageIds.RealWithdrawPanel);
    }

    [Header("按钮")]
    [SerializeField] private BizzaButton faqBtn;
    [SerializeField] private BizzaButton closeBtn;
    [SerializeField] private BizzaButton historyBtn;

    [SerializeField] private BizzaButton withdrawBtn;

    protected override void OnAwake()
    {
        base.OnAwake();
        faqBtn.onClick.AddListener(() => { OnClickQFA(); });
        closeBtn.onClick.AddListener(() => { OnClickClose(); });
        historyBtn.onClick.AddListener(() => { OnClickHistory(); });
        withdrawBtn.onClick.AddListener(() => { OnClickWithdraw(); });
    }
}

public static partial class EventDefine
{
    public static partial class WithDraw
    {
        public static GameEvent<bool> FingerShow = new();
        public static GameEvent RefreshDailyRealPage = new();
        public static GameEvent WithdrawOver = new();
        public static GameEvent<bool> OnClickRealLevelWithdraw = new();
    }
}
#endif
