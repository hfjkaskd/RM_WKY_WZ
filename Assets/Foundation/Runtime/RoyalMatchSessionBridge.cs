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

        internal RoyalMatchLevelResult(int level, RoyalMatchLevelOutcome outcome, int totalTargets, int remainingTargets, int remainingMoves)
        {
            Level = level;
            Outcome = outcome;
            TotalTargets = totalTargets;
            RemainingTargets = remainingTargets;
            RemainingMoves = remainingMoves;
        }
    }

    public static class RoyalMatchSessionBridge
    {
        public static IRoyalMatchSessionBridge Current { get; set; }
        internal static FoundationController ActiveController { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Current = null;
            ActiveController = null;
        }
    }
}
