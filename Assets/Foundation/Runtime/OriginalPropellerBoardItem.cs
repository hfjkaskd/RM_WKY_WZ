using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // PropellerItemModel normal first-board branch; remaining-move collection,
    // charger and Clash owners are inactive in this composition.
    public sealed class OriginalPropellerBoardItem : OriginalGuidedExploderBoardItem
    {
        public override int ItemType => 3;
        public int RemainingMoveCoins { get; internal set; } = 1; // SpecialItemModel +0xb8; remaining conversion writes2/3.
        public bool IsCreatedByLightball { get; }
        protected override bool CreatedByLightball => IsCreatedByLightball;
        public OriginalPropellerItemSpriteView ItemView { get; private set; }
        public override IOriginalSpecialCreationView View => ItemView;
        protected override OriginalPropellerItemSpriteView GuidedView => ItemView;
        public OriginalPropellerBoardItem(int id, Func<float> time, IOriginalSpecialItemStaticState statics, bool createdByLightball = false)
            : base(id, 10, time, statics) { IsCreatedByLightball = createdByLightball; }
        public void BindExplosion(IOriginalPropellerRuntimeServices services) => BindGuidedExplosion(services, new OriginalDefaultPropellerStrategy(services, IsCreatedByLightball));
        public void SetStrategy(IOriginalGuidedExploderStrategy strategy) => guidedStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        public void CreateView(OriginalLevelPresentation presentation, Transform parent, Vector3 position, IOriginalRocketViewServices services)
        {
            ItemView = presentation.SpawnPropellerView();
            ItemView.Initialize(this, parent, position, services);
            BindSwapView(ItemView); MarkViewCreated();
        }
        public override bool Tap(OriginalExplodeData data) // 0x27bff30.
        { if (ItemView.IsSwapping) return false; Explode(data); return true; }
        protected override void TryExplode(OriginalExplodeData data) // 0x27bff9c/0x27c021c.
        {guidedServices.CollectRemainingMovesCoin(CurrentCell,this);base.TryExplode(data);}
    }
}
