using System;using System.Collections.Generic;using System.Globalization;using Whitebox.Foundation;
namespace Whitebox.Foundation{
public struct OriginalCellPoint{public int X,Y;public OriginalCellPoint(int x,int y){X=x;Y=y;}}
public struct OriginalSwapPoint{public float X,Y,Z;public OriginalSwapPoint(float x,float y,float z){X=x;Y=y;Z=z;}}
public struct OriginalSwapSorting{public int Layer,Order;public bool SortEverything;public OriginalSwapSorting(int l,int o,bool s){Layer=l;Order=o;SortEverything=s;}}
public interface IOriginalSwapAnimationView{void SwapAnimationStarted(bool valid,int other);void SwapAnimationEnded();}
public class OriginalGridCell{public int X,Y;public CellData Mediation=new CellData();}
public class CellData{public OriginalBoardItem Item;public OriginalBoardItem CurrentItem{get{Verify.Trace.Add("item");return Item;}}}
public class OriginalBoardItem{public ItemData Mediator=new ItemData();}
public class ItemData{public bool Has;public object CurrentCell{get{Verify.Trace.Add("has:"+(Has?1:0));return Has?this:null;}}}
public class OriginalSwapDecision{public void Clear(){}public bool ShouldPlaySwapAnimation(OriginalGridCell a,OriginalGridCell b,bool c){throw new Exception();}}
}
class Verify:IOriginalImpossibleSwapServices,IOriginalSwapAnimationView,IOriginalLocalPositionTarget{
public static List<string> Trace=new List<string>();public OriginalSwapActionState Action;public OriginalSwapPoint Origin,Scale;public bool StaticExists,Swapping;public List<Action> Pending=new List<Action>();
public OriginalSwapPoint LocalPosition{get;set;}
public static uint Bits(float x){return BitConverter.ToUInt32(BitConverter.GetBytes(x),0);}static float Float(string s){return BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);}
static string V(OriginalSwapPoint v){return Bits(v.X)+":"+Bits(v.Y)+":"+Bits(v.Z);}
public void PlaySfx(int id,float delay){Trace.Add("sfx:"+id+":"+Bits(delay));}
public void PlayHaptic(int id,int type,float strength,float duration,bool replay){Trace.Add("haptic:"+id+":"+type+":"+Bits(strength)+":"+Bits(duration)+":"+(replay?1:0));}
public IOriginalSwapAnimationView GetView(OriginalBoardItem item){Trace.Add("view");if(item==null)throw new NullReferenceException();return this;}
public OriginalSwapPoint GetCellLocalPosition(OriginalGridCell cell){Trace.Add("origin");return Origin;}
public OriginalSwapPoint GetLocalScale(IOriginalSwapAnimationView view){Trace.Add("scale-get");return Scale;}
public OriginalSwapSorting GetSwapStartSorting(){Trace.Add("sort-get");return new OriginalSwapSorting(17,9005,true);}
public void SetSorting(IOriginalSwapAnimationView view,OriginalSwapSorting sort){Trace.Add("sort:"+sort.Layer+":"+sort.Order+":"+(sort.SortEverything?1:0));}
public void ArrangeSelfSorting(IOriginalSwapAnimationView view){Trace.Add("arrange");}
public void MoveToLocal(IOriginalSwapAnimationView view,OriginalSwapPoint end,float duration,int ease,Action done){Trace.Add("move:"+V(end)+":"+Bits(duration)+":"+ease);Pending.Add(done);}
public void ScaleToLocal(IOriginalSwapAnimationView view,OriginalSwapPoint end,float duration,int ease,Action done){Trace.Add("scale:"+V(end)+":"+Bits(duration)+":"+ease);Pending.Add(done);}
public void TryPlayStaticInvalidMoveAnimation(OriginalCellPoint p){Trace.Add("grid:"+p.X+":"+p.Y);if(StaticExists)Trace.Add("static");}
public void SwapAnimationStarted(bool valid,int other){Swapping=true;Trace.Add("begin:"+(valid?1:0)+":"+other+":"+(Action.IsSwapping?1:0));}
public void SwapAnimationEnded(){Trace.Add("end:"+(Action.IsSwapping?1:0));Swapping=false;}
static void Main(){CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;string line;while((line=Console.ReadLine())!=null){Trace.Clear();var a=line.Split(',');
if(a[0]=="E"){var v=new Verify();float dt=Float(a[2]);var tween=new OriginalLocalMoveTween(()=>dt);tween.Configure(v,new OriginalSwapPoint(1,0,0),Float(a[3]),null,int.Parse(a[1]));tween.MoveNext();Console.WriteLine(Bits(v.LocalPosition.X));continue;}
var service=new Verify{Origin=new OriginalSwapPoint(Float(a[5]),Float(a[6]),Float(a[7])),Scale=new OriginalSwapPoint(Float(a[8]),Float(a[9]),Float(a[10])),StaticExists=a[12]=="1"};
var cell=new OriginalGridCell{X=int.Parse(a[1]),Y=int.Parse(a[2])};var item=new OriginalBoardItem();cell.Mediation.Item=item;item.Mediator.Has=a[11]=="1";
service.Action=new OriginalSwapActionState(new OriginalSwapDecision());int events=0;service.Action.OnSwapAnimationStart+=()=>events++;service.Action.OnSwapAnimationEnd+=n=>events++;
var animation=new OriginalImpossibleSwapAnimation(service.Action,cell,new OriginalCellPoint(int.Parse(a[3]),int.Parse(a[4])),service);animation.Play();
for(int i=0;i<service.Pending.Count;i++){service.Pending[i]?.Invoke();}
Console.WriteLine(string.Join("|",Trace)+"#"+(service.Action.IsSwapping?1:0)+","+(service.Action.From==cell?1:0)+","+(service.Action.To==null?1:0)+","+(service.Swapping?1:0)+","+events);
}}}
