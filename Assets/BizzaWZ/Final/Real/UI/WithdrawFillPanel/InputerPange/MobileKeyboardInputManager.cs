#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileKeyboardInputManager : MonoBehaviour
{
    public static MobileKeyboardInputManager Instance;

    [Header("Debug")]
    [SerializeField] private TMP_InputField lastInput;
    [SerializeField] private TMP_InputField currentInput;

    private RectTransform _canvasRect;

    // 拖拽/特殊交互时暂停“抢回焦点”
    private bool _suspendKeepFocus;
    private Coroutine _reselectCo;

    public bool IsOpen => GetKeyboardHeight > 0;
    private float _expectKeyboardUntil = 0f;
    [SerializeField] private float _keyboardCanvasHeight = 0;
    /// <summary>
    /// 是否“期望键盘保持打开”（输入框聚焦 or 刚发生切换聚焦）
    /// </summary>
    public bool ExpectKeyboard
    {
        get
        {
            // currentInput.isFocused 对 TMP_InputField 是可靠的（正在编辑时为 true）
            bool focused = currentInput != null && currentInput.isFocused;
            bool inSwitchWindow = Time.unscaledTime < _expectKeyboardUntil;
            return focused || inSwitchWindow;
        }
    }

    public float GetKeyboardHeight
    {
        get
        {
            float keyboardCanvasHeight = 0;

#if UNITY_EDITOR
            keyboardCanvasHeight = 800f;
#endif

            if (_canvasRect == null)
            {
                _canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            
            var dic = DeviceNativeBridge.AndroidGetKeyboardHeight((int)_canvasRect.rect.height);
            if (dic != null && dic.TryGetValue("keyboardInCanvasHeight", out int h))
            {
                keyboardCanvasHeight = Mathf.Max(0, h);
            }
#endif
            return keyboardCanvasHeight;
        }
    }

    private void Awake()
    {
        Instance = this;
        _canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    private void Start()
    {
       
    }

    /// <summary> 拖拽开始/结束时调用，暂停/恢复抢焦点 </summary>
    public void SetSuspendKeepFocus(bool v)
    {
        _suspendKeepFocus = v;
        if (v) CancelKeepFocus();
    }
    
    public void CancelKeepFocus()
    {
        if (_reselectCo != null)
        {
            StopCoroutine(_reselectCo);
            _reselectCo = null;
        }
    }

    /// <summary>
    /// 请求输入焦点（切换输入框时调用）
    /// </summary>
    public void Focus(TMP_InputField input)
    {
        if (input == null) return;

        // 记录旧输入框
        if (currentInput != null && currentInput != input)
        {
            lastInput = currentInput; // 修复：应该保存旧的 currentInput
        }

        currentInput = input;
        
        _expectKeyboardUntil = Time.unscaledTime + 0.2f; // 0.15~0.25 都行

        // 注意：Focus 时不应该被 suspend 阻止，否则切换输入框会失效
       // currentInput.shouldHideMobileInput = false; -- 
        currentInput.ActivateInputField();
        EventSystem.current.SetSelectedGameObject(currentInput.gameObject);
        KeyboardAvoider.Instance.StartFollow();// 修改

        CancelKeepFocus();
    }

    /// <summary>
    /// 下一帧把焦点抢回当前输入框（用于点到别的 UI 不让键盘消失）
    /// </summary>
    public void KeepFocusNextFrame()
    {
        if (currentInput == null) return;
        if (_suspendKeepFocus) return;

        CancelKeepFocus();

        _reselectCo = StartCoroutine(ReSelectNextFrame());
    }

    private IEnumerator ReSelectNextFrame()
    {
        yield return null;

        if (currentInput == null) yield break;
        if (_suspendKeepFocus) yield break;

        if (EventSystem.current.currentSelectedGameObject == currentInput.gameObject)
            yield break;

        currentInput.ActivateInputField();
        EventSystem.current.SetSelectedGameObject(currentInput.gameObject);
    }

    public TMP_InputField CurrentInput => currentInput;
    
    [SerializeField] private float hiddenStableTime = 0.1f; // 连续为0多久算真正关闭
    [SerializeField] private float zeroDeadZone = 1f;        // 小于等于这个当作0（防抖）
    private float _zeroStartTime = -1f;                      // 开始连续为0的时间点
    private bool _hasClosedTriggered = false;                // 防止重复触发
    
    
    
    
    private void Update()
    {
        float h = GetKeyboardHeight;

        // 1) 高度>deadZone：认为键盘开着，重置“连续为0”计时，并允许下次触发
        if (h > zeroDeadZone)
        {
            _zeroStartTime = -1f;
            _hasClosedTriggered = false;
            return;
        }

        // 2) 这里认为高度为0（或接近0）
        if (_zeroStartTime < 0f)
            _zeroStartTime = Time.unscaledTime;

        // 3) 连续为0超过阈值：判定“确实关闭”，只触发一次
        if (!_hasClosedTriggered && (Time.unscaledTime - _zeroStartTime) >= hiddenStableTime)
        {
            _hasClosedTriggered = true;

            // 你要的：关闭键盘就恢复原位
            KeyboardAvoider.Instance.StartFollow();
            // 如果你不用StartFollow/StopFollow，也可以换成：
            // KeyboardAvoider.Instance.SetOffsetInstant(0f);  // 前提：SetOffsetInstant是public
        }
    }
    
    
}
#endif