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

    [SerializeField, Range(1f, 2f)] private float questionTitleScale = 1f;

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
        if (questionTitleScale > 1f)
        {
            const string closeColor = "</color>";
            int end = desc.IndexOf(closeColor, System.StringComparison.Ordinal);
            if (end >= 0)
            {
                end += closeColor.Length;
                string size = (questionTitleScale * 100f).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                desc = "<size=" + size + "%>" + desc.Substring(0, end) + "</size>" + desc.Substring(end);
            }
        }
        tMP_Text.text = desc;
    }


}
#endif
