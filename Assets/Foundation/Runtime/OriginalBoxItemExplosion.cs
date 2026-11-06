using System;

namespace Whitebox.Foundation
{
    public interface IOriginalBoxExplosionView
    {
        void Explode();
        void Damage(int damagedLayer);
    }
    public interface IOriginalBoxExplosionServices
    {
        void UsefulMoveMade();
    }

    public sealed partial class OriginalBoxBoardItem
    {
        private IOriginalBoxExplosionView explosionView;
        private IOriginalBoxExplosionServices explosionServices;
        public OriginalBoxBoardItem(int id, int itemType, int layerCount) : base(id, itemType, layerCount) { } // 0x2bc58fc.
        public void BindExplosionServices(IOriginalBoxExplosionView view, IOriginalBoxExplosionServices services)
        {
            explosionView = view ?? throw new ArgumentNullException(nameof(view));
            explosionServices = services ?? throw new ArgumentNullException(nameof(services));
        }
        protected override void TryExplode(OriginalExplodeData data) // 0x2bc5b10.
        {
            layer = unchecked(layer - 1);
            var view = explosionView;
            if (view == null) throw new NullReferenceException();
            if (layer == 0)
            {
                view.Explode();
                FinalExplodeCompleted(0.15f);
            }
            else view.Damage(layer);
            if (explosionServices == null) throw new NullReferenceException();
            explosionServices.UsefulMoveMade();
        }
    }
}
