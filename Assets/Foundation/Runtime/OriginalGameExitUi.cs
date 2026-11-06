using System;

namespace Whitebox.Foundation
{
    public interface IOriginalGameExitUiContext
    {
        void SetGameUiActive(bool active);
        void TryShowLandscapeFlags(bool show, bool animate);
        void PrepareHomeBackground();
        IOriginalDialogClearer GetDialogManager();
        void HideSlidingText();
    }
    // GameContextController 0x1e3a2bc. The scene supplies its configured UI,
    // flags/background and sliding-text owners; actual DialogManager clear is
    // shared with the same runtime UiDialog lifecycle used by result views.
    public sealed class OriginalGameExitUi : IOriginalGameExitUi
    {
        private readonly IOriginalGameExitUiContext context;
        public OriginalGameExitUi(IOriginalGameExitUiContext context) { this.context = context; }
        public void PrepareUiForHomeWhenNoLoadingDisplayed()
        {
            (context ?? throw new NullReferenceException()).SetGameUiActive(false);
            context.TryShowLandscapeFlags(true, false);
            context.PrepareHomeBackground();
            (context.GetDialogManager() ?? throw new NullReferenceException()).ClearDialogs();
            context.HideSlidingText();
        }
    }
}
