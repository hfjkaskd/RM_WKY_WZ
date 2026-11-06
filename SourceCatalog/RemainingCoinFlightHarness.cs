using System;
using System.Globalization;
using Whitebox.Foundation;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static readonly Vector3 zero=new Vector3(),up=new Vector3(0,1,0),down=new Vector3(0,-1,0),left=new Vector3(-1,0,0),right=new Vector3(1,0,0),forward=new Vector3(0,0,1);
        public float magnitude=>(float)Math.Sqrt((float)((float)((float)(x*x)+(float)(y*y))+(float)(z*z)));
        public Vector3 normalized{get{float n=magnitude;return n>1e-5f?this/n:zero;}}
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3((float)(a.x+b.x),(float)(a.y+b.y),(float)(a.z+b.z));
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3((float)(a.x-b.x),(float)(a.y-b.y),(float)(a.z-b.z));
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3((float)(a.x*b),(float)(a.y*b),(float)(a.z*b));
        public static Vector3 operator/(Vector3 a,float b)=>new Vector3((float)(a.x/b),(float)(a.y/b),(float)(a.z/b));
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
    }
    public static class Mathf{public static float Clamp01(float n)=>n<0?0:n>1?1:n;}
    public struct Quaternion
    {
        public float x,y,z,w;
        public static Quaternion AngleAxis(float degrees,Vector3 axis)
        {
            axis=axis.normalized;float angle=(float)((float)(degrees*(float)(Math.PI/180))*.5f),s=(float)Math.Sin(angle);
            return new Quaternion{x=axis.x*s,y=axis.y*s,z=axis.z*s,w=(float)Math.Cos(angle)};
        }
        public static Vector3 operator*(Quaternion q,Vector3 v)
        {
            // This harness exercises the production +/-Z circle only. Quaternion
            // engine calls are declared math boundaries in the native emulator.
            float zz=(float)((float)(q.z*2f)*q.z),wz=(float)((float)(q.z*2f)*q.w);
            return new Vector3((float)((float)((float)(1f-zz)*v.x)-(float)(wz*v.y)),(float)((float)(wz*v.x)+(float)((float)(1f-zz)*v.y)),v.z);
        }
    }
}
namespace Whitebox.Foundation{public struct OriginalCellPoint{public int X,Y;public OriginalCellPoint(int x,int y){X=x;Y=y;}}}
class RemainingCoinFlightHarness
{
    static UnityEngine.Vector3 V(float[] a,int i)=>new UnityEngine.Vector3(a[i],a[i+1],a[i+2]);
    static string F(float v)=>v.ToString("R",CultureInfo.InvariantCulture);
    static string S(UnityEngine.Vector3 v)=>F(v.x)+","+F(v.y)+","+F(v.z);
    static void Main()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;string line;
        while((line=Console.ReadLine())!=null)
        {
            float[] a=Array.ConvertAll(line.Split(','),s=>float.Parse(s,CultureInfo.InvariantCulture));
            var start=V(a,4);var first=OriginalRemainingMovesCoinFlight.GetFirstPosition(new OriginalCellPoint((int)a[0],(int)a[1]),start,(int)a[2],(int)a[3]);
            var f=new OriginalRemainingMovesCoinFlight();f.Initialize(start,first,V(a,7),(int)a[2]);
            f.Step(V(a,10),a[13],out bool unused);
            string vectors=S(f.GetFirstVector(a[14]))+"|"+S(f.GetFinalVector(V(a,10),a[14]));
            var next=f.Step(V(a,10),a[14],out bool reached);
            Console.WriteLine(S(first)+"|"+F(f.FirstDuration)+","+F(f.FinalStartTime)+","+F(f.FirstInitialSpeed)+"|"+vectors+"|"+S(next)+"|"+(reached?1:0));
        }
    }
}
