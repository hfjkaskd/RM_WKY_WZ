#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using AdvancedInputFieldPlugin;
using Bizza;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId UIWithdrawalPanel = "UIWithdrawalPanel";
}

public class UIWithdrawalPanel : UIPageBase<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform,
    List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>, E_WithdrawType, Action, bool>
{
    #region ===== Root & Text =====

    [FoldoutGroup("自适应")]
    [LabelText("BG")]
    public RectTransform BG;

    [FoldoutGroup("自适应")]
    [LabelText("额外高度")]
    public float defaultHeight = 470;

    [FoldoutGroup("基础界面")]
    [LabelText("信息填写界面")]
    public GameObject FillingRoot;

    [FoldoutGroup("基础界面")]
    [LabelText("输入框界面")]
    public RectTransform InputRoot;

    [FoldoutGroup("基础界面")]
    [LabelText("选择平台界面")]
    public GameObject PlatformRoot;

    [FoldoutGroup("基础界面")]
    [LabelText("选择平台界面WayItem")]
    public WithdrawWay withdrawWayItem;

    [FoldoutGroup("基础界面")]
    [LabelText("平台图片界面")]
    public GameObject PlatformIconRoot;


    [FoldoutGroup("基础界面")]
    [LabelText("结果界面Root")]
    public GameObject ResultRoot;

    [FoldoutGroup("基础界面")]
    [LabelText("群组链接")]
    public TMP_Text txtGroupLink;

    #endregion

    public TMP_Text balanceText;
    public Image paymentImage;
    public PaymentConfig paymentList;

    [FoldoutGroup("巴西提现")]
    [LabelText("收款方个人识别号码-- CPF/CNP")]
    public AdvancedInputField CPFNumberInput;

    [FoldoutGroup("巴西提现")] public TMP_Text CPFNumberError;
    [FoldoutGroup("巴西提现")] public RectTransform CPFNumberErrorTra;

    [FoldoutGroup("巴西提现")]
    [LabelText("收款方全名")]
    public AdvancedInputField accountNameInput;

    [FoldoutGroup("巴西提现")] public TMP_Text accountNameError;
    [FoldoutGroup("巴西提现")] public RectTransform accountNameErrorTra;

    [FoldoutGroup("巴西提现")]
    [LabelText("收款方PIX绑定账户类型")]
    public List<E_WithdrawChaanelForBaxi> accountTypeDropdown;

    [FoldoutGroup("巴西提现")]
    [LabelText("输入框提示文本")]
    public AdvancedInputField accountIdentificationInput;

    [FoldoutGroup("巴西提现")] public TMP_Text accountIdentificationError;
    [FoldoutGroup("巴西提现")] public RectTransform accountIdentificationErrorTra;

    [FoldoutGroup("美国提现")]
    [LabelText("PayPal邮箱 标题")]
    public TMP_Text paypalMailTitle;

    [FoldoutGroup("美国提现")]
    [LabelText("PayPal邮箱")]
    public AdvancedInputField paypalMailInput;

    [FoldoutGroup("美国提现")] public TMP_Text paypalMailError;
    [FoldoutGroup("美国提现")] public RectTransform paypalMailErrorTra;

    [FoldoutGroup("印尼提现")]
    [LabelText("OVO + Dana 账号")]
    public AdvancedInputField accPhoneMailInput;

    [FoldoutGroup("印尼提现")] public TMP_Text accPhoneMailError;
    [FoldoutGroup("印尼提现")] public RectTransform accPhoneMailErrorTra;

    [LabelText("PIX支付Obj")] public List<GameObject> PIXObjList;
    [LabelText("PagBank支付Obj")] public List<GameObject> PagBankList;
    [LabelText("PayPal支付Obj")] public List<GameObject> PaypalList;
    [LabelText("OVO + Dana 支付Obj")] public List<GameObject> OVOList;
    [LabelText("Error")] public List<GameObject> ErrorList;

    [Header("巴西输入提示")]
    [SerializeField] private GameObject accountNameTitle;
    [SerializeField] private GameObject cpfNumberTitle;
    [SerializeField] private GameObject pixAccountTitle;
    [SerializeField] private GameObject brazilAccountNameTitle;
    [SerializeField] private GameObject brazilCpfNumberTitle;
    [SerializeField] private GameObject brazilPixAccountTitle;
    private string defaultAccountNamePlaceholder;

    [Space(10)]
    [SerializeField] private BizzaButton closeButton;
    [SerializeField] private BizzaButton withdrawButton;
    protected override void OnAwake()
    {
        base.OnAwake();
        closeButton.onClick.AddListener(() => { CloseSelf(); });
        withdrawButton.onClick.AddListener(() => { OnClick_FillPage_ToComfirmBtn(); });

        defaultAccountNamePlaceholder = accountNameInput.PlaceholderTextRenderer.Text;
        EnsureProfiles();
        accountNameInput.OnValueChanged.AddListener(FilterInput);
        InitBaXiChannel();

        var inputs = GetComponentsInChildren<AdvancedInputField>(false);
        foreach (var input in inputs)
        {
            input.CaretWidth = 5;
        }
    }

    public static string pixInfo = "pix.jpg";
    public static string pagBankInfo = "pagbank.jpg";
    public static string paypalInfo = "paypal.jpg";
    public static string ovoInfo = "ovo.jpg";
    public static string danaInfo = "dana.jpg";

    // =============================
    //   Data
    // =============================

    private AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform data;
    private List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform> plats = new();
    public List<WithdrawWay> _withdrawWayItems = new();
    private E_WithdrawType withdrawType;
    public E_WithdrawType WithdrawType => withdrawType;
    private Action callback = null;
    public Action Callback => callback;
    private bool isSelectPlatform;
    private int _pixChannelIndex = 0;

    private int pixChannelIndex
    {
        get
        {
            return _pixChannelIndex;
        }
        set
        {
            _pixChannelIndex = value;
        }
    }

    // =============================
    //   表驱动配置
    // =============================

    [Serializable]
    private class PlatformProfile
    {
        public E_PayeeAccountType PayType;
        public List<GameObject> UiList;
        public Action<UIWithdrawalPanel> OnRefreshExtra; // 刷新时的额外动作（例如：改标题）
        public Func<UIWithdrawalPanel, (AdvancedInputField, bool)> ValidateAndInitConfirm; // 点击提现时：校验+Init
    }

    private Dictionary<string, PlatformProfile> _profiles;
    private bool _profilesInited;

    private void EnsureProfiles()
    {
        if (_profilesInited) return;
        _profilesInited = true;

        _profiles = new Dictionary<string, PlatformProfile>
        {
            [pixInfo] = new PlatformProfile
            {
                PayType = E_PayeeAccountType.PIX,
                UiList = PIXObjList,
                ValidateAndInitConfirm = ui =>
                {
                    var result = ui.ValidatePixInputs(out var cpf, out var name, out var pixAcc);
                    if (!result.Item2)
                        return result;

                    WithDrawInfo info = new WithDrawInfo(callback, withdrawType, E_PayeeAccountType.PIX, ui.CurrentPixType, cpf, name, pixAcc, "", ui.data);
                    UIModule.Instance.OpenPage(UIPageIds.UIWithdrawalConfirmPanel, info);
                    return result;
                }
            },

            [pagBankInfo] = new PlatformProfile
            {
                PayType = E_PayeeAccountType.Pagbank,
                UiList = PagBankList,
                OnRefreshExtra = ui =>
                {
                    if (ui.paypalMailTitle != null)
                        ui.paypalMailTitle.text = LanguageUtils.GetText("UIWithdrawalConfirm_AccountNumber");
                },
                ValidateAndInitConfirm = ui =>
                {
                    string mail = "";
                    // bool ok = ui.IsValidName(ui.accountNameInput.Text)
                    //           && ui.IsValidCpfOrCnpj(ui.CPFNumberInput.Text)
                    //           && ui.ValidateEmailInput(out mail);
                    bool nameValid = ui.IsValidName(ui.accountNameInput.Text);
                    bool cpfValid = ui.IsValidCpfOrCnpj(ui.CPFNumberInput.Text);
                    bool mailValid = ui.ValidateEmailInput(out mail);

                    if (!nameValid) return (accountNameInput, nameValid);
                    if (!cpfValid) return (CPFNumberInput, cpfValid);
                    if (!mailValid) return (paypalMailInput, mailValid);
                    WithDrawInfo info = new WithDrawInfo(callback, withdrawType, E_PayeeAccountType.Pagbank, ui.CurrentPixType,
                        ui.CPFNumberInput.Text, ui.accountNameInput.Text, mail, "", ui.data);
                    UIModule.Instance.OpenPage(UIPageIds.UIWithdrawalConfirmPanel, info);

                    return (accountNameInput, true);
                }
            },

            [paypalInfo] = new PlatformProfile
            {
                PayType = E_PayeeAccountType.Paypal,
                UiList = PaypalList,
                ValidateAndInitConfirm = ui =>
                {
                    bool mailValid = ui.ValidateEmailInput(out var mail);
                    if (!mailValid)
                        return (paypalMailInput, mailValid);
                    WithDrawInfo info = new WithDrawInfo(callback, withdrawType, E_PayeeAccountType.Paypal, ui.CurrentPixType, "", "", mail, "", ui.data);
                    UIModule.Instance.OpenPage(UIPageIds.UIWithdrawalConfirmPanel, info);
                    return (paypalMailInput, true);
                }
            },

            [ovoInfo] = new PlatformProfile
            {
                PayType = E_PayeeAccountType.OVO,
                UiList = OVOList,
                ValidateAndInitConfirm = ui =>
                {
                    var phoneValid = ui.ValidatePhoneAccount(out var phone, out var name);
                    if (!phoneValid.Item2)
                        return phoneValid;
                    WithDrawInfo info = new WithDrawInfo(callback, withdrawType, E_PayeeAccountType.OVO, ui.CurrentPixType, "", name, "", phone, ui.data);
                    UIModule.Instance.OpenPage(UIPageIds.UIWithdrawalConfirmPanel, info);
                    return phoneValid;
                }
            },

            [danaInfo] = new PlatformProfile
            {
                PayType = E_PayeeAccountType.Dana,
                UiList = OVOList, // 保持你原逻辑：Dana 也是打开 OVOList
                ValidateAndInitConfirm = ui =>
                {
                    var phoneValid = ui.ValidatePhoneAccount(out var phone, out var name);
                    if (!phoneValid.Item2)
                        return phoneValid;
                    WithDrawInfo info = new WithDrawInfo(callback, withdrawType, E_PayeeAccountType.Dana, ui.CurrentPixType, "", name, "", phone, ui.data);
                    UIModule.Instance.OpenPage(UIPageIds.UIWithdrawalConfirmPanel, info);
                    return phoneValid;
                }
            },
        };
    }

    private bool TryGetProfile(out PlatformProfile profile)
    {
        EnsureProfiles();
        profile = null;

        if (data == null || string.IsNullOrEmpty(data.Os_Cn))
            return false;

        return _profiles.TryGetValue(data.Os_Cn, out profile);
    }

    // =============================
    //   Unity
    // =============================

    protected override void OnOpen(AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform data,
        List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform> plats, E_WithdrawType withdrawType,
        Action callback, bool isSelectPlatform)
    {
        this.plats?.Clear();
        this.isSelectPlatform = isSelectPlatform;
        this.data = data;
        this.plats = plats;
        this.callback = callback;
        this.withdrawType = withdrawType;

        PlatformRoot.gameObject.SetActive(isSelectPlatform);
        PlatformIconRoot.gameObject.SetActive(!isSelectPlatform);
        pixChannelIndex = SaveDataUtils.GameData.pixChannelIndex;
        OnRefresh(data);

        var inputs = GetComponentsInChildren<AdvancedInputField>(false);
        int count = inputs.Length - 1;
        for (int i = 0; i < count; i++)
        {
            inputs[i].NextInputField = inputs[i + 1];
        }
    }

    private void InitBaXiChannel()
    {
        for (int i = 0; i < accountTypeDropdown.Count; i++)
        {
            int index = i;

            accountTypeDropdown[i].Init(() =>
            {
                OnClickBaXiChannel(index);
            });
        }

        if (accountTypeDropdown.Count > 0)
        {
            OnClickBaXiChannel(0);
        }
    }

    // =============================
    //   Refresh：一条链路完成所有平台
    // =============================

    public void OnRefresh(AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform data)
    {
        this.data = data;
        OnClickBaXiChannel(SaveDataUtils.GameData.pixChannelIndex);
        FillingRoot?.SetActive(true);
        if (ResultRoot != null) ResultRoot.SetActive(false);

        HideAllPayUIs();

        if (!TryGetProfile(out var profile))
        {
            LogLogger.LogInfo(BaseConst.LOG_Withdrawal, "========= 提现界面的支付方式都没有 ==========");
            return;
        }

        LogLogger.LogInfo(BaseConst.LOG_Withdrawal, $"{this.data.Os_Cn} 提现方式");

        SetupPlatformListIfNeeded();
        SetupPaymentIconIfNeeded(profile.PayType);

        profile.OnRefreshExtra?.Invoke(this);
        SetActiveList(profile.UiList, true); /// InputRoot 下的物体在这里设置显示
        SetActiveList(ErrorList, false);
        RefreshBrazilianInputCopy(profile.PayType);

        OnRefreshPos();
        LoadReadData(true);
    }

    private void RefreshBrazilianInputCopy(E_PayeeAccountType payType)
    {
        bool useBrazilianCopy = AccountModule.CountryType == AccountModule.E_CountryType.BR
                               && (payType == E_PayeeAccountType.PIX || payType == E_PayeeAccountType.Pagbank);
        accountNameTitle.SetActive(!useBrazilianCopy);
        cpfNumberTitle.SetActive(!useBrazilianCopy);
        pixAccountTitle.SetActive(!useBrazilianCopy);
        brazilAccountNameTitle.SetActive(useBrazilianCopy);
        brazilCpfNumberTitle.SetActive(useBrazilianCopy);
        brazilPixAccountTitle.SetActive(useBrazilianCopy && payType == E_PayeeAccountType.PIX);
        accountNameInput.PlaceHolderText = useBrazilianCopy
            ? "O nome deve corresponder ao CIN ou RG"
            : defaultAccountNamePlaceholder;
    }

    private void OnRefreshPos()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoRefreshPos());
    }

    private Coroutine _co;

    private IEnumerator CoRefreshPos()
    {
        yield return null; // 等一帧，让布局系统跑完

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(InputRoot);

        BG.sizeDelta = new Vector2(BG.sizeDelta.x, defaultHeight + InputRoot.rect.height);
    }



    // =============================
    //   UI Helpers
    // =============================

    private void SetActiveList(List<GameObject> list, bool active)
    {
        if (list == null) return;
        foreach (var go in list)
            if (go != null)
                go.SetActive(active);
    }

    private void HideAllPayUIs()
    {
        SetActiveList(PIXObjList, false);
        SetActiveList(PagBankList, false);
        SetActiveList(PaypalList, false);
        SetActiveList(OVOList, false);
    }

    private void SetupPlatformListIfNeeded()
    {
        if (!isSelectPlatform) return;

        int count = plats?.Count ?? 0;
        _withdrawWayItems.SetCmptListCount(withdrawWayItem, PlatformRoot.transform, count);

        for (int i = 0; i < count; i++)
        {
            var p = plats[i];
            _withdrawWayItems[i].Init(OnSelectWithdrawWay, p);
            if (p == data) _withdrawWayItems[i].OnSelect();
        }
    }

    private void SetupPaymentIconIfNeeded(E_PayeeAccountType payType)
    {
        if (isSelectPlatform) return;
        if (paymentImage != null && paymentList != null)
            paymentImage.sprite = paymentList.GetSpriteByType(payType);
    }

    private void ShowFillError()
    {
        UIUtils.ShowLanguageTips("UIWithdrawalConfirm_FillInfoError");
    }

    // =============================
    //   CPF/CNPJ
    // =============================

    public bool IsValidCpfOrCnpj(string input)
    {
        CPFNumberErrorTra.gameObject.SetActive(true);
        CPFNumberError.text = LanguageUtils.GetText("UIWithdrawalConfirm_CPFInputFaild");
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string digits = OnlyDigits(input);
        if (digits.Length == 11 && InputVerifyUtil.IsValidCPF(digits))
        {
            CPFNumberErrorTra.gameObject.SetActive(false);
            return true;
        }

        if (digits.Length == 14 && InputVerifyUtil.IsValidCNPJ(digits))
        {
            CPFNumberErrorTra.gameObject.SetActive(false);
            return true;
        }

        return false;
    }

    private static string OnlyDigits(string s) => new string(s.Where(char.IsDigit).ToArray());

    // =============================
    //   Name
    // =============================

    private static readonly Regex NameRegex =
        new Regex(@"^[A-Za-z ]+$", RegexOptions.Compiled);

    private void FilterInput(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        // if (value.Length > 99)
        //     accountNameInput.SetTextWithoutNotify(value.Substring(0, 99));
    }

    public bool IsValidName(string name)
    {
        accountNameErrorTra.gameObject.SetActive(true);
        accountNameError.text = LanguageUtils.GetText("UIWithdrawalConfirm_NameInputFaild");
        if (string.IsNullOrWhiteSpace(name)) return false;
        bool paymentTypeForID = data != null && (data.Os_Cn == ovoInfo || data.Os_Cn == danaInfo);
        if (paymentTypeForID && name.Length >= 45)
        {
            return false;
        }

        if (name.Length >= 100)
        {
            return false;
        }

        if (name.Trim().Length == 0)
        {
            return false;
        }

        if (!NameRegex.IsMatch(name))
        {
            return false;
        }

        accountNameErrorTra.gameObject.SetActive(false);
        return true;
    }

    // =============================
    //   PIX Account Type Select
    // =============================



    private string _cachedCpfCnpj = "";
    private string _cachedFullName = "";

    private string _cachedEmail = "";
    private string _cachedPhone = "";
    private string _cachedEvp = "";
    private string _cachedPixForCpfCnpj = "";

    private PayeeAccountType CurrentPixType => GetWithdrawChannel();

    // private void OnAccountTypeChanged(int dropdownIndex)
    // {
    //     CacheCpfAndFullName();
    //     CacheCurrentPixInputByType();

    //     var type = (PayeeAccountType)dropdownIndex;
    //     ApplyPlaceholder(type);
    //     RestorePixInputByType(type);

    //     SetError("");
    // }

    private void CacheCpfAndFullName()
    {
        if (CPFNumberInput != null) _cachedCpfCnpj = CPFNumberInput.Text ?? "";
        if (accountNameInput != null) _cachedFullName = accountNameInput.Text ?? "";
    }

    private void CacheCurrentPixInputByType()
    {
        var type = GetWithdrawChannel();
        var txt = accountIdentificationInput.Text ?? "";

        switch (type)
        {
            case PayeeAccountType.Email: _cachedEmail = txt; break;
            case PayeeAccountType.Phone: _cachedPhone = txt; break;
            case PayeeAccountType.CpfCnpj: _cachedPixForCpfCnpj = txt; break;
            case PayeeAccountType.Evp: _cachedEvp = txt; break;
        }
    }

    private void RestorePixInputByType(PayeeAccountType type)
    {
        accountIdentificationInput.Text = type switch
        {
            PayeeAccountType.Email => _cachedEmail,
            PayeeAccountType.Phone => _cachedPhone,
            PayeeAccountType.CpfCnpj => _cachedPixForCpfCnpj,
            PayeeAccountType.Evp => _cachedEvp,
            _ => accountIdentificationInput.Text
        };
    }

    private void ApplyPlaceholder(PayeeAccountType type)
    {
        string hint = type switch
        {
            PayeeAccountType.Email => "example@gmail.com",
            PayeeAccountType.Phone => "11912345678",
            PayeeAccountType.CpfCnpj => "99999999999 ou 99999999999999",
            PayeeAccountType.Evp => "123e4567-e89b-12d3-a456-426655440000",
            _ => ""
        };

        // if (accountIdentificationInput.placeholder is TMP_Text tmpText)
        accountIdentificationInput.PlaceHolderText = hint;
    }

    public bool ValidateAndShowError()
    {
        accountIdentificationErrorTra.gameObject.SetActive(true);

        var type = GetWithdrawChannel();
        var value = (accountIdentificationInput.Text ?? "").Trim();

        LogLogger.LogVerbose(BaseConst.LOG_Withdrawal, $"验证 类型 - {type} ： 账号 - {value}");

        if (!Validate(type, value, out var err))
        {
            accountIdentificationError.text = LanguageUtils.GetText(err);
            // SetError(err);
            return false;
        }

        accountIdentificationErrorTra.gameObject.SetActive(false);
        // SetError("");
        return true;
    }

    private bool Validate(PayeeAccountType type, string value, out string error)
    {
        error = "";

        switch (type)
        {
            case PayeeAccountType.Email:
                if (!IsEmail(value))
                {
                    error = "UIWithdrawalConfirm_Email";
                    return false;
                }

                return true;

            case PayeeAccountType.Phone:
                if (!IsAllDigits(value) || value.Length < 3 || value[2] != '9')
                {
                    error = "UIWithdrawalConfirm_Phone";
                    return false;
                }

                return true;

            case PayeeAccountType.CpfCnpj:
                bool con_1 = string.IsNullOrWhiteSpace(value);
                string digits = OnlyDigits(value);
                bool con_2 = (digits.Length == 11 && InputVerifyUtil.IsValidCPF(digits));
                bool con_3 = (digits.Length == 14 && InputVerifyUtil.IsValidCNPJ(digits));
                if (con_1)
                {
                    error = "UIWithdrawalConfirm_CpfCnpj";
                    return false;
                }

                if (con_2 || con_3)
                {
                    return true;
                }

                error = "UIWithdrawalConfirm_CpfCnpj";
                return false;

            case PayeeAccountType.Evp:
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = "UIWithdrawalConfirm_Evp";
                    return false;
                }

                return true;
        }

        return true;
    }

    private void SetError(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        UIUtils.ShowLanguageTips(LanguageUtils.GetText(msg));
    }

    private static bool IsAllDigits(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < s.Length; i++)
            if (s[i] < '0' || s[i] > '9')
                return false;
        return true;
    }

    private static bool IsEmail(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;

        try
        {
            string pattern = @"^[a-zA-Z0-9_+&*-]+(?:\.[a-zA-Z0-9_+&*-]+)*@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,7}$";

            // 3. 执行匹配
            return Regex.IsMatch(s, pattern);
        }
        catch
        {
            return false;
        }
    }

    // =============================
    //   Platform-specific validations (used by profiles)
    // =============================

    private (AdvancedInputField, bool) ValidatePixInputs(out string cpf, out string fullName, out string pixAcc)
    {
        cpf = CPFNumberInput.Text;
        fullName = accountNameInput.Text;
        pixAcc = accountIdentificationInput.Text;
        //bool ok = IsValidName(fullName) && IsValidCpfOrCnpj(cpf) && ValidateAndShowError();
        // LogLogger.LogVerbose(BaseConst.LOG_Withdrawal,$"pixInfo ::: valid - {ok}");
        bool nameValid = IsValidName(fullName);
        bool cpfValid = IsValidCpfOrCnpj(cpf);
        bool pixValid = ValidateAndShowError();
        if (!nameValid) return (accountNameInput, nameValid);
        if (!cpfValid) return (CPFNumberInput, cpfValid);
        if (!pixValid) return (accountIdentificationInput, pixValid);

        return (accountIdentificationInput, true);
    }

    private bool ValidateEmailInput(out string email)
    {
        paypalMailErrorTra.gameObject.SetActive(true);
        paypalMailError.text = LanguageUtils.GetText("UIWithdrawalConfirm_Email");
        email = (paypalMailInput.Text ?? "").Trim();

        bool valid = IsEmail(email);
        if (!valid)
        {
            return false;
        }

        paypalMailErrorTra.gameObject.SetActive(false);
        return valid;
    }

    private (AdvancedInputField, bool) ValidatePhoneAccount(out string phone, out string fullName)
    {
        phone = (accPhoneMailInput.Text ?? "").Trim();
        fullName = accountNameInput.Text;
        bool phoneValid = VialIsAccountPhone(phone);
        bool nameValid = IsValidName(fullName);
        if (!phoneValid) return (accPhoneMailInput, phoneValid);
        if (!nameValid) return (accountNameInput, nameValid);
        return (accPhoneMailInput, true);
    }

    // =============================
    //   邮箱 / 手机账号
    // =============================

    private bool VialIsAccountPhone(string phone)
    {
        accPhoneMailErrorTra.gameObject.SetActive(true);
        accPhoneMailError.text = LanguageUtils.GetText("UIWithdrawalConfirm_PhoneAccount");
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        if (!phone.StartsWith("08"))
        {
            return false;
        }

        if (!phone.All(char.IsDigit))
        {
            return false;
        }

        if (phone.Length > 14 || phone.Length < 9)
        {
            return false;
        }

        accPhoneMailErrorTra.gameObject.SetActive(false);
        return true;
    }

    // =============================
    //   Close & Events
    // =============================

    protected override void OnClose()
    {
    }

    public void OnSelectWithdrawWay(WithdrawWay way)
    {
        if (way == null) return;
        LogLogger.LogVerbose(BaseConst.LOG_Withdrawal, $"way ::: " + way.data.Os_Cn);
        OnRefresh(way.data);
    }

    // =============================
    //   Click Withdraw：表驱动校验 + Init
    // =============================
    private KeyboardClient keyboardClient;
    public KeyboardClient client
    {
        get
        {
            if (keyboardClient == null)
            {
                keyboardClient = GetComponentInChildren<KeyboardClient>();
            }
            return keyboardClient;
        }
    }
    public void OnClick_FillPage_ToComfirmBtn()
    {

        SetActiveList(ErrorList, false);

        if (!TryGetProfile(out var profile))
        {
            LogLogger.LogVerbose(BaseConst.LOG_Withdrawal, "========= 提现界面的支付方式都没有 ==========");
            return;
        }

        var ok = profile.ValidateAndInitConfirm.Invoke(this);
        bool isOk = ok.Item2;
        if (!isOk)
        {
            // ok.Item1
            ShowFillError();
            ok.Item1.ManualSelect();
        }
        else
        {
            client.HideKeyboard();
        }

        LoadReadData(false);
        OnRefreshPos();
    }

    private void LoadReadData(bool load)
    {
        if (load)
        {
            CPFNumberInput.Text = SaveDataUtils.GameData.withdrawCPFInfo;
            accountNameInput.Text = SaveDataUtils.GameData.withdrawNameInfo;

            if (TryGetProfile(out var profile) && profile.PayType == E_PayeeAccountType.PIX)
            {
                var type = (PayeeAccountType)pixChannelIndex;
                switch (type)
                {
                    case PayeeAccountType.CpfCnpj:
                        accountIdentificationInput.Text = SaveDataUtils.GameData.accountIdentificationInfo_C;
                        break;
                    case PayeeAccountType.Phone:
                        accountIdentificationInput.Text = SaveDataUtils.GameData.accountIdentificationInfo_P;
                        break;
                    case PayeeAccountType.Evp:
                        accountIdentificationInput.Text = SaveDataUtils.GameData.accountIdentificationInfo_V;
                        break;
                    case PayeeAccountType.Email:
                        accountIdentificationInput.Text = SaveDataUtils.GameData.accountIdentificationInfo_E;
                        break;
                }
            }

            paypalMailInput.Text = SaveDataUtils.GameData.withdrawEmailInfo;
            accPhoneMailInput.Text = SaveDataUtils.GameData.withdrawPhoneInfo;
        }
        else
        {
            SaveDataUtils.GameData.withdrawCPFInfo = CPFNumberInput.Text;
            SaveDataUtils.GameData.withdrawNameInfo = accountNameInput.Text;


            if (TryGetProfile(out var profile) && profile.PayType == E_PayeeAccountType.PIX)
            {
                var type = (PayeeAccountType)pixChannelIndex;
                switch (type)
                {
                    case PayeeAccountType.CpfCnpj:
                        SaveDataUtils.GameData.accountIdentificationInfo_C = accountIdentificationInput.Text;
                        break;
                    case PayeeAccountType.Phone:
                        SaveDataUtils.GameData.accountIdentificationInfo_P = accountIdentificationInput.Text;
                        break;
                    case PayeeAccountType.Evp:
                        SaveDataUtils.GameData.accountIdentificationInfo_V = accountIdentificationInput.Text;
                        break;
                    case PayeeAccountType.Email:
                        SaveDataUtils.GameData.accountIdentificationInfo_E = accountIdentificationInput.Text;
                        break;
                }
            }
            SaveDataUtils.GameData.withdrawEmailInfo = paypalMailInput.Text;
            SaveDataUtils.GameData.withdrawPhoneInfo = accPhoneMailInput.Text;
            SaveDataUtils.GameData.pixChannelIndex = pixChannelIndex;
            SaveDataUtils.gameStrategy.SaveData();
        }
    }

    private PayeeAccountType GetWithdrawChannel()
    {
        foreach (var item in accountTypeDropdown)
        {
            if (item.selected)
            {
                return item.PayeeAccountType;
            }
        }

        return accountTypeDropdown[0].PayeeAccountType;
    }

    public void OnClickBaXiChannel(int selectIndex)
    {
        if (accountTypeDropdown == null || accountTypeDropdown.Count == 0)
            return;

        if (selectIndex < 0 || selectIndex >= accountTypeDropdown.Count)
            return;

        // 1. 先缓存“旧渠道”的当前输入
        CacheCpfAndFullName();
        CacheCurrentPixInputByType();

        // 2. 更新选中状态
        for (int i = 0; i < accountTypeDropdown.Count; i++)
        {
            bool isSelected = (i == selectIndex);
            accountTypeDropdown[i].selected = isSelected;

            if (accountTypeDropdown[i].selectedIcon != null)
                accountTypeDropdown[i].selectedIcon.SetActive(isSelected);
        }

        // 3. 记录当前索引
        pixChannelIndex = selectIndex;

        // 4. 按新类型刷新UI
        var newType = accountTypeDropdown[selectIndex].PayeeAccountType;
        ApplyPlaceholder(newType);
        RestorePixInputByType(newType);

        // 5. 清空当前错误显示（可选）
        if (accountIdentificationErrorTra != null)
            accountIdentificationErrorTra.gameObject.SetActive(false);
    }
}
[Obfuz.ObfuzIgnore]
public enum E_PayeeAccountType
{
    Paypal,
    Pagbank,
    PIX,
    OVO,
    Dana,
}

[Obfuz.ObfuzIgnore]
public enum E_WithdrawType
{
    Real,
    Fake,
    DailyMission,
    DailyWithdraw,
}

[Serializable]
public class E_WithdrawChaanelForBaxi
{
    public PayeeAccountType PayeeAccountType;
    public GameObject selectedIcon;
    public Button btn;
    public bool selected;

    public void Init(Action callback)
    {
        selected = false;
        selectedIcon.SetActive(selected);
        btn.onClick.AddListener(() => callback?.Invoke());
    }
}

[Obfuz.ObfuzIgnore]
public enum PayeeAccountType
{
    Email,
    CpfCnpj,
    Phone,
    Evp
}
#endif
