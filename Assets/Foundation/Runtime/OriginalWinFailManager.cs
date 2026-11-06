using System;

namespace Whitebox.Foundation
{
    public interface IOriginalWinFailServices
    {
        bool IsBonus { get; }
        bool ShouldSpeedUpLevelEnd { get; }
        bool CanUseMoveTokenInLevel { get; }
        float LevelTime { get; }
        void OnWin();
        void StartWinFlow();
        void TrySendRemainingMoves();
        void StartExtraMoveFlow();
        void StartEgoFlow();
        void StartNoPossibleMoveFlow();
        void StartFailFlow();
        void SpendMovesOnLevelQuit();
        void ReportOperationTimeout(int warningCount, int state);
        void StopFadeOutSoundSourceCoroutine();
    }

    // Native WinFailManager decision loop 0x1edd990. Presentation, session and
    // FlowManager actions are required strongly typed composition boundaries.
    public sealed class OriginalWinFailManager
    {
        private readonly OriginalGameStateManager state;
        private readonly OriginalGoalManager goals;
        private readonly OriginalMoveManager moves;
        private readonly OriginalAutoExplodeManager auto;
        private readonly IOriginalWinFailServices services;
        private bool isOutOfMoves, isAllGoalsReached, isEnabled = true, didStartWinFlow;
        private float lastOperationTime, lastWinBlockingOperationTime, hasOperationSince;
        private int warningCount;
        public bool IsEnabled => isEnabled;
        public bool DidStartWinFlow => didStartWinFlow;
        public bool IsOutOfMoves => isOutOfMoves;
        public OriginalWinFailManager(OriginalGameStateManager state, OriginalGoalManager goals,
            OriginalMoveManager moves, OriginalAutoExplodeManager auto, IOriginalWinFailServices services)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.goals = goals ?? throw new ArgumentNullException(nameof(goals));
            this.moves = moves ?? throw new ArgumentNullException(nameof(moves));
            this.auto = auto ?? throw new ArgumentNullException(nameof(auto));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public void Bind() // 0x1edd1d8 subscriptions.
        { goals.OnGoalUpdated += OnGoalUpdated; moves.OnMoveChanged += MoveChanged; }
        public void Release() { goals.OnGoalUpdated -= OnGoalUpdated; moves.OnMoveChanged -= MoveChanged; }
        private void MoveChanged(int left, int changeType) // 0x1edd6a0.
        {
            if (left > 0) return;
            if (services.IsBonus) Win(); else isOutOfMoves = true;
        }
        private void Win() // 0x1edd734.
        { state.SetStateToWin(); services.OnWin(); }
        private void OnGoalUpdated(int type, int count) // 0x1edd92c: model event, not UI arrival.
        {
            if (!state.IsPlaying()) return;
            isAllGoalsReached = goals.IsAllGoalsReached();
            if (isAllGoalsReached && !state.IsWin()) Win();
        }
        public void OnNoPossibleMove() { state.SetStateToNoPossibleMove(); }
        public bool IsThereAWinBlockingOperation() => state.HasOperation(21) || state.HasOperation(5); // 0x1eddeac.
        public void ManualUpdate()
        {
            if (!isEnabled || state.IsNoGame()) return;
            if (services.ShouldSpeedUpLevelEnd)
            {
                if (IsThereAWinBlockingOperation()) lastWinBlockingOperationTime = services.LevelTime;
                // Native b.lt also rejects unordered values here.
                if (services.LevelTime - lastWinBlockingOperationTime >= .2f && state.IsWin() &&
                    !didStartWinFlow && goals.AreAllGoalChecksShown())
                { didStartWinFlow = true; services.StartWinFlow(); }
            }
            if (state.HasAnyOperation())
            {
                lastOperationTime = services.LevelTime;
                if (state.IsPlaying()) return;
                float previous = hasOperationSince;
                float now = services.LevelTime;
                if (previous <= 0f) { hasOperationSince = now; return; }
                if (!(now - hasOperationSince > 10f)) return;
                if (state.IsWin())
                { state.Operations.OperationReset(0); state.Operations.OperationReset(1); return; }
                warningCount = unchecked(warningCount + 1);
                services.ReportOperationTimeout(warningCount, state.GetGameState());
                hasOperationSince = services.LevelTime;
                return;
            }
            hasOperationSince = -1f;
            // Native b.mi: unordered values continue through this second gate.
            if (services.LevelTime - lastOperationTime < .2f || auto.IsThereAWaitingMatch) return;
            if (state.IsWin())
            {
                if (!services.ShouldSpeedUpLevelEnd) { isEnabled = false; services.StartWinFlow(); }
                else if (didStartWinFlow) { isEnabled = false; services.TrySendRemainingMoves(); }
                return;
            }
            if (state.IsQuit())
            {
                isEnabled = false;
                if (services.CanUseMoveTokenInLevel) services.SpendMovesOnLevelQuit();
                return;
            }
            if (isOutOfMoves)
            {
                if (services.CanUseMoveTokenInLevel) { isOutOfMoves = false; services.StartExtraMoveFlow(); }
                else state.SetStateToEgo();
            }
            if (state.IsEgo()) { isEnabled = false; services.StartEgoFlow(); }
            if (state.IsNoPossibleMove()) { isEnabled = false; services.StartNoPossibleMoveFlow(); }
            if (state.IsFail()) { isEnabled = false; services.StartFailFlow(); }
        }
        public void Clear(bool gameExit) // 0x1edec84.
        {
            isOutOfMoves = false; isAllGoalsReached = false; isEnabled = true;
            lastOperationTime = lastWinBlockingOperationTime = hasOperationSince = 0f;
            warningCount = 0; didStartWinFlow = false; services.StopFadeOutSoundSourceCoroutine();
        }
    }
}
