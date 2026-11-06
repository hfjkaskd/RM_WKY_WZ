using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalGuidedExploderStrategy
    {
        bool ExplodeNeighbors(OriginalGridCell cell, int trigger, OriginalGuidedExploderBoardItem model);
        OriginalExplodeData NextExploder(OriginalGridCell cell);
        void OnTargetFound();
        void OnTargetCancelled();
        void OnTargetReached(OriginalGridCell cell, OriginalExplodeData data);
        void ForceExplode();
        bool IsTargetAmountReached();
        bool IsTntOrRocketCombo { get; }
        IOriginalGuidedExploderPilot GetNewPilot();
    }

    public interface IOriginalPropellerRuntimeServices : IOriginalSpecialItemExplosionServices, IOriginalPropellerPilotContext
    {
        OriginalGameStateManager State { get; }
        OriginalSingleExplodeTargetFinder GuidedTargets { get; }
        OriginalAreaExplodeTargetFinder AreaTargets { get; }
        OriginalCellGrid Grid { get; }
        int GuidedBoardStartY { get; }
        int GuidedBoardEndY { get; }
        float PropellerRocketTargetDelay { get; }
        IOriginalExplodeTarget FindGuidedTarget(OriginalExplodeData data,IOriginalExplodeTarget previous);
        IOriginalExplodeTarget ReservePropellerNeighbor(OriginalGridCell cell, OriginalExplodeData data);
        void StartGameplayCoroutine(System.Collections.IEnumerator routine);
        void StartApplicationCoroutine(IEnumerator routine);
        object Wait(float seconds);
        void UsefulMoveMade();
        void ClearSinglePropellerWinCalculationData();
        void CollectPropellerMadness(OriginalExplodeData data, OriginalGridCell center);
    }

    // GuidedExploderItemModel, normal single-target branch. The formal loader
    // admits no jelly, target-change item helpers, win-condition helpers or
    // goal claimers. Combo strategies and their column-freeze path are separate.
    public abstract class OriginalGuidedExploderBoardItem : OriginalSpecialBoardItem, IOriginalGuidedExploderViewDelegate
    {
        protected IOriginalPropellerRuntimeServices guidedServices;
        protected IOriginalGuidedExploderStrategy guidedStrategy;
        protected abstract OriginalPropellerItemSpriteView GuidedView { get; }
        public OriginalGridCell LastValidCell { get; private set; }
        public IOriginalExplodeTarget TargetItem { get; private set; }
        public OriginalExplodeData TargetExplodeData { get; private set; }
        public bool IsInFlight { get; private set; }
        private Vector3 lastValidTargetPosition;
        private bool stoppedLookingForNewTargets, invalidateTarget, useCellAsTarget;
        private readonly List<OriginalGridCell> frozenCells=new List<OriginalGridCell>(14);
        public int FrozenColumnCellCount=>frozenCells.Count;
        public bool UsesFixedTargetCell => useCellAsTarget;
        public override int GetMoveType => 1; // 0x1c9dd94.

        protected OriginalGuidedExploderBoardItem(int id, int tiledId, Func<float> time, IOriginalSpecialItemStaticState statics)
            : base(id, tiledId, time, statics) { }
        protected void BindGuidedExplosion(IOriginalPropellerRuntimeServices services, IOriginalGuidedExploderStrategy strategy)
        { guidedServices = services; guidedStrategy = strategy; BindSpecialExplosion(services); }
        protected override void TryExplode(OriginalExplodeData incoming) => TryExplodeItem(incoming); // 0x1c9fb18.
        protected virtual void TryExplodeItem(OriginalExplodeData incoming) // 0x1c9fb64.
        {
            OriginalGridCell cell = CurrentCell;
            bool particles = guidedStrategy.ExplodeNeighbors(cell, incoming.Trigger, this);
            TargetExplodeData = guidedStrategy.NextExploder(cell);
            guidedServices.UsefulMoveMade();
            FindTarget(particles);
            if (CurrentCell != null) FinalExplodeCompleted(.15f);
        }
        public void TargetFound(IOriginalExplodeTarget target) // 0x1c9df10.
        {
            TargetItem = target;
            if (target == null) return;
            IsInFlight = true;
            guidedServices.State.Operations.OperationStart(3);
            guidedServices.State.StartSpecialOperation();
        }
        protected virtual void FindTarget(bool particles) // 0x1c9f554, normal trigger 8/30.
        {
            useCellAsTarget = false;
            TargetFound(guidedServices.FindGuidedTarget(TargetExplodeData,TargetItem));
            if (TargetItem == null) { ExplodeWithoutTarget(); return; }
            if (TargetExplodeData.Trigger == 13 && (TargetItem is OriginalMatchBoardItem || TargetItem is OriginalSpecialBoardItem)
                && TargetItem.CurrentCell != null && !TargetItem.HasExplodeScore(TargetExplodeData))
            {
                LastValidCell = TargetItem.CurrentCell;
                lastValidTargetPosition = LastValidCell.GetViewPosition(); useCellAsTarget = true;
            }
            guidedStrategy.OnTargetFound();
            GuidedView.MoveToTarget(particles, TargetReached);
            FreezeColumn();
        }
        private bool TargetValid() => TargetItem.IsValidExplodeTarget(OriginalExplodeTargetRules.AsGuidedType(TargetExplodeData.Trigger));
        public virtual bool IsTargetValid() // 0x1c9df7c.
        {
            if (useCellAsTarget) return true;
            if (TargetItem != null && TargetValid() && !invalidateTarget)
            { LastValidCell = TargetItem.CurrentCell; return true; }
            invalidateTarget = false;
            if (TargetItem != null && stoppedLookingForNewTargets) return true;
            return FindNewTarget();
        }
        public virtual Vector3 GetTargetPosition() // 0x1c9f26c, default strategy uses master center.
        {
            if (useCellAsTarget) return LastValidCell.GetViewPosition();
            if (TargetValid()) lastValidTargetPosition = guidedStrategy.IsTntOrRocketCombo ? TargetItem.GetViewPosition() : TargetItem.GetMasterViewCenterPosition();
            return lastValidTargetPosition;
        }
        public virtual bool FindNewTarget() // 0x1c9e1b4.
        {
            stoppedLookingForNewTargets = false;
            ReleaseFlightOperation();
            guidedStrategy.OnTargetCancelled();
            IOriginalExplodeTarget previous = TargetItem;
            if (previous != null)
            {
                guidedServices.ClearSinglePropellerWinCalculationData();
                previous.RemoveIncomingExploder(TargetExplodeData);
            }
            OriginalGridCell previousCell = LastValidCell;
            FindTarget(false);
            if (TargetItem != null && !TargetValid())
            {
                if (LastValidCell == null) { FakeTargetReached(); ExplodeWithoutTarget(); }
                stoppedLookingForNewTargets = true;
                return true;
            }
            if (ReferenceEquals(previous, TargetItem)) return false;
            if (!ReferenceEquals(previousCell, TargetItem?.CurrentCell)) GuidedView.CancelMove();
            return false;
        }
        public virtual void TargetReached() // 0x1ca067c, no active target-change helper / normal trigger.
        {
            TargetItem.RemoveIncomingExploder(TargetExplodeData);
            guidedStrategy.OnTargetReached(TargetValid() ? TargetItem.CurrentCell : LastValidCell, TargetExplodeData);
            IsInFlight = false;
            ReleaseFlightOperation();
            guidedServices.StartApplicationCoroutine(UnfreezeCellsWithDelay());
        }
        public virtual void FakeTargetReached() // 0x1c9e68c.
        {
            TargetItem.RemoveIncomingExploder(TargetExplodeData);
            IsInFlight = false;
            ReleaseFlightOperation();
            guidedStrategy.ForceExplode();
        }
        private void ReleaseFlightOperation()
        { guidedServices.State.Operations.OperationFinish(3); guidedServices.State.FinishSpecialOperation(); }
        private void ExplodeWithoutTarget() // 0x1c9e65c.
        {UnFreezeCells();GuidedView.ExplodeSelf(false);}
        private void FreezeColumn() // 0x1c9f8e0, cross-fall freeze only.
        {
            if(TargetExplodeData.Trigger!=10)return;UnFreezeCells();
            var cell=TargetItem?.CurrentCell;if(cell==null)return;
            for(int y=guidedServices.GuidedBoardStartY;y<=guidedServices.GuidedBoardEndY;y++)
            {var value=guidedServices.Grid.GetCell(cell.X,y);value.CrossFallFreeze();frozenCells.Add(value);}
        }
        private void UnFreezeCells() // 0x1c9f110.
        {
            if(TargetExplodeData.Trigger!=10)return;
            for(int i=0;i<frozenCells.Count;i++)frozenCells[i].CrossFallUnFreeze();frozenCells.Clear();
        }
        private IEnumerator UnfreezeCellsWithDelay() // Application coroutine, 0x1ca0e4c.
        {yield return new WaitForSeconds(guidedServices.PropellerRocketTargetDelay);UnFreezeCells();}
        public virtual void FinishGuidedExplodeOperation() { } // 0x1c9f10c, native default is empty.
        public virtual IOriginalGuidedExploderPilot GetNewPilot() => guidedStrategy.GetNewPilot(); // 0x1c9f4a8.
        public virtual bool IsTargetAmountReached() => true; // 0x1c9f104.
        public virtual void ForceTargetChange() { FindNewTarget(); } // 0x1ca0af4.
        public void InvalidateTarget() { invalidateTarget = true; } // 0x1ca0cc8.
    }

    public partial class OriginalBoardItem
    {
        public bool CanAddPropellerAroundExploder() => ((IOriginalItemFallReadiness)FallComponent).IsItemSteady; // 0x2b47bf8.
        public virtual Vector3 GetViewPosition() => ((Component)SwapView).transform.position; // 0x2b46bc8.
        public virtual Vector3 GetMasterViewCenterPosition() => ((Component)SwapView).transform.position; // 0x2b46bf8 / 0x2b4afd8.
    }
}
