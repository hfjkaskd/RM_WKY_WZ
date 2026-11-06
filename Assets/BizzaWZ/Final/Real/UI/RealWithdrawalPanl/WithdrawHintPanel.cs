#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

 
public class WithdrawHintPanel : MonoBehaviour
{
    public TMP_Text hintText;

    [Header("按钮")]
    [SerializeField] private BizzaButton closeBtn;

    private void Awake()
    {        closeBtn.onClick.AddListener(() => { OnClickClose(); });
    }
    public void Init(float needBonus, float minBonus)
    {
        gameObject.SetActive(true);
        hintText.text = LanguageUtils.GetFormatText("WithdrawHintPanel_Hint", WithdrawalUtil.GetCustomizedValueByCountryType(minBonus),  WithdrawalUtil.GetCustomizedValueByCountryType(needBonus));
    }

    public void Init(int level, int currentLevel)
    {
        gameObject.SetActive(true);
        var gapLevel = level - currentLevel;
        hintText.text = LanguageUtils.GetFormatText("WithdrawHintPanel_Hint4", level, gapLevel);
    }


    public void OnClickClose()
    {
        gameObject.SetActive(false);
    }
}
#endif