using System;

namespace Whitebox.Foundation
{
    public interface IOriginalSpecialItemExplosionServices
    {
        bool IsPlaying { get; }
        void IncrementAutoTrigger(int itemType);
        void CollectRemainingMovesCoin(OriginalGridCell cell,OriginalSpecialBoardItem item);
    }

    public abstract partial class OriginalSpecialBoardItem
    {
        private IOriginalSpecialItemExplosionServices specialExplosion;
        public void BindSpecialExplosion(IOriginalSpecialItemExplosionServices services)
        { specialExplosion = services ?? throw new ArgumentNullException(nameof(services)); }
        protected virtual bool ExplodeDisabled() => isExploded || !IsActive || SwapView.IsSwapping; // 0x27b49e8.
        protected virtual bool CreatedByLightball => false;
        public override void Explode(OriginalExplodeData data) // SpecialItemModel 0x27b4050, ordinary no-jelly branch.
        {
            // Jelly activation/pre-hit is an independent static-item branch.
            if (ExplodeDisabled()) return;
            isExploded = !ContainsExploder(data.Id);
            if (data.Trigger != 2 && data.Trigger != 3 && data.Trigger != 15 && CurrentCell != null)
                CurrentCell.StaticTouch.ExplodeTopMostBelowItem(data);
            if (!IsPreparedForSpecialUse && !CreatedByLightball && !IsManualTrigger(data.Trigger) && specialExplosion.IsPlaying)
                specialExplosion.IncrementAutoTrigger(ItemType);
            base.Explode(data);
        }
        private static bool IsManualTrigger(int trigger) // 0x318d184.
        {
            uint index = unchecked((uint)(trigger - 2));
            return index < 14 && ((0x2023 >> (int)index) & 1) != 0 || unchecked((uint)(trigger - 10)) < 4;
        }
        public virtual void SwapWith(OriginalBoardItem other, OriginalExplodeData data) => Explode(data);
    }
}
