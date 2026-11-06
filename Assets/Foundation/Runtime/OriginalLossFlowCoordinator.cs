using System;

namespace Whitebox.Foundation
{
    public interface IOriginalLavaLevelEndFlow
    {
        bool TryPushLavaLevelEndFlow(bool isFail, Action onComplete);
    }

    public interface IOriginalAncientLevelEndFlow
    {
        bool TryPushAncientAdventureLevelEndFlow(Action onComplete, bool isFail, bool isQuit);
    }

    public interface IOriginalLossFlowContext
    {
        void StopGameMusic(bool pause);
        void KingBeSad();
        void EgoShown();
        void SessionNoPossibleMove();
        void SessionFail();
        void LevelNoPossibleMove();
        void LevelEnd();
        OriginalFlowManager GetFlowManager();
        void IncreaseTopUiMove();
        void PlaySound(int sound, float offset);
        IOriginalLavaLevelEndFlow GetLavaQuest();
        IOriginalAncientLevelEndFlow GetAncientAdventure();
        bool ShouldShowExtremelyHardOfferAtFail();
        OriginalFlowAction CreateSuperDukeGameplayActionWithoutTutorial();
        OriginalFlowAction CreateOutOfMovesBannerAction();
        OriginalFlowAction CreateNoPossibleMovesDialogAction();
        OriginalFlowAction CreateExtremelyHardOfferAction(int origin, bool isReopen);
        OriginalFlowAction CreateFailDialogAction(bool clearBoosterSelection);
    }

    // Native WinFailManager loss/continuation dispatch. Application, event,
    // session and prefab dialog owners are required; none are treated as
    // inactive merely because they have not been composed into the scene.
    public sealed class OriginalLossFlowCoordinator
    {
        private readonly IOriginalLossFlowContext context;
        public OriginalLossFlowCoordinator(IOriginalLossFlowContext context) { this.context = context; }

        public void StartExtraMoveFlow() // 0x1ede230.
        {
            var owner = context ?? throw new NullReferenceException();
            var flow = owner.GetFlowManager();
            var action = owner.CreateSuperDukeGameplayActionWithoutTutorial();
            (flow ?? throw new NullReferenceException()).Push(action);
            owner.IncreaseTopUiMove();
        }

        public void StartEgoFlow() // 0x1ede420.
        {
            var owner = context ?? throw new NullReferenceException();
            owner.StopGameMusic(true);
            owner.KingBeSad();
            owner.EgoShown();
            // Native currentStep formatting here only feeds Log.Debug.
            var flow = owner.GetFlowManager();
            var action = owner.CreateOutOfMovesBannerAction();
            (flow ?? throw new NullReferenceException()).Push(action);
        }

        public void StartNoPossibleMoveFlow() // 0x1ede668.
        {
            var owner = context ?? throw new NullReferenceException();
            owner.StopGameMusic(false);
            owner.SessionNoPossibleMove();
            owner.LevelNoPossibleMove();
            var flow = owner.GetFlowManager();
            owner.PlaySound(212, .04f);
            var action = owner.CreateNoPossibleMovesDialogAction();
            (flow ?? throw new NullReferenceException()).Push(action);
        }

        public void StartFailFlow() // 0x1ede7f0.
        {
            var owner = context ?? throw new NullReferenceException();
            owner.StopGameMusic(false);
            owner.SessionFail();
            owner.LevelEnd();
            var lava = owner.GetLavaQuest();
            var lavaComplete = new Action(ShowFailDialog);
            if ((lava ?? throw new NullReferenceException()).TryPushLavaLevelEndFlow(true, lavaComplete)) return;
            var ancient = owner.GetAncientAdventure();
            var ancientComplete = new Action(ShowFailDialog);
            if ((ancient ?? throw new NullReferenceException()).TryPushAncientAdventureLevelEndFlow(ancientComplete, true, false)) return;
            ShowFailDialog();
        }

        public void ShowFailDialog() // 0x1edeae8, also the two event completion callbacks.
        {
            var owner = context ?? throw new NullReferenceException();
            var flow = owner.GetFlowManager();
            owner.PlaySound(212, .04f);
            var action = owner.ShouldShowExtremelyHardOfferAtFail()
                ? owner.CreateExtremelyHardOfferAction(3, false)
                : owner.CreateFailDialogAction(true);
            (flow ?? throw new NullReferenceException()).Push(action);
        }
    }
}
