#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

 
public class ScrollDragFocusGuard : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private List<TMP_InputField> inputFields = new(); // 可手动拖，也可运行时填
    [SerializeField] private bool disableRaycastWhileDragging = true;

    // 由你的主脚本注入/或外部设置
    public TMP_InputField CurInput { get; set; }
    public bool IsDragging { get; private set; }

    private readonly List<CanvasGroup> _canvasGroups = new();

    private void Awake()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();

        if (disableRaycastWhileDragging)
        {
            _canvasGroups.Clear();
            foreach (var f in inputFields)
            {
                if (f == null) continue;
                var cg = f.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    Debug.LogError($"{f.name} prefab is missing CanvasGroup.");
                    continue;
                }

                _canvasGroups.Add(cg);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect == null || !scrollRect.vertical) return;
        KeyboardAvoider.isCanDrag = true;
        IsDragging = true;
        
        MobileKeyboardInputManager.Instance?.SetSuspendKeepFocus(true);

        if (disableRaycastWhileDragging)
        {
            foreach (var cg in _canvasGroups)
                if (cg) cg.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 让ScrollRect照常滚
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        IsDragging = false;
        KeyboardAvoider.isCanDrag = false;
        if (disableRaycastWhileDragging)
        {
            foreach (var cg in _canvasGroups)
                if (cg) cg.blocksRaycasts = true;
        }

        // 结束滚动后，如果你希望回到原输入框（且它还存在）
        if (CurInput != null && EventSystem.current != null && !EventSystem.current.alreadySelecting)
            EventSystem.current.SetSelectedGameObject(CurInput.gameObject);
    }
}
#endif
