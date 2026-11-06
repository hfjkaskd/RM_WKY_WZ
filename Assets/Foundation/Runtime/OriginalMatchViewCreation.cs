using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchCreatedItemView : IOriginalItemSwapView, IOriginalCreatedItemView { }
    public interface IOriginalMatchViewCreationServices
    {
        IOriginalMatchCreatedItemView SpawnMatchView();
        void InitializeMatchView(IOriginalMatchCreatedItemView view, OriginalMatchBoardItem model, OriginalSwapPoint position);
    }
    public partial class OriginalBoardItem
    {
        internal void SetCreationSwapView(IOriginalItemSwapView view) { SwapView = view; }
    }
    public sealed partial class OriginalMatchBoardItem
    {
        private IOriginalMatchViewCreationServices viewCreation;
        public void BindViewCreationServices(IOriginalMatchViewCreationServices services) { viewCreation = services; }
        public void CreateView(OriginalSwapPoint position) // MatchItemModel.CreateView 0x21a4fd8.
        {
            var view = viewCreation.SpawnMatchView();
            SetCreationSwapView(view); // Assignment precedes Init, including a null spawn result.
            if (view == null) throw new NullReferenceException();
            viewCreation.InitializeMatchView(view, this, position);
            HasView = true;
        }
    }
}
