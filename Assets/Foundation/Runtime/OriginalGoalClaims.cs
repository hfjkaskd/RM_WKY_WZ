using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Required counting portion of native IGoalClaimer (slots 0,1,2,4,7..10).
    public interface IOriginalGoalClaimer
    {
        bool IsInCurrentBoard();
        bool WantsGoal(int goalType);
        bool HasIncomingGoal(int goalType);
        bool CanRestoreGoal(int goalType);
        int GetOutstandingTargetForGoal(int goalType);
        void DecrementTarget(int goalType);
        void IncrementTarget(int goalType);
        void ItemReached(int goalType);
    }

    // GoalClaimManager registration and ordered queries. Trail spawning and
    // completion scoring remain separate required presentation services.
    public sealed partial class OriginalGoalClaims
    {
        private readonly List<IOriginalGoalClaimer> claimers = new List<IOriginalGoalClaimer>();
        public bool HasClaimers => claimers.Count > 0; // 0x2ee7580.
        public void Register(IOriginalGoalClaimer claimer) // 0x2ee75d0.
        {
            if (!claimers.Contains(claimer)) claimers.Add(claimer);
        }
        public void Unregister(IOriginalGoalClaimer claimer) => claimers.Remove(claimer); // 0x2ee76b8.
        public IOriginalGoalClaimer GetClaimerWantingGoal(int goalType) // 0x2ee7150.
        {
            for (int i = 0; i < claimers.Count; i++)
            {
                var claimer = claimers[i];
                if (claimer.IsInCurrentBoard() && claimer.WantsGoal(goalType)) return claimer;
            }
            return null;
        }
        public IOriginalGoalClaimer GetClaimerAwaitingGoal(int goalType) // 0x2ee7b18.
        {
            for (int i = 0; i < claimers.Count; i++)
            {
                var claimer = claimers[i];
                if (claimer.IsInCurrentBoard() && claimer.HasIncomingGoal(goalType)) return claimer;
            }
            return null;
        }
        public IOriginalGoalClaimer GetClaimerToRestoreGoal(int goalType) // 0x2ee79a0.
        {
            for (int i = 0; i < claimers.Count; i++)
            {
                var claimer = claimers[i];
                if (claimer.IsInCurrentBoard() && claimer.CanRestoreGoal(goalType)) return claimer;
            }
            return null;
        }
        public int GetTotalOutstandingForGoal(int goalType) // 0x2ee7c90.
        {
            int count = 0;
            for (int i = 0; i < claimers.Count; i++)
            {
                var claimer = claimers[i];
                if (claimer.IsInCurrentBoard()) count = unchecked(count + claimer.GetOutstandingTargetForGoal(goalType));
            }
            return count;
        }
    }
}
