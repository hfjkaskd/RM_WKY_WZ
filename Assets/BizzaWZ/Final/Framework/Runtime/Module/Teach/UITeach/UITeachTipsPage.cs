
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class UITeachTipsPage : UIPageBase<UITeachTipsPage.InitParam>, IPointerClickHandler
{
    public struct InitParam
    {
        public string content;
        public int posIdx;
        public bool block;
        public float alpha;
        public float heightValue;
    }

    public PageId LegacyPageType => UIPageIds.UI_TeachTip;

    public Image bg = null;
    public Image panel = null;
    public TMP_Text content = null;
    public RectTransform[] panels;

    private float clickCD = 0;

    protected override void OnOpen(InitParam param)
    {
        content.text = LanguageUtils.GetText(param.content);
        if (param.posIdx >= 0)
        {
            panel.rectTransform.position = panels[param.posIdx].position;
        }
        else
        {
            var parentRect = panel.transform.parent as RectTransform;
            var rect = parentRect.rect;
            float yPos = Mathf.Lerp(rect.yMin, rect.yMax, param.heightValue);
            panel.transform.position = parentRect.TransformPoint(new Vector3(0, yPos));
        }

        bg.raycastTarget = param.block;
        Color color = bg.color;
        color.a = param.alpha / 255;
        bg.color = color;
    }

    private void Update()
    {
        clickCD -= Time.unscaledDeltaTime;
    }

    protected override void OnShow()
    {
    }

    protected override void OnHide()
    {
    }

    protected override void OnClose()
    {
    }

    public void OnClick()
    {
        if (clickCD > 0)
        {
            return;
        }

        CloseSelf();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }

}

public static partial class UIPageIds
{
    public static readonly PageId UI_TeachTip = "UI_TeachTip";
}
