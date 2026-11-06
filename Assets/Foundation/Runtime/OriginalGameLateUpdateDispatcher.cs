using System;

namespace Whitebox.Foundation
{
    public interface IOriginalGameLateUpdateRegistration { void RegisterToLateUpdate(Action callback); }
    // GameContext RegisterToLateUpdate / ManualLateUpdate, 0x1e39d14/0x1e39d68.
    // The application's context owns this dispatcher. Unity callback transport
    // does not gate it on LevelContext.IsActive, just as the original controller.
    public sealed class OriginalGameLateUpdateDispatcher : IOriginalGameLateUpdateRegistration
    {
        private event Action callbacks;
        public void RegisterToLateUpdate(Action callback) { callbacks += callback; }
        public void UnregisterFromLateUpdate(Action callback) { callbacks -= callback; }
        public void ManualLateUpdate() { callbacks?.Invoke(); }
        // The original GameContext.Deactivate clears this delegate before
        // publishing the inactive state. Full Deactivate ordering is separate.
        public void ClearCallbacks() { callbacks = null; }
    }
}
