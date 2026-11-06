using System;

namespace Whitebox.Foundation
{
    public partial class OriginalBoardItem
    {
        public static int StaticId; // Original ItemModel.StaticId, default zero; lifetime/reset belongs to its application owner.
        public int TiledId { get; set; }
        protected OriginalBoardItem(OriginalItemSettings settings, int initialLayer) : this(0, settings.ItemType)
        {
            layer = initialLayer; TiledId = settings.TiledId;
            // Original 0x2b45f5c assigns the current counter after creating its
            // mediator/exploder containers, then increments with int overflow.
            Id = StaticId; StaticId = unchecked(StaticId + 1);
        }
        public virtual void CreateFallComponent() { FallComponent = new OriginalFallComponentState(this); } // 0x2b46230.
    }
    public sealed partial class OriginalMatchBoardItem
    {
        public bool IsHandGiven { get; private set; }
        internal OriginalMatchBoardItem(OriginalItemSettings settings) : base(settings, 1) // 0x21a4e50.
        { MatchType = settings.MatchType; IsHandGiven = settings.IsHandGiven; }

        // MatchItemModel.GetDataLength 0x21a7bd4: three layer bits and one hand-given bit.
        internal override short SaveRestorableData() => (short)(layer | (IsHandGiven ? 8 : 0));
        internal override void RestoreItemData(short data)
        { layer = data & 7; IsHandGiven = (data & 8) != 0; }
    }
    public sealed partial class OriginalBoxBoardItem
    {
        internal OriginalBoxBoardItem(OriginalItemSettings settings) : base(settings, settings.LayerCount) { } // 0x2bc58fc.
    }
    // The match/box construction branches and common post-construction work of
    // ItemModelFactory.CreateItem 0x23ec1e8. Other native model classes and the
    // application SuperBooster query must be composed by the complete factory.
    public static class OriginalCoreItemModels
    {
        public static OriginalBoardItem Create(OriginalItemSettings settings, int[] potionColors = null)
        {
            OriginalBoardItem item;
            switch (settings.ItemType)
            {
                case 1: item = new OriginalMatchBoardItem(settings); break;
                case 6: item = new OriginalBoxBoardItem(settings); break;
                case 7: item = new OriginalVaseBoardItem(settings); break;
                case 9: item = new OriginalEggBoardItem(settings); break;
                case 10: item = new OriginalOwlStatueBoardItem(settings); break;
                case 11: item = new OriginalMailBoxBoardItem(settings); break;
                case 12:
                    item = settings.TiledId == 53 ? (OriginalBoardItem)new OriginalPotionBoardItem(settings, potionColors)
                        : new OriginalPotionFakeBoardItem(settings);
                    break;
                case 13:
                    item = settings.TiledId == 55 ? (OriginalBoardItem)new OriginalBushBoardItem(settings)
                        : new OriginalBushFakeBoardItem(settings);
                    break;
                case 14:
                    item = settings.TiledId == 57 ? (OriginalBoardItem)new OriginalSafeBoardItem(settings)
                        : new OriginalSafeFakeBoardItem(settings);
                    break;
                case 15: item = new OriginalCoinBoardItem(settings); break;
                case 8:
                    item = settings.TiledId == 51 ? (OriginalBoardItem)new OriginalCupboardBoardItem(settings)
                        : new OriginalCupboardFakeBoardItem(settings);
                    break;
                default: throw new NotSupportedException("Concrete original item model is not restored: " + settings.ItemType);
            }
            item.CreateFallComponent();
            item.GoalType = settings.GoalType;
            return item;
        }
    }
}
