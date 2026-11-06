using System;

namespace Whitebox.Foundation
{
    public interface IOriginalStaticFallItem : IOriginalStaticTouchItem
    {
        int StaticItemType { get; }
        OriginalGridCell CurrentCell { get; set; }
        bool DoesBlockFall { get; }
        int RemainingExplodeCount();
    }
    public interface IOriginalRoofFallItem : IOriginalStaticFallItem
    {
        void NotifyForFallUnderneath();
    }
    // Same owner/lists as the existing touch queries. Native static enum has
    // twenty indexed values; do not use reflection/Enum.GetNames to size it.
    public sealed partial class OriginalStaticTouchMediator
    {
        private readonly IOriginalStaticFallItem[] items=new IOriginalStaticFallItem[20];
        public IOriginalStaticFallItem GetStaticItem(int type) => items[type]; // 0x3440a80.
        public bool HasStaticItem(int type) => GetStaticItem(type)!=null; // 0x3440fa4.
        public bool HasNonFallBlockingAboveStaticItem() => HasStaticItem(4)||HasStaticItem(7); // 0x3440fbc.
        public bool HasFallBlockingAboveStaticItem() => HasStaticItem(5)||HasStaticItem(8)||HasStaticItem(11)
            ||HasStaticItem(15)||HasStaticItem(16)||HasStaticItem(17)||HasStaticItem(18)||HasStaticItem(19); // 0x3440ff4.
        public bool HasFallBlockingItem() // 0x3440f00: only above items, in list order.
        {
            for(int i=0;i<AboveItems.Count;i++)
            {
                var item=AboveItems[i] as IOriginalStaticFallItem;
                if(item==null)throw new NullReferenceException();
                if(item.DoesBlockFall)return true;
            }
            return false;
        }
        // The cell supplies its identity explicitly in place of the native
        // mediator's constructor field. Storage/list/callback order is retained.
        internal void AddStaticItem(IOriginalStaticFallItem item, OriginalGridCell owner) // 0x34410b4.
        {
            if(item==null)throw new NullReferenceException();
            items[item.StaticItemType]=item;
            item.CurrentCell=owner;
            if(item.IsTouchBlocking)AboveItems.Add(item);else BelowItems.Add(item);
        }
        public void ClearStaticItem(int type) // 0x34411f4.
        {
            var item=items[type];if(item==null)return;
            items[type]=null;item.CurrentCell=null;
            if(item.IsTouchBlocking)AboveItems.Remove(item);else BelowItems.Remove(item);
        }
    }
    public interface IOriginalExtraStaticFallExploders
    {
        int GetExplodeCountByNeighborsPropellerCombos(OriginalGridCell cell, IOriginalStaticFallItem target);
    }
    public partial class OriginalGridCell
    {
        private IOriginalExtraStaticFallExploders extraStaticFallExploders;
        public int HoneySpreadingCount { get; internal set; } // Native +0x38.
        public int RoofSpreadingCount { get; internal set; } // Native +0x3c.
        public bool IsAboveStaticSpreading => HoneySpreadingCount>0 || RoofSpreadingCount>0; // 0x319e620.
        public void AddStaticItem(IOriginalStaticFallItem item) => StaticTouch.AddStaticItem(item,this);
        public void BindExtraStaticFallExploders(IOriginalExtraStaticFallExploders value)
        { extraStaticFallExploders=value??throw new ArgumentNullException(nameof(value)); }
        public bool IsFallBlocked(bool includeIncomingExploders=false, bool includeExtraIncomingExploders=false) // 0x319d024.
        {
            if(!includeIncomingExploders)return StaticTouch.HasFallBlockingItem();
            if(StaticTouch.HasFallBlockingAboveStaticItem())return true;
            var chain=StaticTouch.GetStaticItem(6);if(chain==null)return false;
            int remaining=chain.RemainingExplodeCount();
            if(includeExtraIncomingExploders)
            {
                if(extraStaticFallExploders==null)throw new NullReferenceException();
                remaining=unchecked(remaining-extraStaticFallExploders.GetExplodeCountByNeighborsPropellerCombos(this,chain));
            }
            return remaining>0;
        }
    }
}
