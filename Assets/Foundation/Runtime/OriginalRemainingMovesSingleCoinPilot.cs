using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalRemainingMovesSingleCoinPilot:OriginalItemParticles
    {
        public OriginalCoinItemSpriteView coinItemView;
        public OriginalRemainingMovesCoinTracer coinTracer;
        private readonly OriginalRemainingMovesCoinFlight flight=new OriginalRemainingMovesCoinFlight();
        private Func<float> delta;
        private Action<OriginalRemainingMovesSingleCoinPilot> reached;
        private Vector3 shadowStart,shadowMax;
        private bool running;
        public bool IsRunning=>running;
        public OriginalRemainingMovesCoinFlight Flight=>flight;
        public override int GetPoolId()=>76; // 0x21847dc.
        public void MoveToTarget(OriginalCoinItemSpriteView coin,Vector3 first,Vector3 final,int index,
            Func<float> levelDelta,Action<OriginalRemainingMovesSingleCoinPilot> onComplete)
        {
            coinItemView=coin;delta=levelDelta;reached=onComplete;
            shadowStart=coin.shadowView.transform.localPosition;shadowMax=shadowStart+new Vector3(.3f,-.5f,0f);
            flight.Initialize(transform.position,first,final,index);running=true;
            coinTracer.ArrangeRateOverDistanceByRatio(1f);coinTracer.Play();
        }
        private void Update()
        {
            if(!running)return;
            transform.position=flight.Step(transform.position,delta(),out bool arrived);
            if(arrived){running=false;reached(this);}
            ArrangeShadow();
        }
        private void ArrangeShadow()
        {
            if(flight.Elapsed<flight.FirstDuration)
            {coinItemView.shadowView.transform.localPosition=flight.GetShadow(shadowStart,shadowMax,transform.position);return;}
            if(Vector3.Distance(flight.FinalTarget,transform.position)<3f)
                coinItemView.shadowView.transform.localPosition=flight.GetShadow(shadowStart,shadowMax,transform.position);
            coinTracer.ArrangeRateOverDistanceByRatio(flight.GetTrailRatio(transform.position));
        }
        public void TapToSkip(){running=false;reached(this);} // 0x2182974.
        public void Recycle()=>OriginalParticleContext.RecycleScheduler.Schedule(this,Time.time,2f); // Native Invoke("RecycleSelf",2).
        public override void CancelScheduledActions(){OriginalParticleContext.RecycleScheduler.Cancel(this);base.CancelScheduledActions();}
    }
}
