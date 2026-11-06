using System;using System.Collections.Generic;using Whitebox.Foundation;
public static class P {
 public static int[] H;public static List<int[]> Entries;public static List<string> Trace;public static int Calls,Created;
 public static void R(string s){Trace.Add(s);if(++Calls==H[3])throw new Exception();}
 public static int[] Ints(string s)=>s==""?new int[0]:Array.ConvertAll(s.Split(','),int.Parse);
 public sealed class Data:IOriginalPredefinedDataProvider{
  public int PredefinedCount{get{R("count");return (H[2]&128)!=0&&Created>0?0:Entries.Count;}}
  public int GetPredefinedColumn(int e){R("column,"+e);return Entries[e][0];}
  public int GetPredefinedItemCount(int e){R("length,"+e);return (H[2]&128)!=0&&Created>0?0:Entries[e].Length-1;}
  public int GetPredefinedItem(int e,int i){R("id,"+e+","+i);return Entries[e][i+1];}}
 public sealed class Context:IOriginalPredefinedPreparationContext,IOriginalPredefinedItemCreator{
  public OriginalLevelRandomSource GetLevelRandom(){R("random");return (H[2]&8)!=0?null:new OriginalLevelRandomSource();}
  public int SelectMatchTileColor(int id,OriginalLevelSetSelection s,OriginalLevelRandomSource r){R("select,"+id+","+(r==null?0:1));return H[4];}
  public IOriginalPredefinedItemCreator ItemCreator=>((H[2]&16)!=0||(H[2]&32)!=0&&Created>0)?null:this;
  public OriginalBoardItem CreateItemModelForStart(int a,int b,OriginalGridCell c){R("create,"+a+","+b+","+(c==null?0:1));Created++;return (H[2]&64)!=0?null:new OriginalBoardItem{Id=Created};}
  public void AddPredefinedItem(int c,OriginalBoardItem i){R("add,"+c+","+(i==null?0:i.Id));}}
 public static void Main(){string l;while((l=Console.ReadLine())!=null){var p=l.Split('#');H=Ints(p[0]);Entries=new List<int[]>();if(p[1]!="")foreach(var s in p[1].Split('/'))Entries.Add(Ints(s));Trace=new List<string>();Calls=Created=0;string result="ok";try{OriginalPredefinedPreparation.Parse((H[2]&1)!=0?null:new Data(),(H[2]&2)!=0?null:(H[2]&4)!=0?new OriginalLevelSetSelection[0]:new OriginalLevelSetSelection[]{new OriginalLevelSetSelection()},H[0],H[1],new Context());}catch{result="error";}Console.WriteLine(result+":"+Created+":"+string.Join("/",Trace));}}
}
namespace Whitebox.Foundation {public class OriginalBoardItem{public int Id;}public class OriginalGridCell{}public class OriginalLevelSetSelection{}public class OriginalLevelRandomSource{}}
