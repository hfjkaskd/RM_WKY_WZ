using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalPotionBottleParticles : OriginalItemParticles
    {
        [SerializeField] private OriginalParticleInfo[] potionParticles, blueParticles, greenParticles, redParticles,
            yellowParticles, pinkParticles;
        protected override void Awake()
        {
            base.Awake();
            InitShadows(potionParticles);
            InitShadows(blueParticles);
            InitShadows(greenParticles);
            InitShadows(redParticles);
            InitShadows(yellowParticles);
            InitShadows(pinkParticles);
        }
        public override int GetPoolId() => 60;
        public void Play(int color, Vector3 position)
        {
            PlayParticles(potionParticles, position);
            PlayParticles(color switch
            {
                1 => blueParticles,
                2 => greenParticles,
                4 => yellowParticles,
                5 => pinkParticles,
                _ => redParticles
            }, position);
        }
    }
}
