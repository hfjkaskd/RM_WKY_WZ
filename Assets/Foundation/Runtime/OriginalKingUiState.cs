using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    // KingUi.Start/MoveChanged/EnableHappyAnimDelayed, 0x18c8e24,
    // 0x18c96b8 and 0x18ca830. Ordinary and bonus boards within levels 1-100.
    internal sealed class OriginalKingUiState
    {
        private readonly float happyDelay;
        private readonly Action<bool> startIdle;
        private OriginalMoveManager moves;
        private IOriginalManualCoroutinePlayer coroutines;
        private bool hasStarted;
        public bool ImprovementEnabled { get; private set; }
        public bool ShouldBeHappy { get; private set; } = true;
        public bool StopAnimationsCycle { get; set; }
        public int MovesLeft { get; private set; }

        public OriginalKingUiState(float happyDelay, Action<bool> startIdle)
        {
            this.happyDelay = happyDelay;
            this.startIdle = startIdle ?? throw new ArgumentNullException(nameof(startIdle));
        }

        public void Bind(OriginalMoveManager source, IOriginalManualCoroutinePlayer player, bool improvementEnabled)
        {
            Release();
            moves = source ?? throw new ArgumentNullException(nameof(source));
            coroutines = player ?? throw new ArgumentNullException(nameof(player));
            ImprovementEnabled = improvementEnabled;
            moves.OnMoveChanged += OnMoveChanged;
            if (!hasStarted)
            {
                hasStarted = true;
                if (improvementEnabled) MovesLeft = moves.LeftMoves; // KingUi.Start snapshots the current MoveManager.
            }
            else OnMoveChanged(moves.LeftMoves, 1); // The next active level dispatches its initial budget.
        }

        public void Release()
        {
            if (moves != null) moves.OnMoveChanged -= OnMoveChanged;
            moves = null;
            coroutines = null;
        }

        private void OnMoveChanged(int left, int changeType)
        {
            if (left == 0 || changeType == 4) return;
            if (ImprovementEnabled && (changeType == 1 || changeType == 3)) ShouldBeHappy = true;
            if (MovesLeft == 0 ? StopAnimationsCycle : MovesLeft < left) startIdle(MovesLeft == 0);
            MovesLeft = left;
            // The non-improved first-100 branch is a bonus board; its legacy
            // KingUi.MoveChanged exits before ordinary low-move worry.
        }

        public bool TryBeHappy(bool useCooldown)
        {
            if (!useCooldown) return true;
            if (!ShouldBeHappy) return false;
            ShouldBeHappy = false;
            // Keep separate outstanding waits, as the original does. A budget
            // reset can admit another happy animation before an older wait ends.
            coroutines.ManualStartCoroutine(EnableHappyAnimDelayed());
            return true;
        }

        public void BeginSad() // KingUi.BeSad 0x18ca2b0 resets the cached move count.
        {
            MovesLeft = 0;
            StopAnimationsCycle = true;
        }

        private IEnumerator EnableHappyAnimDelayed()
        {
            yield return new WaitForSeconds(happyDelay);
            ShouldBeHappy = true;
        }
    }
}
