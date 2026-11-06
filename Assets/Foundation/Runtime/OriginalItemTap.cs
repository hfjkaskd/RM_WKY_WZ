using System;

namespace Whitebox.Foundation
{
    // ITapItem method order, TypeDef 9652. The lightball marker represents the
    // native LightballItemModel hierarchy; its concrete factory is still pending.
    public interface IOriginalTapItem
    {
        int GetMoveType { get; }
        bool IsTapDisabled { get; set; }
        bool Tap(OriginalExplodeData data);
    }
    public interface IOriginalLightballTapItem : IOriginalTapItem { }

    public partial class OriginalBoardItem : IOriginalTapItem
    {
        public virtual int GetMoveType => 0; // ItemModel 0x2b45db4, zero-initialized readonly field.
        public virtual bool IsTapDisabled { get; set; } // 0x2b45dbc / 0x2b45dc4.
        public virtual bool Tap(OriginalExplodeData data) => false; // 0x2b47ac4.
    }
    public sealed partial class OriginalRocketBoardItem
    {
        public override int GetMoveType => 13; // 0x27ba4fc.
        public override bool Tap(OriginalExplodeData data) // 0x27bb244.
        {
            if (ItemView.IsSwapping) return false;
            Explode(data); return true;
        }
    }
    public sealed partial class OriginalTntBoardItem
    {
        public override int GetMoveType => 1; // 0x27b555c.
        public override bool Tap(OriginalExplodeData data) // 0x27b654c.
        {
            if (ItemView.IsSwapping || IsPreparedForSpecialUse) return false;
            Explode(data); return true;
        }
    }
}
