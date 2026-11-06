using System;

namespace Whitebox.Foundation
{
    // Shared publication belongs to the application context. Supplying this
    // state does not construct/bind a playing GameContext or its units.
    public sealed class OriginalGameContextRuntimeState
    {
        public object Controller;
        public bool IsActive;
    }
    public interface IOriginalGameContextItemCleanup
    {
        void RecycleAllSpawned();
        void ClearPools();
    }
    public interface IOriginalGameContextTouchEnabler
    {
        void EnablePinch(object owner);
        void EnableVerticalScroll(object owner);
        void EnableHorizontalScroll(object owner);
    }
    public interface IOriginalGameContextSceneLoader
    {
        void LoadHomeScene();
        void LoadGameScene();
    }
    public interface IOriginalGameContextDeactivationServices
    {
        void ManualUpdateGameUnits();
        void RemoveOrientationChangeListener(OriginalGameContextDeactivation owner);
        IOriginalGameContextUnit[] GetGameUnits();
        void KillAllTweens(bool complete);
        void StopAllManualTweenCoroutines();
        void KillGameCoroutines();
        IOriginalGameContextItemCleanup GetItemFactory();
        void StopAllSounds();
        void ResetCurrentCameraOrientationLock();
        void KillAllAnimatorUpdates();
        IOriginalGameContextTouchEnabler GetUiTouchEnabler();
        bool IsLowEnd { get; }
        bool IsEarlySideProgressionLevel { get; }
        bool IsSideProgressionLevelPlayed();
        void ReloadLevel();
        IOriginalGameContextSceneLoader GetSceneManager();
        void SendGetMessages(object request);
    }
    // GameContext exit portion: ManualUpdate, Deactivate, ResetScene and
    // KillAllAnimations. Activation/factory/layout/scene loading are separate
    // owners; this class never supplies no-op units or a fabricated Home view.
    public sealed class OriginalGameContextDeactivation
    {
        private readonly OriginalGameContextRuntimeState state;
        private readonly OriginalGameExitManager manager;
        private readonly OriginalGameLateUpdateDispatcher lateUpdates;
        private readonly IOriginalGameContextDeactivationServices services;
        public event Action OnDeactivate;
        public OriginalGameContextDeactivation(OriginalGameContextRuntimeState state, OriginalGameExitManager manager,
            OriginalGameLateUpdateDispatcher lateUpdates, IOriginalGameContextDeactivationServices services)
        { this.state = state; this.manager = manager; this.lateUpdates = lateUpdates; this.services = services; }
        public void ManualUpdate() // 0x1e39ba0.
        {
            if (!(state ?? throw new NullReferenceException()).IsActive) return;
            if ((manager ?? throw new NullReferenceException()).ShouldExitGame) Deactivate();
            else RequiredServices.ManualUpdateGameUnits();
        }
        private void Deactivate() // 0x1e39488.
        {
            (lateUpdates ?? throw new NullReferenceException()).ClearCallbacks();
            state.IsActive = false;
            RequiredServices.RemoveOrientationChangeListener(this);
            OnDeactivate?.Invoke();
            ResetScene(true);
            var touch = RequiredServices.GetUiTouchEnabler() ?? throw new NullReferenceException();
            touch.EnablePinch(this); touch.EnableVerticalScroll(this); touch.EnableHorizontalScroll(this);
            if (RequiredServices.IsLowEnd) RequiredItemFactory.ClearPools();
            if (RequiredServices.IsEarlySideProgressionLevel && RequiredServices.IsSideProgressionLevelPlayed())
            {
                RequiredServices.ReloadLevel();
                (RequiredServices.GetSceneManager() ?? throw new NullReferenceException()).LoadGameScene();
            }
            else (RequiredServices.GetSceneManager() ?? throw new NullReferenceException()).LoadHomeScene();
            RequiredServices.SendGetMessages(null);
            state.Controller = null;
        }
        public void ResetScene(bool gameExit) // 0x1e398b8: borrowed array, live slots, ascending order.
        {
            var units = RequiredServices.GetGameUnits() ?? throw new NullReferenceException();
            for (int i = 0; i < units.Length; i++) units[i]?.Clear(gameExit);
            KillAllAnimations();
        }
        public void KillAllAnimations() // 0x1e39a14.
        {
            RequiredServices.KillAllTweens(false);
            RequiredServices.StopAllManualTweenCoroutines();
            RequiredServices.KillGameCoroutines();
            RequiredItemFactory.RecycleAllSpawned();
            RequiredServices.StopAllSounds();
            RequiredServices.ResetCurrentCameraOrientationLock();
            RequiredServices.KillAllAnimatorUpdates();
        }
        private IOriginalGameContextDeactivationServices RequiredServices => services ?? throw new NullReferenceException();
        private IOriginalGameContextItemCleanup RequiredItemFactory => RequiredServices.GetItemFactory() ?? throw new NullReferenceException();
    }
}
