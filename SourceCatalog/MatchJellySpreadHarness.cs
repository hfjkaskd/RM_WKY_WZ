using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace Whitebox.Foundation {
 public class OriginalInitialItemAmounts {public bool IsThereStaticItemInLevel(int type){if(type!=3)throw new Exception();return V.H[0]!=0;}}
 public class OriginalGridCell {public int Id;}
 public struct OriginalMatchCells {public OriginalGridCell[] Values;public int Count=>Values.Length;public OriginalGridCell Get(int i)=>Values[i];}
 public struct OriginalMatchGroupSnapshot {public OriginalMatchCells Cells;}
}
public static class V {
 public static int[] H;static int calls;static List<string> trace=new List<string>();
 public static void Record(string s){trace.Add(s);if(++calls==H[4])throw new InvalidOperationException();}
 sealed class Views:IOriginalMatchJellyViews {public bool HasJellyView(OriginalGridCell c){Record("view,"+c.Id);return (H[3]&(1<<c.Id))!=0;}public void SetSpreadData(OriginalMatchGroupSnapshot g){Record("spread,"+g.Cells.Count);}}
 public static void Main(){string line;while((line=Console.ReadLine())!=null){H=Array.ConvertAll(line.Split(','),int.Parse);calls=0;trace.Clear();var cells=new OriginalGridCell[H[2]];for(int i=0;i<cells.Length;i++)cells[i]=i==H[5]?null:new OriginalGridCell{Id=i};var group=new OriginalMatchGroupSnapshot{Cells=new OriginalMatchCells{Values=cells}};var data=new OriginalExplodeData(2,71,OriginalCellPoint.Default,2,H[1]!=0);var helper=new OriginalMatchGroupJellySpread(new OriginalInitialItemAmounts(),new Views());string result;try{result=helper.Init(group,ref data)?"1":"0";}catch{result="error";}Console.WriteLine(result+":"+(data.CanSpreadJelly?1:0)+":"+string.Join("/",trace));}}
}
