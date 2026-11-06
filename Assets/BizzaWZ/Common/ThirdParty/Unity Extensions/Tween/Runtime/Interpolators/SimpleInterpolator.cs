using System;
using UnityEngine;

namespace UnityExtensions.Tween
{
    /// <summary>
    /// Simple Interpolator
    /// </summary>
    [Serializable]
    public struct SimpleInterpolator
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
            Jitter
        }


        public Type type;
        public float strength;


        /// <summary>
        /// Calculate interpolation value
        /// </summary>
        /// <param name="t"> normalized time </param>
        /// <returns> result </returns>
        public float this[float t] => Interpolator._interpolators[(int)type](t, strength);


        public SimpleInterpolator(Type type, float strength = 0.5f)
        {
            this.type = type;
            this.strength = strength;
        }

    } // struct SimpleInterpolator

} // namespace UnityExtensions.Tween