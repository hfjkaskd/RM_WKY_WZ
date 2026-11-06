#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using Obfuz;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId GetRewardPanel = "GetRewardPanel";
}


public class GetRewardPanel : UIPageBase<ItemEntry, ItemEntry, DoubleGetRewardPanel.E_UseScene, Action<bool>>
{
    public BizzaButton claimBtn;
    public BizzaButton closeBtn;
    public ItemEntry itemA;
    public ItemEntry itemB;
    // private DoubleGetRewardPanel.E_UseScene doubleGetRewardPanel;
    public GameObject LevelObj;
    public TMP_Text itemATxt;
    public TMP_Text itemBTxt; // 另一个货币的
    public TMP_Text levelTxt;
    public Transform itemAPos;
    public Transform itemBPos;
    public GameObject levelTips;
    public TMP_Text noThanksText;
    public TMP_Text rewardText;

    private string iconName = AccountModule.CountryType switch
    {
        AccountModule.E_CountryType.BR => "3",
        AccountModule.E_CountryType.ID => "1",
        AccountModule.E_CountryType.US => "5",
        _ => "3"
    };

    public BonusRate bonusRate;
    public WithdrawProgress progress;

    public GameObject MaxDollarTip;

    private DoubleGetRewardPanel.E_UseScene _useScene;

    private bool isLookAd = false;
    private bool isNoCD;

    // 每次打开独立保存领取状态，旧广告回调不能关闭复用后的新界面。
    private sealed class WinClaim
    {
        public int LevelNumber; // 本次胜利所属关卡，避免关闭时读取到下一关进度。
        public bool Started;
        public bool Finished;
        public bool PageClosed;
        public bool HasDollar;
        public float NormalRewardCount;
        public Vector3 RewardPosition;
        public Action<bool> Callback;
    }

    private WinClaim winClaim;
    private static bool HasDollar => !ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode;

    void Awake()
    {
        noThanksText.spriteAsset = commonSpriteAsset;

        claimBtn.onClick.AddListener(() => { LookAd(); });

        closeBtn.onClick.AddListener(() =>
        {
            WinClaim claim = winClaim;
            if (claim != null)
            {
                if (!TryBeginWinClaim(claim)) return;
                try
                {
                    NumbericalStatistics.CheckCloseGetReward(E_AdPos.GetReward, dollarCount, null,
                        levelOverride: claim.LevelNumber);
                }
                finally
                {
                    FinishWinClaim(claim, true);
                }
                return;
            }

            LogLogger.LogInfo("胜利关闭广告 " + isLookAd);
            NumbericalStatistics.CheckCloseGetReward(E_AdPos.GetReward, dollarCount, null);
            CloseSelf();
        });

        void LookAd()
        {
#if BIZZA_REAL_WITHDRAW
            WinClaim claim = winClaim;
            if (claim != null)
            {
                if (!TryBeginWinClaim(claim)) return;
                if (!BizzaSdk.Ad.Inited)
                {
                    FinishWinClaim(claim, true, false);
                    return;
                }

                try
                {
                    // 由 SDK 保留“插屏补充激励”；失败兜底不累计普通领取关闭次数。
                    BizzaSdk.Ad.ShowRewardAd(E_AdPos.AddCoin.ToString(), HasDollar ? itemB.Count : 0,
                        result => FinishWinClaim(claim, !result.success, result.success));
                    UIModule.Instance.m_curadvertistics--;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    FinishWinClaim(claim, true, false);
                }
                return;
            }

            BizzaSdk.Ad.ShowRewardAd(E_AdPos.AddCoin.ToString(), HasDollar ? itemB.Count : 0, (showAdResult) =>
             {
                 if (this == null || !this || gameObject == null) return;
                 bool success = showAdResult.success;
                 // 真正成功
                 callback?.Invoke(success);
                 isLookAd = success;
                 CloseSelf();
             });
             UIModule.Instance.m_curadvertistics--;
#else
#endif
        }
    }

    protected override void OnClose()
    {
        callback = null;
        if (winClaim != null)
        {
            winClaim.PageClosed = true;
            // 原生关闭按普通奖励处理；广告进行中交给该次广告回调结算。
            if (!winClaim.Started) FinishWinClaim(winClaim, true);
        }
    }

    private bool TryBeginWinClaim(WinClaim claim)
    {
        if (claim.Started || claim.Finished || claim.PageClosed) return false;

        claim.Started = true;
        isRecover = true;
        claimBtn.interactable = false;
        closeBtn.interactable = false;
        return true;
    }

    private void FinishWinClaim(WinClaim claim, bool grantNormalReward, bool? adSuccess = null)
    {
        if (claim.Finished) return;
        claim.Finished = true;

        try
        {
            if (adSuccess.HasValue && this != null && ReferenceEquals(winClaim, claim) && !claim.PageClosed)
            {
                isLookAd = adSuccess.Value;
                claim.Callback?.Invoke(adSuccess.Value);
            }
        }
        finally
        {
            claim.Callback = null;
            if (grantNormalReward && claim.HasDollar && claim.NormalRewardCount > 0)
            {
                ItemUtils.AddItem(ItemUtils.ToCorrect(new ItemEntry
                {
                    Type = E_ItemType.Dollar,
                    Count = claim.NormalRewardCount,
                }), new AddItemParam
                {
                    playAnim = true,
                    isAd = false,
                    startPos = claim.RewardPosition,
                    bUiPos = false,
                    source = DoubleGetRewardPanel.GetItemSource(DoubleGetRewardPanel.E_UseScene.CloseGetReward),
                });
            }

            if (ReferenceEquals(winClaim, claim))
            {
                if (this != null && !claim.PageClosed)
                {
                    claim.PageClosed = true;
                    CloseSelf();
                }
                CustomWinClose();
            }
        }
    }

    private Action<bool> callback;

    protected override void OnOpen(ItemEntry a, ItemEntry b, DoubleGetRewardPanel.E_UseScene useScene,
        Action<bool> _callback)
    {
        LogLogger.LogVerbose(BaseConst.LOG_Game, $"打开了界面 GetRewardPanel");
        itemA = a;
        itemB = b;
        callback = _callback;
        _useScene = useScene;
        winClaim = null;
        isLookAd = false;
        {
            curTime = 0;
            isRecover = false;
            claimBtn.interactable = false;
            closeBtn.interactable = true;
        }
        isNoCD = useScene == DoubleGetRewardPanel.E_UseScene.DailyTask;
        LevelObj.SetActive(false);
        OnRefresh();
        if (useScene == DoubleGetRewardPanel.E_UseScene.WinPanel)
        {
            Transform rewardOrigin = BroadcastBarController.Instance != null
                ? BroadcastBarController.Instance.entryAObj?.transform
                : null;
            winClaim = new WinClaim
            {
                LevelNumber = FlowModule.WinLevelNumber > 0
                    ? FlowModule.WinLevelNumber
                    : SaveDataUtils.GameData.playerSelectedLv,
                HasDollar = HasDollar,
                NormalRewardCount = dollarCount,
                RewardPosition = rewardOrigin != null ? rewardOrigin.position
                    : itemBPos != null ? itemBPos.position : transform.position,
                Callback = _callback,
            };
        }
    }

    private float timer = 0.5f;
    private float curTime = 0;
    private bool isRecover = false;
    [SerializeField] private TMP_SpriteAsset commonSpriteAsset;

    private void Update() // 这里是为了防止恭喜获得界面弹出， 玩家点击游戏物体不小心点击到按钮做的防误触
    {
        if (isRecover) return;

        curTime += Time.deltaTime;
        if (curTime >= timer)
        {
            claimBtn.interactable = true;
            isRecover = true;
        }
    }

    private float dollarCount = 0;
    /// <summary>
    /// 打开时的界面
    /// </summary>
    private void OnRefresh()
    {
        MaxDollarTip.gameObject.SetActive(true);
        dollarCount = HasDollar ? itemB.Count : 0;
        itemB.Count = dollarCount;
        itemBTxt.text = ItemUtils.GetItemText(itemB); // 真网赚下会进行小数点修复
        noThanksText.text = LanguageUtils.GetText("Btn_NoThanks");

        bool win = _useScene == DoubleGetRewardPanel.E_UseScene.WinPanel;
        if (win)
        {
#if BIZZA_REAL_WITHDRAW
            if (!ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
            {
                itemB.Count = dollarCount * 2;
                rewardText.text = $"{rewardText.text}×2";
            }
            // SaveDataUtils.GameData.playerUnlockedLv++;
            itemBTxt.text = ItemUtils.GetItemText(itemB);
#endif

#if BIZZA_REAL_WITHDRAW
            var bridge = WkyGameplayBridge.Instance;
            Action<FailHttpResponse<AccountModule.OceanShineUserReachReportResponse>> onReport = null;
            if (bridge == null || bridge.TryCaptureWinReport(out onReport))
            {
                try
                {
                    AccountModule.Instance.Request_UserReachLevelReportRequest(onReport);
                    LogLogger.LogVerbose(BaseConst.LOG_Game, "上报 关卡");
                }
                catch (Exception error) when (onReport != null)
                {
                    Debug.LogException(error);
                    onReport(default);
                }
            }
            LevelObj.SetActive(true);
            int winLevel = FlowModule.WinLevelNumber > 0
                ? FlowModule.WinLevelNumber
                : SaveDataUtils.GameData.playerSelectedLv;
            levelTxt.text = LanguageUtils.GetFormatText("Menu_LevelBtn", winLevel);
            if (!Bizza.Sdk.ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
            {
                string _clash = LanguageUtils.GetText("CurrencyToken") + WithdrawalUtil.GetCustomizedFloatByCountryType(dollarCount);
                noThanksText.text = $"{LanguageUtils.GetFormatText("GetRewardPage_NoThanks", iconName, _clash)}";
            }
#endif
            levelTips.SetObjActive(_useScene == DoubleGetRewardPanel.E_UseScene.WinPanel);
            SoundManager.Instance.PlaySFX("Win");
            SaveDataUtils.Save();

            // BizzaEventSystem.Emit(EventDefine.BizzaPlayerAction.PlayerOpenUI, UIPageIds.GetRewardPanel + "_Win");
        }
        else
        {
            //BizzaEventSystem.Emit(EventDefine.BizzaPlayerAction.PlayerOpenUI, UIPageIds.GetRewardPanel);
        }

#if BIZZA_REAL_WITHDRAW
        float value = AccountModule.Instance.GetMaxDrawithRatio(true);
        bonusRate.gameObject.SetActive(value > 0);
        bonusRate.Init($"{value}%", false, true);
#else
#endif
    }

    ///
    /// 自定义的游戏胜利
    private void CustomWinClose()
    {
        FlowModule.LoadGameLevel();
    }
}
#endif
