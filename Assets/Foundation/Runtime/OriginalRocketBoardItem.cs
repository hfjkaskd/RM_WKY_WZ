using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalRocketItemStaticState : IOriginalSpecialItemChainedStaticState { }

    // RocketItemModel creation/view portion 0x27ba504/0x27ba54c. Explosion
    // and combo travel are separate native paths and are not approximated here.
    public sealed partial class OriginalRocketBoardItem : OriginalSpecialBoardItem, IOriginalRocketItemViewDelegate
    {
        private readonly IOriginalRocketItemStaticState staticState;
        public int Orientation { get; private set; }
        public int RemainingMoveCoins { get; private set; }
        public bool IsCreatedByLightball { get; private set; }
        public OriginalRocketItemSpriteView ItemView { get; private set; }
        public override int ItemType => 2; // 0x27ba4f4, independent of the constructor's tiled id.
        public override IOriginalSpecialCreationView View => ItemView;
        public OriginalRocketBoardItem(int id, int orientation, bool isCreatedByLightball, int tiledId,
            Func<float> levelTime, IOriginalRocketItemStaticState staticState) : base(id, tiledId, levelTime, staticState)
        {
            this.staticState = staticState;
            Orientation = orientation;
            RemainingMoveCoins = 2;
            IsCreatedByLightball = isCreatedByLightball;
        }
        public void CreateView(OriginalLevelPresentation presentation, Transform parent, Vector3 position,
            IOriginalRocketViewServices services)
        {
            ItemView = presentation.SpawnRocketView();
            presentation.InitializeRocketView(ItemView, this, parent, position, services);
            BindSwapView(ItemView);
            MarkViewCreated();
        }
        public bool IsUnderHoney() => staticState.IsUnderHoney(this);
        public bool IsUnderRoof() => staticState.IsUnderRoof(this);
        public bool IsUnderChain() => staticState.IsUnderChain(this);
        public void ChangeOrientation(int orientation) // 0x27bb2b0.
        {
            Orientation = orientation;
            ItemView.ReinitializeOrientation(this);
        }
        // Admitted rocket path of SpecialItemModel.ChangePosition 0x27b4740
        // and ItemView.ChangePosition 0x2b4ad78: local position, no mediation.
        public void ChangePosition(Vector3 position) { ItemView.transform.localPosition = position; }
    }
}
