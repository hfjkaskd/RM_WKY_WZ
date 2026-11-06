using UnityEngine;

namespace Whitebox.Foundation
{
    // RemainingMovesSingleCoinPilot 0x2182354/0x2184254/0x2184400/0x2183fa4.
    // The first impulse decelerates from its initial position; the final
    // impulse continuously homes toward the goal. These are not Bezier flights.
    public sealed class OriginalRemainingMovesCoinFlight
    {
        public Vector3 InitialPosition { get; private set; }
        public Vector3 FirstTarget { get; private set; }
        public Vector3 FinalTarget { get; private set; }
        public float FirstInitialSpeed { get; private set; }
        public float FirstDuration { get; private set; }
        public float FinalStartTime { get; private set; }
        public float Elapsed { get; private set; }
        public void Initialize(Vector3 start,Vector3 first,Vector3 final,int index)
        {
            InitialPosition=start;FirstTarget=first;FinalTarget=final;Elapsed=0f;
            FirstDuration=(float)((float)(index*.1f)+.3f);
            FinalStartTime=(float)(FirstDuration-.05f);
            FirstInitialSpeed=(float)((float)(Vector3.Distance(first,start)*2f)/FirstDuration);
        }
        private static Vector3 CapDirection(Vector3 vector)
        {return vector.magnitude>1f?vector.normalized:vector;}
        public Vector3 GetFirstVector(float delta)
        {
            float ratio=Mathf.Clamp01(Elapsed/FirstDuration);
            float speed=(float)(FirstInitialSpeed+(float)((float)(0f-FirstInitialSpeed)*ratio));
            return CapDirection(FirstTarget-InitialPosition)*(float)(delta*speed);
        }
        public Vector3 GetFinalVector(Vector3 position,float delta)
        {
            float ratio=Mathf.Clamp01((float)((float)(Elapsed-FinalStartTime)/.6f));
            return CapDirection(FinalTarget-position)*(float)(delta*(float)(ratio*20f));
        }
        public Vector3 Step(Vector3 position,float delta,out bool arrived)
        {
            Elapsed=(float)(Elapsed+delta);
            var movement=GetFirstVector(delta)+GetFinalVector(position,delta);
            var remaining=FinalTarget-position;
            if(movement.magnitude>remaining.magnitude)movement=remaining;
            var next=position+movement;arrived=Vector3.Distance(next,FinalTarget)<.1f;return next;
        }
        public Vector3 GetShadow(Vector3 start,Vector3 max,Vector3 position)
        {
            if(Elapsed<FirstDuration)return Vector3.Lerp(start,max,Elapsed/FirstDuration);
            float distance=Vector3.Distance(FinalTarget,position);
            return distance<3f?Vector3.Lerp(max,start,(float)((float)(distance/-3f)+1f)):max;
        }
        public float GetTrailRatio(Vector3 position)=>Vector3.Distance(FinalTarget,position)/Vector3.Distance(FinalTarget,FirstTarget);

        // 0x21820b4/0x218250c: first-board native fan directions and five-coin arc.
        public static Vector3 GetFirstPosition(OriginalCellPoint cell,Vector3 start,int index,int maxIndex)
        {
            if(maxIndex>=4)
            {
                float ratio=Mathf.Clamp01((float)index/maxIndex);
                var direction=(Quaternion.AngleAxis((float)((float)(ratio*180f)+90f),Vector3.forward)*Vector3.up).normalized;
                return start+new Vector3((float)(direction.x*2f),(float)((float)(direction.y*2f)-ratio),(float)(direction.z*2f));
            }
            Vector3 vector=Vector3.zero;
            if(cell.X<=1)
            {if(index==0)vector=Vector3.down;else if(index==1)vector=new Vector3(1f,-1f,0f);else if(index==2)vector=Vector3.right;}
            else if(cell.X>=7)
            {if(index==0)vector=Vector3.left;else if(index==1)vector=new Vector3(-1f,-1f,0f);else if(index==2)vector=Vector3.down;}
            else if(maxIndex==1)
            {if(index==0)vector=new Vector3(-.6f,-1f,0f);else if(index==1)vector=new Vector3(.6f,-1f,0f);}
            else if(maxIndex==2)
            {if(index==0)vector=new Vector3(-1f,-1f,0f);else if(index==1)vector=Vector3.down;else if(index==2)vector=new Vector3(1f,-1f,0f);}
            else vector=new Vector3(.1f,-1f,0f);
            return start+vector.normalized*(maxIndex==0?2f:2.5f);
        }
    }
}
