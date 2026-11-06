#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIKeepKeyboard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        KeyboardAvoider.isCanDrag = true;
        KeyboardAvoider.Instance.StopFollow(false);
        // 点击任何 UI 时，都尝试保持输入框焦点（如果正在输入）
        MobileKeyboardInputManager.Instance?.KeepFocusNextFrame();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        KeyboardAvoider.isCanDrag = false;
    }
}
#endif