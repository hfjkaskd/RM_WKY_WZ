using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public static class DOTweenBezierExtensions
{
    public static Tweener DOBezier(this Transform target, Vector2 start, Vector2 end, float duration, float distPercent = 0.5f, bool revertDir = false)
    {
        Vector2 mid = (start + end) / 2f;
        Vector2 dir = (end - start).normalized;
        var mag = (end - start).magnitude;

        var p1 = mid - dir.Rotate((revertDir ? -1 : 1) * 90) * mag * distPercent;
        return DOVirtual.Float(0, 1, duration, t =>
        {
            Vector3 pos = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * end;
            target.position = pos;
        }).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 线性逐字：按固定字符/秒递增 maxVisibleCharacters。
    /// 优点：不创建新字符串，GC 极低；支持富文本；Emoji 正常。
    /// </summary>
    /// <param name="label">TMP_Text 或 TextMeshProUGUI</param>
    /// <param name="fullText">完整文本（可含富文本标签）</param>
    /// <param name="charsPerSecond">打字速度（字符/秒）</param>
    /// <param name="onComplete">完成回调（可选）</param>
    public static Tween DOTypewriterLinear(this TMP_Text label, string fullText, float charsPerSecond = 30f, Action onComplete = null)
    {
        DOTween.Kill(label);
        label.text = string.Empty;

        int len = fullText.Length;
        if (len == 0) { onComplete?.Invoke(); return null; }

        float duration = len / Math.Max(1f, charsPerSecond);
        int current = 0;

        return DOTween.To(
                () => current,
                v => { current = v; label.text = fullText.Substring(0, v); },
                len,
                duration)
            .SetEase(Ease.Linear)
            .SetTarget(label)
            .OnComplete(() => onComplete?.Invoke());
    }
}