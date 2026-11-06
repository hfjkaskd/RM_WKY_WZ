using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace Whitebox.Foundation {
 public interface IOriginalStaticTouchItem{}
 public interface IOriginalStaticFallItem:IOriginalStaticTouchItem{int StaticItemType{get;}}
 public interface IOriginalCellStaticExplosionServices{bool TryExplodeTopMostAboveItem(OriginalGridCell c,OriginalExplodeData d);}
 public class OriginalGridCell{public OriginalStaticTouchMediator StaticTouch;}
 public sealed class OriginalExploderContainer{readonly HashSet<int> values=new HashSet<int>();public bool ContainsExploder(int id)=>values.Contains(id);public void AddToExploders(int id){values.Add(id);}}
 public sealed partial class OriginalStaticTouchMediator {
  public List<IOriginalStaticTouchItem> AboveItems=new List<IOriginalStaticTouchItem>(),BelowItems=new List<IOriginalStaticTouchItem>();
  readonly IOriginalStaticFallItem[] items=new IOriginalStaticFallItem[20];
  public void Set(int i,IOriginalStaticFallItem v){items[i]=v;}
  public string State(int id)=>(aboveExploders.ContainsExploder(id)?1:0)+","+(belowExploders.ContainsExploder(id)?1:0);
 }
}
public static class StaticExplosionDispatchHarness {
 public static int[] H;public static int Calls;public static List<string> Trace=new List<string>();
 public static void Record(string value){Trace.Add(value);if(++Calls==H[8])throw new InvalidOperationException();}
 sealed class Item:IOriginalStaticExplosionItem {
  public int Type;public int StaticItemType=>Type;
  public bool CanExplodeWithMatch{get{Record("match,"+Type);return H[3]!=0;}}
  public bool CanExplodeWithNearMatch{get{Record("near,"+Type);return H[2]!=0;}}
  public void Explode(OriginalExplodeData d){Record("explode,"+Type+","+d.Id+","+d.Trigger+","+d.MatchType+","+(d.CanSpreadJelly?1:0));}
  public void ExplodeAll(OriginalExplodeData d){Record("all,"+Type+","+d.Id+","+d.Trigger+","+d.MatchType+","+(d.CanSpreadJelly?1:0));}
 }
 public static void Main(){string line;while((line=Console.ReadLine())!=null){H=Array.ConvertAll(line.Split(','),int.Parse);var mediator=new OriginalStaticTouchMediator();var above=new Item{Type=6};var first=new Item{Type=H[9]};var second=new Item{Type=4};
 for(int i=0;i<H[0];i++)mediator.AboveItems.Add(above);if(H[1]>0)mediator.BelowItems.Add(first);if(H[1]>1)mediator.BelowItems.Add(second);
 if(H[10]==0){mediator.Set(6,above);mediator.Set(first.Type,first);mediator.Set(4,second);}
 var data=new OriginalExplodeData(H[4],H[5],new OriginalCellPoint(2,3),H[6],H[7]!=0);var output=new List<string>();
 foreach(var command in new[]{"b","b","a","b","a","e2","e3","e6"}){Trace.Clear();Calls=0;string result="ok";try{if(command=="a")result=mediator.ExplodeTopMostAboveItem(data)?"1":"0";else if(command=="b")mediator.ExplodeTopMostBelowItem(data);else mediator.ExplodeStaticItem(int.Parse(command.Substring(1)),data);}catch{result="error";}output.Add(result+":"+mediator.State(data.Id)+":"+string.Join("/",Trace));}Console.WriteLine(string.Join("|",output));}}
}
