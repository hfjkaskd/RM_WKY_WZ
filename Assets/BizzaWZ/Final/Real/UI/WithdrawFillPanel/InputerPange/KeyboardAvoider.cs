#if BIZZA_REAL_WITHDRAW
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class KeyboardAvoider : MonoBehaviour
{
    public static KeyboardAvoider Instance;

    [Header("需要被顶起的根节点")]
    public RectTransform targetRoot;

    public RectTransform ViewportRect;
    public float offset = 20f;

    [Header("跟随时间（越小越跟手，越大越柔和）")]
    public float smoothTime = 0.08f;

    [Header("键盘高度小于此值时认为已收起（防抖）")]
    public float keyboardDeadZone = 1f;

    [Header("键盘高度瞬间为 0 时的宽限时间（切换焦点不回弹）")]
    public float closeGraceTime = 0.2f;

    private float originalHeight;
    private float currentOffset;
    private float targetOffset;
    private float velocity;

    private Coroutine _followCo;
    private bool _isFollowing = false;

    public static bool isCanDrag = false;

    // 记录最后一次“可靠的非零键盘高度”
    private float _lastNonZeroKb = 0f;
    private float _lastNonZeroTime = -999f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        originalHeight = ViewportRect.rect.height / 2f - targetRoot.rect.height / 2f + offset;
        SetOffsetInstant(0f);
        StopFollow();
        // 注意：不再自动 StartCoroutine
        // _followCo = StartCoroutine(FollowKeyboardLoop());
    }

    /// <summary>
    /// 外部调用：开始实时跟随键盘（开启协程）
    /// </summary>
    [Button("StartFollow")]
    public void StartFollow(bool stepOnceImmediately = true)
    {
        if (_isFollowing) return;

        _isFollowing = true;

        // 开始跟随前，先重置一下缓冲，避免旧数据影响
        velocity = 0f;
        // 不强制清 lastNonZero，这样切换时更平滑；你也可以按需清零
        // _lastNonZeroKb = 0f; _lastNonZeroTime = -999f;

        if (stepOnceImmediately)
            StepOnce();

        _followCo = StartCoroutine(FollowKeyboardLoop());
    }

    /// <summary>
    /// 外部调用：停止实时跟随键盘（停止协程）
    /// </summary>
    /// <param name="resetToZero">停止时是否立刻缩回</param>
    [Button("StopFollow")]
    public void StopFollow(bool resetToZero = false)
    {
        if (!_isFollowing) return;

        _isFollowing = false;

        if (_followCo != null)
        {
            StopCoroutine(_followCo);
            _followCo = null;
        }

        if (resetToZero)
            SetOffsetInstant(0f);
    }

    /// <summary>
    /// 外部可选：不想开协程的话，你自己每帧调用一次
    /// </summary>
    public void StepOnce()
    {
        if (isCanDrag) return;

        float rawKb = MobileKeyboardInputManager.Instance.GetKeyboardHeight;
        bool kbIsZero = rawKb <= keyboardDeadZone;

        if (!kbIsZero)
        {
            _lastNonZeroKb = rawKb;
            _lastNonZeroTime = Time.unscaledTime;
        }

        float kbForCalc = rawKb;

        if (kbIsZero)
        {
            bool expectKeyboard = MobileKeyboardInputManager.Instance.ExpectKeyboard;
            bool withinGrace = (Time.unscaledTime - _lastNonZeroTime) <= closeGraceTime;

            if (expectKeyboard && withinGrace && _lastNonZeroKb > keyboardDeadZone)
                kbForCalc = _lastNonZeroKb;
            else
                kbForCalc = 0f;
        }

        if (kbForCalc <= keyboardDeadZone)
        {
            targetOffset = 0f;
        }
        else
        {
            targetOffset = (kbForCalc < originalHeight)
                ? 0f
                : Mathf.Max(0, targetRoot.rect.height / 2f - (ViewportRect.rect.height / 2f - kbForCalc));
        }

        currentOffset = Mathf.SmoothDamp(currentOffset, targetOffset, ref velocity, smoothTime);
        ApplyOffset(currentOffset);
    }

    private IEnumerator FollowKeyboardLoop()
    {
        while (_isFollowing)
        {
            StepOnce();
            yield return null;
        }
    }

    private void ApplyOffset(float y)
    {
        var pos = targetRoot.anchoredPosition;
        pos.y = y;
        targetRoot.anchoredPosition = pos;
    }

    public void SetOffsetInstant(float y)
    {
        currentOffset = y;
        targetOffset = y;
        velocity = 0f;
        ApplyOffset(y);
    }

    // 你原来的 Debug 方法保留
    public void OnDeubgInfo()
    {
        var canvas = ViewportRect.GetComponentInParent<Canvas>();

        Debug.LogWarning($"Screen.height={Screen.height}");
        Debug.LogWarning($"canvas.scaleFactor={canvas.scaleFactor}");
        Debug.LogWarning($"CanvasRect.height={canvas.GetComponent<RectTransform>().rect.height}");

        Debug.LogWarning($"Viewport rect.height={ViewportRect.rect.height}, sizeDelta={ViewportRect.sizeDelta}, lossyScale={ViewportRect.lossyScale}");
        Debug.LogWarning($"Target rect.height={targetRoot.rect.height}, sizeDelta={targetRoot.sizeDelta}, lossyScale={targetRoot.lossyScale}");

        float kb = MobileKeyboardInputManager.Instance.GetKeyboardHeight;
        Debug.LogWarning($"Keyboard raw={kb}");
        Debug.LogWarning($"Keyboard canvasEst(raw/scaleFactor)={kb / canvas.scaleFactor}");
    }
}

#endif