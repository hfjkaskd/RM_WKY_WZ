using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedInputFieldPlugin;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeyBoardPanel : MonoBehaviour
#if (UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR)
    , IBeginDragHandler, IDragHandler, IEndDragHandler
#endif
{
    [Header("References")]
    [SerializeField] private RectTransform panel;

    [Header("Keyboard Move")]
    [SerializeField] private float transitionTime = 0.25f;
    [SerializeField] private float desiredGapToKeyboard = 350;

    private Canvas canvas;
    private Vector2 originalPanelPos;

    public float autoOffsetY;
    private Vector2 startPos;
    private Vector2 endPos;
    private float currentTime;

    private int lastKeyboardHeight;
    private GameObject lastSelectedObject;

    private Canvas Canvas
    {
        get
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }
            return canvas;
        }
    }

    private void Start()
    {
        if (panel == null)
        {
            Debug.LogError("[KeyBoardPanel] Panel is null.");
            enabled = false;
            return;
        }

        if (Canvas == null)
        {
            Debug.LogError("[KeyBoardPanel] Canvas not found in parent.");
            enabled = false;
            return;
        }

        originalPanelPos = panel.anchoredPosition;
        currentTime = transitionTime;
    }

    private void OnEnable()
    {
        NativeKeyboardManager.AddKeyboardHeightChangedListener(OnKeyboardHeightChanged);

        StartCoroutine(DelayDo());
    }

    private IEnumerator DelayDo()
    {
        yield return new WaitForEndOfFrame();
        _originMap = new();
        var AdvancedInputFields = GetComponentsInChildren<AdvancedInputField>();
        Vector3[] corners = new Vector3[4];
        foreach (var v in AdvancedInputFields)
        {
            v.gameObject.GetComponent<RectTransform>().GetWorldCorners(corners);
            float inputBottomScreenY;
            inputBottomScreenY = corners[0].y;
            _originMap.Add(v, inputBottomScreenY);
        }

        panelSizeRect.GetWorldCorners(corners);
        _normalPosDelta = - corners[0].y;
    }

    public RectTransform panelSizeRect;
    private float _normalPosDelta = 0;
    private Dictionary<AdvancedInputField, float> _originMap = new();

    private void OnDisable()
    {
        NativeKeyboardManager.RemoveKeyboardHeightChangedListener(OnKeyboardHeightChanged);
    }

    private void Update()
    {
        if (_needAutoMove)
        {
            if (currentTime < transitionTime)
            {
                currentTime += Time.deltaTime;
                if (currentTime > transitionTime)
                {
                    currentTime = transitionTime;
                }

                float t = transitionTime <= 0.0001f ? 1f : currentTime / transitionTime;
                panel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                
            }
        }


        if (lastKeyboardHeight > 0 && dragable == true)
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected != lastSelectedObject)
            {
                lastSelectedObject = selected;

                if (selected.GetComponent<AdvancedInputField>() != null)
                {
                    RecalcAutoOffset(lastKeyboardHeight);
                    AnimateToTarget();
                }
            }
        }
    }

    private void OnKeyboardHeightChanged(int keyboardHeight)
    {
        lastKeyboardHeight = keyboardHeight;

        if (keyboardHeight > 0)
        {
            RecalcAutoOffset(keyboardHeight);
        }
        else
        {
            autoOffsetY = 0f;
            lastSelectedObject = null;
        }

        AnimateToTarget();
    }

    private bool _needAutoMove;
    private void RecalcAutoOffset(int keyboardHeight)
    {
        if (EventSystem.current == null)
        {
           // autoOffsetY = 0f;
            return;
        }

        GameObject targetObject = EventSystem.current.currentSelectedGameObject;
        if (targetObject == null)
        {
            //autoOffsetY = 0f;
            return;
        }

        AdvancedInputField inputField = targetObject.GetComponent<AdvancedInputField>();
        if (inputField == null)
        {
        // autoOffsetY = 0f;
            return;
        }

        RectTransform target = targetObject.GetComponent<RectTransform>();
        if (target == null)
        {
            //autoOffsetY = 0f;
            return;
        }

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        float inputBottomScreenY;

        float nowButtonScreenY = 0;
        if (Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            inputBottomScreenY = corners[0].y;
        }
        else
        {
            Camera cam = Canvas.worldCamera;
            if (cam == null)
            {
                autoOffsetY = 0f;
                return;
            }

            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            inputBottomScreenY = screenPoint.y;
        }

        nowButtonScreenY = inputBottomScreenY;
        if (_originMap.ContainsKey(inputField))
        {
            inputBottomScreenY = _originMap[inputField];
        }

        _needAutoMove = nowButtonScreenY < inputBottomScreenY + _normalPosDelta + lastKeyboardHeight;

        float keyboardTopScreenY = keyboardHeight;
        float currentGapPx = inputBottomScreenY - keyboardTopScreenY;
        float desiredGapPx = desiredGapToKeyboard * Canvas.scaleFactor;

        float needMovePx = desiredGapPx - currentGapPx;

        if (needMovePx <= 0f)
        {
            autoOffsetY = 0f;
            return;
        }

        autoOffsetY = needMovePx / Canvas.scaleFactor;
    }

    private void AnimateToTarget()
    {
        dragOffsetY = 0;
        startPos = panel.anchoredPosition;
        endPos = new Vector2(originalPanelPos.x, originalPanelPos.y + autoOffsetY + dragOffsetY);
        currentTime = 0f;
        float height = GetInputToKeyboardHeight();
        if (height <= 0 && height != -1)
        {
            // endPos = new Vector2(originalPanelPos.x, originalPanelPos.y + desiredGapToKeyboard);
            dragOffsetY = 0;
        }
    }

#if (UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR)
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 不做也行，留给你扩展
    }

    public float dragOffsetY = 0f;
    [LabelText("是否可以拖拽")] public bool dragable = true;
    public void OnDrag(PointerEventData eventData)
    {
        if (!dragable) return;
        // if (!enableDragWhenKeyboardVisible) return;
        if (lastKeyboardHeight <= 0) return; // 只在键盘出现时允许拖动（可按需要改成一直可拖）

        // 注意：PointerEventData.delta 是屏幕像素，需要除以 canvas.scaleFactor 才是 UI 坐标增量
        float deltaY = eventData.delta.y / Canvas.scaleFactor;
        dragOffsetY += deltaY;

        // 立即更新目标（拖动要跟手）
        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, panel.anchoredPosition.y + deltaY);

        // 同时更新动画目标，避免松手后被动画拉回
        AnimateToTarget();
        currentTime = transitionTime; // 取消平滑，保持跟手（也可以不取消）
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 你也可以在这里做惯性、回弹等
    }
#endif

    public float GetInputToKeyboardHeight()
    {
        // 如果没有键盘高度或者没有选择输入框，返回 -1 表示无效值
        if (lastKeyboardHeight <= 0 || EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
        {
            return -1f;
        }

        // 获取当前选择的输入框
        GameObject targetObject = EventSystem.current.currentSelectedGameObject;
        if (targetObject.GetComponent<AdvancedInputField>() == null)
        {
            return -1f;
        }

        RectTransform target = targetObject.GetComponent<RectTransform>();

        // 获取输入框的底部位置
        if (!TryGetInputBottomScreenY(target, out float inputBottomScreenY))
        {
            return -1f;
        }

        // 计算键盘顶部的位置
        float keyboardTopScreenY = lastKeyboardHeight;

        // 返回输入框底部到键盘顶部的距离
        return inputBottomScreenY - keyboardTopScreenY;
    }

    private bool TryGetInputBottomScreenY(RectTransform target, out float inputBottomScreenY)
    {
        inputBottomScreenY = 0f; Vector3[] corners = new Vector3[4]; target.GetWorldCorners(corners); // 0 = 左下 
        Vector3 bottomLeftWorld = corners[0];
        if (Canvas.renderMode == RenderMode.ScreenSpaceOverlay) { inputBottomScreenY = bottomLeftWorld.y; return true; }
        Camera cam = Canvas.worldCamera; if (cam == null)
        {
            Debug.LogWarning("[KeyBoardPanel] Canvas.worldCamera is null.");
            return false;
        }
        inputBottomScreenY = RectTransformUtility.WorldToScreenPoint(cam, bottomLeftWorld).y; return true;
    }
}