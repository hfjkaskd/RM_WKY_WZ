using UnityEngine;
using System.Collections;

public class FadeOnMove : MonoBehaviour
{
    [Header("渐变设置")]
    public float fadeInDuration = 0.5f;    // 淡入时间
    public float fadeOutDuration = 1.0f;   // 淡出时间
    public float stationaryDelay = 2.0f;  // 静止多久后开始淡出

    [Header("组件引用")]
    public Renderer targetRenderer;        // 物体的渲染器
    public CanvasGroup canvasGroup;        // 如果是UI物体，用这个

    private Vector3 _lastPosition;         // 上一帧的位置
    private Coroutine _fadeCoroutine;       // 当前运行的协程引用
    private bool _isVisible = false;       // 当前是否可见

    void Start()
    {
        // 记录初始位置
        _lastPosition = transform.position;

        // 初始化为完全透明
        SetAlpha(0f);
    }

    void Update()
    {
        // 检查物体是否移动了
        if (HasMoved())
        {
            OnObjectMoved();
        }

        // 更新上一帧位置
        _lastPosition = transform.position;
    }

    // 检查物体是否移动
    private bool HasMoved()
    {
        return Vector3.Distance(transform.position, _lastPosition) > 0.001f;
    }

    // 当物体移动时的处理
    private void OnObjectMoved()
    {
        // 如果已经在淡入过程中，先停止之前的协程
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        // 如果当前不可见，开始淡入
        if (!_isVisible)
        {
            _fadeCoroutine = StartCoroutine(FadeIn());
        }
        else
        {
            // 如果已经可见，重新开始计时（延迟淡出）
            _fadeCoroutine = StartCoroutine(DelayedFadeOut());
        }
    }

    // 淡入协程
    private IEnumerator FadeIn()
    {
        _isVisible = true;
        float timer = 0f;
        float startAlpha = GetCurrentAlpha();

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeInDuration;
            SetAlpha(Mathf.Lerp(startAlpha, 1f, progress));
            yield return null;
        }

        SetAlpha(1f); // 确保最终完全显示

        // 淡入完成后开始延迟淡出计时
        _fadeCoroutine = StartCoroutine(DelayedFadeOut());
    }

    // 延迟后淡出
    private IEnumerator DelayedFadeOut()
    {
        // 等待静止时间
        yield return new WaitForSeconds(stationaryDelay);

        // 开始淡出
        yield return StartCoroutine(FadeOut());
    }

    // 淡出协程
    private IEnumerator FadeOut()
    {
        float timer = 0f;
        float startAlpha = GetCurrentAlpha();

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeOutDuration;
            SetAlpha(Mathf.Lerp(startAlpha, 0f, progress));
            yield return null;
        }

        SetAlpha(0f); // 确保最终完全透明
        _isVisible = false;
    }

    // 获取当前透明度
    private float GetCurrentAlpha()
    {
        if (targetRenderer != null)
        {
            return targetRenderer.material.color.a;
        }
        else if (canvasGroup != null)
        {
            return canvasGroup.alpha;
        }

        // 默认返回完全不透明（安全值）
        return 1f;
    }

    // 设置透明度
    private void SetAlpha(float alpha)
    {
        if (targetRenderer != null)
        {
            Color color = targetRenderer.material.color;
            color.a = alpha;
            targetRenderer.material.color = color;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
}