using System;

namespace Whitebox.Foundation
{
    // Additional native IGoalClaimer slots 3 and 12. All presentation claimers
    // used for trails must implement these alongside their counting contract.
    public interface IOriginalGoalTrailClaimer : IOriginalGoalClaimer
    {
        bool HasClaimAwaitingTrail(int goalType);
        bool TryCollectWithTrail(int goalType, float x, float y, float z);
    }

    public sealed partial class OriginalGoalClaims
    {
        public bool TryCollectWithTrail(int goalType, float x, float y, float z) // 0x2ee7710.
        {
            if (!HasClaimers) return false;
            for (int i = 0; i < claimers.Count; i++)
            {
                var claimer = claimers[i];
                if (claimer.IsInCurrentBoard() && RequireTrail(claimer).HasClaimAwaitingTrail(goalType))
                    return RequireTrail(claimer).TryCollectWithTrail(goalType, x, y, z);
            }
            var wanting = GetClaimerWantingGoal(goalType);
            return wanting != null && RequireTrail(wanting).TryCollectWithTrail(goalType, x, y, z);
        }
        private static IOriginalGoalTrailClaimer RequireTrail(IOriginalGoalClaimer claimer)
        {
            var trail = claimer as IOriginalGoalTrailClaimer;
            if (trail == null) throw new InvalidOperationException("Goal claimer trail presentation is not restored.");
            return trail;
        }
    }
}
