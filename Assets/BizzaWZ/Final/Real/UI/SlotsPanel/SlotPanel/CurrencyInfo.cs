#if BIZZA_REAL_WITHDRAW
using TMPro;
using UnityEngine;
using DG.Tweening;

public class CurrencyInfo : MonoBehaviour
{
    public TMP_Text valueText;
    public E_ItemType e_ItemType;

    [Header("Tween")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private string numberFormat = "F2"; // 保留两位小数，可改成 F0 / F1 / F3

    private Tween valueTween;
    private float currentDisplayValue;
    private bool hasDisplayedValue;

    private void OnEnable()
    {
        BizzaEventSystem.On(EventDefine.Login.InitContentByCountry, RefreshImmediately);
        RefreshImmediately();
    }

    private void OnDisable()
    {
        BizzaEventSystem.Off(EventDefine.Login.InitContentByCountry, RefreshImmediately);
        valueTween?.Kill();
    }

    private void RefreshImmediately()
    {
        valueTween?.Kill();
        if (TryReadValue(out float value)) DisplayValue(value);
    }

    public void Refresh()
    {
        valueTween?.Kill();
        if (!TryReadValue(out float value)) return;
        if (!hasDisplayedValue)
        {
            DisplayValue(value);
            return;
        }

        valueTween = DOVirtual.Float(currentDisplayValue, value, duration, DisplayValue).SetEase(ease);
    }

    private bool TryReadValue(out float value)
    {
        value = 0f;
        var account = AccountModule.Instance;
        if (account?.AccountReady != true || account.Os_Current_Uso == null ||
            AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            if (!hasDisplayedValue) valueText.text = "—";
            return false;
        }

        switch (e_ItemType)
        {
            case E_ItemType.Gold:
                value = (float)account.Os_Current_Uso.GetBalance();
                return true;
            case E_ItemType.Dollar:
                value = ItemUtils.Get(E_ItemType.Dollar).Count;
                return true;
            default:
                return false;
        }
    }

    private void DisplayValue(float value)
    {
        if (e_ItemType == E_ItemType.Gold)
        {
            currentDisplayValue = value;
            valueText.text = AccountModule.CountryType == AccountModule.E_CountryType.BR
                ? WithdrawalUtil.GetCustomizedIntByCountryType2(value)
                : value.ToString();
        }
        else
        {
            currentDisplayValue = WithdrawalUtil.GetCustomizedFloatByCountryType(value);
            valueText.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(currentDisplayValue)}";
        }

        hasDisplayedValue = true;
    }
}
#endif
