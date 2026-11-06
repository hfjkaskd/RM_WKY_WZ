using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace Whitebox.Foundation{
public struct OriginalCellPoint{public readonly int X,Y;public OriginalCellPoint(int x,int y){X=x;Y=y;}}
public class OriginalGridCell{public int X,Y,Id;}
public interface IOriginalItemSwapAvailability{}
public class OriginalCellGrid{public OriginalGridCell To;public OriginalGridCell GetCell(int x,int y){Verify.Trace.Add("get:"+x+":"+y);return To;}}
public static class OriginalCellTouchRules{
public static bool CanSwapFrom(OriginalGridCell cell,IOriginalItemSwapAvailability a){if(cell==null)throw new NullReferenceException();Verify.Trace.Add("from");return Verify.CanFrom;}
public static bool CanSwapTo(OriginalGridCell cell,IOriginalItemSwapAvailability a){Verify.Trace.Add("to");return Verify.CanTo;}}
public class OriginalSwapDecision{public int Id;public void Clear(){Verify.Trace.Add("clear:"+Id);}public bool ShouldPlaySwapAnimation(OriginalGridCell from,OriginalGridCell to,bool ignore){Verify.Trace.Add("start:"+Id);return Verify.Valid;}}
}
class Verify:IOriginalLevelSwapServices,IOriginalItemSwapAvailability,IOriginalSwapAnimationBackend{
public static List<string> Trace=new List<string>();public static bool CanFrom,CanTo,Valid;public bool Disabled;
public bool IsImpossibleMoveDisabledForCell(OriginalCellPoint p){Trace.Add("tutorial:"+p.X+":"+p.Y);return Disabled;}
public void StartImpossibleSwapAnimation(OriginalSwapActionState a,OriginalGridCell from,OriginalCellPoint to){Trace.Add("impossible:"+a.Decision.Id+":"+to.X+":"+to.Y);a.MarkAnimating();}
public void IncreaseFalseSwipeCount(){Trace.Add("false");}public void CheckForUsefulMove(){Trace.Add("useful");}
public void PlayValidSwap(OriginalSwapActionState a){a.MarkAnimating();}public void PlayWrongSwap(OriginalSwapActionState a){a.MarkAnimating();}
static void Main(){string line;while((line=Console.ReadLine())!=null){var f=Array.ConvertAll(line.Split(','),int.Parse);var v=new Verify{Disabled=f[5]!=0};CanFrom=f[2]!=0;CanTo=f[4]!=0;Valid=f[6]!=0;
var first=new OriginalSwapActionState(new OriginalSwapDecision{Id=0});var second=new OriginalSwapActionState(new OriginalSwapDecision{Id=1});if(f[0]!=0)first.MarkAnimating();if(f[1]!=0)second.MarkAnimating();
var from=f[8]!=0?null:new OriginalGridCell{X=1,Y=5,Id=1};var grid=new OriginalCellGrid{To=f[3]!=0?new OriginalGridCell{X=2,Y=5,Id=2}:null};var r=new OriginalLevelSwapRouter(grid,first,second,v,v,v,v);
if(f[7]!=0)r.OnSwap+=(a,b,c)=>Trace.Add("event:"+(a==null?0:a.Id)+":"+(b==null?0:b.Id)+":"+(c?1:0));
Trace.Clear();bool ok=false,error=false;try{ok=r.SwapCells(from,new OriginalCellPoint(2,5));}catch(NullReferenceException){error=true;}
Console.WriteLine((ok?1:0)+","+(error?1:0)+","+(first.IsSwapping?1:0)+","+(second.IsSwapping?1:0)+";"+string.Join("|",Trace));}}
}
