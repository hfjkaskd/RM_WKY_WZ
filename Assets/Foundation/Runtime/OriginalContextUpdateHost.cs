using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Prefab-owned Unity callback transport for a supplied live context. The
    // application/session must provide its real units before binding; initial
    // board preview alone does not create a playing context or no-op units.
    public sealed class OriginalContextUpdateHost : MonoBehaviour
    {
        public OriginalContextContainer Context { get; private set; }
        public int CompletedUpdateCount { get; private set; }
        public int CompletedLateUpdateCount { get; private set; }
        public OriginalGameLateUpdateDispatcher LateUpdates { get; } = new OriginalGameLateUpdateDispatcher();
        private Action manualTweenUpdate;
        private Action manualContextUpdate;
        private OriginalGameTouchInput gameTouchInput;
        private OriginalGameContextDeactivation gameContextExitOwner;
        public OriginalCoroutinePlayer ApplicationCoroutines { get; private set; }
        private void Awake(){ApplicationCoroutines=new OriginalCoroutinePlayer(this);}
        private void OnDestroy(){ApplicationCoroutines.KillAll();}
        public OriginalDeltaTimeManager CreateDeltaTimeManager() => new OriginalDeltaTimeManager(LateUpdates, OriginalUnityTimeSource.Instance);
        internal void BindGameTouchInput(OriginalGameTouchInput input) => gameTouchInput = input;
        public void BindContext(OriginalContextContainer context, Action manualTweens = null)
        { Context = context ?? throw new ArgumentNullException(nameof(context)); manualTweenUpdate = manualTweens; manualContextUpdate = UpdateGameContext; }
        public void BindGameContextExit(OriginalGameContextDeactivation owner)
        {
            if (Context == null) throw new InvalidOperationException("Bind live context units before their GameContext dispatcher.");
            gameContextExitOwner = owner ?? throw new ArgumentNullException(nameof(owner)); manualContextUpdate = owner.ManualUpdate;
        }
        public void ReleaseGameContextExit(OriginalGameContextDeactivation owner)
        {
            if (!ReferenceEquals(gameContextExitOwner, owner)) return;
            gameContextExitOwner = null; manualContextUpdate = Context == null ? null : (Action)UpdateGameContext;
        }
        public void ReleaseContext(OriginalContextContainer context)
        { if (ReferenceEquals(Context, context)) { Context = null; manualTweenUpdate = manualContextUpdate = null; gameContextExitOwner = null; } }
        private void UpdateGameContext()
        {
            var levelContext = Context;
            // GameContext.Bind 0x1e38370 registers GameTouchListener before LevelContext.
            if (gameTouchInput != null) gameTouchInput.ManualUpdate();
            levelContext.ManualUpdate();
        }
        private void Update()
        {
            var current = Context;
            if (current == null) return;
            manualTweenUpdate?.Invoke(); // Application TweenManager precedes the game-context dispatch.
            (manualContextUpdate ?? throw new NullReferenceException())();
            CompletedUpdateCount = unchecked(CompletedUpdateCount + 1);
        }
        internal void DispatchGameLateUpdate()
        {
            LateUpdates.ManualLateUpdate();
            CompletedLateUpdateCount = unchecked(CompletedLateUpdateCount + 1);
        }
    }
}
