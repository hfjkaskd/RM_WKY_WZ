#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public partial class UIPageIds
{
    public static readonly PageId DailyMissionPanel = "DailyMissionPanel";
}

 
public class DailyMissionPanel : UIPageBase, IAutoPopupContent
{
    public TMP_Text hintsTxt;
    public TMP_Text refreshTimeTxt;
    // public TMP_Text adsCountTxt;

    public TMP_Text claimedHint;

    public GameObject GoObj;
    public GameObject WithdrawObj;
    public GameObject ClaimedObj;


    private List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform> plats = new();

    public static bool isTestWithdraw = false;
    private float ecpmLimit = 1.5f;

    [Header("内容加载")]
    [SerializeField, Min(1f)] private float contentTimeoutSeconds = 20f;
    [SerializeField] private UIContentStateView contentView = new();
    private readonly UIContentLoadSession content = new();
    private AccountModule.RoutineTaskLookAdMoneyResponse pendingMission;
    private bool missionReceived, platformsReceived, adPending;

    // Manual opens keep the local retry view. If queued automatically, the existing
    // queue owns failure/retry and must not mark a failed load as successfully shown.
    public bool ContentReady => content.State == UIContentState.Ready || content.State == UIContentState.Empty;
    public bool ContentFailed => content.State == UIContentState.Failed;

    private void SetLinster(bool enable)
    {
        BizzaEventSystem.Set(EventDefine.WithDraw.RefreshDailyMissionPage, OnrequestDailyMission, enable);
        BizzaEventSystem.Set(EventDefine.WithDraw.RefreshDailyMissionPageFalse, OnRequestDailyMission, enable);
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, RefreshLocalizedContent, enable);
    }


     [Header("按钮")]
    [SerializeField] private BizzaButton closeBtn;
    [SerializeField] private BizzaButton goBtn;
    [SerializeField] private BizzaButton withdrawBtn;
    [SerializeField] private BizzaButton claimedBtn;
    protected override void OnAwake()
    {
        closeBtn.onClick.AddListener(OnClickClose);
        goBtn.onClick.AddListener(OnClickGoStateBtn);
        claimedBtn.onClick.AddListener(OnClickWithdrawedStateBtn);
        withdrawBtn.onClick.AddListener(OnClickWithdrawStateBtn);
        contentView.Bind(hintsTxt, goBtn);
        content.StateChanged += OnContentStateChanged;
    }
    
    protected override void OnOpen()
    {
        OnRequestData();
        SetLinster(true);
    }

    public void OnRequestData()
    {
        RequestContent(true);
    }

    public void OnrequestDailyMission()
    {
        RequestContent(true);
    }

    public void OnRequestDailyMission()
    {
        RequestContent(false);
    }

    private void RequestContent(bool update)
    {
        if (IsClosing || !gameObject.activeInHierarchy || adPending) return;
        pendingMission = null;
        missionReceived = platformsReceived = false;
        plats.Clear();
        int version = content.Begin(Time.unscaledTime, contentTimeoutSeconds);
        try
        {
            AccountModule.Instance.Request_RoutineTaskLookAdMoneyRequest(update, response =>
            {
                if (AcceptResponse(version)) OnResultCallback(version, response);
            }, false);
            if (!AcceptResponse(version)) return;
            AccountModule.Instance.Request_WithdrawalPageRequest(response =>
            {
                if (AcceptResponse(version)) Refresh(version, response);
            }, block: false);
        }
        catch (Exception exception)
        {
            if (AcceptResponse(version)) content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            Debug.LogException(exception);
        }
    }

    private bool IsCurrent(int version) => this != null && version == content.Version &&
        !IsClosing && gameObject.activeInHierarchy;

    private bool AcceptResponse(int version) => IsCurrent(version) && content.Accept(version, Time.unscaledTime);

    private void OnContentStateChanged(UIContentState state)
    {
        bool ready = state == UIContentState.Ready;
        GoObj.SetActive(!ready || pendingMission.Os_Ss == 0 || pendingMission.Os_Ss == 1);
        WithdrawObj.SetActive(ready && pendingMission.Os_Ss == 2);
        ClaimedObj.SetActive(ready && pendingMission.Os_Ss == 3);
        claimedHint.gameObject.SetActive(ready && pendingMission.Os_Ss == 3);
        refreshTimeTxt.gameObject.SetActive(ready);
        withdrawBtn.interactable = claimedBtn.interactable = ready && !adPending;
        contentView.Show(state);
        if (adPending) goBtn.interactable = false;
    }

    private void OnResultCallback(int version, FailHttpResponse<List<AccountModule.RoutineTaskLookAdMoneyResponse>> responses)
    {
        if (missionReceived) return;
        if (!responses.success || responses.data == null)
        {
            content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            return;
        }
        if (responses.data.Count == 0)
        {
            content.Complete(version, UIContentState.Empty, Time.unscaledTime);
            return;
        }
        var data = responses.data[0];
        if (data == null || data.Os_Ss < 0 || data.Os_Ss > 3 || data.Os_An < 0)
        {
            content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            return;
        }
        pendingMission = data;
        missionReceived = true;
        TryShowContent(version);
    }

    private void TryShowContent(int version)
    {
        if (!missionReceived || !platformsReceived) return;
        try
        {
            RefreshMissionText();
            content.Complete(version, UIContentState.Ready, Time.unscaledTime);
            timer = 0;
            UpdateRemainingTime();
        }
        catch (Exception exception)
        {
            content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            Debug.LogException(exception);
        }
    }

    private void RefreshMissionText()
    {
        // Read this response, not AccountModule's cache: the SDK updates that cache after our callback.
        int max = pendingMission.Os_An;
        int cur = Math.Clamp(pendingMission.Os_Ln, 0, max);
        hintsTxt.text = LanguageUtils.GetFormatText("DailyWithdrawMissionPanel_Hint", max,
            LanguageUtils.GetText("CurrencyToken") + WithdrawalUtil.GetCustomizedValueByCountryType((float)pendingMission.Os_My))
            + $" (<color=#9039D8>{cur}/{max}</color>)";
    }

    private void RefreshLocalizedContent()
    {
        OnContentStateChanged(content.State);
        if (content.State == UIContentState.Ready)
        {
            RefreshMissionText();
            UpdateRemainingTime();
        }
    }

    private float timer;
    private void Update()
    {
        if (IsClosing) return;
        content.Tick(Time.unscaledTime);
        if (content.State != UIContentState.Ready) return;
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f) // 每秒更新一次
        {
            timer = 0f;
            UpdateRemainingTime();
        }
    }

    private void UpdateRemainingTime()
    {
        DateTime now = DateTime.Now;
        DateTime tomorrow = now.Date.AddDays(1); // 明天 00:00
        TimeSpan remain = tomorrow - now;
        refreshTimeTxt.text = LanguageUtils.GetFormatText("DailyMissionPanel_RefreshTime", $"{remain.Hours:D2}:{remain.Minutes:D2}:{remain.Seconds:D2}");
    }

    public void OnClickGoStateBtn()
    {
        if (IsClosing || !gameObject.activeInHierarchy || adPending) return;
        if (content.State == UIContentState.Failed || content.State == UIContentState.Empty)
        {
            OnRequestData();
            return;
        }
        if (content.State != UIContentState.Ready || !GoObj.activeSelf) return;
        int version = content.Version;
        adPending = true;
        OnContentStateChanged(content.State);
        try
        {
            BizzaSdk.Ad.ShowRewardAd(E_AdPos.DailyMission.ToString(), 0, result =>
            {
                if (!IsCurrent(version) || !adPending) return;
                adPending = false;
                OnGoResponse(result);
            }, ecpmLimit);
        }
        catch
        {
            adPending = false;
            OnContentStateChanged(content.State);
            throw;
        }
        UIModule.Instance.m_curadvertistics--;
        SaveDataUtils.GameData.btnDailyTaskClick++;
    }

    private void OnGoResponse(Bizza.Sdk.ShowAdResult showAdResult)
    {
         bool success = showAdResult.success;
        if (success)
        {
            LogLogger.LogVerbose(LogTag.DailyAD, "用户点击了观看每日任务");
            SaveDataUtils.GameData.userLookDailyAdCount++;
            RequestContent(true);
        }
        else
        {
            //  UIUtils.ShowTips(LanguageUtils.GetText("DailyMissionPanel_NoAd"));
            UIModule.Instance.ClosePage(UIPageIds.DailyMissionPanel);
            LogLogger.LogVerbose(LogTag.DailyAD, $"获取服务器数据 OceanShineRoutineTaskLookAdMoneyResponse 失败 ");
        }
    }

    private void Refresh(int version, FailHttpResponse<AccountModule.OceanShineWithdrawalPageResponse> response)
    {
        if (platformsReceived) return;
        if (!response.success || response.data?.Os_Wwf == null || response.data.Os_Wwf.Count == 0 ||
            response.data.Os_Wwf.Exists(platform => platform == null || string.IsNullOrEmpty(platform.Os_Cn)))
        {
            LogLogger.LogVerbose(LogTag.DailyAD, "每日提现任务 ：为空 ");
            content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            return;
        }

        LogLogger.LogVerbose(LogTag.DailyAD, "每日任务 ： " + response.data.Os_Wwf);
        // Own the list so retrying cannot clear the SDK's shared cache.
        plats = new List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>(response.data.Os_Wwf);
        platformsReceived = true;
        TryShowContent(version);
    }

    public void OnClickWithdrawStateBtn()
    {
        if (content.State != UIContentState.Ready || IsClosing || !gameObject.activeInHierarchy || !WithdrawObj.activeSelf) return;
        bool isSelectPlatform = false;
        AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform plat = null;
        foreach (var _plat in plats)
        {
            if (AccountModule.CountryType == AccountModule.E_CountryType.US
                && _plat.Os_Cn.Equals(UIWithdrawalPanel.paypalInfo))
            {
                plat = _plat;
                break;
            }
            else if (AccountModule.CountryType == AccountModule.E_CountryType.BR
                     && _plat.Os_Cn.Equals(UIWithdrawalPanel.pagBankInfo))
            {
                plat = _plat;
                break;
            }
            else if (AccountModule.CountryType == AccountModule.E_CountryType.ID
                     && _plat.Os_Cn.Equals(UIWithdrawalPanel.danaInfo))
            {
                isSelectPlatform = true;
                plat = _plat;
                break;
            }
        }

        UIModule.Instance.OpenPage<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform, List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>, E_WithdrawType, Action, bool>
            (UIPageIds.UIWithdrawalPanel, plat, plats, E_WithdrawType.DailyMission, null, isSelectPlatform).Forget();
        SaveDataUtils.GameData.btnWithdrawClick++;
    }

    public void OnClickWithdrawedStateBtn()
    {
        if (content.State != UIContentState.Ready || IsClosing || !gameObject.activeInHierarchy || !ClaimedObj.activeSelf) return;
        UIUtils.ShowTips(LanguageUtils.GetText("DailyMissionPage_Claimed"));

    }

    public void OnClickClose()
    {
        UIModule.Instance.ClosePage(this);
    }

    protected override void OnClose()
    {
        content.Close();
        adPending = false;
        pendingMission = null;
        SetLinster(false);
    }

    private void OnDisable()
    {
        content.Close();
        adPending = false;
        SetLinster(false);
    }

    //编辑器下测试每日提现
    public void EditorTestDailyMission()
    {
        StartCoroutine(_EditorTestDailyMission());
    }

    private IEnumerator _EditorTestDailyMission()
    {
        int max = SaveDataUtils.GameData.userLookDailyAdCountMax;
        var cfg = ChannelConfig.Instance;
        var old = cfg.real_CustomConfig;
        for (int i = SaveDataUtils.GameData.userLookDailyAdCount; i <= max; i++)
        {
            cfg.real_CustomConfig.testECPM1000 = true;
            cfg.real_CustomConfig.TestECPMValue = Random.Range(200f, 250f);
            OnClickGoStateBtn();

            yield return new WaitForSeconds(5f);
            var btnGo = GameObject.Find("Rewarded(Clone)/Panel/MaxRewardedCloseButton");
            if (btnGo != null)
            {
                var btn = btnGo.GetComponent<Button>();
                btn.onClick?.Invoke();
            }
            yield return new WaitForSeconds(10f);
        }

        cfg.real_CustomConfig = old;
        Debug.LogError("============每日任务广告观看完成============");
    }
}

public static partial class EventDefine
{
    public static partial class WithDraw
    {
        public static GameEvent RefreshDailyMissionPage = new();
        public static GameEvent RefreshDailyMissionPageFalse = new();
    }
}
#endif
