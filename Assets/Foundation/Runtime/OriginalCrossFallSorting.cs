using System;

namespace Whitebox.Foundation
{
    public static class OriginalCrossFallSorting
    {
        // Sorting.GetCrossSorting 0x12d81cc. Both cells use touch blocking,
        // rather than fall blocking; below-list items do not participate.
        public static OriginalSwapSorting Get(OriginalBoardItem item, int itemLayer)
        {
            if(item==null)throw new NullReferenceException();
            bool blocked=false;
            if(item.Mediator.CurrentCell!=null)
            {
                var current=item.Mediator.CurrentCell;
                if(current==null || current.Owner==null)throw new NullReferenceException();
                blocked=current.Owner.StaticTouch.HasTouchBlockingItem();
            }
            if(!blocked && item.Mediator.NextCell!=null)
            {
                var next=item.Mediator.NextCell;
                if(next==null || next.Owner==null)throw new NullReferenceException();
                blocked=next.Owner.StaticTouch.HasTouchBlockingItem();
            }
            return new OriginalSwapSorting(itemLayer,blocked?1550:7000,true);
        }
    }
}
