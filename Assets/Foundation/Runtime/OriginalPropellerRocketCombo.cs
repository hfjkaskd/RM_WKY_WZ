using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    // PropellerRocketCombo 0x1e2db40/0x1e2de28, primary existing-item branch.
    public sealed class OriginalPropellerRocketCombo
    {
        private readonly OriginalPlayableBoard game;
        public OriginalPropellerRocketCombo(OriginalPlayableBoard game){this.game=game;}
        public void Explode(OriginalSpecialBoardItem to,OriginalSpecialBoardItem from,OriginalExplodeData data)
        {
            var empty=to.CurrentCell;var center=from.CurrentCell;
            var propeller=(to as OriginalPropellerBoardItem)??(OriginalPropellerBoardItem)from;
            var rocket=(to as OriginalRocketBoardItem)??(OriginalRocketBoardItem)from;
            if(ReferenceEquals(to,propeller))
            {propeller.Mediator.ClearFromAllRegisteredCells();center.Mediation.ClearAllItems();center.Mediation.SetAllItems(propeller);}
            bool rightToLeft=empty.X>center.X,fromPropeller=from.ItemType==3;
            rocket.Mediator.ClearFromAllRegisteredCells();rocket.ItemView.StopCreationAnimation();
            game.PlaySfxInLoop(296);game.PlayHaptic(30,2,.7f,0f,false);empty.FreezeFallForDuration(.15f);
            propeller.SetStrategy(new OriginalPropellerRocketStrategy(game,rocket,rightToLeft,fromPropeller));propeller.Explode(data);
        }
    }
    internal sealed class OriginalPropellerRocketStrategy:IOriginalGuidedExploderStrategy
    {
        private readonly OriginalPlayableBoard game;
        private readonly OriginalRocketBoardItem rocket;
        private readonly OriginalDefaultPropellerStrategy normal;
        private readonly bool rightToLeft,fromPropeller;
        private readonly int orientation;
        public OriginalPropellerRocketStrategy(OriginalPlayableBoard game,OriginalRocketBoardItem rocket,bool rightToLeft,bool fromPropeller)
        {this.game=game;this.rocket=rocket;this.rightToLeft=rightToLeft;this.fromPropeller=fromPropeller;orientation=rocket.Orientation;normal=new OriginalDefaultPropellerStrategy(game,false);}
        public bool IsTntOrRocketCombo=>true;
        public bool ExplodeNeighbors(OriginalGridCell cell,int trigger,OriginalGuidedExploderBoardItem model)=>normal.ExplodeNeighbors(cell,trigger,model);
        public OriginalExplodeData NextExploder(OriginalGridCell cell)=>OriginalExploder.Next(orientation==0?11:10,new OriginalCellPoint(cell.X,cell.Y));
        private int Operation=>orientation==0?10:11; // 0x1f3d468, different identity from trigger.
        public void OnTargetFound()=>game.State.Operations.OperationStart(Operation);
        public void OnTargetCancelled()=>game.State.Operations.OperationFinish(Operation);
        public void OnTargetReached(OriginalGridCell cell,OriginalExplodeData data)
        {
            game.StartGameplayCoroutine(FinishOperation());game.StartGameplayCoroutine(ExplodeWithDelay(cell,data));game.State.StartSpecialOperation();
        }
        private IEnumerator FinishOperation(){yield return game.Wait(.5f);game.State.Operations.OperationFinish(Operation);}
        private IEnumerator ExplodeWithDelay(OriginalGridCell cell,OriginalExplodeData data) // 0x1f3d480: Unity-scaled wait, not gameplay wait.
        {
            bool jelly=data.CanSpreadJelly;rocket.ItemView.Hide();yield return new WaitForSeconds(game.PropellerRocketTargetDelay);rocket.ItemView.Show();
            var next=NextExploder(cell);next.CanSpreadJelly|=jelly;rocket.Explode(next);game.State.FinishSpecialOperation();
        }
        public void ForceExplode(){OnTargetCancelled();rocket.RecycleView();}
        public bool IsTargetAmountReached()=>true;
        public IOriginalGuidedExploderPilot GetNewPilot()=>new OriginalPropellerSpecialComboPilot(game,rocket,rightToLeft,fromPropeller);
    }
}
