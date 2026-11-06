using System;

namespace Whitebox.Foundation
{
    public interface IOriginalItemViewRecycleModel
    {
        bool HasView { get; set; }
        IOriginalItemRecycleView GetItemView();
    }
    public interface IOriginalItemRecycleView
    {
        bool IsAlive { get; }
        bool IsRootHidden { get; }
        bool IsFillMaskEnabled { get; }
        void Show();
        void DisableFillMask();
        void ResetAnimationToDefault();
    }
    public interface IOriginalItemViewRecycleFactory
    {
        void Recycle(IOriginalItemRecycleView view);
    }
    public static class OriginalItemViewRecycle
    {
        // ItemModel.RecycleView 0x2b46db0. HasView is cleared before the view
        // lookup, including null/destroyed views and later callback failures.
        public static void Recycle(IOriginalItemViewRecycleModel model, IOriginalItemViewRecycleFactory factory)
        {
            if (!model.HasView) return;
            model.HasView = false;
            var view = model.GetItemView();
            if (view == null || !view.IsAlive) return;
            if (view.IsRootHidden) view.Show();
            if (view.IsFillMaskEnabled) view.DisableFillMask();
            view.ResetAnimationToDefault();
            if (factory == null) throw new NullReferenceException();
            factory.Recycle(view);
        }
    }
}
