using System;
using System.Collections.Generic;
using Whitebox.Foundation;
class Unit : IOriginalGameContextUnit, IOriginalContextBehaviour
{
    public int Key, Kind = -1;
    public int Id => Key;
    public void Bind() { Verify.Record("bind,"+Key); }
    public void ManualUpdate() { Verify.Record("update,"+Key); }
    public void Clear(bool exit)
    {
        Verify.Record("clear,"+Key+","+(exit?1:0));
        if(Key==2 && Verify.Mutate) OriginalLevelContext.GetAllUnits()[16]=new Unit { Key=99 };
    }
}
class RandomUnit : Unit, IOriginalLevelRandomContextUnit
{ public void ResetSeed() { Verify.Record("seed"); } }
class Win : Unit, IOriginalWinFailContextUnit { }
class Story : Win, IOriginalStoryWinFailContextUnit { }
class Water : Win, IOriginalWaterWinFailContextUnit { }
class Table : Win, IOriginalTableWinFailContextUnit { }
class Late : Unit, IOriginalLateContextUnit
{ public void OnRemove() { Verify.Record("remove,"+Key); } }
class Clock : IOriginalLevelContextClock
{
    public int Serial;
    public int FrameCount => Serial;
    public float DeltaTime => .001f;
    public float Time => 0;
    public float TimeScale => 1;
    public void SetTimeScale(float value) { Verify.Record("scale"); }
}
class Factory : IOriginalLevelContextFactory
{
    public int Clocks;
    public bool IsTableGameLevel { get { Verify.Record("table"); return (Verify.Flags&1)!=0; } }
    public bool IsStoryLevel { get { Verify.Record("story"); return (Verify.Flags&2)!=0; } }
    public bool IsWaterLevel { get { Verify.Record("water"); return (Verify.Flags&4)!=0; } }
    public IOriginalGameContextUnit CreatePermanentUnit(int id)
    {
        if(OriginalLevelContext.Current==null || OriginalLevelContext.Current.Container==null) throw new Exception("Unpublished ctor");
        Verify.Constructed.Add(id);
        return id==0 ? new RandomUnit {Key=id} : new Unit {Key=id};
    }
    public IOriginalLevelContextClock CreateDeltaTimeManager()
    { Verify.Record("clock"); return new Clock {Serial=++Clocks}; }
    public IOriginalGameContextUnit CreateWinFailManager(OriginalWinFailContextKind kind)
    { Verify.Record("win,"+(int)kind);return Make((int)kind); }
    public static Unit Make(int kind)
    {
        Unit unit;
        switch(kind) { case 0:unit=new Win();break;case 1:unit=new Story();break;case 2:unit=new Water();break;case 3:unit=new Table();break;default:unit=new Unit();break; }
        unit.Key=16;unit.Kind=kind;return unit;
    }
}
class Verify
{
    public static List<int> Constructed=new List<int>();
    public static bool Mutate;
    public static int Flags;
    static int fail,calls;
    static List<string> trace=new List<string>();
    public static void Record(string s) { trace.Add(s);if(++calls==fail)throw new Exception("Injected"); }
    static string Slot(OriginalLevelContext c,int id) { var unit=(Unit)c.Container.GetAllUnits()[id];return unit==null?"-":unit.Key+","+unit.Kind; }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var rows=line.Split(';');fail=0;Constructed.Clear();var factory=new Factory();var context=new OriginalLevelContext(factory);
            if(Constructed.Count!=OriginalLevelUnitRegistration.PermanentUnitCount)throw new Exception("Ctor count");
            for(int i=0;i<Constructed.Count;i++)if(Constructed[i]!=OriginalLevelUnitRegistration.GetPermanentUnitId(i))throw new Exception("Ctor order");
            // Reset static activity through the actual lifecycle; then supply
            // explicit small unit fixtures for native lifecycle comparisons.
            context.Clear(false);context.Container.Clear();context.Container.Add(new RandomUnit {Key=0});context.Container.Add(new Unit {Key=2});
            context.Container.Add(new Late {Key=75,Kind=4});
            var output=new List<string>();
            foreach(var command in rows)
            {
                var r=Array.ConvertAll(command.Split(','),int.Parse);Flags=r[2];fail=r[3];calls=0;trace.Clear();Mutate=r[4]!=0;string result="-";
                try
                {
                    switch(r[0])
                    {
                        case 0:context.Activate(r[1]!=0);break;
                        case 1:context.Clear(r[1]!=0);break;
                        case 2:context.Container.GetAllUnits()[16]=r[1]==-2?null:Factory.Make(r[1]);break;
                        case 3:context.ManualUpdate();break;
                        case 4:context.RemoveLateUnits();break;
                    }
                }
                catch(Exception){result="error";}
                int serial=0;try{serial=OriginalLevelContext.FrameCount;}catch(NullReferenceException){}
                output.Add(result+":"+(OriginalLevelContext.IsActive?1:0)+":"+serial+":"+Slot(context,16)+":"+Slot(context,75)+":"+string.Join("/",trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
