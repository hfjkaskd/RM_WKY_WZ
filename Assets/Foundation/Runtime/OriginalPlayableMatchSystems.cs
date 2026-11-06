using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard
    {
        // First-board composition has no optional collection mechanics or
        // match-color goals. Their native predicates prevent these calls.
        public IOriginalMatchCollectManager GetCurtainForMatchType(int type) => throw MissingMatchMechanic();
        public IOriginalMatchCollectManager GetDrillForMatchType(int type) => throw MissingMatchMechanic();
        public IOriginalMatchCollectManager GetCryptexForMatchType(int type) => throw MissingMatchMechanic();
        public void DecrementPurpleOnBoard() => throw MissingMatchMechanic();
        public void TryDamageNeighborIceBorders(OriginalMatchBoardItem item) => throw MissingMatchMechanic();
        public void CollectToGoal(OriginalMatchBoardItem item) => throw MissingMatchMechanic();
        public void CollectToManager(OriginalMatchBoardItem item, IOriginalMatchCollectManager manager) => throw MissingMatchMechanic();
        private static Exception MissingMatchMechanic() => new NotSupportedException("This first-board composition has no match collection mechanic.");

        public void HandleMadness(OriginalMatchBoardItem item, OriginalExplodeData data) // 0x21a6834.
        {
            if (data.Trigger == 2 || data.Trigger == 4) return;
            // Lightball-combo and guided-exploder static overrides are not
            // active in this composition. Native ordinary trigger mask follows.
            uint trigger = unchecked((uint)data.Trigger);
            if (trigger <= 29 && ((0x2000c200u >> (int)trigger) & 1) != 0)
            {
                Session.IncrementMadnessMatchItemCount(item.MatchType, data.Id);
                return;
            }
            var p = ((Component)item.SwapView).transform.position;
            Session.TryCollectMadness(item.MatchType, new OriginalSwapPoint(p.x, p.y, p.z), 1, 5, null);
        }
    }
}
