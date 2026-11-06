using UnityEngine;
using System;

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Rect Transform/Size Delta", "Rect Transform Size Delta")]
    public class TweenRectTransformSizeDelta : TweenVector2<RectTransform>
    {
        public override Vector2 GetCurrentState(RectTransform target)
        {
            return target ? target.sizeDelta : default;
        }

        public override void SetCurrentStateRaw(RectTransform target, Vector2 value)
        {
            if (target) target.sizeDelta = value;
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Anchored Position", "Rect Transform Anchored Position")]
    public class TweenRectTransformAnchoredPosition : TweenVector2<RectTransform>
    {
        public override Vector2 GetCurrentState(RectTransform target)
        {
            return target ? target.anchoredPosition : default;
        }

        public override void SetCurrentStateRaw(RectTransform target, Vector2 value)
        {
            if (target) target.anchoredPosition = value;
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Offset Max", "Rect Transform Offset Max")]
    public class TweenRectTransformOffsetMax : TweenVector2<RectTransform>
    {
        public override Vector2 GetCurrentState(RectTransform target)
        {
            return target ? target.offsetMax : default;
        }

        public override void SetCurrentStateRaw(RectTransform target, Vector2 value)
        {
            if (target) target.offsetMax = value;
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Offset Min", "Rect Transform Offset Min")]
    public class TweenRectTransformOffsetMin : TweenVector2<RectTransform>
    {
        public override Vector2 GetCurrentState(RectTransform target)
        {
            return target ? target.offsetMin : default;
        }

        public override void SetCurrentStateRaw(RectTransform target, Vector2 value)
        {
            if (target) target.offsetMin = value;
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Anchor Max", "Rect Transform Anchor Max")]
    public class TweenRectTransformAnchorMax : TweenVector2<RectTransform>
    {
        public override Vector2 GetCurrentState(RectTransform target)
        {
            return target ? target.anchorMax : default;
        }

        public override void SetCurrentStateRaw(RectTransform target, Vector2 value)
        {
            if (target) target.anchorMax = value;
        }
    }

    [Serializable, TweenAnimation("Rect Transform/Anchor Min", "Rect Transform Anchor Min")]
    public class TweenRectTransformAnchorMin : TweenVector2<RectTransform>
    {
        public override Vector2 GetCurrentState(RectTransform target)
        {
            return target ? target.anchorMin : default;
        }

        public override void SetCurrentStateRaw(RectTransform target, Vector2 value)
        {
            if (target) target.anchorMin = value;
        }
    }

} // namespace UnityExtensions.Tween