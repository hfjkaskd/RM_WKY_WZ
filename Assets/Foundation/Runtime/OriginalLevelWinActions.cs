using System;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelWinAds
    {
        void TryShowForCoin(Action<bool> completed);
        void TryShowAfterLevelWin(Action completed);
    }
    public interface IOriginalLevelWinGameManager { void ExitGameWhenPossible(); }
    public interface IOriginalLevelWinLavaFlow { bool TryPushLevelEndFlow(int state, Action completed); }
    public interface IOriginalLevelWinAncientFlow { bool TryPushLevelEndFlow(Action completed, bool didFail, bool didWin); }
    public interface IOriginalLevelWinCloseContext
    {
        IOriginalLevelWinAds GetAds();
        void EnableOrientationForStarAnimation();
        void EnableBottomPanelOrientation();
        IOriginalLevelWinGameManager GetGameManager();
        IOriginalLevelWinLavaFlow GetLavaFlow();
        IOriginalLevelWinAncientFlow GetAncientFlow();
        void CloseDialog();
    }
    // LevelWinDialog control portion0x11205f0/0x112060c/0x1121aa0/
    //0x1121b74. SDK work stays behind the existing typed ad transport.
    public sealed class OriginalLevelWinActions
    {
        private readonly IOriginalLevelWinCloseContext context;
        public bool CallToActionClicked { get; private set; }
        public OriginalLevelWinActions(IOriginalLevelWinCloseContext context) { this.context = context; }
        public void OnContinueButton()
        {
            if (CallToActionClicked) return;
            CallToActionClicked = true;
            ShowAd(false);
        }
        public void OnCoinRewardedAdButtonClicked() => ShowAd(true);
        private void ShowAd(bool rewarded)
        {
            var ads = (context ?? throw new NullReferenceException()).GetAds();
            if (rewarded) (ads ?? throw new NullReferenceException()).TryShowForCoin(RewardedCompleted);
            else (ads ?? throw new NullReferenceException()).TryShowAfterLevelWin(TryCloseDialog);
        }
        private void RewardedCompleted(bool received) { if (received) TryCloseDialog(); }
        public void OnClosePressed() =>
            ((context ?? throw new NullReferenceException()).GetAds() ?? throw new NullReferenceException()).TryShowAfterLevelWin(TryCloseDialog);
        public void TryCloseDialog()
        {
            (context ?? throw new NullReferenceException()).EnableOrientationForStarAnimation();
            context.EnableBottomPanelOrientation();
            var manager = context.GetGameManager(); // Native closure captures this lookup before either event gate.
            Action exit = () => (manager ?? throw new NullReferenceException()).ExitGameWhenPossible();
            var lava = context.GetLavaFlow();
            if ((lava ?? throw new NullReferenceException()).TryPushLevelEndFlow(2, exit))
            { context.CloseDialog(); return; }
            var ancient = context.GetAncientFlow();
            if ((ancient ?? throw new NullReferenceException()).TryPushLevelEndFlow(exit, false, true))
            { context.CloseDialog(); return; }
            (manager ?? throw new NullReferenceException()).ExitGameWhenPossible();
        }
    }
}
