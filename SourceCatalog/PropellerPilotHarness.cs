using System;
using System.Collections.Generic;
using Whitebox.Foundation;
using UnityEngine;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3(0,0,0);public static Vector3 one=>new Vector3(1,1,1);
        public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 back=>new Vector3(0,0,-1);
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3((float)(a.x+b.x),(float)(a.y+b.y),(float)(a.z+b.z));
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3((float)(a.x-b.x),(float)(a.y-b.y),(float)(a.z-b.z));
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3((float)(a.x*b),(float)(a.y*b),(float)(a.z*b));
        public static Vector3 operator/(Vector3 a,float b)=>new Vector3((float)(a.x/b),(float)(a.y/b),(float)(a.z/b));
        public float sqrMagnitude=>(float)((float)((float)(x*x)+(float)(y*y))+(float)(z*z));
        public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized=>magnitude>1e-5f?this/magnitude:zero;
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
        public static float Dot(Vector3 a,Vector3 b)=>(float)((float)((float)(a.x*b.x)+(float)(a.y*b.y))+(float)(a.z*b.z));
        public static float Angle(Vector3 a,Vector3 b){float d=(float)Math.Sqrt((float)(a.sqrMagnitude*b.sqrMagnitude));if(d<1e-15f)return 0;return (float)((float)Math.Acos(Mathf.Clamp((float)(Dot(a,b)/d),-1,1))*57.29578f);}
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static Vector3 MoveTowards(Vector3 a,Vector3 b,float d)
        {Vector3 v=b-a;float q=v.sqrMagnitude;if(q==0||(d>=0&&q<=(float)(d*d)))return b;return a+(v/(float)Math.Sqrt(q))*d;}
        public static Vector3 SmoothDamp(Vector3 current,Vector3 target,ref Vector3 velocity,float smoothTime,float maxSpeed,float delta)
        {
            smoothTime=Mathf.Max(.0001f,smoothTime);float omega=(float)(2f/smoothTime),x=(float)(omega*delta);
            float exp=(float)(1f/(float)((float)((float)(1f+x)+(float)((float)(.48f*x)*x))+(float)((float)((float)(.235f*x)*x)*x)));
            Vector3 change=current-target,original=target;float maxChange=(float)(maxSpeed*smoothTime);
            if(change.sqrMagnitude>(float)(maxChange*maxChange))change=change/change.magnitude*maxChange;
            target=current-change;Vector3 temp=(velocity+change*omega)*delta;
            velocity=(velocity-temp*omega)*exp;Vector3 output=target+(change+temp)*exp;
            if(Dot(original-current,output-original)>0){output=original;velocity=(output-original)/delta;}
            return output;
        }
    }
    public struct Quaternion
    {
        public float x,y,z,w,angle;
        public static Quaternion AngleAxis(float angle,Vector3 axis)
        {double rad=(double)angle*Math.PI/180.0/2.0;float s=(float)Math.Sin(rad);return new Quaternion{x=(float)(axis.x*s),y=(float)(axis.y*s),z=(float)(axis.z*s),w=(float)Math.Cos(rad),angle=angle};}
        public Vector3 eulerAngles=>new Vector3(0,0,Positive((float)((float)(angle*.0174532924f)*57.29578f)));
        public static float Positive(float v){if(v<-.005729578f)v=(float)(v+360f);else if(v>359.99427f)v=(float)(v-360f);return v;}
        public static Vector3 operator*(Quaternion q,Vector3 v)
        {
            float xx=(float)(q.x*2f),yy=(float)(q.y*2f),zz=(float)(q.z*2f);
            float x2=(float)(q.x*xx),y2=(float)(q.y*yy),z2=(float)(q.z*zz),xy=(float)(q.x*yy),xz=(float)(q.x*zz),yz=(float)(q.y*zz),wx=(float)(q.w*xx),wy=(float)(q.w*yy),wz=(float)(q.w*zz);
            return new Vector3((float)((float)((float)((float)(1f-(float)(y2+z2))*v.x)+(float)((float)(xy-wz)*v.y))+(float)((float)(xz+wy)*v.z)),
                (float)((float)((float)((float)(xy+wz)*v.x)+(float)((float)(1f-(float)(x2+z2))*v.y))+(float)((float)(yz-wx)*v.z)),
                (float)((float)((float)((float)(xz-wy)*v.x)+(float)((float)(yz+wx)*v.y))+(float)((float)(1f-(float)(x2+y2))*v.z)));
        }
    }
    public class Transform
    {
        public Transform parent;public Vector3 localPosition,localScale=Vector3.one;public Quaternion rotation,localRotation;
        public Vector3 position{get=>parent==null?localPosition:parent.position+localPosition;set=>localPosition=parent==null?value:value-parent.position;}
        public Vector3 eulerAngles=>new Vector3(0,0,Quaternion.Positive(rotation.angle));
        public Vector3 InverseTransformPoint(Vector3 value)=>value-position;
    }
    public static class Mathf
    {
        public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float v,float min,float max)=>v<min?min:v>max?max:v;public static float Clamp01(float v)=>Clamp(v,0,1);
        public static float SmoothDamp(float current,float target,ref float velocity,float smoothTime,float maxSpeed,float delta)
        {
            smoothTime=Max(.0001f,smoothTime);float omega=(float)(2f/smoothTime),x=(float)(omega*delta);
            float exp=(float)(1f/(float)((float)((float)(1f+x)+(float)((float)(.48f*x)*x))+(float)((float)((float)(.235f*x)*x)*x)));
            float change=(float)(current-target),original=target,maxChange=(float)(maxSpeed*smoothTime);change=Clamp(change,-maxChange,maxChange);target=(float)(current-change);
            float temp=(float)((float)(velocity+(float)(omega*change))*delta);velocity=(float)((float)(velocity-(float)(omega*temp))*exp);
            float output=(float)(target+(float)((float)(change+temp)*exp));
            if((original-current>0)==(output>original)){output=original;velocity=(float)((float)(output-original)/delta);}return output;
        }
    }
}
class PropellerPilotHarness : IOriginalPropellerPilotView,IOriginalGuidedExploderViewDelegate,IOriginalPropellerPilotContext
{
    public Transform Root=new Transform(),Animator=new Transform(),Fan=new Transform(),Shadow=new Transform();
    public Transform PilotTransform=>Root;public Transform PilotAnimatorTransform=>Animator;public Transform PilotFanTransform=>Fan;public Transform PilotShadowTransform=>Shadow;
    public int FrameCount{get;set;}public float LevelTime{get;set;}public float LevelDeltaTime{get;set;}public float UnityDeltaTime{get;set;}
    public float[] random;private int randomIndex;public Vector3 Target;public bool Valid=true;public bool Ended;public OriginalDefaultPropellerPilot Pilot;
    public List<string> Events=new List<string>();
    public float RandomValue()=>random[randomIndex++];public void ReportLargeDelta(float delta){}public void ReportStalledFlight(bool forced){}
    public void PlayStartFlyAnimation(bool swipe,bool sound){Events.Add("Start");}public void UpdatePilotSorting(bool down,bool all){Events.Add(down?"SortDown":"SortFly");}
    public void TriggerTargetReached(){Events.Add("Hit");}public void TriggerFakeTargetReached(){Events.Add("Fake");}
    public void ExplodeSelf(bool combo){Events.Add("Explode");Ended=true;}
    public bool IsTargetValid()=>Valid;public Vector3 GetTargetPosition()=>Target;public IOriginalGuidedExploderPilot GetNewPilot()=>Pilot;
    public void ForceTargetChange(){Events.Add("Force");Pilot.TargetChanged();}public void FakeTargetReached(){}public bool IsTargetAmountReached()=>true;public void FinishGuidedExplodeOperation(){}
    private static float Float(string s)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);
    private static string Bits(float f)=>BitConverter.ToUInt32(BitConverter.GetBytes(f),0).ToString();
    private string Snapshot()
    {
        var v=new List<string>{((int)Pilot.FlyState).ToString()};foreach(Vector3 q in new[]{Root.position,Root.localScale,Shadow.localPosition}){v.Add(Bits(q.x));v.Add(Bits(q.y));v.Add(Bits(q.z));}
        v.Add(Bits(Animator.rotation.angle));v.Add(Bits(Fan.localRotation.angle));return string.Join(",",v)+"#"+string.Join(",",Events);
    }
    static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('|');var h=new PropellerPilotHarness();var init=Array.ConvertAll(p[0].Split(','),Float);
            var parent=new Transform{localPosition=new Vector3(init[0],init[1],0)};h.Root.parent=parent;h.Root.localPosition=new Vector3(init[2],init[3],0);h.Shadow.localPosition=new Vector3(.01f,-.2f,0);
            h.Target=parent.position+new Vector3(init[4],init[5],0);h.random=new[]{init[6],init[7]};h.Pilot=new OriginalDefaultPropellerPilot(h);
            var frames=p[1].Split(';');var output=new List<string>();
            for(int frame=0;frame<frames.Length;frame++)
            {
                var values=frames[frame].Split(',');h.FrameCount=int.Parse(values[0]);h.UnityDeltaTime=Float(values[1]);h.LevelDeltaTime=Float(values[2]);h.LevelTime=Float(values[3]);h.Valid=values[4]=="1";
                if(values[5]=="1")h.Pilot.TargetChanged();if(frame==0)h.Pilot.StartFlying(h,h);else h.Pilot.Move();
                output.Add(h.Snapshot());h.Events.Clear();if(h.Ended)break;
            }
            Console.WriteLine(string.Join(";",output));
        }
    }
}
