using System;using System.Collections.Generic;using Whitebox.Foundation;
public static class A {
 public static List<string> Trace=new List<string>();public static int Frame,Flying,Edge,Failure,Calls;
 public static void R(string t){Trace.Add(t);if(++Calls==Failure)throw new Exception();}
 public static int[] Ints(string s)=>Array.ConvertAll(s.Split(','),int.Parse);
 public sealed class Context:IOriginalLevelSetAvailabilityContext {
  public OriginalGridCell[] Cells;public int FrameCount{get{R("frame");return Frame;}}
  public OriginalGridIterator GetCurrentBoardHorizontalIterator(){R("iterator");return new OriginalGridIterator(Cells);}
  public int GetFlyingThrowItemCount(int g){R("flying,"+g);return Flying;}
 }
 public static void Main(){string line;while((line=Console.ReadLine())!=null){var p=line.Split('#');var h=Ints(p[0]);Frame=h[0];Flying=h[1];Edge=h[2];Failure=h[3];Calls=0;var items=new List<OriginalBoardItem>();if(p[1]!="")foreach(var raw in p[1].Split('/')){var v=Ints(raw);items.Add(new OriginalBoardItem{Id=v[0],ItemType=v[1],Mediator=new OriginalItemMediation{TargetCell=v[2]!=0?new object():null}});}var cells=new List<OriginalGridCell>();if(p[2]!="")foreach(var raw in p[2].Split('/')){var v=Ints(raw);var c=v[0]!=0?(OriginalGridCell)new OriginalFillingCell():new OriginalGridCell();c.IsNormalCell=v[1]!=0;c.Mediation=new OriginalCellMediation{CurrentItem=v[2]<0?null:items[v[2]],TargetItem=v[3]<0?null:items[v[3]]};if(c is OriginalFillingCell f)f.LastCreatedItem=v[4]<0?null:items[v[4]];cells.Add(c);}var set=new OriginalLevelSetSelection(h[4],h[5],h[6]!=0);set.BindAvailabilityContext(new Context{Cells=cells.ToArray()});var output=new List<string>();foreach(var cmd in p[3].Split(';')){Trace.Clear();var c=cmd.Split(',');string result="ok";try{switch(c[0]){case "m":result="value,"+(set.CanFillNewItemWithMaxCount(int.Parse(c[1]))?1:0);break;case "p":result="value,"+(set.CanGetPrioritizedItem()?1:0);break;case "f":Frame=int.Parse(c[1]);break;case "y":Flying=int.Parse(c[1]);break;case "consume":set.ConsumePrioritizedItem();break;}}catch{result="error";}output.Add(result+":"+set.Snapshot()+":"+string.Join("/",Trace));}Console.WriteLine(string.Join("|",output));}}
}
namespace Whitebox.Foundation {
 public class OriginalLevelRandomSource {public int DifficultyNext(int a,int b){A.R("random,"+a+","+b);if(b<a)throw new Exception();return A.Edge!=0?Math.Max(a,b-1):a;}}
 public class OriginalBoardItem{public int Id;public int ItemType;public OriginalItemMediation Mediator;}
 public class OriginalItemMediation{public object TargetCell;}
 public class OriginalCellMediation{public OriginalBoardItem CurrentItem,TargetItem;public bool HasTargetItem()=>TargetItem!=null;public bool HasCurrentItem()=>CurrentItem!=null;}
 public class OriginalGridCell{public bool IsNormalCell;public OriginalCellMediation Mediation;}
 public class OriginalFillingCell:OriginalGridCell{public int FillType;public OriginalBoardItem LastCreatedItem;public bool HasCreatedItem=>LastCreatedItem!=null;}
 public struct OriginalGridIterator{private OriginalGridCell[] cells;private int index;public OriginalGridCell Cell{get;private set;}public OriginalGridIterator(OriginalGridCell[] c){cells=c;index=-1;Cell=c.Length>0?c[0]:null;}public void Reset(){A.R("reset");index=-1;Cell=cells.Length>0?cells[0]:null;}public OriginalGridCell MoveNext(){A.R("next");index++;Cell=index<cells.Length?cells[index]:null;return Cell;}}
 public static class OriginalGoalTypes{public static int FromItemType(int t)=>t+1000;}
 public static class OriginalLevelRules{public static int ToMatchType(int t)=>t>=1&&t<=6?t:0;}
 public static class OriginalTiledItemTypes{public static int AsItemType(int t)=>t;}
 public sealed partial class OriginalLevelSetSelection{
  private int createRatio;private int[] ids=new int[0],targetFills=new int[0];private bool[] available=new bool[0];private List<int> targetColumns=new List<int>();private Dictionary<int,int> maxItemCounts=new Dictionary<int,int>();private OriginalLevelRandomSource random=new OriginalLevelRandomSource();
  public bool IsAvailableForFill{get;private set;}public int RemainingItemCount{get;private set;}
  public OriginalLevelSetSelection(int t,int n,bool a){maxItemCounts.Add(t,n);IsAvailableForFill=a;}
  public string Snapshot()=>availabilityFrame+","+emptyCellCount+","+RemainingItemCount;
 }
}
