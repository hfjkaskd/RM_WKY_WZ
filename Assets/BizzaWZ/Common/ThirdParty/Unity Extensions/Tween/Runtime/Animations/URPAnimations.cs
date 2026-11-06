#if USE_URP

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Rendering/Light 2D Outer Angle", "Light 2D Outer Angle")]
    public class TweenLight2DOuterAngle : TweenFloat<Light2D>
    {
        public override float GetCurrentState(Light2D target)
        {
            return target ? target.pointLightOuterAngle : 30f;
        }

        public override void SetCurrentState(Light2D target, float value)
        {
            if (target) target.pointLightOuterAngle = value;
        }
    }

    [Serializable, TweenAnimation("Rendering/Light 2D Outer Radius", "Light 2D Outer Radius")]
    public class TweenLight2DOuterRadius : TweenFloat<Light2D>
    {
        public override float GetCurrentState(Light2D target)
        {
            return target ? target.pointLightOuterRadius : 10f;
        }

        public override void SetCurrentState(Light2D target, float value)
        {
            if (target) target.pointLightOuterRadius = value;
        }
    }

    [Serializable, TweenAnimation("Rendering/Light 2D Intensity", "Light 2D Intensity")]
    public class TweenLight2DIntensity : TweenFloat<Light2D>
    {
        public override float GetCurrentState(Light2D target)
        {
            return target ? target.intensity : 1f;
        }

        public override void SetCurrentState(Light2D target, float value)
        {
            if (target) target.intensity = value;
        }
    }

    [Serializable, TweenAnimation("Rendering/Light 2D Color", "Light 2D Color")]
    public class TweenLight2DColor : TweenColor<Light2D>
    {
        public override Color GetCurrentState(Light2D target)
        {
            return target ? target.color : Color.white;
        }

        public override void SetCurrentStateRaw(Light2D target, Color value)
        {
            if (target) target.color = value;
        }
    }

} // namespace UnityExtensions.Tween

#endif