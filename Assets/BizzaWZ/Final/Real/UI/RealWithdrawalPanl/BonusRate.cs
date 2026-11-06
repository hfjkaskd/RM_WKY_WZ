#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

 
public class BonusRate : MonoBehaviour
{
    public Image coin_normal;
    public Image coin_Bubble;
    
    public TMP_Text bonusRateTxt;


    public void Init(string bonusRate, bool isWitheColor, bool showBubble = true)
    {
        LogLogger.LogVerbose(BaseConst.LOG_Info, $"[BonusRate.Init] this={(this? "OK":"NULL")} " +
                  $"txt={(bonusRateTxt? "OK":"NULL")} " +
                  $"bubble={(coin_Bubble? "OK":"NULL")} " +
                  $"normal={(coin_normal? "OK":"NULL")} " +
                  $"bonusRate={bonusRate}");
        bonusRateTxt.text = bonusRate;
        coin_Bubble.gameObject.SetActive(showBubble);
        // coin_normal.gameObject.SetActive(!showBubble);
    }
}
#endif