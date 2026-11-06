using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalFlowDialog { void Show(); }
    public interface IOriginalWinLogoDialog : IOriginalFlowDialog { void PlayWinLogoAnimation(); }
    public interface IOriginalWinLogoActionContext
    {
        float LogoBeforeShowDelay { get; }
        float LogoBeforePlayDelay { get; }
        void DisableUiTouch();
        void EnableUiTouch();
        Coroutine StartApplicationCoroutine(IEnumerator routine);
        IOriginalWinLogoDialog InstantiateWinLogoDialog(OriginalFlowAction action);
    }
    public sealed class OriginalShowWinLogoDialogAction : OriginalFlowAction
    {
        private readonly IOriginalWinLogoActionContext context;
        public OriginalShowWinLogoDialogAction(IOriginalWinLogoActionContext context) { this.context = context; }
        public override int Type => 2; // 0x18d0430: interrupt action.
        public override void Execute() { context.StartApplicationCoroutine(ShowLogoWithDelay()); }
        private IEnumerator ShowLogoWithDelay() // 0x18d054c.
        {
            context.DisableUiTouch();
            yield return new WaitForSeconds(context.LogoBeforeShowDelay);
            var dialog = context.InstantiateWinLogoDialog(this) ?? throw new NullReferenceException();
            dialog.Show();
            yield return new WaitForSeconds(context.LogoBeforePlayDelay);
            dialog.PlayWinLogoAnimation();
            context.EnableUiTouch();
        }
        public override void Cancel(OriginalFlowAction interruptAction) { } // 0x18d053c.
    }
    public interface IOriginalRemainingMovesActionContext
    {
        OriginalRemainingMovesManager GetRemainingMovesManager();
        IOriginalFlowDialog InstantiateRemainingMovesDialog(OriginalFlowAction action);
        void ResetTopUiSorting();
    }
    public sealed class OriginalRemainingMovesDialogAction : OriginalFlowAction
    {
        private readonly IOriginalRemainingMovesActionContext context;
        private OriginalRemainingMovesManager manager;
        public OriginalRemainingMovesDialogAction(IOriginalRemainingMovesActionContext context) { this.context = context; }
        public override void Execute() // 0x1118a64.
        {
            manager = context.GetRemainingMovesManager() ?? throw new NullReferenceException();
            if (manager.ShouldSendMoves())
                (context.InstantiateRemainingMovesDialog(this) ?? throw new NullReferenceException()).Show();
            else
            { context.ResetTopUiSorting(); manager.CompleteRemainingMovesImmediately(); Complete(); }
        }
        public override void Cancel(OriginalFlowAction interruptAction) { } // 0x1118bc8.
    }
    public interface IOriginalLevelWinDialog : IOriginalFlowDialog
    { void InjectBundle(AssetBundle bundle, sbyte difficulty); }
    public interface IOriginalLevelWinActionContext
    {
        void ResetTopUiSorting();
        OriginalActiveLevelData GetCurrentSession();
        IOriginalFlowDialog InstantiateDefaultWinDialog(OriginalFlowAction action);
        IOriginalLevelWinDialog InstantiateLeagueWinDialog(OriginalFlowAction action);
        bool IsHardOrExtremeLevelBundleAvailable { get; }
        bool IsLeagueBundleDownloaded { get; }
        AssetBundle TryLoadLeaguePopupBundle();
    }
    public sealed class OriginalShowLevelWinDialogAction : OriginalFlowAction
    {
        private readonly IOriginalLevelWinActionContext context;
        public OriginalShowLevelWinDialogAction(IOriginalLevelWinActionContext context) { this.context = context; }
        public override void Execute() // 0x133ef58 + default dialog 0x133f218.
        {
            context.ResetTopUiSorting();
            var session = context.GetCurrentSession() ?? throw new NullReferenceException();
            if (!session.IsLeague)
            { (context.InstantiateDefaultWinDialog(this) ?? throw new NullReferenceException()).Show(); return; }
            var dialog = context.InstantiateLeagueWinDialog(this);
            if (context.IsHardOrExtremeLevelBundleAvailable &&
                (session.IsLeagueHard || session.IsLeagueSuperHard || session.IsLeagueExtremelyHard))
                (dialog ?? throw new NullReferenceException()).InjectBundle(null, session.Difficulty);
            else if (context.IsLeagueBundleDownloaded)
            {
                var bundle = context.TryLoadLeaguePopupBundle();
                (dialog ?? throw new NullReferenceException()).InjectBundle(bundle, 0);
            }
            (dialog ?? throw new NullReferenceException()).Show();
        }
        public override void Cancel(OriginalFlowAction interruptAction) { } // 0x133f284.
    }
}
