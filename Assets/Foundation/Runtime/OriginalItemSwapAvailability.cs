using System;

namespace Whitebox.Foundation
{
    public interface IOriginalItemFallReadiness
    {
        bool IsItemSteady { get; }
    }
    public interface IOriginalItemSwapView
    {
        bool IsSwapping { get; }
    }
    public interface IOriginalSwapAnimationView : IOriginalItemSwapView
    {
        void SwapAnimationStarted(bool isValid, int otherItem);
        void SwapAnimationEnded();
    }

    public partial class OriginalBoardItem
    {
        public virtual bool IsSwappable => throw new NotSupportedException("Item swap capability is not restored.");
        public virtual bool IsSpecialItem => false; // ItemModel 0x2b46bc0.
        public IOriginalItemSwapView SwapView { get; private set; }
        public void BindSwapView(IOriginalItemSwapView view) { SwapView = view ?? throw new ArgumentNullException(nameof(view)); }
        public virtual bool IsAvailableForSwap() // 0x2b46380.
        {
            if (!IsSwappable) return false;
            var fall = FallComponent as IOriginalItemFallReadiness;
            if (fall == null) throw new NullReferenceException();
            if (!fall.IsItemSteady) return false;
            var view = SwapView;
            if (view == null) throw new NullReferenceException();
            return !view.IsSwapping;
        }
    }
    public sealed partial class OriginalMatchBoardItem
    {
        public override bool IsSwappable => true; // MatchItemModel 0x21a5a5c.
    }
    public sealed partial class OriginalBoxBoardItem : OriginalBoardItem
    {
        public OriginalBoxBoardItem(int id, int itemType) : base(id, itemType) { }
        public override bool IsSwappable => false; // BoxItemModel 0x2bc5b08.
    }

    // ItemModel.IsAvailableForSwap 0x2b46380. Keep native short-circuit order:
    // an unswappable item does not need its fall component or view queried.
    public sealed class OriginalItemSwapAvailability : IOriginalItemSwapAvailability
    {
        public bool IsAvailableForSwap(OriginalBoardItem item)
        {
            return item.IsAvailableForSwap();
        }
    }
}
