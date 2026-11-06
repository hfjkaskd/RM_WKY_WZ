using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMoveGameState
    {
        int GetGameState();
        bool HasOperation(int operation);
    }
    public interface IOriginalMoveTouchEvents
    {
        event Action<OriginalGridCell> OnTap;
        event Action<OriginalGridCell, OriginalGridCell, bool> OnSwap;
        event Action<int> OnMoveCompleted;
    }
    // MoveManager spend/count/time/event portion. Fresh/saved move budgets are
    // supplied by the original active-level provider, not read from raw config.
    public sealed class OriginalMoveManager
    {
        private IOriginalMoveGameState state;
        private IOriginalMoveTouchEvents touches;
        public int LeftMoves { get; private set; }
        public int MadeMoves { get; private set; }
        public int MaxSeconds { get; private set; }
        public int Seconds { get; private set; }
        public event Action<int, int> OnMoveChanged;
        public event Action<int> OnSecondsChanged, OnMoveCompleted;
        public void Bind(IOriginalMoveGameState state, IOriginalMoveTouchEvents touches) // 0x1eda228.
        {
            this.state = state; this.touches = touches;
            var source = this.touches;
            if (source == null) throw new NullReferenceException();
            source.OnTap += OnTap;
            source = this.touches;
            if (source == null) throw new NullReferenceException();
            source.OnSwap += OnSwap;
            source = this.touches;
            if (source == null) throw new NullReferenceException();
            source.OnMoveCompleted += CompleteMove;
        }
        public void IncreaseMove(int amount, int changeType) // 0x1eda060.
        {
            var changed = OnMoveChanged;
            LeftMoves = unchecked(LeftMoves + amount);
            changed?.Invoke(LeftMoves, changeType);
        }
        public void TriggerMoveChanged(int changeType) => OnMoveChanged?.Invoke(LeftMoves, changeType); // 0x1eda088.
        private void MakeMove() // 0x1eda0ac.
        {
            var changed = OnMoveChanged;
            LeftMoves = unchecked(LeftMoves - 1); MadeMoves = unchecked(MadeMoves + 1);
            changed?.Invoke(LeftMoves, 0);
        }
        public bool HasMoves() => LeftMoves > 0; // 0x1eda0e4.
        public void SetMaxMoves(int moves) // 0x1eda0f4.
        {
            if (moves < 0) { SetMaxSeconds(unchecked(-moves)); moves = 100; }
            var changed = OnMoveChanged;
            LeftMoves = moves; changed?.Invoke(LeftMoves, 1);
        }
        public void SetMovesForStart(int maxMoves, int leftMoves, int madeMoves) // 0x1eda130.
        {
            if (maxMoves < 0) { SetMaxSeconds(unchecked(-maxMoves)); leftMoves = 100; }
            var changed = OnMoveChanged;
            LeftMoves = leftMoves; MadeMoves = madeMoves; changed?.Invoke(LeftMoves, 1);
        }
        public bool CanUserMakeMove() // 0x1eda16c.
        {
            if (state == null) throw new NullReferenceException();
            bool enabled = state.GetGameState() == 1 && !state.HasOperation(6);
            int budget = MaxSeconds > 0 ? Seconds : LeftMoves;
            return enabled & budget > 0;
        }
        public void SetMaxSeconds(int seconds) { MaxSeconds = seconds; Seconds = seconds; } // 0x1eda128.
        public void DecreaseSecond() // 0x1eda200.
        {
            var changed = OnSecondsChanged;
            Seconds = unchecked(Seconds - 1); changed?.Invoke(Seconds);
        }
        private void OnTap(OriginalGridCell cell) => MakeMove(); // 0x1eda8dc / OnTap 0x1eda668.
        private void OnSwap(OriginalGridCell from, OriginalGridCell to, bool valid) // 0x1eda914 / 0x1eda62c.
        { if (valid) MakeMove(); }
        public void CompleteMoveByType(int type) => OnMoveCompleted?.Invoke(type); // 0x1eda6a0.
        private void CompleteMove(int type) => OnMoveCompleted?.Invoke(type); // 0x1eda6bc.
        public void Clear(bool gameExit) // 0x1eda5e4. Native touch subscriptions are not removed here.
        {
            LeftMoves = 0; MadeMoves = 0; MaxSeconds = 0; Seconds = 0;
            if (!gameExit) return;
            OnMoveCompleted = null; OnMoveChanged = null; OnSecondsChanged = null;
        }
    }
}
