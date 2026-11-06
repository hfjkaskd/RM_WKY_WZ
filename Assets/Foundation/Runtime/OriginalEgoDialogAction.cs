using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalEgoDialog : IOriginalFlowDialog
    {
        void Init(int originType, bool isOriginAddCoin, float backgroundFadeInDuration);
    }

    public interface IOriginalEgoDialogContext
    {
        OriginalActiveLevelData GetCurrentSession();
        bool ShouldUpdateUiForPortalLevel();
        bool IsHardOrExtremeLevelBundleAvailable { get; }
        bool IsLeagueBundleDownloaded { get; }
        AssetBundle TryLoadLeaguePopupBundle();
        IOriginalEgoDialog InstantiateDefaultEgoDialog(OriginalFlowAction action);
        IOriginalEgoDialog InstantiateLeagueEgoDialog(OriginalFlowAction action);
    }

    public sealed class OriginalShowEgoDialogAction : OriginalFlowAction
    {
        private readonly IOriginalEgoDialogContext context;
        private readonly bool isOriginAddCoin;
        private readonly float backgroundFadeInDuration;
        private IOriginalEgoDialog dialog;
        public int OriginType { get; }
        public override int Type => 1; // 0x1594a20: priority, unlike the interrupt banner.

        public OriginalShowEgoDialogAction(IOriginalEgoDialogContext context, int originType = 0,
            bool isOriginAddCoin = false, float backgroundFadeInDuration = .2f) // 0x158a8fc.
        {
            this.context = context;
            OriginType = originType;
            this.isOriginAddCoin = isOriginAddCoin;
            this.backgroundFadeInDuration = backgroundFadeInDuration;
        }

        public override void Execute() // 0x1594a28.
        {
            var owner = context ?? throw new NullReferenceException();
            var session = owner.GetCurrentSession();
            if (owner.ShouldUpdateUiForPortalLevel()) dialog = owner.InstantiateDefaultEgoDialog(this);
            else if (!(session ?? throw new NullReferenceException()).IsLeague) dialog = owner.InstantiateDefaultEgoDialog(this);
            else if (owner.IsHardOrExtremeLevelBundleAvailable &&
                (session.IsLeagueHard || session.IsLeagueSuperHard || session.IsLeagueExtremelyHard))
                dialog = owner.InstantiateDefaultEgoDialog(this);
            else if (!owner.IsLeagueBundleDownloaded) dialog = owner.InstantiateDefaultEgoDialog(this);
            else
            {
                var bundle = owner.TryLoadLeaguePopupBundle();
                if (ReferenceEquals(bundle, null)) { Complete(); return; } // Native cbz, not Unity Object equality.
                // The native action tests the bundle, then instantiates the
                // league resource. It does not inject that bundle into Ego.
                dialog = owner.InstantiateLeagueEgoDialog(this);
            }
            (dialog ?? throw new NullReferenceException()).Init(OriginType, isOriginAddCoin, backgroundFadeInDuration);
            // Init may synchronously invoke other owners; native rereads dialog.
            (dialog ?? throw new NullReferenceException()).Show();
        }

        public override void Cancel(OriginalFlowAction interruptAction) { } // 0x1594d2c.
    }
}
