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
    public static readonly PageId UIWithdrawalConfirmPanel = "UIWithdrawalConfirmPanel";
}


public class UIWithdrawalConfirmPanel : UIPageBase<WithDrawInfo>
{
    public PaymentConfig paymentList;

    public TMP_Text CPF_CNPJText;
    public TMP_Text CPFTitleText;
    public GameObject CPFObj;

    public TMP_Text NameText;
    public TMP_Text NameTitleText;
    public GameObject NameObj;

    public TMP_Text EmailText;
    public TMP_Text EmailitleText;
    public GameObject EmailObj;

    public Image paymentImage;
    public TMP_Text PaymentValueText;

    [SerializeField] private GameObject brazilNameCpfWarning;
    [SerializeField] private GameObject defaultConfirmationHint;

    // private UIWithdrawalPanel _page;
    private AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform data;

    private PayeeAccountType type;
    private string Re;
    private string Ra;
    private string Name;
    private string CPF_CNPJ;
    private E_PayeeAccountType payType;
    private E_WithdrawType withdrawType;
    private Action callback = null;

    [Header("按钮")]
    [SerializeField] private BizzaButton withdrawalBtn;
    [SerializeField] private BizzaButton closeBtn;

    protected override void OnAwake()
    {
        base.OnAwake();
        withdrawalBtn.onClick.AddListener(() => { OnClickWithdrawBtn(); });
        closeBtn.onClick.AddListener(() => { CloseSelf(); });
    }

    protected override void OnClose()
    {
        // MobileInputBinderManager.Instance.SetectFouscus(true);
    }

    protected override void OnOpen(WithDrawInfo info)
    {
        this.callback = info.callback;
        this.withdrawType = info.withdrawType;
        this.data = info.data;
        this.type = info.type;
        this.Re = info.Re;
        this.Ra = info.Ra;
        this.Name = info.Name;
        this.CPF_CNPJ = info.CPF_CNPJ;
        this.payType = info.payType;

        paymentImage.sprite = paymentList.GetSpriteByType(payType);
        gameObject.SetActive(true);
        CPF_CNPJText.text = CPF_CNPJ;
        NameText.text = Name;
        EmailText.text = Re;
        if (AccountModule.CountryType == AccountModule.E_CountryType.ID)
        {
            EmailText.text = Ra;
        }
        if (withdrawType == E_WithdrawType.DailyMission)
        {
            PaymentValueText.text = $"{LanguageUtils.GetText("CurrencyToken")}0,2";
        }
        else if (withdrawType == E_WithdrawType.Fake)
        {
            float value = 0.01f;
            switch (AccountModule.CountryType)
            {
                case AccountModule.E_CountryType.BR:
                    value = 0.01f;
                    break;
                case AccountModule.E_CountryType.ID:
                    value = 20f;
                    break;
                case AccountModule.E_CountryType.US:
                    value = 0.01f;
                    break;
            }

            PaymentValueText.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(value)}";
        }
        else if (withdrawType == E_WithdrawType.Real)
        {
            PaymentValueText.text =
                $"{LanguageUtils.GetText("CurrencyToken")}{AccountModule.Instance.Get_S_Ewl()}";
        }

        CPFObj.gameObject.SetActive(!string.IsNullOrEmpty(CPF_CNPJ));
        NameObj.gameObject.SetActive(!string.IsNullOrEmpty(Name));
        EmailObj.gameObject.SetActive(!string.IsNullOrEmpty(Re) || !string.IsNullOrEmpty(Ra));
        bool showBrazilWarning = AccountModule.CountryType == AccountModule.E_CountryType.BR;
        brazilNameCpfWarning.SetActive(showBrazilWarning);
        defaultConfirmationHint.SetActive(!showBrazilWarning);
        if (showBrazilWarning)
        {
            brazilNameCpfWarning.transform.SetAsLastSibling();
        }
    }


    public void OnClickWithdrawBtn()
    {
        if (payType == E_PayeeAccountType.Paypal) // 美国的邮箱就是账号
        {
            Ra = Re;
            Re = "";
        }

        string _Os_Cp = payType == E_PayeeAccountType.PIX || payType == E_PayeeAccountType.Pagbank ? CPF_CNPJ : "";
        int _Os_Mid = data.Os_Mid;
        string _Os_Pb = payType != E_PayeeAccountType.PIX
            ? ""
            : type switch
            {
                PayeeAccountType.Email => "e",
                PayeeAccountType.Phone => "p",
                PayeeAccountType.CpfCnpj => "c",
                PayeeAccountType.Evp => "b",
                _ => _Os_Cp
            };
        string _Os_Ra = Ra;
        string _Os_Re = Re;
        string _Os_Rm = "";
        string _Os_Rn = payType == E_PayeeAccountType.Paypal ? "" : Name;

        string _Os_Mn = withdrawType == E_WithdrawType.DailyMission
            ? $"task_{AccountModule.Instance.routineTaskLookAdMoneyResponse.Os_Tid}"
            : "";
        string _Os_At = (withdrawType == E_WithdrawType.DailyMission || withdrawType == E_WithdrawType.Fake)
            ? $"money"
            : "";
        SaveDataUtils.GameData.lastWithdrawTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (withdrawType == E_WithdrawType.Fake || withdrawType == E_WithdrawType.DailyMission)
        {
            var requestFake = AccountModule.Instance.GetApplyWithdrawalRequestFake(_Os_Cp, _Os_Mid, _Os_Pb, _Os_Ra, _Os_Re, _Os_Rm, _Os_Rn, _Os_Mn, _Os_At);
            LogLogger.LogInfo(BaseConst.LOG_Game, "进入假提现");
            AccountModule.Instance.Request_ApplyWithdrawalMoneyRollReqRequest(requestFake, InfoResult);
        }
        else
        {
            var requestReal = AccountModule.Instance.GetApplyWithdrawalRequestReal(_Os_Cp, _Os_Mid, _Os_Pb, _Os_Ra, _Os_Re, _Os_Rm, _Os_Rn, _Os_Mn, _Os_At);
            LogLogger.LogInfo(BaseConst.LOG_Game, "进入真提现");
            AccountModule.Instance.Request_ApplyWithdrawalRequest(requestReal, InfoResult);
        }

        OpenPendingPanel();
    }



    public void InfoResult(FailHttpResponse<AccountModule.OceanShineApplyWithdrawalResponse> response)
    {
        CloseSelf();
        UIModule.Instance.ClosePage(UIPageIds.UIWithdrawalPanel);
        bool isSuccess = response.success && response.errorCode == "200";
        if (isSuccess)
        {
            if (response.errorCode.Equals("200"))
            {
                callback?.Invoke();
            }

            //_page.OnClick_ResultPage_OkBtn();
            BizzaEventSystem.Emit(EventDefine.WithDraw.FingerShow, true);
            BizzaEventSystem.Emit(EventDefine.WithDraw.WithdrawOver);
            BizzaEventSystem.Emit(EventDefine.WithDraw.RefreshDailyRealPage);
        }

        AccountModule.Instance.Request_WithdrawalPageRequest((a) =>
        {
            UIWithdrawalPendingPanel.NotifyNetworkCallback(isSuccess, GetPendingResultText(response));

        }, true, false);

        if (withdrawType == E_WithdrawType.DailyMission)
        {
            BizzaEventSystem.Emit(EventDefine.WithDraw.RefreshDailyMissionPage);
        }

        // var args = new CommonConfirmTipsPanel.Args()
        // {
        //     isLanguage = string.IsNullOrEmpty(response.message),
        //     // 如果服务器给了 message，用服务器的
        //     // 否则兜底用本地文案
        //     des = string.IsNullOrEmpty(response.message)
        //         ? "Tips_NetworkError"
        //         : response.message
        // };
        // SDKAssetHandler.OpenCommonConfirmTipsPanel(args).Forget();

        // bool isWithdrawal = SaveDataUtils.GameData.isWithdrawal;
        // if (isSuccess && !isWithdrawal)
        // {
        //     SaveDataUtils.GameData.isWithdrawal = true;
        //     SaveDataUtils.gameStrategy.SaveData();
        //     UIModule.Instance.OpenPage(UIPageIds.StarRatingPopup).Forget();
        // }
        bool isWithdrawal = SaveDataUtils.GameData.isWithdrawal;
        if (isSuccess && !isWithdrawal)
        {
            openStarRatingAfterPending = true;
        }
    }

    private static bool openStarRatingAfterPending;

    private void OpenPendingPanel()
    {
        openStarRatingAfterPending = false;
        var info = new UIWithdrawalPendingInfo(
            payType,
            PaymentValueText.text,
            paymentIconKey: data?.Os_Cn,
            successHintText: LanguageUtils.GetText("InfoConfirm_WithdrawSuccessful"),
            failHintText: LanguageUtils.GetText("InfoConfirm_WithdrawFailed"),
            onResultClose: OpenStarRatingAfterPending);
        UIModule.Instance.OpenPage(UIPageIds.UIWithdrawalPendingPanel, info).Forget();
    }

    private static string GetPendingResultText(FailHttpResponse<AccountModule.OceanShineApplyWithdrawalResponse> response)
    {
        return string.IsNullOrEmpty(response.message) ? null : response.message;
    }

    private static void OpenStarRatingAfterPending(bool success)
    {
        if (!success || !openStarRatingAfterPending)
        {
            return;
        }
        openStarRatingAfterPending = false;
        bool isWithdrawal = SaveDataUtils.GameData.isWithdrawal;

        if (!isWithdrawal)
        {
            SaveDataUtils.GameData.isWithdrawal = true;
            SaveDataUtils.gameStrategy.SaveData();
            UIModule.Instance.OpenPage(UIPageIds.StarRatingPopup).Forget();
        }
    }
}


public struct WithDrawInfo
{
    public Action callback;
    public E_WithdrawType withdrawType;
    public E_PayeeAccountType payType;
    public PayeeAccountType type;
    public string CPF_CNPJ;
    public string Name;
    public string Re;
    public string Ra;
    public AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform data;

    public WithDrawInfo(Action callback, E_WithdrawType withdrawType, E_PayeeAccountType payType, PayeeAccountType type, string CPF_CNPJ,
        string Name, string Re, string Ra, AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform data)
    {
        this.callback = callback;
        this.withdrawType = withdrawType;
        this.data = data;
        this.type = type;
        this.Re = Re;
        this.Ra = Ra;
        this.Name = Name;
        this.CPF_CNPJ = CPF_CNPJ;
        this.payType = payType;
    }
}
#endif
