using System;
using System.Collections;

namespace Whitebox.Foundation
{
    // PropellerDefaultStrategy's normal and lightball-created single-target paths.
    public sealed class OriginalDefaultPropellerStrategy : IOriginalGuidedExploderStrategy
    {
        private readonly IOriginalPropellerRuntimeServices services;
        private readonly bool createdByLightball;
        private static readonly int[] touching = { 1, 3, 5, 7 };
        public OriginalDefaultPropellerStrategy(IOriginalPropellerRuntimeServices services, bool createdByLightball)
        { this.services = services ?? throw new ArgumentNullException(nameof(services)); this.createdByLightball = createdByLightball; }
        public bool ExplodeNeighbors(OriginalGridCell cell, int trigger, OriginalGuidedExploderBoardItem model) // 0x1f3bf00.
        {
            cell.StaticTouch.ExplodeTopMostBelowItem(OriginalExploder.Next(8));
            if (trigger != 15) services.StartGameplayCoroutine(ExplodeNeighbors(cell, OriginalExploder.Next(9, new OriginalCellPoint(cell.X, cell.Y))));
            return true;
        }
        private IEnumerator ExplodeNeighbors(OriginalGridCell center, OriginalExplodeData data) // 0x1f3c580.
        {
            var estimated = new IOriginalExplodeTarget[touching.Length];
            for (int i = 0; i < touching.Length; i++)
            {
                OriginalGridCell cell = center.Neighbors.Get(touching[i]);
                if (cell == null) continue;
                IOriginalExplodeTarget target = services.ReservePropellerNeighbor(cell, data);
                if (target == null) continue;
                if (target.CanAddPropellerAroundExploder()) estimated[i] = target;
                else target.RemoveIncomingExploder(data);
            }
            yield return services.Wait(.05f);
            for (int i = 0; i < touching.Length; i++)
            {
                OriginalGridCell cell = center.Neighbors.Get(touching[i]);
                if (cell == null) continue;
                cell.ExplodeCurrentItem(data);
                cell.FreezeFallForDuration(.25f);
                estimated[i]?.RemoveIncomingExploder(data);
            }
            services.CollectPropellerMadness(data, center);
        }
        public OriginalExplodeData NextExploder(OriginalGridCell cell) =>
            OriginalExploder.Next(createdByLightball ? 30 : 8, new OriginalCellPoint(cell.X, cell.Y)); // 0x1f3c188.
        public void OnTargetReached(OriginalGridCell cell, OriginalExplodeData data) { if (cell != null) cell.ExplodeCurrentItem(data); } // 0x1f3c268.
        public IOriginalGuidedExploderPilot GetNewPilot() => new OriginalDefaultPropellerPilot(services); // 0x1f3c304.
        public void OnTargetFound() { } // 0x1f3c368.
        public void OnTargetCancelled() { } // 0x1f3c36c.
        public void ForceExplode() { } // 0x1f3c370.
        public bool IsTargetAmountReached() => true; // 0x1f3c574.
        public bool IsTntOrRocketCombo => false;
    }
}
