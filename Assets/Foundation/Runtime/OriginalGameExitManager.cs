using System;

namespace Whitebox.Foundation
{
    public interface IOriginalGameExitScene { bool WillShowLoadingOnLoadHome(); }
    public interface IOriginalGameExitUi { void PrepareUiForHomeWhenNoLoadingDisplayed(); }

    // Exit portion of GameManager, 0x1e3b604/0x1e3b62c. Scene deactivation
    // remains owned by GameContext.ManualUpdate, after this method returns.
    public sealed class OriginalGameExitManager : IOriginalLevelWinGameManager
    {
        private readonly OriginalGameStateManager state;
        private readonly IOriginalGameExitScene scene;
        private readonly Func<IOriginalGameExitUi> controller;
        public bool ShouldExitGame;
        public event Action OnGameExit;
        public OriginalGameExitManager(OriginalGameStateManager state, IOriginalGameExitScene scene, Func<IOriginalGameExitUi> controller)
        { this.state = state; this.scene = scene; this.controller = controller; }
        public void ExitGameWhenPossible()
        {
            (state ?? throw new NullReferenceException()).SetStateToNoGame();
            if (!(scene ?? throw new NullReferenceException()).WillShowLoadingOnLoadHome())
                ((controller ?? throw new NullReferenceException())() ?? throw new NullReferenceException()).PrepareUiForHomeWhenNoLoadingDisplayed();
            OnGameExit?.Invoke();
            ShouldExitGame = true;
        }
        public void Clear(bool gameExit) // GameManager.Clear 0x1e3ac98, exit-owned fields.
        {
            ShouldExitGame = false;
            if (gameExit) OnGameExit = null;
        }
    }

    // SceneChangeManager 0x3908264. Native lazy lookups retain their branch
    // order; loading history/force flags belong to the actual scene owner.
    public sealed class OriginalSceneHomeLoadingDecision : IOriginalGameExitScene
    {
        private readonly Func<bool> isStory, isTableGame, isLava, hasHomeArea;
        public bool ShouldShowLoadingAccordingToPreviousSceneLoad, ForceLoadHomeScene;
        public OriginalSceneHomeLoadingDecision(Func<bool> isStory, Func<bool> isTableGame, Func<bool> isLava, Func<bool> hasHomeArea)
        { this.isStory = isStory; this.isTableGame = isTableGame; this.isLava = isLava; this.hasHomeArea = hasHomeArea; }
        public bool WillShowLoadingOnLoadHome()
        {
            if (ShouldShowLoadingAccordingToPreviousSceneLoad) return true;
            if ((isStory ?? throw new NullReferenceException())()) return true;
            if ((isTableGame ?? throw new NullReferenceException())()) return true;
            if ((isLava ?? throw new NullReferenceException())() && !(hasHomeArea ?? throw new NullReferenceException())()) return true;
            return ForceLoadHomeScene;
        }
    }
}
