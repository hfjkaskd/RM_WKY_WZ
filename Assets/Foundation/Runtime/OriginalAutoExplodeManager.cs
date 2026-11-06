using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalAutoExplodeMatchServices
    {
        IReadOnlyList<OriginalResolvedMatchGroup> FindAllMatches();
        bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group, OriginalExplodeData data, bool isFromAutoMatch);
    }
    // AutoExplodeManager 0x1c93898..0x1c93b14. Binding follows the current
    // level owners; no play state or session data is manufactured here.
    public sealed class OriginalAutoExplodeManager
    {
        private readonly Func<int> frameCount;
        private OriginalGameStateManager state;
        private IOriginalAutoExplodeMatchServices matches;
        internal float hasNoOperationSinceFrame;
        internal bool enabled = true;
        public bool IsThereAWaitingMatch { get; private set; }
        public OriginalAutoExplodeManager(Func<int> frameCount)
        { this.frameCount = frameCount ?? throw new ArgumentNullException(nameof(frameCount)); }
        public void Bind(OriginalGameStateManager state, IOriginalAutoExplodeMatchServices matches)
        { this.state = state; this.matches = matches; }
        public void Disable() { enabled = false; }
        public void Clear(bool gameExit)
        { hasNoOperationSinceFrame = 0f; IsThereAWaitingMatch = false; enabled = true; }
        public void ManualUpdate() // 0x1c938a0.
        {
            if (!enabled) return;
            if ((state ?? throw new NullReferenceException()).HasOperation(6))
            { hasNoOperationSinceFrame = -1f; return; }
            IsThereAWaitingMatch = false;
            if ((state ?? throw new NullReferenceException()).HasAnyOperation()) hasNoOperationSinceFrame = -1f;
            else
            {
                float previous = hasNoOperationSinceFrame;
                int frame = frameCount();
                // Native b.pl treats NaN like >=0; b.gt below rejects only >10.
                if (previous < 0f) hasNoOperationSinceFrame = frame;
                else if ((float)((float)frame - hasNoOperationSinceFrame) > 10f) return;
            }
            DestroyAllMatches();
        }
        private void DestroyAllMatches() // 0x1c9394c.
        {
            var groups = (matches ?? throw new NullReferenceException()).FindAllMatches();
            if (groups == null) throw new NullReferenceException();
            for (int i = 0; i < groups.Count; i++)
            {
                var data = OriginalExploder.Next(4);
                var group = groups[i].CaptureSnapshot();
                bool exploded = (matches ?? throw new NullReferenceException()).ExplodeMatchGroup(group, data, true);
                IsThereAWaitingMatch = IsThereAWaitingMatch || !exploded;
            }
            if (IsThereAWaitingMatch) hasNoOperationSinceFrame = -1f;
        }
    }
    public sealed class OriginalBoardAutoExplodeMatchServices : IOriginalAutoExplodeMatchServices
    {
        private readonly OriginalMatchManager matches;
        private readonly OriginalMatchGroupExplosion explosion;
        public OriginalBoardAutoExplodeMatchServices(OriginalMatchManager matches, OriginalMatchGroupExplosion explosion)
        {
            this.matches = matches ?? throw new ArgumentNullException(nameof(matches));
            this.explosion = explosion ?? throw new ArgumentNullException(nameof(explosion));
        }
        public IReadOnlyList<OriginalResolvedMatchGroup> FindAllMatches() => matches.FindAllMatches();
        public bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group, OriginalExplodeData data, bool automatic) =>
            explosion.ExplodeMatchGroup(group, data, automatic);
    }
}
