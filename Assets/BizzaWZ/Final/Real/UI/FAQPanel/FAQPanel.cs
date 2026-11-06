#if BIZZA_REAL_WITHDRAW
using System;
#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public partial class UIPageIds
{
    public static readonly PageId QFA = "FAQPanel";
}

public class FAQPanel : UIPageBase
{
    [Header("按钮")]
    [SerializeField] private BizzaButton clickBtn;

#if UNITY_EDITOR
    public Color color;
#endif 
    public ColorReplace titleColor = new ColorReplace
    {
      replaceKey = "{titleColor}",
      replaceValue = "#242eb8"
    };
    public ColorReplace contentColor = new ColorReplace
    {
      replaceKey = "{contentColor}",
      replaceValue = "#5AAE32"
    };
    public ColorReplace highlightColor = new ColorReplace
    {
      replaceKey = "{highlightColor}",
      replaceValue = "#FF0000"
    };

    protected override void OnAwake()
    {

        clickBtn.onClick.AddListener(OnClickCloseBtn);
    }


    protected override void OnOpen()
    {

    }

    protected override void OnClose()
    {

    }

    public void OnClickCloseBtn()
    {
        UIModule.Instance.ClosePage(UIPageIds.QFA);
    }

    [Button("刷新")]
    public void Refresh()
    {
        var descs = GetComponentsInChildren<FAQDesc>();
        foreach (var desc in descs)
        {
            desc.Refresh();
        }
    }
}
#endif

[Serializable]
public struct ColorReplace
{
    public string replaceKey;
    public string replaceValue;
}
#endif


public static class ColorReplaceExt
{
    #if BIZZA_REAL_WITHDRAW
    public static string GetReplaceDesc(this string desc, ColorReplace colorReplace)
    {
        return desc.Replace(colorReplace.replaceKey, colorReplace.replaceValue);
    }
    #endif
}