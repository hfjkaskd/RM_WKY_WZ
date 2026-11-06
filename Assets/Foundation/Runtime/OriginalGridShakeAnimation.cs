using UnityEngine;

namespace Whitebox.Foundation
{
    // ShakeAnimation 0x2c96bdc/0x2c96eec. Bound to the actual prefab board
    // root and gameplay clock; no camera substitute or Editor-only transport.
    public sealed class OriginalGridShakeAnimation : MonoBehaviour
    {
        private OriginalDeltaTimeManager clock;
        private Vector3 initialPosition;
        private bool isShaking,visitCenter;
        private float animationTime,finishTime,min,mid,max;
        private int frame,direction;
        public bool IsShaking=>isShaking;
        internal Vector3 RestWorldPosition
        {
            get
            {
                if (!isShaking) return transform.position;
                var parent = transform.parent;
                return parent != null ? parent.TransformPoint(initialPosition) : initialPosition;
            }
        }
        public void Bind(OriginalDeltaTimeManager value) { clock=value; }
        public void SetRestHeight(float height)
        {
            var position = transform.localPosition;
            if (isShaking)
            {
                position.y += height - initialPosition.y;
                initialPosition.y = height;
            }
            else
            {
                position.y = height;
                initialPosition = position;
            }
            transform.localPosition = position;
        }
        public void Shake(OriginalTntShakeConfig value)
        {
            if (isShaking)
            { if(value.ShouldVisitCenter&&!visitCenter)return;StopShaking(); }
            initialPosition=transform.localPosition;visitCenter=value.ShouldVisitCenter;animationTime=value.Duration;
            finishTime=clock.Time+animationTime+value.Delay;min=value.MinVibrate;mid=value.MidVibrate;max=value.MaxVibrate;
            isShaking=true;direction=value.Direction;
        }
        private void Update()
        {
            if(!isShaking)return;
            float remaining=finishTime-clock.Time;
            if(remaining<0f){StopShaking();return;}
            if(remaining-animationTime>0f)return;
            if(!visitCenter)
            {
                float x=initialPosition.x,y=initialPosition.y;
                if(direction!=1)x=Random.Range(x+min,x+max);
                if(direction!=2)y=Random.Range(y+min,y+max);
                transform.localPosition=new Vector3(x,y,0f);return;
            }
            if(frame==0)transform.localPosition+=Offset(mid,max);
            else if((frame&1)==0)transform.localPosition=initialPosition;
            else transform.localPosition+=remaining>animationTime*.5f?Offset(min,mid):Offset(0f,min);
            frame=unchecked(frame+1);
        }
        private Vector3 Offset(float a,float b)=>new Vector3(direction==1?0f:RandomPosition(a,b),direction==2?0f:RandomPosition(a,b),0f);
        private static float RandomPosition(float a,float b)
        { float value=Random.Range(a,b);return Random.Range(0,2)<1?-value:value; }
        public void StopShaking(){frame=0;isShaking=false;transform.localPosition=initialPosition;}
    }
}
