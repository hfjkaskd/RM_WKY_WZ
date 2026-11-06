using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    // TntPropellerCombo 0x1e2e0b0 / 0x1e2e398. The native primary
    // branch carries one existing TNT; it does not create extra projectiles.
    public sealed class OriginalTntPropellerCombo
    {
        private readonly OriginalPlayableBoard game;
        public OriginalTntPropellerCombo(OriginalPlayableBoard game) { this.game = game; }
        public void Explode(OriginalSpecialBoardItem to, OriginalSpecialBoardItem from, OriginalExplodeData data)
        {
            OriginalGridCell empty = to.CurrentCell, center = from.CurrentCell;
            var propeller = (to as OriginalPropellerBoardItem) ?? (OriginalPropellerBoardItem)from;
            var tnt = (to as OriginalTntBoardItem) ?? (OriginalTntBoardItem)from;
            if (ReferenceEquals(to, propeller))
            {
                propeller.Mediator.ClearFromAllRegisteredCells();
                center.Mediation.ClearAllItems(); center.Mediation.SetAllItems(propeller);
            }
            bool rightToLeft = empty.X > center.X, fromPropeller = from.ItemType == 3;
            tnt.Mediator.ClearFromAllRegisteredCells(); tnt.ItemView.StopCreationAnimation();
            game.PlaySfxInLoop(296); game.PlayHaptic(35, 2, .7f, 0f, false);
            empty.FreezeFallForDuration(.15f);
            propeller.SetStrategy(new OriginalPropellerTntStrategy(game, tnt, rightToLeft, fromPropeller));
            propeller.Explode(data);
        }
    }

    // PropellerSpecialItemStrategy / PropellerTntStrategy, normal primary
    // combo (isExtraCombo=false). Inactive first-board event owners stay inactive.
    internal sealed class OriginalPropellerTntStrategy : IOriginalGuidedExploderStrategy
    {
        private readonly OriginalPlayableBoard game;
        private readonly OriginalTntBoardItem tnt;
        private readonly OriginalDefaultPropellerStrategy normal;
        private readonly bool rightToLeft, fromPropeller;
        public OriginalPropellerTntStrategy(OriginalPlayableBoard game, OriginalTntBoardItem tnt, bool rightToLeft, bool fromPropeller)
        {
            this.game = game; this.tnt = tnt; this.rightToLeft = rightToLeft; this.fromPropeller = fromPropeller;
            normal = new OriginalDefaultPropellerStrategy(game, false);
        }
        public bool IsTntOrRocketCombo => true;
        public bool ExplodeNeighbors(OriginalGridCell cell, int trigger, OriginalGuidedExploderBoardItem model) => normal.ExplodeNeighbors(cell, trigger, model);
        public OriginalExplodeData NextExploder(OriginalGridCell cell) => OriginalExploder.Next(13, new OriginalCellPoint(cell.X, cell.Y)); // 0x1f3dac8.
        public void OnTargetFound() => game.State.Operations.OperationStart(9);
        public void OnTargetCancelled() => game.State.Operations.OperationFinish(9);
        public void OnTargetReached(OriginalGridCell cell, OriginalExplodeData data) // 0x1f3d708 / 0x1f3db98.
        {
            var next = NextExploder(cell); next.CanSpreadJelly |= data.CanSpreadJelly;
            tnt.Explode(next); game.StartGameplayCoroutine(FinishOperation());
        }
        private IEnumerator FinishOperation() // 0x1f3d98c uses gameplay time, not wall time.
        { yield return game.Wait(.5f); game.State.Operations.OperationFinish(9); }
        public void ForceExplode() // 0x1f3d85c invokes ItemModel.RecycleView, vslot71.
        { OnTargetCancelled(); tnt.RecycleView(); }
        public bool IsTargetAmountReached() => true;
        public IOriginalGuidedExploderPilot GetNewPilot() => new OriginalPropellerSpecialComboPilot(game, tnt, rightToLeft, fromPropeller);
    }

    public sealed partial class OriginalPlayableBoard
    {
        internal Transform PropellerBoardRoot => board.BoardRoot;
        internal OriginalSwapSorting NextPropellerComboSorting() =>
            new OriginalSwapSorting(UiSortingLayer, unchecked(150 + NextPropellerSortingOffset()), true);
    }
}
