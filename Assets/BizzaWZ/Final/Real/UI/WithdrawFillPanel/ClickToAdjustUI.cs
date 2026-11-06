#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

 
public class ClickToAdjustUI : MonoBehaviour, IPointerDownHandler, ISelectHandler
{
    public System.Action onClicked;

    public void OnPointerDown(PointerEventData eventData)
    {
        onClicked?.Invoke();
    }

    public void OnSelect(BaseEventData eventData)
    {
        // 有些情况下是“选中”而不是 pointer down（例如 Tab/代码触发）
        onClicked?.Invoke();
    }
}
#endif