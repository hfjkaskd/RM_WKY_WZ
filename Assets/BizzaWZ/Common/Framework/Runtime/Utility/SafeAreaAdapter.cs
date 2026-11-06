using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
[ExecuteInEditMode]
public class SafeAreaAdapter : MonoBehaviour
{
    [Header("适配设置")]
    [Tooltip("启用安全区域适配")]
    public bool enableSafeArea = true;

    [Tooltip("适配屏幕顶部（如刘海、挖孔屏）")]
    public bool adaptTop = true;

    [Tooltip("适配屏幕底部（如虚拟按键条）")]
    public bool adaptBottom = true;

    [Tooltip("适配屏幕左侧")]
    public bool adaptLeft = true;

    [Tooltip("适配屏幕右侧")]
    public bool adaptRight = true;

    public float minTopOffset;
    public float minBottomOffset;
    public float minLeftOffset;
    public float minRightOffset;

    [Header("额外边距")]
    [Tooltip("在安全区域基础上额外增加的边距")]
    public float extraTopMargin = 0f;
    public float extraBottomMargin = 0f;
    public float extraLeftMargin = 0f;
    public float extraRightMargin = 0f;

    [Header("调试信息")]
    [SerializeField] private Rect lastSafeArea = Rect.zero;
    [SerializeField] private Vector2 lastScreenSize = Vector2.zero;
    [SerializeField] private Vector4 appliedOffsets = Vector4.zero;

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (rectTransform == null)
        {
            Debug.LogError("SafeAreaAdapter 需要 RectTransform 组件");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (enableSafeArea)
        {
            ApplySafeArea();
        }
    }

    private void Update()
    {
        // 在编辑模式下或运行时检测屏幕尺寸和安全区域变化
        if (enableSafeArea && HasScreenPropertiesChanged())
        {
            ApplySafeArea();
        }
    }

    /// <summary>
    /// 检查屏幕尺寸或安全区域是否发生变化
    /// </summary>
    private bool HasScreenPropertiesChanged()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        bool changed = safeArea != lastSafeArea || screenSize != lastScreenSize;

        if (changed)
        {
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
        }

        return changed;
    }

    /// <summary>
    /// 应用安全区域适配
    /// </summary>
    public void ApplySafeArea()
    {
        if (rectTransform == null || canvas == null)
            return;

        Rect safeArea = Screen.safeArea;

        // 获取画布的渲染模式
        bool isScreenSpaceOverlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
        bool isScreenSpaceCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera;

        // 计算安全区域的偏移量
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        // 计算各边的安全区域距离
        float safeAreaTop = screenSize.y - (safeArea.y + safeArea.height);
        float safeAreaBottom = safeArea.y;
        float safeAreaLeft = safeArea.x;
        float safeAreaRight = screenSize.x - (safeArea.x + safeArea.width);

        // 根据设置应用适配
        float topOffset = adaptTop ? safeAreaTop + extraTopMargin : extraTopMargin;
        float bottomOffset = adaptBottom ? safeAreaBottom + extraBottomMargin : extraBottomMargin;
        float leftOffset = adaptLeft ? safeAreaLeft + extraLeftMargin : extraLeftMargin;
        float rightOffset = adaptRight ? safeAreaRight + extraRightMargin : extraRightMargin;

        // 转换到 Canvas 坐标系（如果需要）
        if (isScreenSpaceCamera && canvas.worldCamera != null)
        {
            Camera cam = canvas.worldCamera;

            // 将屏幕坐标转换到 Canvas 的本地坐标
            Vector2 canvasSize = (canvas.transform as RectTransform).rect.size;
            Vector2 canvasScreenRatio = new Vector2(canvasSize.x / screenSize.x, canvasSize.y / screenSize.y);

            topOffset *= canvasScreenRatio.y;
            bottomOffset *= canvasScreenRatio.y;
            leftOffset *= canvasScreenRatio.x;
            rightOffset *= canvasScreenRatio.x;
        }

        topOffset = Mathf.Max(topOffset, minTopOffset);
        bottomOffset = Mathf.Max(bottomOffset, minBottomOffset);
        leftOffset = Mathf.Max(leftOffset, minLeftOffset);
        rightOffset = Mathf.Max(rightOffset, minRightOffset);

        // 应用边距
        ApplyMargins(topOffset, bottomOffset, leftOffset, rightOffset);

        // 记录应用的偏移量
        appliedOffsets = new Vector4(topOffset, bottomOffset, leftOffset, rightOffset);

        Debug.Log($"安全区域适配已应用:\n" +
                 $"安全区域: {safeArea}\n" +
                 $"屏幕尺寸: {screenSize}\n" +
                 $"应用边距: Top={topOffset}, Bottom={bottomOffset}, Left={leftOffset}, Right={rightOffset}");
    }

    /// <summary>
    /// 应用边距到 RectTransform
    /// </summary>
    private void ApplyMargins(float top, float bottom, float left, float right)
    {
        // offsetMin: (Left, Bottom)
        // offsetMax: (-Right, -Top)
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>
    /// 手动重新适配
    /// </summary>
    public void Refresh()
    {
        ApplySafeArea();
    }

    /// <summary>
    /// 重置所有边距
    /// </summary>
    public void ResetMargins()
    {
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        appliedOffsets = Vector4.zero;
    }

    /// <summary>
    /// 获取当前安全区域信息
    /// </summary>
    public Rect GetCurrentSafeArea()
    {
        return Screen.safeArea;
    }

    /// <summary>
    /// 获取应用的安全区域偏移量
    /// </summary>
    public Vector4 GetAppliedOffsets()
    {
        return appliedOffsets;
    }

    /// <summary>
    /// 检查当前设备是否有安全区域（如刘海屏）
    /// </summary>
    public bool HasNotchOrCutout()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        // 如果安全区域小于屏幕区域，说明有安全区域
        return safeArea.width < screenSize.x - 1 || safeArea.height < screenSize.y - 1;
    }

    #if UNITY_EDITOR
    [ContextMenu("立即应用安全区域")]
    private void EditorApplySafeArea()
    {
        if (!Application.isPlaying)
        {
            // 在编辑器模式下模拟安全区域
            lastSafeArea = new Rect(0, 0, Screen.width, Screen.height);
            lastScreenSize = new Vector2(Screen.width, Screen.height);

            // 创建模拟的安全区域（例如：模拟 iPhone X 的刘海）
            float notchHeight = 100f;
            lastSafeArea = new Rect(0, notchHeight, Screen.width, Screen.height - notchHeight * 2);

            ApplySafeArea();
        }
        else
        {
            ApplySafeArea();
        }
    }

    [ContextMenu("重置边距")]
    private void EditorResetMargins()
    {
        ResetMargins();
    }

    [ContextMenu("模拟iPhone X刘海")]
    private void SimulateiPhoneXNotch()
    {
        if (!Application.isPlaying)
        {
            // 保存原始值
            Rect originalSafeArea = lastSafeArea;
            Vector2 originalScreenSize = lastScreenSize;

            // 模拟 iPhone X 的安全区域
            float screenWidth = 1125f;
            float screenHeight = 2436f;
            float notchHeight = 132f; // 刘海高度

            lastScreenSize = new Vector2(screenWidth, screenHeight);
            lastSafeArea = new Rect(0, notchHeight, screenWidth, screenHeight - notchHeight * 2);

            ApplySafeArea();

            // 恢复原始值
            lastSafeArea = originalSafeArea;
            lastScreenSize = originalScreenSize;
        }
    }
    #endif
}
