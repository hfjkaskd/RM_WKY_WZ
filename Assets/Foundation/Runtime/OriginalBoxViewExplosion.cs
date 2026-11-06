using System;

namespace Whitebox.Foundation
{
    public interface IOriginalBoxViewExplosionServices
    {
        int NextApplicationRandom(int min, int max);
        void PlaySfx(int type, float delay);
        void SetSpriteForLayer(int layer);
        void PlayExplodeParticle(int particleLayer);
        void RecycleView();
    }

    // BoxItemView.Explode / Damage 0x2bc5b6c / 0x2bc5c58. Concrete sprite,
    // particle and item-factory operations are required presentation services.
    public sealed class OriginalBoxViewExplosion : IOriginalBoxExplosionView
    {
        private readonly IOriginalBoxViewExplosionServices services;
        public OriginalBoxViewExplosion(IOriginalBoxViewExplosionServices services)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public void Explode()
        {
            int random = services.NextApplicationRandom(0, 3);
            services.PlaySfx(random == 0 ? 33 : random == 1 ? 34 : 35, 0.04f);
            services.PlayExplodeParticle(0);
            services.RecycleView();
        }
        public void Damage(int damagedLayer)
        {
            int random = services.NextApplicationRandom(0, 3);
            services.PlaySfx(random == 0 ? 36 : random == 1 ? 37 : 38, 0.04f);
            services.SetSpriteForLayer(damagedLayer);
            services.PlayExplodeParticle(damagedLayer);
        }
    }
}
