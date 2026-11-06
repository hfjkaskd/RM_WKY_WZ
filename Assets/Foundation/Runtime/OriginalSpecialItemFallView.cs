using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Common ItemView motion callbacks. Each concrete recovered prefab owns its
    // original RootTransform/FallSquash; motion does not replace those effects.
    public sealed class OriginalSpecialItemFallView : IOriginalFallItemView, IOriginalItemFallSortingSurface
    {
        private readonly OriginalSpecialBoardItem model;
        private readonly Transform transform;
        private readonly int layer;
        private readonly OriginalItemFallCallbacks callbacks;
        private readonly OriginalRootTransform root;
        public OriginalSpecialItemFallView(OriginalSpecialBoardItem model, int layer)
        {
            this.model = model; this.layer = layer;
            transform = ((Component)model.View).transform;
            if (model is OriginalRocketBoardItem rocket) root = rocket.ItemView.rootTransform;
            else if (model is OriginalTntBoardItem tnt) root = tnt.ItemView.rootTransform;
            else if (model is OriginalLightballBoardItem lightball) root = lightball.ItemView.rootTransform;
            else if (model is OriginalPropellerBoardItem propeller) root = propeller.ItemView.rootTransform;
            else throw new NotSupportedException("Concrete special root is required.");
            callbacks = new OriginalItemFallCallbacks(this); callbacks.BindModel(model);
        }
        public OriginalSwapPoint Position
        {
            get { var p = transform.position; return new OriginalSwapPoint(p.x, p.y, p.z); }
            set { transform.position = new Vector3(value.X, value.Y, value.Z); }
        }
        public OriginalSwapPoint LocalPosition { get { var p = transform.localPosition; return new OriginalSwapPoint(p.x, p.y, p.z); } }
        public bool IsFillMaskEnabled => ((OriginalItemSwapView)model.View).IsFillMaskEnabled;
        public bool IsUnderNonFallBlockingAboveStaticItem => callbacks.IsUnderNonFallBlockingAboveStaticItem;
        public void DisableFillMask() => ((OriginalItemSwapView)model.View).DisableFillMask();
        public void ArrangeSelfSorting() => UpdateSorting(OriginalItemSortingRules.Default(layer));
        public void UpdateSorting(OriginalSwapSorting sorting) => model.View.UpdateSorting(sorting);
        public OriginalSwapSorting GetSorting()
        {
            if (model is OriginalRocketBoardItem rocket) return rocket.ItemView.GetSorting();
            if (model is OriginalTntBoardItem tnt) return tnt.ItemView.GetSorting();
            if (model is OriginalLightballBoardItem lightball) return lightball.ItemView.GetSorting();
            if (model is OriginalPropellerBoardItem propeller) return propeller.ItemView.GetSorting();
            throw new NotSupportedException("Concrete special view sorting is required.");
        }
        public void UpdateFallState(int state, float speed, float maxSpeed) => root.UpdateFallState(state, speed, maxSpeed);
        public void FallStarted() { if (model is OriginalPropellerBoardItem propeller) propeller.ItemView.FallStarted(); } // Other types inherit ItemView 0x2b4b02c.
        public void FallEnded() { if (model is OriginalPropellerBoardItem propeller) propeller.ItemView.FallEnded(); } // Other types inherit ItemView 0x2b4b030.
        public void FallAnimationStarted() => callbacks.FallAnimationStarted();
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under)
        { callbacks.SetIsUnderNonFallBlockingAboveStaticItem(under); model.View.SetIsUnderNonFallBlockingAboveStaticItem(under); }
        public int GetAtlasId() => -1;
        public bool ShouldSortByCell() => false;
        public bool IsReverseSorted() => false;
        public OriginalSwapSorting GetItemSorting(int atlas, bool byCell, int x, int y, bool reverse) => OriginalItemSortingRules.Default(layer);
    }
}
