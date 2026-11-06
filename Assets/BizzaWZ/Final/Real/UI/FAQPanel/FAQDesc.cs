#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FAQDesc : MonoBehaviour
{
    public FAQPanel fAQPanel;

    public ColorReplace titleColor => fAQPanel.titleColor;
    public ColorReplace contentColor => fAQPanel.contentColor;
    public ColorReplace highlightColor => fAQPanel.highlightColor;

    public TMP_Text tMP_Text;

    public string key;

    private string desc;

    public void Start()
    { 
        Refresh();
    }

    [Button("Refresh")]
    public void Refresh()
    {
        desc = LanguageUtils.GetText(key);
        desc = desc.GetReplaceDesc(titleColor).GetReplaceDesc(contentColor).GetReplaceDesc(highlightColor);
        tMP_Text.text = desc;
    }


}
#endif
