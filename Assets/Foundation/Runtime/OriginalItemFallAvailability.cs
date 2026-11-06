using System;

namespace Whitebox.Foundation
{
    public partial class OriginalBoardItem
    {
        public virtual bool IsFallable => throw new NotSupportedException("Item fall capability is not restored.");
        public virtual bool CanFallCross => false; // 0x2b4702c.
        public virtual bool IsCrossFallBlocking => false; // 0x2b47a94.
        public virtual bool IsAvailableForFall(OriginalGridCell toCell) // 0x2b462f8; toCell is unused.
        {
            if(!IsFallable)return false;
            var view=SwapView;if(view==null)throw new NullReferenceException();
            if(view.IsSwapping)return false;
            if(Mediator.CurrentCell==null)return false;
            var cell=Mediator.CurrentCell;if(cell==null || cell.Owner==null)throw new NullReferenceException();
            return !cell.Owner.StaticTouch.HasFallBlockingItem();
        }
        private bool IsUnderStaticItem(int type)
        {
            if(Mediator.CurrentCell!=null)
            {
                var cell=Mediator.CurrentCell;if(cell==null || cell.Owner==null)throw new NullReferenceException();
                if(cell.Owner.StaticTouch.HasStaticItem(type))return true;
            }
            if(Mediator.NextCell==null)return false;
            var next=Mediator.NextCell;if(next==null || next.Owner==null)throw new NullReferenceException();
            return next.Owner.StaticTouch.HasStaticItem(type);
        }
        public bool IsUnderHoney() => IsUnderStaticItem(4); // 0x2b4744c.
        public bool IsUnderRoof() => IsUnderStaticItem(7); // 0x2b474d8.
    }
    public static class OriginalItemFallQueries
    {
        public static bool IsUnderNonFallBlockingAboveStaticItem(OriginalBoardItem item) => item.IsUnderHoney()||item.IsUnderRoof(); // 0x2b470cc.
        public static bool IsUnderNonFallBlockingAboveStaticItemSpreading(OriginalBoardItem item) // 0x2b47564.
        {
            if(item.Mediator.CurrentCell==null)return false;
            var cell=item.Mediator.CurrentCell;if(cell==null || cell.Owner==null)throw new NullReferenceException();
            return cell.Owner.IsAboveStaticSpreading;
        }
    }
    public sealed partial class OriginalMatchBoardItem
    {
        public override bool IsFallable => true;
        public override bool CanFallCross => true; // 0x21a77b0.
    }
    public sealed partial class OriginalBoxBoardItem
    {
        public override bool IsFallable => false;
    }
    public abstract partial class OriginalSpecialBoardItem
    {
        public override bool IsFallable => true; // 0x27b4308.
        public override bool CanFallCross => true; // 0x27b49d0.
    }
}
