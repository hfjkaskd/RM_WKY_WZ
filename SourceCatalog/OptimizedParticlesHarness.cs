// Supplied Unity engine/storage boundary for verify_optimized_particles.py.
// Production methods are compiled unchanged alongside this standalone harness.
using System;
using System.Collections.Generic;
using UnityEngine;
using Whitebox.Foundation;
namespace UnityEngine
{
    public class GameObject { public bool activeSelf = true, activeInHierarchy = true; }
    public class Transform
    {
        public GameObject gameObject = new GameObject();
        public Transform transform => this;
        public Vector3 position, eulerAngles, lossyScale = Vector3.one, localScale = Vector3.one;
    }
    public class MonoBehaviour { }
    public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 one => new Vector3(1,1,1);
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator /(Vector3 a,float b) => new Vector3(a.x/b,a.y/b,a.z/b);
    }
    public static class Time { public static float deltaTime; }
    public static class Random
    {
        public static Queue<float> Values = new Queue<float>();
        public static float Range(float a,float b) { return Values.Dequeue(); }
        public static int Range(int a,int b) { return (int)Values.Dequeue(); }
    }
    public enum ParticleSystemScalingMode { Hierarchy, Local, Shape }
    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemCurveMode { Constant, Curve, TwoCurves, TwoConstants }
    public class ParticleSystem
    {
        public Transform transform = new Transform();
        public MainModule main => new MainModule { P = this };
        public EmissionModule emission => new EmissionModule { P = this };
        public ParticleSystemScalingMode Scaling;
        public MinMaxCurve Size, X, Y, Delay, Rate, Distance, BurstCount;
        public bool Loop;
        public int Bursts, Count, Max = int.MaxValue;
        public short Min, MaxBurst;
        public float Duration;
        public bool isPlaying => false;
        public uint randomSeed { set { } }
        public int particleCount => Count;
        public void Emit(EmitParams emit,int amount) { Verify.Emits.Add(amount); }
        public int GetParticles(Particle[] p) { return 0; }
        public void SetParticles(Particle[] p,int count) { }
        public struct Particle { public uint randomSeed; public Vector3 position, startSize3D; public float remainingLifetime; }
        public struct EmitParams { public Vector3 position, startSize3D; public bool applyShapeToPosition; }
        public struct MainModule
        {
            public ParticleSystem P;
            public ParticleSystemScalingMode scalingMode => P.Scaling;
            public ParticleSystemSimulationSpace simulationSpace => ParticleSystemSimulationSpace.World;
            public MinMaxCurve startSize => P.Size;
            public MinMaxCurve startSizeX => P.X;
            public MinMaxCurve startSizeY => P.Y;
            public MinMaxCurve startDelay => P.Delay;
            public bool loop => P.Loop;
            public float duration => P.Duration;
            public int maxParticles { get { return P.Max; } set { P.Max=value; } }
        }
        public struct EmissionModule
        {
            public ParticleSystem P;
            public int burstCount => P.Bursts;
            public MinMaxCurve rateOverTime => P.Rate;
            public MinMaxCurve rateOverDistance => P.Distance;
            public Burst GetBurst(int index) => new Burst { count=P.BurstCount, minCount=P.Min, maxCount=P.MaxBurst };
        }
        public struct Burst { public MinMaxCurve count; public short minCount, maxCount; }
        public struct MinMaxCurve
        {
            public ParticleSystemCurveMode mode;
            public float constant, constantMin, constantMax;
            public object curve;
            public float Evaluate(float time) { Verify.Evaluations.Add(Verify.Bits(time)); return constant; }
        }
    }
}
namespace Whitebox.Foundation
{
    public abstract class OriginalItemParticles : MonoBehaviour
    {
        protected virtual void Awake() { }
        public abstract int GetPoolId();
        public virtual void OnRecycle() { }
    }
    public class OriginalAutoShadowEmitParticles
    {
        public ParticleSystem system;
        public void Init() { }
        public void EmitShadows(Vector3 p,int amount) { }
    }
    public static class OriginalLevelClock { public static float QuantizeDelta(float value) => value; }
}
class Fixture : OriginalOptimizedItemParticles
{
    private DelayedEmitParticle entry;
    public override int GetPoolId() => 5;
    public void Initialize() { particleInfoLayers = new OriginalParticleInfoLayer[0]; base.Awake(); }
    public string Size(ParticleSystem system,Transform master,Vector3 previous,Vector3 multiplier)
    {
        masterLastScales[master]=previous;
        entry=new DelayedEmitParticle(new OriginalParticleInfo { particle=system },new ParticleSystem.EmitParams(),Vector3.zero,master);
        entry.scaleMultiplier=multiplier;
        var result=SetStartSize3D(system.main,master,entry);
        return Verify.Point(result)+","+Verify.Point(entry.scaleMultiplier);
    }
    public void Entry(ParticleSystem system,float remaining,float delay,float accumulator,Vector3 velocity)
    {
        entry=new DelayedEmitParticle(new OriginalParticleInfo { particle=system },new ParticleSystem.EmitParams(),Vector3.zero,null);
        entry.remainingTime=remaining; entry.startRemainingTime=delay; entry.emitAccumulator=accumulator; entry.velocity=velocity;
        delayedEmitParticles.Add(entry);
    }
    public string Rate() { EmitRateOverTimeAndDistanceParticles(entry); return Verify.Bits(entry.emitAccumulator); }
    public string Step()
    {
        CheckAndEmitDelayedEmitParticles();
        return delayedEmitParticles.Count+","+Verify.Bits(entry.remainingTime)+","+Verify.Bits(entry.startRemainingTime)+","+Verify.Bits(entry.emitAccumulator);
    }
    public uint Seed(uint seed,uint[] occupied)
    {
        for(int i=0;i<occupied.Length;i++) particleMasters.Add(occupied[i],null);
        return GetParticleRandomSeed(seed);
    }
}
class Verify
{
    public static List<int> Emits=new List<int>();
    public static List<string> Evaluations=new List<string>();
    public static float Float(string s) => BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);
    public static string Bits(float f) => float.IsNaN(f)?"nan":BitConverter.ToUInt32(BitConverter.GetBytes(f),0).ToString();
    public static string Point(Vector3 v) => Bits(v.x)+","+Bits(v.y)+","+Bits(v.z);
    static Vector3 Point(string[] a,int i) => new Vector3(Float(a[i]),Float(a[i+1]),Float(a[i+2]));
    static ParticleSystem.MinMaxCurve Curve(float value,int mode=0,bool curve=false) => new ParticleSystem.MinMaxCurve { constant=value,mode=(ParticleSystemCurveMode)mode,curve=curve?new object():null };
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var a=line.Split(','); var fixture=new Fixture(); fixture.Initialize(); Emits.Clear();Evaluations.Clear();UnityEngine.Random.Values.Clear();
            if(a[0]=="S")
            {
                var system=new ParticleSystem { Scaling=(ParticleSystemScalingMode)int.Parse(a[1]),Size=Curve(0,int.Parse(a[2])),X=Curve(Float(a[12])),Y=Curve(Float(a[13])) };
                UnityEngine.Random.Values.Enqueue(Float(a[14]));UnityEngine.Random.Values.Enqueue(Float(a[15]));
                Console.WriteLine(fixture.Size(system,new Transform { lossyScale=Point(a,3) },Point(a,6),Point(a,9)));
            }
            else if(a[0]=="R" || a[0]=="D")
            {
                var system=new ParticleSystem { Scaling=ParticleSystemScalingMode.Local, Duration=Float(a[1]),Rate=Curve(Float(a[2]),0,a[3]=="1"),Distance=Curve(Float(a[4]),0,a[5]=="1"),Loop=a[13]=="1",Bursts=int.Parse(a[14]),BurstCount=Curve(Float(a[15])) };
                Time.deltaTime=Float(a[6]);fixture.Entry(system,Float(a[7]),Float(a[8]),Float(a[9]),Point(a,10));
                int frames=a[0]=="D"?12:1;
                for(int i=0;i<frames;i++)
                {
                    Emits.Clear();Evaluations.Clear();
                    string state=a[0]=="D"?fixture.Step():fixture.Rate();
                    Console.WriteLine(state+";"+string.Join(",",Emits)+";"+string.Join(",",Evaluations));
                }
            }
            else
            {
                int count=int.Parse(a[2]);var occupied=new uint[count];for(int i=0;i<count;i++)occupied[i]=uint.Parse(a[3+i]);
                for(int i=count+3;i<a.Length;i++)UnityEngine.Random.Values.Enqueue(Float(a[i]));
                Console.WriteLine(fixture.Seed(uint.Parse(a[1]),occupied));
            }
        }
    }
}
