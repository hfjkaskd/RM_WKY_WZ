using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalPlayableBoard : IOriginalBallBallComboServices,IOriginalBallBallViewServices
    {
        private readonly List<OriginalBallBallComboView> ballBallAnimations = new List<OriginalBallBallComboView>(2);
        private void TickBallBallAnimations()
        {
            for(int i=0;i<ballBallAnimations.Count;)
            {
                if(ballBallAnimations[i].ManualTweenUpdate(board.Clock.DeltaTime))ballBallAnimations.RemoveAt(i);
                else i++;
            }
        }
        OriginalGridCell IOriginalBallBallComboServices.GetCell(OriginalCellPoint point)=>Grid.GetCell(point.X,point.Y);
        void IOriginalBallBallComboServices.RecycleLightballForCombo(OriginalLightballBoardItem item)=>Pool.Recycle(item.ItemView);
        void IOriginalBallBallComboServices.StartSpecialOperation()=>State.StartSpecialOperation();
        void IOriginalBallBallComboServices.FinishSpecialOperation()=>State.FinishSpecialOperation();
        void IOriginalBallBallComboServices.DisableComboTouch()=>TouchListener.DisableTouch();
        void IOriginalBallBallComboServices.EnableComboTouch()=>TouchListener.EnableTouch();
        void IOriginalBallBallComboServices.PlayBallBallView(Vector3 position,int swipe,Action complete)
        {
            var view=Pool.Spawn<OriginalBallBallComboView>(31,true);
            view.Initialize(this,position,swipe,complete);ballBallAnimations.Add(view);
        }
        void IOriginalBallBallComboServices.ShakeBallBallGrid()=>board.ShakeGrid(board.Presentation.LightballAssets.ballBall.shake);
        void IOriginalBallBallComboServices.StartBallBallCoroutine(IEnumerator routine)=>StartGameplayCoroutine(routine);
        object IOriginalBallBallComboServices.WaitBallBall(float seconds)=>Wait(seconds);
        void IOriginalBallBallComboServices.CollectBallBallMadness(OriginalExplodeData data,OriginalGridCell center)
        { Vector3 p=center.GetViewPosition();Session.TryCollectMadnessForExploder(data,new OriginalSwapPoint(p.x,p.y,p.z),0); }
    }
}
