using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalLeagueOutOfMovesBanner : IOriginalFlowDialog
    {
        void Initialize(AssetBundle bundle);
    }

    public interface IOriginalOutOfMovesBannerContext
    {
        OriginalActiveLevelData GetCurrentSession();
        bool ShouldUpdateUiForPortalLevel();
        bool IsHardOrExtremeLevelBundleAvailable { get; }
        bool IsLeagueBundleDownloaded { get; }
        AssetBundle TryLoadLeaguePopupBundle();
        IOriginalFlowDialog InstantiateDefaultBanner(OriginalFlowAction action);
        IOriginalLeagueOutOfMovesBanner InstantiateLeagueBanner(OriginalFlowAction action);
    }

    // Native ShowOutOfMovesBannerAction.Execute0x1595634. A missing downloaded
    // league bundle completes the action without showing the default banner.
    public sealed class OriginalShowOutOfMovesBannerAction : OriginalFlowAction
    {
        private readonly IOriginalOutOfMovesBannerContext context;
        public OriginalShowOutOfMovesBannerAction(IOriginalOutOfMovesBannerContext context) { this.context = context; }
        public override int Type => 2;

        public override void Execute()
        {
            var owner = context ?? throw new NullReferenceException();
            // Native captures the active-level pointer before querying portal
            // state; the portal branch permits that pointer to be absent.
            var session = owner.GetCurrentSession();
            if (owner.ShouldUpdateUiForPortalLevel()) { ShowDefault(owner); return; }
            if (!(session ?? throw new NullReferenceException()).IsLeague) { ShowDefault(owner); return; }
            if (owner.IsHardOrExtremeLevelBundleAvailable &&
                (session.IsLeagueHard || session.IsLeagueSuperHard || session.IsLeagueExtremelyHard))
            { ShowDefault(owner); return; }
            if (!owner.IsLeagueBundleDownloaded) { ShowDefault(owner); return; }
            var bundle = owner.TryLoadLeaguePopupBundle();
            if (ReferenceEquals(bundle, null)) { Complete(); return; } // Native cbz, not Unity Object equality.
            var dialog = owner.InstantiateLeagueBanner(this) ?? throw new NullReferenceException();
            dialog.Initialize(bundle);
            dialog.Show();
        }

        private void ShowDefault(IOriginalOutOfMovesBannerContext owner) =>
            (owner.InstantiateDefaultBanner(this) ?? throw new NullReferenceException()).Show();

        public override void Cancel(OriginalFlowAction interruptAction) { } // 0x1595910.
    }
}
