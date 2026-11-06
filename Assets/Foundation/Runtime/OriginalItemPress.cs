using System;

namespace Whitebox.Foundation
{
    public partial class OriginalBoardItem
    {
        public virtual bool PlayTouchDownAnimation() => false; // ItemModel 0x2b47acc.
        public virtual void StopTouchDownAnimation() { } // 0x2b47ad4.
    }
    public sealed partial class OriginalMatchBoardItem : IOriginalMatchTypeProvider
    {
        public override bool PlayTouchDownAnimation() // MatchItemModel 0x21a6abc.
        {
            RequireHighlightView().Highlight(true);
            return base.PlayTouchDownAnimation();
        }
        public override void StopTouchDownAnimation() => RequireHighlightView().Highlight(false); // 0x21a6b58.
        private IOriginalMatchHighlightView RequireHighlightView()
        {
            var view = SwapView as IOriginalMatchHighlightView;
            if (view == null) throw new NullReferenceException();
            return view;
        }
    }
    public sealed partial class OriginalBoxBoardItem
    {
        public override bool PlayTouchDownAnimation() // BoxItemModel 0x2bc5d50.
        {
            ItemView.PlayTapAnimation();
            return true;
        }
    }
}
