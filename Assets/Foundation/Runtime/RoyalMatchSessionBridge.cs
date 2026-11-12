using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IRoyalMatchSessionBridge
    {
        bool TryGetCurrentLevel(out int level);
        bool BlocksGameplayInput { get; }
        void OnAttemptReady(int level);
        void OnAttemptEnded(RoyalMatchLevelResult result);
    }

    public interface IRoyalMatchReviveBridge
    {
        bool TryOfferRevive(RoyalMatchLevelResult result, Action<bool> complete);
    }

    public interface IRoyalMatchSynthesisBridge
    {
        void OnSynthesisResolved(int remainingTargets, Vector3 worldPosition, Camera worldCamera);
    }

    public interface IRoyalMatchTutorialBridge
    {
        void OnOpeningTutorialCompleted();
    }

    public interface IRoyalMatchBoardPointerBlocker
    {
        bool BlocksBoardPointer(Vector2 screenPosition);
    }

    public enum RoyalMatchLevelOutcome
    {
        Won,
        OutOfMoves,
        NoPossibleMoves,
        Quit,
    }

    public readonly struct RoyalMatchLevelResult
    {
        public int Level { get; }
        public RoyalMatchLevelOutcome Outcome { get; }
        public int TotalTargets { get; }
        public int RemainingTargets { get; }
        public int RemainingMoves { get; }
        public int CoinsEarned { get; }

        internal RoyalMatchLevelResult(int level, RoyalMatchLevelOutcome outcome, int totalTargets, int remainingTargets, int remainingMoves, int coinsEarned)
        {
            Level = level;
            Outcome = outcome;
            TotalTargets = totalTargets;
            RemainingTargets = remainingTargets;
            RemainingMoves = remainingMoves;
            CoinsEarned = Math.Max(0, coinsEarned);
        }
    }

    public static class RoyalMatchSessionBridge
    {
        public static IRoyalMatchSessionBridge Current { get; set; }
        public static IRoyalMatchBoardPointerBlocker BoardPointerBlocker { get; set; }
        internal static FoundationController ActiveController { get; set; }

        public static int BoosterCoinBalance => ActiveController != null
            ? ActiveController.Progress?.BoosterCoins ?? 0
            : 0;

        public static int AddBoosterCoins(int amount) => ActiveController != null
            ? ActiveController.Progress?.AddBoosterCoins(amount) ?? 0
            : 0;

        public static bool TrySpendBoosterCoins(int amount) => ActiveController != null &&
            ActiveController.Progress != null && ActiveController.Progress.TrySpendBoosterCoins(amount);

        public static bool TryGetActiveBoardScreenRect(out Rect screenRect)
        {
            var controller = ActiveController;
            if (controller != null)
                return controller.TryGetBoardScreenRect(out screenRect);

            screenRect = default;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Current = null;
            BoardPointerBlocker = null;
            ActiveController = null;
        }
    }
}
