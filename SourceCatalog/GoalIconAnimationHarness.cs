using System;
using System.Globalization;
using Whitebox.Foundation;
using UnityEngine;
namespace UnityEngine
{
    public sealed class Transform { public Vector3 localScale; }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public static Vector3 LerpUnclamped(Vector3 a,Vector3 b,float t)=>new Vector3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);
    }
    public static class Mathf { public static float Clamp01(float t)=>t<0f?0f:t>1f?1f:t; public static float Min(float a,float b)=>Math.Min(a,b); }
    public static class Time { public static float deltaTime; }
}
namespace Whitebox.Foundation
{
    public sealed class OriginalGoalUiAssets
    {
        public Vector3 flyingHitMultiplier=new Vector3(1.1f,.9f,0),settledHitMultiplier=new Vector3(.8f,.8f,0);
        public float flyingHitDuration=.08f,flyingReturnDuration=.11f,settledHitDuration=.1f;
    }
}
internal static class GoalIconHarness
{
    static float F(string v)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(v)),0);
    static uint B(float v)=>BitConverter.ToUInt32(BitConverter.GetBytes(v),0);
    static void Main()
    {
        var image=new Transform();var settings=new OriginalGoalUiAssets();
        var animation=new OriginalGoalIconAnimation();string line;
        while((line=Console.ReadLine())!=null)
        {
            var a=line.Split(',');
            if(a[0]=="I"){image.localScale=new Vector3(F(a[1]),F(a[2]),F(a[3]));animation.Initialize(image,image.localScale,settings);}
            else if(a[0]=="S")image.localScale=new Vector3(F(a[1]),F(a[2]),F(a[3]));
            else if(a[0]=="P"){Console.WriteLine(animation.Play(a[1]=="1")?"1":"0");continue;}
            else if(a[0]=="T"){Time.deltaTime=F(a[2]);animation.Tick(F(a[1]));}
            else if(a[0]=="C")animation.Clear();
            else if(a[0]=="G")
            {
                animation.Clear();animation.Play(true);animation.Tick(.001f);
                for(int i=0;i<1000;i++)animation.Tick(0.0000001f);
                long start=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<10000;i++)animation.Tick(0.0000001f);
                Console.WriteLine("GC,"+(GC.GetAllocatedBytesForCurrentThread()-start));continue;
            }
            Console.WriteLine(animation.ActiveCount+","+(animation.IsFlyingActive?1:0)+","+B(image.localScale.x)+","+B(image.localScale.y)+","+B(image.localScale.z));
        }
    }
}
