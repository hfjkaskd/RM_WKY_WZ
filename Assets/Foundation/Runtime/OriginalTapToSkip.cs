using System;

namespace Whitebox.Foundation
{
    public interface IOriginalTapToSkipContext
    {
        OriginalActiveLevelData GetCurrentSession();
        bool HasRemainingMovesManager();
        void TriggerTapToSkipClick();
    }
    public interface IOriginalTapToSkipPresentation
    {
        void SetInteractionEnabled(bool enabled);
        void SetTextEnabled(bool enabled);
        void ArrangeTextPosition();
        void ShowText();
    }
    // TapToSkip0x18d084c/0x18d088c/0x18d0970/0x18d0ecc.
    // The original Collider/UiButton transport is replaced by a prefab Button.
    public sealed class OriginalTapToSkip
    {
        private readonly IOriginalTapToSkipContext context;
        private readonly IOriginalTapToSkipPresentation presentation;
        private OriginalActiveLevelData levelData;
        private int state; // Native:0 can skip,1 cannot skip,2 skipped.
        private bool isTextShown;
        public int State => state;
        public bool IsTextShown => isTextShown;
        public OriginalTapToSkip(IOriginalTapToSkipContext context, IOriginalTapToSkipPresentation presentation)
        { this.context = context; this.presentation = presentation; }
        public void Hide()
        {
            state = 1;
            (presentation ?? throw new NullReferenceException()).SetInteractionEnabled(false);
            presentation.SetTextEnabled(false);
        }
        public void EnableSkipWithoutText()
        {
            levelData = (context ?? throw new NullReferenceException()).GetCurrentSession();
            isTextShown = false;
            if ((levelData ?? throw new NullReferenceException()).Number == 1) return;
            state = 0;
            (presentation ?? throw new NullReferenceException()).SetInteractionEnabled(true);
        }
        public bool ShouldShowText()
        {
            int number = (levelData ?? throw new NullReferenceException()).Number;
            return number != 1 && number < 31;
        }
        public void ShowSkipText()
        {
            (presentation ?? throw new NullReferenceException()).ArrangeTextPosition();
            if (!ShouldShowText() || isTextShown) return;
            isTextShown = true;
            presentation.ShowText();
        }
        public void Clicked()
        {
            if (state != 0) return;
            if ((context ?? throw new NullReferenceException()).HasRemainingMovesManager()) context.TriggerTapToSkipClick();
            state = 2; // A dispatch failure preserves CanSkip, as in the ELF.
        }
    }
}
