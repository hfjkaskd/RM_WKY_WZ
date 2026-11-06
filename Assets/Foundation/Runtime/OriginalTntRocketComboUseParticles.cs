using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalTntRocketComboUseParticles : OriginalItemParticles
    {
        public ParticleSystem circleParticle, baseParticle;
        public override int GetPoolId() => 21;
        public void Init(bool horizontal) // 0x27ba300; preserve original particle hierarchy and material bindings.
        { baseParticle.transform.localScale = horizontal ? new Vector3(1.1f, .6f, 1f) : new Vector3(.6f, 1.1f, 1f); }
        public void Play() { circleParticle.Play(); } // 0x27ba34c.
    }
}
