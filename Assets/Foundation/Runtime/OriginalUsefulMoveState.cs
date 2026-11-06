using System;

namespace Whitebox.Foundation
{
    public interface IOriginalKingRelief { void TryKingRelief(); }

    // Useful/useless move portion of UserActiveLevelData. Full session loading,
    // move completion and persistence are composed separately.
    public sealed class OriginalUsefulMoveState
    {
        private readonly Func<IOriginalKingRelief> currentKing;
        private readonly IOriginalUsefulMoveCounters counters;
        private sealed class Counters : IOriginalUsefulMoveCounters
        {
            public bool IsUsefulMoveMade { get; set; }
            public int UselessMoveCount { get; set; }
            public int MaxUselessMoveStreak { get; set; }
            public int CurrentUselessMoveStreak { get; set; }
        }
        public bool IsUsefulMoveMade { get => counters.IsUsefulMoveMade; set => counters.IsUsefulMoveMade = value; }
        public int UselessMoveCount { get => counters.UselessMoveCount; set => counters.UselessMoveCount = value; }
        public int MaxUselessMoveStreak { get => counters.MaxUselessMoveStreak; set => counters.MaxUselessMoveStreak = value; }
        public int CurrentUselessMoveStreak { get => counters.CurrentUselessMoveStreak; set => counters.CurrentUselessMoveStreak = value; }
        public OriginalUsefulMoveState(Func<IOriginalKingRelief> currentKing)
            : this(currentKing, new Counters()) { }
        public OriginalUsefulMoveState(Func<IOriginalKingRelief> currentKing, IOriginalUsefulMoveCounters counters)
        {
            this.currentKing = currentKing ?? throw new ArgumentNullException(nameof(currentKing));
            this.counters = counters ?? throw new ArgumentNullException(nameof(counters));
        }
        public void UsefulMoveMade() // 0x32ba8b4.
        {
            var king = currentKing();
            if (king == null) throw new NullReferenceException();
            king.TryKingRelief();
            IsUsefulMoveMade = true;
        }
        public void ResetIsUsefulMoveMade() { IsUsefulMoveMade = false; } // 0x32ba8a4.
        public void ResetUselessMoveStreak() { CurrentUselessMoveStreak = 0; } // 0x32ba8ac.
        public void IncreaseUselessMoveCount() // 0x32ba87c.
        {
            int count = unchecked(CurrentUselessMoveStreak + 1);
            UselessMoveCount = unchecked(UselessMoveCount + 1);
            CurrentUselessMoveStreak = count;
            if (count > MaxUselessMoveStreak) MaxUselessMoveStreak = count;
        }
    }

    // Static ItemModel.UsefulMoveMade 0x2b46b54 resolves the active session on
    // every call; a missing session or King manager remains a required failure.
    public sealed class OriginalItemUsefulMoveServices : IOriginalBoxExplosionServices
    {
        private readonly Func<OriginalUsefulMoveState> currentLevel;
        public OriginalItemUsefulMoveServices(Func<OriginalUsefulMoveState> currentLevel)
        { this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel)); }
        public void UsefulMoveMade()
        {
            var level = currentLevel();
            if (level == null) throw new NullReferenceException();
            level.UsefulMoveMade();
        }
    }
}
