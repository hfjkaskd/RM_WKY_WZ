using System;using System.Collections;using System.Collections.Generic;using UnityEngine;using Whitebox.Foundation;
namespace UnityEngine{
public class MonoBehaviour{public void StartCoroutine(IEnumerator e){Verify.Trace.Add("start");Verify.Routines.Add(e);}}
public struct Color32{public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
public class Transform{public Vector3 eulerAngles,lossyScale,localScale;}
public enum ParticleSystemSimulationSpace{Local,World,Custom}public enum ParticleSystemScalingMode{Hierarchy,Local,Shape}public enum ParticleSystemCurveMode{Constant,Curve,TwoCurves,TwoConstants}
public static class Random{public static float Range(float min,float max)=>Verify.Seed;public static int Range(int min,int max){Verify.Trace.Add("range:"+min+":"+max);return min;}}
public class ParticleSystem{
public int Id;public int Max,Count,Bursts;public float Amount,Delay;public short Low,High;public bool Playing;public ParticleSystemCurveMode Mode;public Transform transform=new Transform();
public bool isPlaying=>Playing;public int particleCount=>Count;public uint randomSeed{set{Verify.Trace.Add("seed:"+Id+":"+value);}}
public MainModule main=>new MainModule{P=this};public EmissionModule emission=>new EmissionModule{P=this};
public void Emit(EmitParams emit,int count){Verify.Trace.Add("emit:"+Id+":"+count+":"+Verify.Vec(emit.position)+":"+(emit.applyShapeToPosition?1:0));}
public struct EmitParams{public Vector3 position;public bool applyShapeToPosition;}
public struct MainModule{public ParticleSystem P;public int maxParticles{get{return P.Max;}set{P.Max=value;Verify.Trace.Add("max:"+P.Id+":"+value);}}public ParticleSystemSimulationSpace simulationSpace=>ParticleSystemSimulationSpace.World;public ParticleSystemScalingMode scalingMode=>ParticleSystemScalingMode.Shape;public MinMaxCurve startDelay=>new MinMaxCurve{constant=P.Delay};}
public struct EmissionModule{public ParticleSystem P;public int burstCount=>P.Bursts;public Burst GetBurst(int i)=>new Burst{count=new MinMaxCurve{constant=P.Amount,mode=P.Mode},minCount=P.Low,maxCount=P.High};}
public struct MinMaxCurve{public float constant;public ParticleSystemCurveMode mode;}public struct Burst{public MinMaxCurve count;public short minCount,maxCount;}
}}
namespace Whitebox.Foundation{
public abstract partial class OriginalItemParticles:MonoBehaviour{protected readonly object waitDuration=Verify.Wait;protected virtual void Awake(){}public abstract int GetPoolId();}
public struct OriginalParticleInfo{public ParticleSystem particle;public OriginalAutoShadowEmitParticles shadow;}
public class OriginalAutoShadowEmitParticles{public int Id;public ParticleSystem system;public void Init(){Verify.Trace.Add("init:"+Id);}public void EmitShadows(Vector3 p,int count){Verify.Trace.Add("shadow:"+Id+":"+count+":"+Verify.Vec(p));}}
class Basic:OriginalItemParticles{public override int GetPoolId()=>0;public void Play(OriginalParticleInfo[] infos,Vector3 p){PlayParticles(infos,p);}}
}
class Verify{
public static List<string> Trace=new List<string>();public static List<IEnumerator> Routines=new List<IEnumerator>();public static readonly object Wait=new object();public static float Seed;
public static uint Bits(float f)=>BitConverter.ToUInt32(BitConverter.GetBytes(f),0);public static float Float(string s)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);public static string Vec(Vector3 v)=>Bits(v.x)+","+Bits(v.y)+","+Bits(v.z);
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');int layer=int.Parse(a[1]),bursts=int.Parse(a[2]);bool playing=a[3]=="1",shadow=a[4]=="1";int mode=int.Parse(a[5]),max=int.Parse(a[6]),count=int.Parse(a[7]);float amount=Float(a[8]),delay=Float(a[9]);Seed=Float(a[10]);short low=short.Parse(a[11]),high=short.Parse(a[12]);var position=new Vector3(Float(a[13]),Float(a[14]),Float(a[15]));var infos=new OriginalParticleInfo[4];
for(int i=0;i<4;i++){var p=new ParticleSystem{Id=i,Max=max,Count=count,Bursts=bursts,Playing=playing,Mode=(ParticleSystemCurveMode)mode,Amount=amount,Delay=delay,Low=low,High=high};infos[i]=new OriginalParticleInfo{particle=p,shadow=shadow&&i!=1?new OriginalAutoShadowEmitParticles{Id=i,system=new ParticleSystem{Id=10+i,Count=count+1,Max=max}}:null};}
Trace.Clear();Routines.Clear();if(a[0]=="P")new Basic().Play(new[]{infos[0],infos[1],infos[2]},position);else new OriginalBoxExplodeParticles{layer1=new[]{infos[0]},layer2=new[]{infos[1]},layer3=new[]{infos[2]},layer4=new[]{infos[3]}}.Play(layer,position);
for(int i=0;i<Routines.Count;i++){int waits=0;while(Routines[i].MoveNext()){if(!ReferenceEquals(Routines[i].Current,Wait))throw new Exception("Wrong shared wait object");if(++waits>120)throw new Exception("Delay did not finish");}Trace.Add("waits:"+waits);}
Console.WriteLine(string.Join("|",Trace));
}}}
