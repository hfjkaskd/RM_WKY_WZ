using UnityEngine;

namespace Whitebox.Foundation
{
    // Native normal pilot retains this child through the two-second pooled
    // tail. The unused legacy detach/DestroySelf branch is not substituted here.
    public sealed class OriginalRemainingMovesCoinTracer:MonoBehaviour
    {
        public ParticleSystem[] particles;
        private float[] initialRateOverDistances;
        private void Awake()
        {
            initialRateOverDistances=new float[particles.Length];
            for(int i=0;i<particles.Length;i++)
            {var emission=particles[i].emission;emission.enabled=true;initialRateOverDistances[i]=emission.rateOverDistance.constant;}
        }
        public void ArrangeRateOverDistanceByRatio(float ratio) // 0x2182ac8, native max3/min1.
        {
            float scale=(float)(1f+(float)(Mathf.Clamp01(ratio)*2f));
            for(int i=0;i<particles.Length;i++)
            {var emission=particles[i].emission;emission.enabled=true;emission.rateOverDistance=initialRateOverDistances[i]*scale;}
        }
        public void Play() // 0x2182bc4.
        {
            transform.localPosition=Vector3.zero;
            for(int i=0;i<particles.Length;i++){var emission=particles[i].emission;emission.enabled=true;particles[i].Play();}
        }
    }
}
