using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchTypeProvider { int MatchType { get; } }
    public interface IOriginalMatchHighlightView { void Highlight(bool enable); }
    public interface IOriginalMatchHighlightSurface
    {
        void SetHighlightActive(bool active);
        float HighlightAlpha { get; set; }
    }
    // MatchItemView.Highlight / SetHighlightRatio / GetMaxHighlightRatio,
    // 0x21a6ae8 / 0x21a74f8 / 0x21a76e4. Inline state in each pooled view;
    // alpha writes preserve the prefab renderer's RGB channels.
    public struct OriginalMatchHighlight
    {
        private readonly IOriginalMatchHighlightSurface surface;
        private IOriginalMatchTypeProvider model;
        public bool IsHighlighted { get; private set; }
        // The native ItemView +0x68 input. Static-item composition owns it;
        // this is not the full base-view setter/sorting lifecycle.
        public bool IsUnderNonFallBlockingAboveStaticItem { get; set; }
        public OriginalMatchHighlight(IOriginalMatchHighlightSurface surface)
        {
            this.surface = surface ?? throw new ArgumentNullException(nameof(surface));
            model = null; IsHighlighted = false; IsUnderNonFallBlockingAboveStaticItem = false;
        }
        public void BindModel(IOriginalMatchTypeProvider value) { model = value; }
        public float GetHighlightRatio() => surface.HighlightAlpha;
        public float GetMaxHighlightRatio()
        {
            if (model == null) return 0f;
            // Original read-only table at ELF 0x40408a8, indexed by match type.
            switch (model.MatchType)
            {
                case 1: return .5098039507865906f;
                case 2: return .5490196347236633f;
                case 3: return 1f;
                case 4: return .6666666865348816f;
                case 5: return .7843137383460999f;
                default: return 1f;
            }
        }
        public void Highlight(bool enable)
        {
            bool active = !IsUnderNonFallBlockingAboveStaticItem && enable;
            surface.SetHighlightActive(active);
            IsHighlighted = active;
            if (active) SetHighlightRatio(GetMaxHighlightRatio());
        }
        public void SetHighlightRatio(float ratio)
        {
            // Ordered > preserves native b.le behavior for NaN.
            if (ratio > 0f && !IsHighlighted) Highlight(true);
            else if (IsHighlighted && ratio > -.001f && ratio < .001f) Highlight(false);
            surface.HighlightAlpha = ratio;
        }
    }
}
