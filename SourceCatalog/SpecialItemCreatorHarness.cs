using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine
{
    public abstract class CustomYieldInstruction { public abstract bool keepWaiting { get; } }
    public class Renderer { public int sortingLayerID, sortingOrder; }
    public class SpriteRenderer : Renderer { }
    public class Transform { }
}
namespace Whitebox.Foundation
{
    public class OriginalGridCell
    {
        public int Id, X, Y, Freeze, Spread;
        public void FreezeFall() { Freeze++; Verify.Trace.Add("freeze:"+Id); }
        public void UnFreezeFall() { Freeze--; Verify.Trace.Add("unfreeze:"+Id); }
    }
    public class OriginalItemMediation { public CellSlot CurrentCell; }
    public class CellSlot { public OriginalGridCell Owner; }
    public class OriginalBoardItem
    {
        public int Id;
        public int ItemType { get; set; }
        public OriginalItemMediation Mediator = new OriginalItemMediation();
        public OriginalBoardItem(int id,int type) { Id=id; ItemType=type; }
        public virtual bool IsSwappable => false;
        public virtual bool IsSpecialItem => false;
        public virtual bool CanExplodeWithNearMatch => false;
    }
    public class OriginalMatchBoardItem : OriginalBoardItem
    {
        public byte NativeMatchGroupId;
        public OriginalMatchBoardItem() : base(0,1) { }
    }
    public struct OriginalSwapSorting
    {
        public int Layer,Order; public bool SortEverything;
        public OriginalSwapSorting(int layer,int order,bool all) { Layer=layer;Order=order;SortEverything=all; }
    }
    public struct OriginalSwapPoint { public float X,Y,Z;public OriginalSwapPoint(float x,float y,float z) { X=x;Y=y;Z=z; } }
    public class OriginalSpecialItemCreationParticles
    {
        public List<int> GetParticles(UnityEngine.Transform owner) { Verify.Trace.Add("snapshot");return new List<int>(); }
        public void StopParticles(UnityEngine.Transform owner,int layer,bool clear) { Verify.Trace.Add("clear:"+layer+":"+(clear?1:0)); }
    }
    public class OriginalSpecialItemCreationParticlesBack
    {
        public void Init(UnityEngine.Transform owner) { Verify.Trace.Add("back-init"); }
        public void PlayParticles(List<int> particles) { Verify.Trace.Add("back-play"); }
    }
    public class OriginalGameObjectPool
    {
        public T Spawn<T>(int id,bool active) where T:class
        {
            Verify.Trace.Add("spawn:"+id+":"+(active?1:0));
            return new OriginalSpecialItemCreationParticlesBack() as T;
        }
    }
}
class Model : OriginalSpecialBoardItem
{
    public Model(Services services) : base(1,10,services.ReadTime,services) { }
    public override IOriginalSpecialCreationView View
    {
        get
        {
            Verify.Gets++;
            return Verify.Gets==Verify.MissingView ? null : Verify.View;
        }
    }
}
class WrongModel : IOriginalSpecialCreationItem
{
    public int ItemType => Verify.Model.ItemType;
    public OriginalGridCell CurrentCell => Verify.Model.CurrentCell;
    public IOriginalSpecialCreationView View => Verify.View;
    public void SetSpecialItemActive(bool active) { throw new Exception("Wrong model accepted"); }
    public bool IsUnderNonFallBlockingAboveStaticItem() { throw new Exception("Wrong model accepted"); }
    public bool IsUnderNonFallBlockingAboveStaticItemSpreading() { throw new Exception("Wrong model accepted"); }
}
class View : IOriginalSpecialCreationView
{
    public bool Active=true;
    public void SetGameObjectActive(bool active) { Active=active; Verify.Trace.Add("active:"+(active?1:0)); }
    public void UpdateSorting(OriginalSwapSorting sorting) { Verify.Trace.Add("sort:"+sorting.Layer+":"+sorting.Order+":"+(sorting.SortEverything?1:0)); }
    public void PlayCreationAnimation(float start,bool sound)
    {
        Verify.Trace.Add("animation:"+Verify.Bits(start)+":"+(sound?1:0));
        if(Verify.RemoveOnAnimation) Verify.Model.Mediator.CurrentCell=null;
    }
    public void SetIsUnderNonFallBlockingAboveStaticItem(bool under) { Verify.Trace.Add("under:"+(under?1:0)); }
    public void SetCreationParticles(OriginalSpecialItemCreationParticles particles,bool above) { Verify.Trace.Add("particles:"+(above?1:0)); }
}
class Services : IOriginalSpecialItemCreationServices,IOriginalSpecialItemStaticState
{
    public IEnumerator Routine;
    public int Layer;public bool Playing,Started,FactoryNull,FactoryWrong,Honey,Roof;public sbyte Difficulty;
    public float LevelTime,Delta;
    public IOriginalSpecialCreationItem CreateItemAtCell(int tiledId,OriginalGridCell cell)
    {
        Verify.Trace.Add("create:"+tiledId+":"+cell.Id);
        if(FactoryNull)return null;
        Verify.Model.ItemType=OriginalTiledItemTypes.AsItemType(tiledId);
        Verify.Model.Mediator.CurrentCell=new CellSlot { Owner=cell };
        if(FactoryWrong)return new WrongModel();
        return Verify.Model;
    }
    public void ManualStartCoroutine(IEnumerator routine)
    {
        Routine=routine; Verify.Trace.Add("start");
        bool moved=routine.MoveNext();Verify.Trace.Add("initial:"+(moved?1:0)+":"+(routine.Current is OriginalGameplayWait?"w":"-"));
    }
    public object CreateGameplayWait(float duration) { Verify.Trace.Add("wait:"+Verify.Bits(duration));return new OriginalGameplayWait(duration,()=>Delta); }
    public int ItemSortingLayer => Layer;
    public bool IsPlaying => Playing;
    public bool IsSessionStarted => Started;
    public sbyte CurrentLevelDifficulty => Difficulty;
    public float ReadTime() { Verify.Trace.Add("time");return LevelTime; }
    public bool IsUnderHoney(OriginalBoardItem item) { Verify.Trace.Add("honey");return Honey; }
    public bool IsUnderRoof(OriginalBoardItem item) { Verify.Trace.Add("roof");return Roof; }
    public bool IsAboveStaticSpreading(OriginalGridCell cell) { Verify.Trace.Add("above:"+cell.Id);return cell.Spread>0; }
    public void IncrementItemCreation(int type,bool auto) { Verify.Trace.Add("increment:"+type+":"+(auto?1:0)); }
    public void SpecialItemCreated(IOriginalSpecialCreationItem model,OriginalGridCell cell,int delay,int count,bool sticker)
    {
        Verify.Trace.Add("position:"+cell.Id);Verify.Trace.Add("record:"+delay+":"+count+":"+(sticker?1:0));
    }
    public void TryCollectMadness(OriginalGridCell cell,int type,int count,int delay,IOriginalSpecialCreationItem model,bool remaining)
    {
        Verify.Trace.Add("position:"+cell.Id);Verify.Trace.Add("madness:"+type+":"+count+":"+delay+":"+(remaining?1:0));
    }
    public void AddMissionEvent(int type,sbyte difficulty,int count) { Verify.Trace.Add("mission:"+type+":"+difficulty+":"+count); }
    public void UsefulMoveMade() { Verify.Trace.Add("useful"); }
    public void ReportMissingCreationCell() { Verify.Trace.Add("missing-cell"); }
    public OriginalSpecialItemCreationParticles PlayAppearParticle(IOriginalSpecialCreationView view)
    {
        Verify.Trace.Add("appear");return Verify.Particles;
    }
}
class Verify
{
    public static List<string> Trace=new List<string>();
    public static Model Model;public static View View;public static OriginalSpecialItemCreationParticles Particles=new OriginalSpecialItemCreationParticles();
    public static int Gets,MissingView;public static bool RemoveOnAnimation;
    public static float Float(string value) => BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(value)),0);
    public static string Bits(float f) => float.IsNaN(f)?"nan":BitConverter.ToUInt32(BitConverter.GetBytes(f),0).ToString();
    static string Snapshot(string value,OriginalGridCell[] cells,string fault)
    {
        var freezes=new string[cells.Length];for(int i=0;i<cells.Length;i++)freezes[i]=cells[i].Freeze.ToString();
        return value+","+(Model.IsActive?1:0)+","+(View.Active?1:0)+","+Gets+","+string.Join(":",freezes)+","+fault+";"+string.Join("|",Trace);
    }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var a=line.Split(',');Trace.Clear();Gets=0;MissingView=0;RemoveOnAnimation=false;
            if(a[0]=="P") { Console.WriteLine(OriginalSpecialItemCreator.PatternToTiledId(int.Parse(a[1])));continue; }
            if(a[0]=="W")
            {
                float delta=0;var wait=new OriginalGameplayWait(Float(a[1]),()=>delta);
                for(int i=2;i<a.Length;i++) { delta=Float(a[i]);Console.WriteLine(wait.keepWaiting?"1":"0"); }
                continue;
            }
            if(a[0]=="T")
            {
                var transfer=new OriginalSpecialCreationParticleTransfer(new UnityEngine.Transform(),new OriginalGameObjectPool());
                var source=a[1]=="1"?Particles:null;
                transfer.SetCreationParticles(source,a[2]=="1",a[3]=="1");
                Console.WriteLine((transfer.CreationParticles==null?0:1)+";"+string.Join("|",Trace));
                transfer.DecreaseCreationParticleSorting();
                Console.WriteLine((transfer.CreationParticles==null?0:1)+";"+string.Join("|",Trace));
                transfer.DecreaseCreationParticleSorting();
                Console.WriteLine((transfer.CreationParticles==null?0:1)+";"+string.Join("|",Trace));
                continue;
            }
            if(a[0]=="A")
            {
                var activationServices=new Services();Model=new Model(activationServices);
                for(int i=1;i<a.Length;i+=2)
                {
                    activationServices.LevelTime=Float(a[i]);int operation=int.Parse(a[i+1]);
                    if(operation<2)Model.SetSpecialItemActive(operation==1);
                    bool active=Model.IsActive,swappable=Model.IsSwappable,special=Model.IsSpecialItem;
                    float duration=Model.GetActiveDuration();
                    Console.WriteLine((active?1:0)+","+(swappable?1:0)+","+(special?1:0)+","+(Model.CanExplodeWithNearMatch?1:0)+","+Bits(duration)+";"+string.Join("|",Trace));
                }
                continue;
            }
            int pattern=int.Parse(a[1]),count=int.Parse(a[2]),missing=int.Parse(a[9]);
            var services=new Services { Playing=a[3]=="1",Started=a[4]=="1",FactoryNull=a[5]=="1",FactoryWrong=a[22]=="1",Honey=a[6]=="1",Roof=a[7]=="1",Layer=int.Parse(a[10]),Difficulty=sbyte.Parse(a[11]),LevelTime=Float(a[12]) };
            MissingView=missing;RemoveOnAnimation=a[13]=="1";Model=new Model(services);View=new View();
            var cells=new OriginalGridCell[4];for(int i=0;i<4;i++)cells[i]=new OriginalGridCell { Id=i,X=i,Y=i+2,Spread=i==0?int.Parse(a[8]):0 };
            var group=new OriginalMatchGroupSnapshot(1,pattern,3);
            for(int i=0;i<count;i++)group.Cells.Add(cells[i%4]);
            string fault="";
            try { new OriginalSpecialItemCreator(services,new OriginalMatchMergeSettings { Duration=Float(a[14]),ExplodeCompleteRatio=Float(a[15]) }).CreateSpecialItemForMatchGroup(group,cells[0],a[16]=="1"); }
            catch(NullReferenceException) { fault="null"; }
            catch(InvalidCastException) { fault="cast"; }
            Console.WriteLine(Snapshot("created",cells,fault));
            // Reset the caller's value after dispatch, verifying the inline copy.
            group.Cells=default(OriginalMatchCells);group.Reset();
            for(int step=0;step<7;step++)
            {
                string value="-";
                if(services.Routine!=null && fault=="")
                {
                    try
                    {
                        if(step<4)
                        {
                            services.Delta=Float(a[17+step]);var wait=services.Routine.Current as OriginalGameplayWait;
                            value=wait!=null?(wait.keepWaiting?"w1":"w0"):"-";
                        }
                        else
                        {
                            if(step==4 && a[21]=="1")Model.Mediator.CurrentCell=null;
                            bool moved=services.Routine.MoveNext();value=(moved?"1":"0")+":"+(services.Routine.Current==null?"-":"w");
                        }
                    }
                    catch(NullReferenceException) { fault="null"; }
                }
                Console.WriteLine(Snapshot(value,cells,fault));
            }
        }
    }
}
