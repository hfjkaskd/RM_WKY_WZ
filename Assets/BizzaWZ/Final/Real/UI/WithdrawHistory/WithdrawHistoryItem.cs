#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

 
public class WithdrawHistoryItem : MonoBehaviour
{
    public TMP_Text nameTxt;
    public TMP_Text timeTxt;
    public TMP_Text emailTxt;
    public TMP_Text cpfTxt;
    public TMP_Text dueText;
    
    public GameObject successObj;
    public GameObject processingObj;
    public GameObject failObj;
    
    public Image withdrawImg;
    public TMP_Text amountTxt;
    
    [SerializeField] private PaymentConfig paymentConfig;
     
    public void Init(AccountModule.OceanShineWithdrawalRecord data)
    {
        nameTxt.gameObject.SetActive(false);
        emailTxt.gameObject.SetActive(false);
        cpfTxt.gameObject.SetActive(false);
        // amountTxt.gameObject.SetActive(false);
        
        // nameTxt.text = data.Os_Rn;
        timeTxt.text = $"{data.Os_Dat}";
        // emailTxt.text = $"Email:{data.Os_Re}";
        // cpfTxt.text = $"CPE/CNPJ::{data.Os_Cp}";
        
        amountTxt.text = $"{LanguageUtils.GetText("CurrencyToken")} {WithdrawalUtil.GetCustomizedValueByCountryType((float)data.Os_Prc)}";
        switch (AccountModule.CountryType)
        {
            case AccountModule.E_CountryType.US:
                //amountTxt.text = $"{LanguageUtils.GetText("CurrencyToken")} {data.Os_Prc}";
                emailTxt.text = $"Email:{data.Os_Ra}";
                emailTxt.gameObject.SetActive(true);
                break;
            case AccountModule.E_CountryType.BR:
                //amountTxt.text = $"{LanguageUtils.GetText("CurrencyToken")} {data.Os_Prc}";
                cpfTxt.text = $"CPF/CNPJ:{data.Os_Cp}";
                cpfTxt.gameObject.SetActive(true);
                emailTxt.text = $"Account:{data.Os_Re}";
                emailTxt.gameObject.SetActive(true);
                nameTxt.text = $"Name:{data.Os_Rn}";
                nameTxt.gameObject.SetActive(true);
                break;
            case AccountModule.E_CountryType.ID:
                //amountTxt.text = $"{LanguageUtils.GetText("CurrencyToken")} {data.Os_Prc}";
                emailTxt.text = $"Account:{data.Os_Ra}";
                emailTxt.gameObject.SetActive(true);
                nameTxt.text = $"Name:{data.Os_Rn}";
                nameTxt.gameObject.SetActive(true);
                break;
        }
        
        withdrawImg.sprite = paymentConfig.GetSpriteByPayKey(data.Os_Pym);
        
        successObj.SetActive(data.Os_Sts == 3);
        processingObj.SetActive(data.Os_Sts == 1);
        bool isFail = data.Os_Sts == 2 || data.Os_Sts > 3;
        failObj.SetActive(isFail);
        dueText.gameObject.SetActive(isFail);
        dueText.text = data.Os_Tsm;

        float length = transform.GetComponent<RectTransform>().sizeDelta.x;
        transform.GetComponent<RectTransform>().sizeDelta = isFail ? new Vector2(length, 300) : new Vector2(length, 230);

    }
}

#endif