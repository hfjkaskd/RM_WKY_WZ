#if BIZZA_REAL_WITHDRAW
using Sirenix.OdinInspector;
using UnityEngine;

public class ViewportResizer : MonoBehaviour
{
    public RectTransform viewportRect; // 拖拽赋值你的 Viewport
    public float bottomPadding = 300f; // 你想留出的底部距离

    [Button("Update Viewport Bottom")]
    public void UpdateViewportBottom(float value)
    {
        //Debug.LogError("Update Viewport Bottom " + value);
        float _bottomPadding = 0;
        if (value <= 0)
        {
            _bottomPadding = 0;
        }
        else
        {
            _bottomPadding = bottomPadding + value;
        }
        //Debug.LogError("Update Viewport Bottom2 " + _bottomPadding);
        // 1. 获取当前的锚点
        Vector2 anchorMin = viewportRect.anchorMin;
        Vector2 anchorMax = viewportRect.anchorMax;

        // 2. 修改 Min Y，使其距离底部 bottomPadding 像素
        // Screen.height 是屏幕总高度，除以它得到百分比
        float newY = _bottomPadding / Screen.height;

        // 3. 重新赋值锚点
        // 注意：Stretch 状态下，Max Y 通常是 1 (贴顶部)，不要动它
        viewportRect.anchorMin = new Vector2(anchorMin.x, newY);
        
        // 可选：如果你希望 Viewport 的高度也自动减小（保持拉伸），这行代码就够了
        // 如果你希望 Viewport 高度不变，只是底部上移导致顶部超出屏幕，需要配合下面的方法
    }
}
#endif