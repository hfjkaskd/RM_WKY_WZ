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
    private float currentDisplayValue = 0f;

    private void OnEnable()
    {
        #if BIZZA_REAL_WITHDRAW
        switch (e_ItemType)
        {
            case E_ItemType.Gold:
                float _value = (float)AccountModule.Instance.Os_Current_Uso.GetBalance();
                if (AccountModule.CountryType == AccountModule.E_CountryType.BR)
                {
                    string _currentDisplayValue = WithdrawalUtil.GetCustomizedIntByCountryType2(_value);
                    valueText.text = $"{_currentDisplayValue}";
                }
                else
                {
                    valueText.text = $"{_value}";
                }

                break;

            case E_ItemType.Dollar:
                var value = ItemUtils.Get(E_ItemType.Dollar).Count;
                currentDisplayValue = WithdrawalUtil.GetCustomizedFloatByCountryType(value);
                valueText.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(currentDisplayValue)}";
                break;
        }
        #endif
        //LogUtil.Error("currentDisplayValue: " + currentDisplayValue );
        //BizzaEventSystem.Set(EventDefine.Item.ItemChanged, Refresh, true);
    }

    private void OnDisable()
    {
        valueTween?.Kill();
        //BizzaEventSystem.Set(EventDefine.Item.ItemChanged, Refresh, false);
    }

    public void Refresh()
    {
        switch (e_ItemType)
        {
            case E_ItemType.Gold:
                RefreshGold();
                break;

            case E_ItemType.Dollar:
                RefreshMoney();
                break;
        }

        // BizzaEventSystem.Emit(EventDefine.Item.ItemChanged);
    }

    private void RefreshGold()
    {
        #if BIZZA_REAL_WITHDRAW
        float targetValue = (float)AccountModule.Instance.Os_Current_Uso.GetBalance();


        valueTween?.Kill();

        //LogUtil.Error("targetValue: " + targetValue + " currentDisplayValue: " + currentDisplayValue);
        valueTween = DOVirtual.Float(currentDisplayValue, targetValue, duration, value =>
        {
            currentDisplayValue = value;
            if (AccountModule.CountryType == AccountModule.E_CountryType.BR)
            {
                string _currentDisplayValue = WithdrawalUtil.GetCustomizedIntByCountryType2(value);
                valueText.text = $"{_currentDisplayValue}";
            }
            else
            {
                valueText.text = $"{value}";
            }
            // valueText.text = value.ToString(numberFormat);
        })
        .SetEase(ease);
        #endif
    }

    private void RefreshMoney()
    {
        var value = ItemUtils.Get(E_ItemType.Dollar).Count;
        float targetValue = WithdrawalUtil.GetCustomizedFloatByCountryType(value);
        string valueNum = valueText.text;
        valueTween?.Kill();

        //LogUtil.Error("targetValue: " + targetValue + " currentDisplayValue: " + currentDisplayValue);
        valueTween = DOVirtual.Float(currentDisplayValue, targetValue, duration, value =>
        {
            value = WithdrawalUtil.GetCustomizedFloatByCountryType(value);
            currentDisplayValue = value;
            valueText.text = $"{LanguageUtils.GetText("CurrencyToken")}{WithdrawalUtil.GetCustomizedValueByCountryType(currentDisplayValue)}";
        })
        .SetEase(ease);
    }
}
#endif
