#if BIZZA_REAL_WITHDRAW
using UnityEngine;
using UnityEngine.EventSystems;

public class InputerPageScroll : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static InputerPageScroll Instance;
    
    public RectTransform content;

    public float minY = -500f;
    public float maxY = 800f;

    private void Awake()
    {
        Instance = this;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanScroll())
            return;
        KeyboardAvoider.isCanDrag = true;
        var cur = MobileKeyboardInputManager.Instance?.CurrentInput;
        if (cur != null) cur.interactable = false; // 或者关 RaycastTarget（见下）

        // 拖拽期间暂停抢焦点，否则会打断拖拽
        MobileKeyboardInputManager.Instance?.SetSuspendKeepFocus(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanScroll())
            return;

        var canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;

        Vector2 delta = eventData.delta / scale;

        Vector2 pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y + delta.y, minY, maxY);
        content.anchoredPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        KeyboardAvoider.isCanDrag = false;
        var cur = MobileKeyboardInputManager.Instance?.CurrentInput;
        if (cur != null) cur.interactable = true;
        MobileKeyboardInputManager.Instance?.SetSuspendKeepFocus(false);
        
        // 拖拽结束，如果你仍想保持键盘，下一帧抢回
        MobileKeyboardInputManager.Instance?.KeepFocusNextFrame();
    }

    private bool CanScroll()
    {
        if (MobileKeyboardInputManager.Instance == null)
            return false;

        // 只有键盘打开才允许手动滑动（按你原需求）
        return MobileKeyboardInputManager.Instance.IsOpen;
    }
}
#endif