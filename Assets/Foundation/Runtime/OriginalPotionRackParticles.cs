using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalPotionRackParticles : OriginalItemParticles
    {
        [SerializeField] private OriginalParticleInfo[] explodeParticles;
        protected override void Awake()
        {
            base.Awake();
            InitShadows(explodeParticles);
        }
        public override int GetPoolId() => 61;
        public void Play(Vector3 position) => PlayParticles(explodeParticles, position);
    }
}
