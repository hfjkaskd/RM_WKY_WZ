#if BIZZA_REAL_WITHDRAW
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WithdrawProgress : MonoBehaviour
{
    [Header("UI")]
    public Image progressImg;
    public Image paymentImg;
    public TMP_Text progressTxt;

    [Header("Config")]
    public PaymentConfig payCfg;
    public WithDrawMissionSO USMission;
    public WithDrawMissionSO IDMission;

    [Header("Animation")]
    public float animDuration = 0.6f; 
    public bool alwaysFromZero = true;
    public Ease ease = Ease.OutCubic;
    public UnityEvent onReachCurrentValue;

    private Tween _tween;

    private void OnEnable()
    {
        float cur = ItemUtils.GetItemCount(E_ItemType.Dollar);
        float target = AccountModule.CountryType == AccountModule.E_CountryType.ID
            ? IDMission.withdrawMoney
            : USMission.withdrawMoney;

        if (cur >= target)
        {
            gameObject.SetActive(false);
            return;
        }

        // 文案
        string earn = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(target - cur)}";
        string withdraw = $"{LanguageUtils.GetText("CurrencyToken")}{target}";
        progressTxt.text = LanguageUtils.GetFormatText("WithdrawProgressHint", earn, withdraw);

        // icon
        paymentImg.sprite = AccountModule.CountryType switch
        {
            AccountModule.E_CountryType.ID => payCfg.GetSmallSpriteByType(E_PayeeAccountType.Dana),
            AccountModule.E_CountryType.US => payCfg.GetSmallSpriteByType(E_PayeeAccountType.Paypal),
            AccountModule.E_CountryType.BR => payCfg.GetSmallSpriteByType(E_PayeeAccountType.PIX),
            _ => paymentImg.sprite
        };

        float targetFill = Mathf.Clamp01(cur / target);
        float startFill = alwaysFromZero ? 0f : progressImg.fillAmount;

        progressImg.fillAmount = startFill;

        // 确保不叠加
        _tween?.Kill();

        _tween = progressImg
            .DOFillAmount(targetFill, animDuration)
            .SetEase(ease)
            .SetUpdate(true) // 不受 Time.timeScale 影响（UI 很重要）
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                onReachCurrentValue?.Invoke();
            });
    }

    private void OnDisable()
    {
        // 非必须，但好习惯
        _tween?.Kill();
        _tween = null;
    }
}
#endif