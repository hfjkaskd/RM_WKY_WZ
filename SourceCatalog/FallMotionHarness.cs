using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;
namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
namespace Whitebox.Foundation
{
    public sealed class OriginalGameOperations { public void OperationStart(int n) { } public void OperationFinish(int n) { } }
    public interface IOriginalMatchFreezeOperations { void FreezeFall(OriginalGridCell cell); }
    public interface IOriginalItemFallReadiness { bool IsItemSteady { get; } }
    public interface IOriginalFallUpdateComponent : IOriginalFillingFallComponent, IOriginalItemFallReadiness { void ManualUpdate(); }
    public struct OriginalSwapPoint { public float X,Y,Z; public OriginalSwapPoint(float x,float y,float z){X=x;Y=y;Z=z;} }
    public struct OriginalSwapSorting { public int Layer,Order;public bool SortEverything;public OriginalSwapSorting(int l,int o,bool a){Layer=l;Order=o;SortEverything=a;} }
    public sealed partial class OriginalFallComponentState { public float StateChangedTime {get;set;} }
}
static class FallMotionHarness
{
    static float Parse(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
    static string Bits(float f)=>BitConverter.ToUInt32(BitConverter.GetBytes(f),0).ToString("x8");
    sealed class View:IOriginalFallItemView
    {
        readonly Inputs f;readonly int id;public OriginalSwapPoint P;public bool Mask=true,Under;
        public View(Inputs f,int id){this.f=f;this.id=id;}
        public OriginalSwapPoint Position {get{f.Record("position,"+id);return P;}set{f.Record("set_position,"+id);P=value;}}
        public OriginalSwapPoint LocalPosition {get{f.Record("local,"+id);return new OriginalSwapPoint(P.X,P.Y+f.V[14],P.Z);}}
        public bool IsFillMaskEnabled=>Mask;
        public bool IsUnderNonFallBlockingAboveStaticItem=>Under;
        public void DisableFillMask(){f.Record("mask,"+id);Mask=false;}
        public void ArrangeSelfSorting(){f.Record("arrange,"+id);}
        public void UpdateSorting(OriginalSwapSorting s){f.Record("sorting,"+id);}
        public void UpdateFallState(int s,float previous,float max){f.Record("state,"+id+","+s);f.LastPrevious=previous;f.LastMax=max;}
        public void FallStarted(){f.Record("started,"+id);}
        public void FallEnded(){f.Record("ended,"+id);}
        public void FallAnimationStarted(){f.Record("animation,"+id);}
        public void SetIsUnderNonFallBlockingAboveStaticItem(bool under){f.Record("set_under,"+id+","+(under?1:0));Under=under;}
    }
    sealed class Inputs:IOriginalFallMotionServices
    {
        public int[] H;public float[] V;public OriginalGridCell[] Cells=new OriginalGridCell[12];
        public OriginalBoardItem[] Items=new OriginalBoardItem[2];public OriginalFallComponentState[] Falls=new OriginalFallComponentState[2];
        public View[] Views=new View[2];public List<string> Trace=new List<string>();public int Calls;public float LastPrevious,LastMax;
        public void Record(string s){Trace.Add(s);if(++Calls==H[16])throw new InvalidOperationException();}
        public int Id(OriginalGridCell c){if(c==null)return -1;return c.Y*3+c.X;}
        public OriginalGridCell Cell(int i)=>i<0?null:Cells[i];
        public float LevelTime {get{Record("time");return V[5];}}
        public float LevelDeltaTime {get{Record("delta");return V[6];}}
        public float MaxFallSpeed {get{if(H[19]!=0)throw new NullReferenceException();return H[18]!=0?28f:20f;}}
        public IOriginalFallItemView GetView(OriginalBoardItem item){Record("view,"+item.Id);return (H[14]&(1<<(item.Id-1)))!=0?null:Views[item.Id-1];}
        public OriginalSwapPoint GetItemPosition(OriginalBoardItem item){Record("item_pos,"+item.Id);return Views[item.Id-1].P;}
        public OriginalSwapPoint GetCellPosition(OriginalGridCell cell){Record("cell_pos,"+Id(cell));return new OriginalSwapPoint(cell.X,cell.Y,0);}
        public bool IsInCurrentBoard(OriginalGridCell cell){Record("board,"+Id(cell));return cell.BoardId==H[17];}
        public OriginalSwapSorting GetCrossSorting(OriginalBoardItem item){Record("cross_sort");return new OriginalSwapSorting(3,9010,false);}
        public bool HasNonFallBlockingAboveStaticItemInLevel(){if(H[20]!=0)throw new NullReferenceException();Record("has_static");return H[10]!=0;}
        public bool IsUnderNonFallBlockingAboveStaticItem(OriginalBoardItem item){Record("under_item");return H[11]!=0;}
        public bool HasStaticItem(OriginalGridCell cell,int type){Record("static,"+Id(cell)+","+type);return ((H[type==4?12:13]>>Id(cell))&1)!=0;}
        public void PlayFallHit(){if(H[21]!=0)throw new NullReferenceException();Record("sfx");}
        public void Create()
        {
            for(int i=0;i<12;i++){Cells[i]=i==10?new OriginalFillingCell(i%3,i/3,2,i/9,1,false,false,0):new OriginalGridCell(i%3,i/3,1,i/9);Cells[i].BindGameOperations(new OriginalGameOperations());if((H[9]&(1<<i))!=0)Cells[i].FreezeFall();}
            for(int i=0;i<12;i++)for(int d=0;d<8;d++){Cells[i].Neighbors.GetPoint(d,out int x,out int y);Cells[i].Neighbors.Set(d,x<0||x>=3||y<0||y>=4?null:Cells[y*3+x]);}
            for(int i=0;i<2;i++){Items[i]=new OriginalBoardItem(i+1,1);Views[i]=new View(this,i+1){P=new OriginalSwapPoint(V[7+i*3],V[8+i*3],V[9+i*3]),Under=H[11]!=0};Falls[i]=new OriginalFallComponentState(Items[i]);Falls[i].BindMotionServices(this);if((H[15]&(1<<i))==0)Items[i].FallComponent=Falls[i];}
            var f=Falls[0];f.CurrentFallState=H[0];f.IsFallingCross=H[1]!=0;f.PlayedSound=H[2]!=0;f.HasEverFallCross=H[3]!=0;f.FallAnimationHasStarted=H[4]!=0;
            f.SetFillingCell(Cell(H[5])as OriginalFillingCell);f.CurrentSpeed=V[0];f.StateChangedTime=V[1];f.FallStartLocalPosition=new OriginalSwapPoint(V[2],V[3],V[4]);Falls[1].CurrentSpeed=V[13];
            Place(0,H[6],H[7],H[8]);if(H[5]>=0)((OriginalFillingCell)Cell(H[5])).LastCreatedItem=Items[0];
        }
        public void Place(int item,int current,int next,int target){Items[item].Mediator.ClearFromAllRegisteredCells();if(current>=0)Cells[current].Mediation.SetCurrentItem(Items[item]);if(next>=0)Cells[next].Mediation.SetNextItem(Items[item]);if(target>=0)Cells[target].Mediation.SetTargetItem(Items[item]);}
        public string State()
        {
            var f=Falls[0];var vals=new List<string>();var ints=new List<int>{f.CurrentFallState,f.IsFallingCross?1:0,f.PlayedSound?1:0,f.HasEverFallCross?1:0,f.FallAnimationHasStarted?1:0,Id(f.FillingCell),((OriginalFillingCell)Cells[10]).LastCreatedItem?.Id??0,Views[0].Mask?1:0,Views[0].Under?1:0};
            foreach(var c in Cells){ints.Add(c.Mediation.CurrentItem?.Id??0);ints.Add(c.Mediation.NextItem?.Id??0);ints.Add(c.Mediation.TargetItem?.Id??0);}
            vals.Add(string.Join(",",ints));foreach(float v in new[]{f.CurrentSpeed,f.StateChangedTime,f.FallStartLocalPosition.X,f.FallStartLocalPosition.Y,f.FallStartLocalPosition.Z,Views[0].P.X,Views[0].P.Y,Views[0].P.Z,LastPrevious,LastMax})vals.Add(Bits(v));
            return string.Join("/",vals);
        }
    }
    static void Main(){string line;while((line=Console.ReadLine())!=null){var p=line.Split('#');var f=new Inputs{H=Array.ConvertAll(p[0].Split(','),int.Parse),V=Array.ConvertAll(p[1].Split(','),Parse)};f.Create();var output=new List<string>();
        foreach(var cmd in p[2].Split(';')){f.Calls=0;f.Trace.Clear();var a=cmd.Split(',');string result="-";var fall=f.Falls[0];try{switch(a[0])
        {
            case "m":fall.ManualUpdate();break;case "s":fall.StopIfReached();break;case "c":fall.FallWithConstantSpeed();break;case "f":fall.FallToFillingCell();break;
            case "n":fall.SelectNextCell();break;case "t":fall.SetFallState(int.Parse(a[1]));break;case "r":fall.ArrangeIsUnderNonFallBlockingAboveStaticItem();break;
            case "g":result=Bits(fall.GetAcceleratedSpeed(int.Parse(a[1])!=0));break;case "u":fall.FallToNextCell();break;case "p":fall.CopyFrom(f.Falls[1]);break;
            case "b":f.Place(1,int.Parse(a[1]),int.Parse(a[2]),-1);break;case "z":f.Place(0,int.Parse(a[1]),int.Parse(a[2]),int.Parse(a[3]));break;
            case "v":f.Views[0].P=new OriginalSwapPoint(Parse(a[1]),Parse(a[2]),Parse(a[3]));break;case "h":int n=int.Parse(a[1]);f.H[n]=int.Parse(a[2]);if(n==9)for(int i=0;i<12;i++){if(f.Cells[i].IsFallFrozen)f.Cells[i].UnFreezeFall();if((f.H[9]&(1<<i))!=0)f.Cells[i].FreezeFall();}break;
        }}catch{result="error";}output.Add(result+":"+f.State()+":"+string.Join(";",f.Trace));}Console.WriteLine(string.Join("|",output));}}
}
