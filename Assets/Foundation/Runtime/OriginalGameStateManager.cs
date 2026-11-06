using System;

namespace Whitebox.Foundation
{
    public interface IOriginalGameStateServices
    {
        void StartProfilingLevel();
        void ClearFlow(bool gameExit);
    }
    // Native state/operation portion. Application/HomeContext subscription,
    // profiling and FlowManager are required composition boundaries.
    public sealed class OriginalGameStateManager : IOriginalMoveGameState
    {
        private readonly Func<int> frameCount;
        private readonly Func<IOriginalGameStateServices> services;
        private int gameState, specialOperations, shifterOperations;
        private bool isShifting, shouldShift;
        public OriginalGameOperations Operations { get; }
        public OriginalGameStateManager(Func<int> frameCount, Func<IOriginalGameStateServices> services)
        {
            this.frameCount = frameCount ?? throw new ArgumentNullException(nameof(frameCount));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            Operations = new OriginalGameOperations(frameCount);
        }
        public int GetGameState() => gameState; // 0x1ee5868.
        public bool IsNoGame() => gameState == 0;
        public bool IsPlaying() => gameState == 1; // 0x1eda1d8.
        public bool IsWin() => gameState == 2;
        public bool IsEgo() => gameState == 3;
        public bool IsFail() => gameState == 4;
        public bool IsQuit() => gameState == 5;
        public bool IsNoPossibleMove() => gameState == 6;
        // Native setters also emit diagnostic Log.Debug, omitted here.
        public void SetStateToNoGame() { gameState = 0; } // 0x1ee5690.
        public void SetStateToPlay() { gameState = 1; } // 0x1ee57bc.
        public void SetStateToWin() { gameState = 2; } // 0x1edbcdc.
        public void SetStateToEgo() { gameState = 3; } // 0x1ede364.
        public void SetStateToFail() { gameState = 4; } // 0x1edb0bc.
        public void SetStateToNoPossibleMove() { gameState = 6; } // 0x1edd5f4.
        public void SetStateToQuit() // 0x1ee5870.
        {
            var context = services(); if (context == null) throw new NullReferenceException();
            context.ClearFlow(true); gameState = 5;
        }
        public void StartLevel() // 0x1ee5738. State changes before required profiling lookup.
        {
            SetStateToPlay();
            var context = services(); if (context == null) throw new NullReferenceException();
            context.StartProfilingLevel();
        }
        public void OnHomeContextActivated() // 0x1ee5974.
        { if (gameState != 0) Clear(true); }
        public void Clear(bool gameExit) // 0x1ee5648. Shifting flags are retained.
        {
            Operations.Clear(); specialOperations = 0; shifterOperations = 0;
            if (gameExit) SetStateToNoGame();
        }
        public bool HasOperation(int operation) => Operations.Container.Has(operation); // 0x1eda1e8.
        public bool HasAnyOperation() => Operations.Container.HasAny(); // 0x1edb430.
        public void StartSpecialOperation() { specialOperations = unchecked(specialOperations + 1); }
        public void FinishSpecialOperation() { specialOperations = unchecked(specialOperations - 1); }
        public int GetSpecialOperationCount() => specialOperations;
        public void StartShifterSpecialOperation() { shifterOperations = unchecked(shifterOperations + 1); }
        public void FinishShifterSpecialOperation() { shifterOperations = unchecked(shifterOperations - 1); }
        public int GetSpecialOperationCountForShifter() => unchecked(specialOperations + shifterOperations); // 0x1ee5b08.
        public void SetShiftingStatus(bool value) { isShifting = value; }
        public bool AreCellsBeingShifted() => isShifting;
        public void SetShouldShift(bool value) { shouldShift = value; }
        public bool ShouldTryToShiftCells() => shouldShift;
        public bool IsGameStable() // 0x1ee5b3c.
        {
            if (Operations.Container.HasAny() || Operations.LastOperationFrame < 0) return false;
            return unchecked(frameCount() - Operations.LastOperationFrame) > 1;
        }
    }
}
