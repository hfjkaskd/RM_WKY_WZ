using UnityEngine;
using System;

#if USE_RENDER_PIPELINES
using UnityEngine.Rendering;
#endif

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Rendering/Light Color", "Light Color")]
    public class TweenLightColor : TweenColor<Light>
    {
        public override Color GetCurrentState(Light target)
        {
            return target ? target.color : Color.white;
        }

        public override void SetCurrentStateRaw(Light target, Color value)
        {
            if (target) target.color = value;
        }
    }

    [Serializable, TweenAnimation("Rendering/Light Intensity", "Light Intensity")]
    public class TweenLightIntensity : TweenFloat<Light>
    {
        public override float GetCurrentState(Light target)
        {
            return target ? target.intensity : 1f;
        }

        public override void SetCurrentState(Light target, float value)
        {
            if (target) target.intensity = value;
        }
    }

    [Serializable, TweenAnimation("Rendering/Light Range", "Light Range")]
    public class TweenLightRange : TweenFloat<Light>
    {
        public override float GetCurrentState(Light target)
        {
            return target ? target.range : 10f;
        }

        public override void SetCurrentState(Light target, float value)
        {
            if (target) target.range = value;
        }
    }

    [Serializable, TweenAnimation("Rendering/Camera Field of View", "Camera Field of View")]
    public class TweenCameraFieldOfView : TweenFloat<Camera>
    {
        public override float GetCurrentState(Camera target)
        {
            return target ? target.fieldOfView : 60f;
        }

        public override void SetCurrentState(Camera target, float value)
        {
            if (target) target.fieldOfView = value;
        }
    }

    [Serializable, TweenAnimation("Rendering/Camera Orthographic Size", "Camera Orthographic Size")]
    public class TweenCameraOrthographicSize : TweenFloat<Camera>
    {
        public override float GetCurrentState(Camera target)
        {
            return target ? target.orthographicSize : 5f;
        }

        public override void SetCurrentState(Camera target, float value)
        {
            if (target) target.orthographicSize = value;
        }
    }

#if USE_RENDER_PIPELINES

    [Serializable, TweenAnimation("Rendering/Volume Weight", "Volume Weight")]
    public class TweenVolumeWeight : TweenFloat<Volume>
    {
        public override float GetCurrentState(Volume target)
        {
            return target? target.weight: 1f;
        }

        public override void SetCurrentState(Volume target, float value)
        {
            if (target) target.weight = value;
        }
    }

#endif

} // namespace UnityExtensions.Tween