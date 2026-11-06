using System;
using System.Diagnostics;

namespace Whitebox.Foundation
{
    public sealed class OriginalGameStartClock : IOriginalGameStartClock
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;
        public bool IsRunning => stopwatch.IsRunning;
        public void Restart() => stopwatch.Restart();
        public void Stop() => stopwatch.Stop();
    }
    // The captured session remains the same object even if a callback replaces
    // CurrentUser.Session.ActiveLevel later in GameManager.StartGame.
    public sealed class OriginalActiveLevelGameStart : IOriginalGameStartSession
    {
        private readonly OriginalActiveLevelData level;
        private readonly IOriginalActiveLevelStartContext context;
        public OriginalActiveLevelGameStart(OriginalActiveLevelData level, IOriginalActiveLevelStartContext context)
        { this.level = level; this.context = context; }
        private OriginalActiveLevelData Level => level ?? throw new NullReferenceException();
        public void Start() => Level.Start(context);
        public bool IsSideProgressionLevel => Level.IsSideProgressionLevel;
        public bool IsStory => Level.StoryLevel < 0;
        public bool IsAdmin => Level.IsAdmin;
        public int Number => Level.Number;
    }
    public sealed class OriginalGameStartLevelContext : IOriginalGameStartLevelContext
    {
        private readonly OriginalLevelContext level;
        public OriginalGameStartLevelContext(OriginalLevelContext level) { this.level = level; }
        public void Activate(bool forNewLevel) => (level ?? throw new NullReferenceException()).Activate(forNewLevel);
        public void RemoveLateUnits() => (level ?? throw new NullReferenceException()).RemoveLateUnits();
    }
}
