#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


 
public class WithdrawalResult : MonoBehaviour
{
    public TMP_Text TitleText;
    public TMP_Text ResultText;


    public void Init(bool isSuccess)
    {
        if (isSuccess)
        {
            TitleText.text = "SUCCESS";
            ResultText.text = "Withdrawal successful";
        }
        else
        {
            TitleText.text = "FAIL";
            ResultText.text = "Withdrawal failed";
        }
    }
}
#endif