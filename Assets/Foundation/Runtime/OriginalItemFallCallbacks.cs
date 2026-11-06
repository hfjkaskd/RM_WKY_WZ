using System;

namespace Whitebox.Foundation
{
    public interface IOriginalItemFallViewDelegate { OriginalGridCell CurrentCell { get; } }
    public interface IOriginalItemFallSortingSurface
    {
        int GetAtlasId();
        bool ShouldSortByCell();
        bool IsReverseSorted();
        OriginalSwapSorting GetItemSorting(int atlasId,bool byCell,int x,int y,bool reverse);
        OriginalSwapSorting GetSorting();
        void UpdateSorting(OriginalSwapSorting sorting);
    }
    // Common ItemView fall notification and under-static setter. Roof model
    // behavior stays with its typed owner, rather than being silently ignored.
    public sealed class OriginalItemFallCallbacks
    {
        private IOriginalItemFallViewDelegate model;
        private readonly IOriginalItemFallSortingSurface surface;
        public bool IsUnderNonFallBlockingAboveStaticItem { get; private set; }
        public OriginalItemFallCallbacks(IOriginalItemFallSortingSurface surface)
        { this.surface=surface??throw new ArgumentNullException(nameof(surface)); }
        public void BindModel(IOriginalItemFallViewDelegate value) { model=value; }
        public void FallAnimationStarted() // 0x2b4b034.
        {
            if(model==null)return;
            var cell=model.CurrentCell;
            var candidate=cell==null?null:cell.StaticTouch?.GetStaticItem(7);
            IOriginalRoofFallItem roof=null;
            if(candidate!=null)
            {
                roof=candidate as IOriginalRoofFallItem;
                if(roof==null)throw new InvalidCastException();
            }
            // Native re-reads its delegate and still checks below when no roof
            // was found. Keep the second query and its observable call order.
            var current=model==null?null:model.CurrentCell;
            var below=current?.Neighbors?.Get(5);
            bool? hasRoof=below?.StaticTouch==null?(bool?)null:below.StaticTouch.HasStaticItem(7);
            if(roof!=null && hasRoof!=true)roof.NotifyForFallUnderneath();
        }
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) // 0x2b4b288.
        {
            IsUnderNonFallBlockingAboveStaticItem=under;
            var cell=model==null?null:model.CurrentCell;
            int x=cell==null?9:cell.X,y=cell==null?11:cell.Y;
            int atlas=surface.GetAtlasId();bool byCell=surface.ShouldSortByCell();bool reverse=surface.IsReverseSorted();
            var sorting=surface.GetItemSorting(atlas,byCell,x,y,reverse);
            surface.UpdateSorting(sorting);
        }
    }
    public sealed partial class OriginalMatchBoardItem : IOriginalItemFallViewDelegate
    {
        OriginalGridCell IOriginalItemFallViewDelegate.CurrentCell => Mediator.CurrentCell?.Owner;
    }
}
