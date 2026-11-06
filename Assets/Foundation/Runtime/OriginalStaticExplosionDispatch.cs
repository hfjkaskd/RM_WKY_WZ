using System;

namespace Whitebox.Foundation
{
    public interface IOriginalStaticExplosionItem : IOriginalStaticFallItem
    {
        bool CanExplodeWithMatch { get; }
        bool CanExplodeWithNearMatch { get; }
        void Explode(OriginalExplodeData data);
        void ExplodeAll(OriginalExplodeData data);
    }
    public sealed partial class OriginalStaticTouchMediator
    {
        private readonly OriginalExploderContainer aboveExploders = new OriginalExploderContainer();
        private readonly OriginalExploderContainer belowExploders = new OriginalExploderContainer();

        public bool ExplodeTopMostAboveItem(OriginalExplodeData data) // 0x3440544.
        {
            if (aboveExploders.ContainsExploder(data.Id)) return true;
            if (AboveItems.Count == 0) return false;
            var item = RequireExplosion(AboveItems[AboveItems.Count - 1]);
            if (!item.CanExplodeWithNearMatch && data.Trigger == 5) return true;
            aboveExploders.AddToExploders(data.Id);
            belowExploders.AddToExploders(data.Id);
            ExplodeStaticItem(item.StaticItemType, data);
            return true;
        }
        public void ExplodeStaticItem(int type, OriginalExplodeData data) // 0x3440658.
        {
            var item = items[type];
            if (item == null) return;
            var explodable = RequireExplosion(item);
            if (data.ShouldExplodeAll) explodable.ExplodeAll(data);
            else explodable.Explode(data);
        }
        public void ExplodeTopMostBelowItem(OriginalExplodeData data) // 0x34407f4.
        {
            if (BelowItems.Count == 0 || belowExploders.ContainsExploder(data.Id)) return;
            // Unlike GetTopMostBelowItem, this native method reads index0.
            var item = RequireExplosion(BelowItems[0]);
            if (item.StaticItemType == 3)
            {
                if (data.CanSpreadJelly || data.Trigger == 6)
                {
                    belowExploders.AddToExploders(data.Id);
                    ExplodeStaticItem(item.StaticItemType, data);
                }
                else ExplodeStaticItem(3, data); // No exploder is recorded.
                return;
            }
            if (data.ShouldExplodeAll) return;
            if (!item.CanExplodeWithMatch)
            {
                if (data.Trigger == 2 || (data.Trigger & -2) == 4) return;
                if (data.Trigger == 15 && data.MatchType != 0) return;
            }
            belowExploders.AddToExploders(data.Id);
            ExplodeStaticItem(item.StaticItemType, data);
        }
        private static IOriginalStaticExplosionItem RequireExplosion(IOriginalStaticTouchItem item)
        {
            if (item == null) throw new NullReferenceException();
            var result = item as IOriginalStaticExplosionItem;
            if (result == null) throw new InvalidCastException("Static item explosion capability is required.");
            return result;
        }
    }
    public sealed class OriginalCellStaticExplosionServices : IOriginalCellStaticExplosionServices
    {
        public bool TryExplodeTopMostAboveItem(OriginalGridCell cell, OriginalExplodeData data) => cell.StaticTouch.ExplodeTopMostAboveItem(data);
    }
}
