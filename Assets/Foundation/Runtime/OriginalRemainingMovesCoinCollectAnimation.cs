using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalRemainingMovesCoinServices
    {
        OriginalGameObjectPool Pool { get; }
        OriginalGoalManager Goals { get; }
        OriginalGameStateManager State { get; }
        float DeltaTime { get; }
        Vector3 GoalPosition { get; }
        OriginalSwapSorting NextCoinSorting();
        void SetOrientationEnabled(bool enabled);
        void PlayCoinSound(int sound,float offset);
        float RandomFlipTime();
    }
    // RemainingMovesCoinCollectAnimation 0x21819a8..0x2182970. Models update
    // when coins depart; UI updates only when their actual pilots arrive.
    public sealed class OriginalRemainingMovesCoinCollectAnimation
    {
        private readonly IOriginalRemainingMovesCoinServices services;
        private readonly List<OriginalCoinItemSpriteView> spawnedItems=new List<OriginalCoinItemSpriteView>(10);
        private readonly List<OriginalRemainingMovesSingleCoinPilot> coinPilots=new List<OriginalRemainingMovesSingleCoinPilot>(10);
        private readonly Func<float> delta;
        private readonly Action<OriginalRemainingMovesSingleCoinPilot> onReached;
        public int FlyingCount=>coinPilots.Count;
        public OriginalRemainingMovesCoinCollectAnimation(IOriginalRemainingMovesCoinServices services)
        {this.services=services??throw new ArgumentNullException(nameof(services));delta=()=>services.DeltaTime;onReached=OnItemReached;}
        public void Play(OriginalCellPoint point,Vector3 start,int count)
        {
            Vector3 goal=services.GoalPosition;services.PlayCoinSound(83,.04f);
            for(int i=0;i<count;i++)
            {
                var pilot=services.Pool.Spawn<OriginalRemainingMovesSingleCoinPilot>(76,true);pilot.transform.position=start;
                var coin=services.Pool.Spawn<OriginalCoinItemSpriteView>(68,true);coin.transform.SetParent(pilot.transform);
                spawnedItems.Add(coin);var sorting=services.NextCoinSorting();coin.SetCollectionSorting(sorting.Layer,sorting.Order);
                coin.PlayFlipAnimation(services.RandomFlipTime());coin.shadowView.color=new Color(1f,1f,1f,.3f);
                coin.transform.localPosition=Vector3.zero;coin.transform.localScale=Vector3.one;coin.baseView.color=Color.white;
                services.SetOrientationEnabled(false);services.Goals.IncreaseGoal(21);services.Goals.FlyingToBeCollected(21);
                services.State.Operations.OperationStart(5);coinPilots.Add(pilot);
                pilot.MoveToTarget(coin,OriginalRemainingMovesCoinFlight.GetFirstPosition(point,start,i,count-1),goal,i,delta,onReached);
            }
        }
        private void OnItemReached(OriginalRemainingMovesSingleCoinPilot pilot) // 0x21826f8, including both native sound requests.
        {
            var coin=pilot.coinItemView;spawnedItems.Remove(coin);if(coin!=null)services.Pool.Recycle(coin);
            coinPilots.Remove(pilot);pilot.Recycle();services.SetOrientationEnabled(true);
            services.PlayCoinSound(84,.04f);services.Goals.IncreaseGoalUi(21);services.State.Operations.OperationFinish(5);services.PlayCoinSound(84,.04f);
        }
        public void TapToSkip() // Backward traversal; callbacks remove their own pilots.
        {for(int i=coinPilots.Count-1;i>=0;i--)coinPilots[i].TapToSkip();}
    }
    public sealed class OriginalRemainingMovesCoinServices:IOriginalRemainingMovesCoinServices
    {
        private readonly OriginalPlayableBoard game;
        private readonly OriginalInitialBoardView board;
        private readonly OriginalGameplayHud hud;
        private readonly OriginalSfxPlayback audio;
        public OriginalRemainingMovesCoinServices(OriginalPlayableBoard game,OriginalInitialBoardView board,OriginalGameplayHud hud,OriginalSfxPlayback audio)
        {this.game=game;this.board=board;this.hud=hud;this.audio=audio;}
        public OriginalGameObjectPool Pool=>board.Presentation.Pool;
        public OriginalGoalManager Goals=>board.Goals;
        public OriginalGameStateManager State=>game.State;
        public float DeltaTime=>board.Clock.DeltaTime;
        public Vector3 GoalPosition=>game.GoalPanel.GetGoalPosition(21);
        public OriginalSwapSorting NextCoinSorting()=>game.NextPropellerComboSorting();
        public void SetOrientationEnabled(bool enabled)=>hud.SetOrientationEnabled(enabled);
        public void PlayCoinSound(int sound,float offset)=>audio.PlaySound(sound,offset);
        public float RandomFlipTime()=>UnityEngine.Random.Range(0f,.1f);
    }
}
