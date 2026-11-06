using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero => new Vector3(0,0,0);
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 down => new Vector3(0,-1,0);
        public static Vector3 forward => new Vector3(0,0,1);
        public static Vector3 left => new Vector3(-1,0,0);
        public static Vector3 right => new Vector3(1,0,0);
        public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));
    }
    public class GameObject {public void SetActive(bool value){}}
    public class Transform {public Vector3 position,localPosition,localScale;public GameObject gameObject=new GameObject();public void SetParent(Transform t){}}
    public class SpriteRenderer {public Transform transform=new Transform();public GameObject gameObject=new GameObject();}
    public static class Mathf {public static float Abs(float value)=>Math.Abs(value);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace Whitebox.Foundation
{
    public class OriginalRocketViewAssets {public int useSound;public float useSoundOffset,speed=13.5f,accelerationPerFrame=.18f,hitDistance=.8f,horizontalScreenMargin=1,verticalScreenMargin=2,wideDeviceUnfreezeX=5.65f,recycleTail=1;}
    public class OriginalRocketItemParticles {public UnityEngine.Transform transform=new UnityEngine.Transform();public void SetParticlePosition(UnityEngine.Vector3 d){}public void Play(){}}
    public class OriginalGameObjectPool {public void Recycle(object value){}}
    public class OriginalGameStateManager {public bool IsNoGame()=>false;}
    public class OriginalBoardLayout {public int Count=1,CurrentBoardIndex;}
    public class OriginalCoroutinePlayer {public void ManualStartCoroutine(IEnumerator e){}}
    public interface IOriginalRocketFlightServices
    {
        OriginalGameObjectPool Pool {get;} OriginalGameStateManager State {get;} OriginalCellGrid Grid {get;} OriginalBoardLayout RocketLayout {get;}
        OriginalCoroutinePlayer RocketCoroutines {get;} float RocketDeltaTime {get;} float CameraWidth {get;} float CameraHeight {get;}
        void PlaySfx(int t,float d);OriginalRocketItemParticles SpawnRocketParticles();
    }
    public class OriginalCellGrid {public readonly Dictionary<long,OriginalGridCell> Cells=new Dictionary<long,OriginalGridCell>();public OriginalGridCell GetCell(int x,int y){OriginalGridCell c;Cells.TryGetValue((long)x+((long)y<<32),out c);return c;}}
    public class OriginalGridCell
    {
        public int X,Y,Id;public UnityEngine.Vector3 Position;
        public UnityEngine.Vector3 GetViewPosition()=>Position;
        public void FreezeFall(){Verify.Trace.Add("F"+Id);}
        public void UnFreezeFall(){Verify.Trace.Add("U"+Id);}
        public void CrossFallFreeze(){}public void CrossFallUnFreeze(){}
        public void ExplodeCurrentItem(OriginalExplodeData d){Verify.Trace.Add("E"+Id+":"+d.Point.X+","+d.Point.Y+","+d.Trigger+","+d.Id+":"+(d.IsHorizontalRocket?1:0));}
    }
    public class OriginalRocketItemSpriteView
    {
        public UnityEngine.SpriteRenderer BaseRenderer=new UnityEngine.SpriteRenderer(),FirstPart=new UnityEngine.SpriteRenderer(),SecondPart=new UnityEngine.SpriteRenderer();
        public UnityEngine.Transform HorizontalAnimation=new UnityEngine.Transform(),VerticalAnimation=new UnityEngine.Transform();
        public void RocketPartStart(){Verify.Trace.Add("Start");}public void RocketPartEnd(){Verify.Trace.Add("End");}public void Recycle(){}
    }
    public abstract partial class OriginalRocketCreationStrategy
    {
        protected readonly OriginalRocketItemSpriteView view=new OriginalRocketItemSpriteView();
        protected readonly OriginalRocketViewAssets assets=new OriginalRocketViewAssets();
        protected readonly IOriginalRocketFlightServices services;
        protected OriginalRocketCreationStrategy(IOriginalRocketFlightServices services){this.services=services;}
    }
    public sealed partial class OriginalHorizontalRocketCreationStrategy : OriginalRocketCreationStrategy
    {
        private readonly bool isWideOrLandscapeDevice;
        public OriginalHorizontalRocketCreationStrategy(IOriginalRocketFlightServices services,bool wide):base(services){isWideOrLandscapeDevice=wide;finishedPartCount=1;}
        public IEnumerator Run(OriginalExplodeData data,UnityEngine.Transform part,UnityEngine.Vector3 target,float unfreeze,OriginalGridCell center,List<OriginalCellPoint> cells)=>MoveToGlobalAndDestroy(data,part,target,unfreeze,center,cells);
    }
    public partial class OriginalVerticalRocketCreationStrategy : OriginalRocketCreationStrategy
    {
        public OriginalVerticalRocketCreationStrategy(IOriginalRocketFlightServices services):base(services){}
        public IEnumerator Run(OriginalExplodeData data,UnityEngine.Transform part,UnityEngine.Vector3 target,OriginalGridCell center,List<OriginalCellPoint> cells)=>MoveToGlobalAndDestroy(data,part,target,cells,center,()=>Verify.Trace.Add("Complete"));
    }
    public sealed partial class OriginalWaterVerticalRocketCreationStrategy : OriginalVerticalRocketCreationStrategy
    {public OriginalWaterVerticalRocketCreationStrategy(IOriginalRocketFlightServices services):base(services){}}
}
class Services : IOriginalRocketFlightServices
{
    public OriginalGameObjectPool Pool {get;}=new OriginalGameObjectPool();public OriginalGameStateManager State {get;}=new OriginalGameStateManager();
    public OriginalCellGrid Grid {get;}=new OriginalCellGrid();public OriginalBoardLayout RocketLayout {get;}=new OriginalBoardLayout();
    public OriginalCoroutinePlayer RocketCoroutines {get;}=new OriginalCoroutinePlayer();public float Delta;
    public float RocketDeltaTime=>Delta;public float CameraWidth=>10;public float CameraHeight=>18;
    public void PlaySfx(int t,float d){}public OriginalRocketItemParticles SpawnRocketParticles()=>new OriginalRocketItemParticles();
}
class Verify
{
    public static List<string> Trace=new List<string>();
    static float Float(string text)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(text)),0);
    static uint Bits(float value)=>BitConverter.ToUInt32(BitConverter.GetBytes(value),0);
    static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var a=line.Split('|');bool vertical=a[0]=="1",wide=a[1]=="1";int origin=int.Parse(a[3]);var services=new Services();
            var center=new OriginalGridCell{Id=99,X=origin,Y=3};var cells=new List<OriginalCellPoint>();
            for(int i=0;i<(vertical?8:9);i++)
            {
                var c=new OriginalGridCell{Id=i,X=vertical?3:i,Y=vertical?i:3,Position=vertical?new UnityEngine.Vector3(.25f,i-3.5f,0):new UnityEngine.Vector3(i-4,.25f,0)};
                services.Grid.Cells.Add((long)c.X+((long)c.Y<<32),c);
            }
            if(a[7].Length>0)foreach(var i in a[7].Split(',')){int v=int.Parse(i);cells.Add(new OriginalCellPoint(vertical?3:v,vertical?v:3));}
            var part=new UnityEngine.Transform{position=vertical?new UnityEngine.Vector3(.25f,Float(a[4]),0):new UnityEngine.Vector3(Float(a[4]),.25f,0)};
            var target=vertical?new UnityEngine.Vector3(0,Float(a[5]),0):new UnityEngine.Vector3(Float(a[5]),0,0);
            var data=new OriginalExplodeData(6,91,new OriginalCellPoint(origin,3),0,false,!vertical);
            IEnumerator routine=vertical?new OriginalVerticalRocketCreationStrategy(services).Run(data,part,target,center,cells):new OriginalHorizontalRocketCreationStrategy(services,wide).Run(data,part,target,Float(a[6]),center,cells);
            var frames=new List<string>();foreach(var step in a[8].Split(','))
            {
                services.Delta=Float(step);Trace.Clear();bool result=routine.MoveNext();var p=part.position;
                frames.Add((result?"1":"0")+","+Bits(p.x)+","+Bits(p.y)+","+Bits(p.z)+"#"+string.Join(",",Trace));if(!result)break;
            }
            Console.WriteLine(string.Join(";",frames));
        }
    }
}
