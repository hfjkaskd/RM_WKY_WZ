using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine {
[AttributeUsage(AttributeTargets.Field)]public sealed class SerializeField:Attribute{}
public static class Time{public static float time;}
public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 zero=>new Vector3(0,0,0);}
public struct Quaternion{public float x,y,z,w;public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Quaternion identity=>new Quaternion(0,0,0,1);}
public class Transform{public Transform Parent;public Vector3 localScale{set{Verify.Trace.Add("scale:"+Verify.V(value));}}public Vector3 localPosition{set{Verify.Trace.Add("pos:"+Verify.V(value));}}public Quaternion localRotation{set{Verify.Trace.Add("rot:"+Verify.B(value.x)+":"+Verify.B(value.y)+":"+Verify.B(value.z)+":"+Verify.B(value.w));}}public void SetParent(Transform t){Parent=t;Verify.Trace.Add("parent");}}
public class Renderer{public int Id;int layer,order;public int sortingLayerID{get{return layer;}set{layer=value;Verify.Trace.Add("L"+Id+":"+value);}}public int sortingOrder{get{return order;}set{order=value;Verify.Trace.Add("O"+Id+":"+value);}}}
public class SpriteRenderer:Renderer{}
public class ParticleSystemRenderer:Renderer{}
public class ParticleSystem{public int Id;public ParticleSystemRenderer Renderer;public void Play(){Verify.Trace.Add("play:"+Id);}public T GetComponent<T>() where T:class{Verify.Trace.Add("get-renderer");return Renderer as T;}}
public class MonoBehaviour{public Transform transform=new Transform();public Renderer[] Renderers;public T[] GetComponentsInChildren<T>(){Verify.Trace.Add("get-renderers");return Renderers as T[];}}
}
namespace Whitebox.Foundation{
public static class OriginalParticleContext{public static OriginalParticleRecycleScheduler RecycleScheduler;}
public abstract class OriginalItemParticles:UnityEngine.MonoBehaviour{protected virtual void Awake(){Verify.Trace.Add("base");}public void RunAwake(){Awake();}public abstract int GetPoolId();public virtual void RecycleSelf(){Verify.Trace.Add("recycle");}public virtual void CancelScheduledActions(){} }
public abstract class OriginalOptimizedItemParticles:OriginalItemParticles{protected override void Awake(){Verify.Trace.Add("optimized");}}
public struct OriginalSwapSorting{public int Layer,Order;public bool SortEverything;public OriginalSwapSorting(int l,int o,bool s){Layer=l;Order=o;SortEverything=s;}}
}
class Verify{
public static List<string> Trace=new List<string>();public static string B(float v)=>BitConverter.ToUInt32(BitConverter.GetBytes(v),0).ToString();public static string V(UnityEngine.Vector3 v)=>B(v.x)+":"+B(v.y)+":"+B(v.z);
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');Trace.Clear();bool error=false;var schedule=new OriginalParticleRecycleScheduler();OriginalParticleContext.RecycleScheduler=schedule;UnityEngine.Time.time=20f;
if(a[0]=="F"){var flash=new OriginalSwapParticles();flash.circleParticle=a[1]=="1"?null:new UnityEngine.ParticleSystem{Renderer=new UnityEngine.ParticleSystemRenderer{Id=0}};try{flash.RunAwake();}catch(NullReferenceException){error=true;}Console.WriteLine(string.Join("|",Trace)+"#"+(error?"E":"OK"));continue;}
var p=new OriginalMatchItemSwapParticles();var systems=new UnityEngine.ParticleSystem[4];for(int i=0;i<4;i++)systems[i]=int.Parse(a[1])==i?null:new UnityEngine.ParticleSystem{Id=i};p.topParticles=systems[0];p.bottomParticles=systems[1];p.leftParticles=systems[2];p.rightParticles=systems[3];p.Renderers=new UnityEngine.Renderer[]{new UnityEngine.Renderer{Id=0},new UnityEngine.Renderer{Id=1},new UnityEngine.Renderer{Id=2}};
if(a[0]=="A"){p.RunAwake();Console.WriteLine(string.Join("|",Trace));continue;}
p.RunAwake();Trace.Clear();try{switch(a[0]){case "I":p.Init(new UnityEngine.Transform());break;case "G":var selected=p.GetParticleForDirection(int.Parse(a[2]));Trace.Add(selected==null?"null":selected.Id.ToString());break;case "P":p.Play(int.Parse(a[2]));schedule.Tick(20.999f);Trace.Add("before");schedule.Tick(21f);break;case "S":p.UpdateSorting(new OriginalSwapSorting(int.Parse(a[2]),int.Parse(a[3]),a[4]=="1"));break;case "C":p.Play(1);p.CancelScheduledActions();schedule.Tick(21f);break;}}
catch(NullReferenceException){error=true;}Console.WriteLine(string.Join("|",Trace)+"#"+(error?"E":"OK"));}}}
