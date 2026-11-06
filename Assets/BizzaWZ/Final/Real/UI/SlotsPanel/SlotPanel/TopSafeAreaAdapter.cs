#if BIZZA_REAL_WITHDRAW
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class TopSafeAreaAdapter : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private bool applyLeftSafeArea = false;
    [SerializeField] private bool applyRightSafeArea = false;
    [SerializeField] private float extraTopPadding = 0f;
    [SerializeField] private float extraLeftPadding = 0f;
    [SerializeField] private float extraRightPadding = 0f;

    private RectTransform parentRect;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;

    private void Awake()
    {
        Init();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        Init();
        ApplySafeArea();
    }

    private void Update()
    {
        if (target == null)
            return;

        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height)
        {
            ApplySafeArea();
        }
    }

    private void Init()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        if (target.parent != null)
            parentRect = target.parent as RectTransform;
    }

    [ContextMenu("Apply Safe Area")]
    public void ApplySafeArea()
    {
        if (target == null || parentRect == null)
            return;

        Rect safeArea = Screen.safeArea;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float topInset = screenHeight - safeArea.yMax;
        float leftInset = safeArea.xMin;
        float rightInset = screenWidth - safeArea.xMax;

        Vector2 offsetMin = target.offsetMin;
        Vector2 offsetMax = target.offsetMax;

        // 顶部避开刘海
        offsetMax.y = -(topInset + extraTopPadding);

        // 左右是否也跟随安全区
        if (applyLeftSafeArea)
            offsetMin.x = leftInset + extraLeftPadding;

        if (applyRightSafeArea)
            offsetMax.x = -(rightInset + extraRightPadding);

        target.offsetMin = offsetMin;
        target.offsetMax = offsetMax;
    }
}
#endif