#if BIZZA_REAL_WITHDRAW
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId UIWithdrawalPendingPanel = "UIWithdrawalPendingPanel";
}

public class UIWithdrawalPendingPanel : UIPageBase<UIWithdrawalPendingInfo>
{
    public static event Action<UIWithdrawalPendingResult> NetworkCallback;
    private static bool hasCachedCallbackResult;
    private static UIWithdrawalPendingResult cachedCallbackResult;

    
    private const string DefaultAmount = "";
    private const string DefaultHint = "";
    private const string DefaultFailHint = "";
    

    [Header("Data")]
    public PaymentConfig paymentList;

    [Header("Text")]
    public TMP_Text titleText;
    public TMP_Text amountText;
    public TMP_Text progressText;
    public TMP_Text hintText;
    public TMP_Text confirmText;

    [Header("Image")]
    public Image paymentImage;
    public Image progressFill;
    public Image resultIcon;
    public Sprite successResultSprite;
    public Sprite failResultSprite;

    [Header("State")]
    public GameObject progressRoot;
    public GameObject resultRoot;

    [Header("Button")]
    [SerializeField] private BizzaButton confirmButton;
    [SerializeField] private BizzaButton closeButton;

    [Header("Progress")]
    [SerializeField] private int progressTotalStep = 20;
    [SerializeField] private float progressSoftCap = 0.9f;
    [SerializeField] private float autoProgressDuration = 4f;
    [SerializeField] private float completeProgressDuration = 0.45f;

    private UIWithdrawalPendingInfo _info;
    private float _progress;
    private bool _networkReturned;
    private bool _completed;
    private UIWithdrawalPendingResult _result;

    public static void NotifyNetworkCallback()
    {
        NotifyNetworkCallback(true);
    }

    public static void NotifyNetworkCallback(bool success, string resultText = null)
    {
        NotifyNetworkCallback(new UIWithdrawalPendingResult(success, resultText));
    }

    public static void ResetNetworkCallbackState()
    {
        hasCachedCallbackResult = false;
        cachedCallbackResult = default;
    }

    private static void NotifyNetworkCallback(UIWithdrawalPendingResult result)
    {
        hasCachedCallbackResult = true;
        cachedCallbackResult = result;
        NetworkCallback?.Invoke(result);
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        confirmButton?.onClick.AddListener(OnClickConfirm);
        closeButton?.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(UIWithdrawalPendingInfo info)
    {
        _info = info;
        _progress = 0f;
        _networkReturned = false;
        _completed = false;
        _result = new UIWithdrawalPendingResult(true, null);
        NativeClose = false;

        RefreshContent();
        SetProgress(0f);
        SetActive(progressRoot, true);
        SetActive(resultRoot, false);
        SetButtonsEnabled(false);
    }

    protected override void SetListener(bool addOrRemove)
    {
        if (addOrRemove)
        {
            NetworkCallback += OnNetworkCallback;
            if (hasCachedCallbackResult)
            {
                OnNetworkCallback(cachedCallbackResult);
            }
        }
        else
        {
            NetworkCallback -= OnNetworkCallback;
        }
    }

    protected override void OnClose()
    {
        NetworkCallback -= OnNetworkCallback;
    }

    private void Update()
    {
        if (_completed)
        {
            return;
        }

        float target = _networkReturned ? 1f : progressSoftCap;
        float duration = _networkReturned ? completeProgressDuration : autoProgressDuration;
        float speed = 1f / Mathf.Max(0.01f, duration);

        if (!_networkReturned)
        {
            speed *= Mathf.Clamp01(progressSoftCap);
        }

        SetProgress(Mathf.MoveTowards(_progress, target, speed * Time.unscaledDeltaTime));

        if (_networkReturned && Mathf.Approximately(_progress, 1f))
        {
            CompleteProgress();
        }
    }

    private void RefreshContent()
    {
        
        SetText(amountText, _info.AmountText, DefaultAmount);
        SetText(hintText, _info.HintText, DefaultHint);
    

        if (paymentImage != null && paymentList != null)
        {
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(_info.PaymentIconKey))
            {
                sprite = paymentList.GetSpriteByIconKey(_info.PaymentIconKey);
            }

            if (sprite == null)
            {
                sprite = paymentList.GetSpriteByType(_info.PayType);
            }

            if (sprite != null)
            {
                paymentImage.sprite = sprite;
            }
        }
    }

    private static void SetText(TMP_Text text, string value, string fallback)
    {
        if (text != null)
        {
            text.text = string.IsNullOrEmpty(value) ? fallback : value;
        }
    }

    private void OnNetworkCallback(UIWithdrawalPendingResult result)
    {
        _result = result;
        _networkReturned = true;
        ResetNetworkCallbackState();
    }

    private void SetProgress(float value)
    {
        _progress = Mathf.Clamp01(value);
        if (progressFill != null)
        {
            progressFill.fillAmount = _progress;
        }

        RefreshProgressText();
    }

    private void RefreshProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        if (progressTotalStep <= 0)
        {
            progressText.text = $"{Mathf.RoundToInt(_progress * 100f)}%";
            return;
        }

        int step = Mathf.Clamp(Mathf.RoundToInt(_progress * progressTotalStep), 0, progressTotalStep);
        progressText.text = $"{step}/{progressTotalStep}";
    }

    private void CompleteProgress()
    {
        _completed = true;
        NativeClose = true;
        SetProgress(1f);
        RefreshResultState();
        SetButtonsEnabled(true);
        _info.OnProgressComplete?.Invoke(_result.Success);
    }

    private void RefreshResultState()
    {
        SetActive(progressRoot, false);
        SetActive(resultRoot, true);

        if (resultIcon != null)
        {
            resultIcon.sprite = _result.Success ? successResultSprite : failResultSprite;
        }

        if (_result.Success)
        {
            string successHint = LanguageUtils.GetText("UIWithdrawalConfirm_Success");
            SetText(hintText, successHint, DefaultHint);
            return;
        }

        string failHint = string.IsNullOrEmpty(_info.FailHintText) ? DefaultFailHint : _info.FailHintText;
        SetText(hintText, _result.Message, failHint);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        SetButtonEnabled(confirmButton, enabled);
        SetButtonEnabled(closeButton, enabled);
    }

    private static void SetButtonEnabled(BizzaButton button, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = enabled;
        button.enabled = enabled;

        if (button.TryGetComponent<Image>(out var image))
        {
            Color color = image.color;
            color.a = enabled ? 1f : 0.6f;
            image.color = color;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void OnClickConfirm()
    {
        if (!_completed)
        {
            return;
        }

        _info.OnConfirm?.Invoke();
        CloseResultPanel();
    }

    private void OnClickClose()
    {
        if (_completed)
        {
            CloseResultPanel();
        }
    }

    private void CloseResultPanel()
    {
        _info.OnResultClose?.Invoke(_result.Success);
        CloseSelf();
    }
}

public struct UIWithdrawalPendingInfo
{
    public E_PayeeAccountType PayType;
    public string PaymentIconKey;
    public string AmountText;
    public string TitleText;
    public string HintText;
    public string SuccessHintText;
    public string FailHintText;
    public string ConfirmText;
    public Action OnConfirm;
    public Action<bool> OnProgressComplete;
    public Action<bool> OnResultClose;

    public UIWithdrawalPendingInfo(
        E_PayeeAccountType payType,
        string amountText,
        string paymentIconKey = null,
        Action onConfirm = null,
        Action<bool> onProgressComplete = null,
        string titleText = null,
        string hintText = null,
        string successHintText = null,
        string failHintText = null,
        string confirmText = null,
        Action<bool> onResultClose = null)
    {
        PayType = payType;
        PaymentIconKey = paymentIconKey;
        AmountText = amountText;
        TitleText = titleText;
        HintText = hintText;
        SuccessHintText = successHintText;
        FailHintText = failHintText;
        ConfirmText = confirmText;
        OnConfirm = onConfirm;
        OnProgressComplete = onProgressComplete;
        OnResultClose = onResultClose;
    }
}

public struct UIWithdrawalPendingResult
{
    public bool Success;
    public string Message;

    public UIWithdrawalPendingResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}
#endif
