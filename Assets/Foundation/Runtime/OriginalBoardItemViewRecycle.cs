namespace Whitebox.Foundation
{
    public partial class OriginalBoardItem : IOriginalItemViewRecycleModel
    {
        bool IOriginalItemViewRecycleModel.HasView { get => HasView; set => HasView = value; }
        IOriginalItemRecycleView IOriginalItemViewRecycleModel.GetItemView() => (IOriginalItemRecycleView)SwapView;
    }
}
