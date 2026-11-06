using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalPropellerRocketComboParticles:OriginalItemParticles
    {
        public OriginalParticleInfo[] system;
        protected override void Awake(){base.Awake();InitShadows(system);}
        public override int GetPoolId()=>13;
        public void Play(Vector3 position)=>PlayParticles(system,position); // 0x27b24a0.
    }
}
