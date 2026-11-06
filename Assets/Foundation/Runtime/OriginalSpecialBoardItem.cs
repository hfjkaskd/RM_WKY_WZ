using System;

namespace Whitebox.Foundation
{
    public interface IOriginalSpecialItemStaticState
    {
        bool IsUnderHoney(OriginalBoardItem item);
        bool IsUnderRoof(OriginalBoardItem item);
        bool IsAboveStaticSpreading(OriginalGridCell cell);
    }
    public interface IOriginalSpecialItemChainedStaticState : IOriginalSpecialItemStaticState
    {
        bool IsUnderChain(OriginalBoardItem item);
    }

    // SpecialItemModel's common activation/swap state. Item-specific explosion
    // and view implementations remain responsibilities of its concrete types.
    public abstract partial class OriginalSpecialBoardItem : OriginalBoardItem, IOriginalSpecialCreationItem, IOriginalItemFallViewDelegate, IOriginalSpecialItemCombo
    {
        private readonly Func<float> levelTime;
        private readonly IOriginalSpecialItemStaticState staticState;
        private bool active = true; // 0x27b3f7c, set before ItemModel construction.
        private float activeSince;
        protected bool isExploded;
        public bool IsExploded => isExploded; // 0x27b4300.
        public bool IsPreparedForSpecialUse { get; set; } // Native +0xb2.
        protected OriginalSpecialBoardItem(int id, int tiledId, Func<float> levelTime, IOriginalSpecialItemStaticState staticState)
            : base(id, OriginalTiledItemTypes.AsItemType(tiledId))
        {
            this.levelTime = levelTime ?? throw new ArgumentNullException(nameof(levelTime));
            this.staticState = staticState ?? throw new ArgumentNullException(nameof(staticState));
        }
        public OriginalGridCell CurrentCell => Mediator.CurrentCell == null ? null : Mediator.CurrentCell.Owner;
        public abstract IOriginalSpecialCreationView View { get; }
        public OriginalSpecialItemFallView FallView { get; internal set; }
        public bool IsActive => active; // 0x27b4368.
        public override bool IsSwappable => active; // 0x27b4310.
        public override bool IsSpecialItem => true; // 0x27b4318.
        public virtual bool CanComboWith(OriginalBoardItem other) => true; // CanSwapWith 0x27b4808.
        // All four match-created special types reject adjacent normal hits:
        // rocket 0x27ba90c, TNT 0x27b5958, lightball 0x1f3de38 and the
        // propeller's GuidedExploderItemModel base 0x1c9df74.
        public override bool CanExplodeWithNearMatch => false;
        public void SetSpecialItemActive(bool value) // 0x27b4320.
        {
            active = value;
            if (value) activeSince = levelTime();
        }
        // Hide/Show 0x27b479c/0x27b47d0 write the flag directly and preserve activeSince.
        internal void SetVisibilityActiveFlag(bool value) { active = value; }
        public float GetActiveDuration() => levelTime() - activeSince; // 0x27b4348.
        public bool IsUnderNonFallBlockingAboveStaticItem() =>
            staticState.IsUnderHoney(this) || staticState.IsUnderRoof(this); // 0x2b470cc.
        public bool IsUnderNonFallBlockingAboveStaticItemSpreading() // 0x2b47564.
        {
            if (Mediator.CurrentCell == null) return false;
            return staticState.IsAboveStaticSpreading(CurrentCell);
        }
    }
}
