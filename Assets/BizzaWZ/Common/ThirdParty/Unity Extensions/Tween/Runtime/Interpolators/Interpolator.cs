using System;
using UnityEngine;

namespace UnityExtensions.Tween
{
    /// <summary>
    /// Interpolator
    /// </summary>
    [Serializable]
    public partial struct Interpolator
    {
        public enum Type
        {
            Linear = 0,
            Accelerate,
            Decelerate,
            AccelerateDecelerate,
            Anticipate,
            Overshoot,
            AnticipateOvershoot,
            Bounce,
            Parabolic,
            Sine,
            Cosine,
            Jitter,

            CustomCurve = -1
        }


        public Type type;
        public float strength;
        public AnimationCurve customCurve;


        internal static readonly Func<float, float, float>[] _interpolators =
        {
            Linear,
            Accelerate,
            Decelerate,
            AccelerateDecelerate,
            Anticipate,
            Overshoot,
            AnticipateOvershoot,
            Bounce,
            Parabolic,
            Sine,
            Cosine,
            Jitter,
        };


        /// <summary>
        /// Calculate interpolation value
        /// </summary>
        /// <param name="t"> normalized time </param>
        /// <returns> result </returns>
        public float this[float t] => type == Type.CustomCurve ? customCurve.Evaluate(t) : _interpolators[(int)type](t, strength);


        public Interpolator(Type type, float strength = 0.5f, AnimationCurve customCurve = null)
        {
            this.type = type;
            this.strength = strength;
            this.customCurve = customCurve;
        }

    } // struct Interpolator

} // namespace UnityExtensions.Tween