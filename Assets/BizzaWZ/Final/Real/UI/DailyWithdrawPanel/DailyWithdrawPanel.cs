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
    public static readonly PageId DailyWithdrawPanel = "DailyWithdrawPanel";
}

 
public class DailyWithdrawPanel : UIPageBase, IAutoPopupContent
{
    public bool ContentReady { get; private set; }
    public bool ContentFailed { get; private set; }
    private int contentVersion;
    private object contentLoadingBlock;

    public TMP_Text clashTxt;

    public TMP_Text balanceTxt;
    public TMP_Text withdrawalTxt;

    public RectTransform root;

    private List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform> plats = new();

    [Header("按钮")]
    [SerializeField] private BizzaButton withdrawBtn;
    [SerializeField] private BizzaButton clickBtn;
    protected override void OnAwake()
    {
        
        withdrawBtn.onClick.AddListener(OnClickWithdrawBtn);
        clickBtn.onClick.AddListener(CloseSelf);
    }
    protected override void OnOpen()
    {
        int version = ++contentVersion;
        ContentReady = false;
        ContentFailed = false;
        plats.Clear();
        try
        {
            ReleaseContentLoadingBlock();
            contentLoadingBlock = new object();
            LoadingBlock.AddBlock(contentLoadingBlock).Forget();
            AccountModule.Instance.Request_WithdrawalPageRequest(response =>
            {
                if (this == null || version != contentVersion || IsClosing || !gameObject.activeInHierarchy) return;
                Refresh(response);
            }, block: false);
        }
        catch
        {
            ContentFailed = true;
            ReleaseContentLoadingBlock();
            throw;
        }
    }

    private void Refresh(FailHttpResponse<AccountModule.OceanShineWithdrawalPageResponse> response)
    {
        ReleaseContentLoadingBlock();
        if (!response.success || response.data == null)
        {
            ContentFailed = true;
            CloseSelf();
            return;
        }

        try
        {
            plats = response.data.Os_Wwf ?? new();
            OnRefresh();
            ContentReady = true;
        }
        catch (Exception exception)
        {
            ContentFailed = true;
            CloseSelf();
            Debug.LogException(exception);
        }
    }

    private void OnRefresh()
    {
        double coin = AccountModule.Instance.Os_Current_Uso.GetBalance();
        double clash =  AccountModule.Instance.Os_Current_Uso.Os_Ewl;
        string clashContent = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType((float)clash)}";
        clashTxt.text = clashContent;
        balanceTxt.text = $"{WithdrawalUtil.GetCustomizedValueByCountryType((float)coin)}";
        withdrawalTxt.text = clashContent;
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    protected override void OnClose()
    {
        contentVersion++;
        ReleaseContentLoadingBlock();
        if (!ContentReady) ContentFailed = true;
    }

    private void ReleaseContentLoadingBlock()
    {
        var block = contentLoadingBlock;
        contentLoadingBlock = null;
        if (block != null) LoadingBlock.RemoveBlock(block).Forget();
    }

    public void OnClickWithdrawBtn()
    {
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

        if (plat == null)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Game, "没有找到平台");
        }

        CloseSelf();
        // UIModule.Instance.OpenPage<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform, List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>, E_WithdrawType, Action, bool>
        //     (E_UIPageType.WithdrawConfirmPanel, plat, plats, E_WithdrawType.DailyWithdraw, null, isSelectPlatform);

        UIModule.Instance.OpenPage(UIPageIds.RealWithdrawPanel);
    }
}
#endif
