using System;using System.Collections.Generic;using Whitebox.Foundation;
public static class G
{
 public static List<string> Trace=new List<string>();public static int Calls,Failure;public static int[] H;
 public static void R(string s){Trace.Add(s);if(++Calls==Failure)throw new Exception();}
 public static int[] Ints(string s)=>Array.ConvertAll(s.Split(','),int.Parse);
 public static void Main(){string line;while((line=Console.ReadLine())!=null){var p=line.Split('#');H=Ints(p[0]);Calls=0;Failure=H[4];var sets=new List<OriginalLevelSetSelection>();if(p[1]!="")foreach(var raw in p[1].Split('/'))sets.Add(new OriginalLevelSetSelection(sets.Count,Ints(raw)));var creator=new OriginalFillSetGeneration(new OriginalLevelRandomSource(),new OriginalTargetColumnSetContainer(sets),new Context());var cell=new OriginalFillingCell{FillType=H[1]};var output=new List<string>();foreach(var raw in p[2].Split(';')){Trace.Clear();var c=raw.Split(',');string result="ok";try{switch(c[0]){case "add":creator.AddAvailableSets(sets.ToArray());break;case "reset":creator.SetupAvailableFillItems();break;case "calc":creator.CalculateRandomFactors();break;case "fill":result="value,"+creator.GetRandomFillItem(cell);break;case "target":result="value,"+creator.GetRandomTargetColumnItem(cell,H[2]);break;case "color":result="value,"+creator.GetRandomColor(cell);break;case "new":creator.OnNewItemCreated(int.Parse(c[1]));break;case "rule":result="value,"+(OriginalFillSetGeneration.IsFillingObstacle(int.Parse(c[1]))?1:0);break;}}catch{result="error";}var state=new List<string>();foreach(var s in sets)state.Add((s.IsAvailableForFill?1:0)+","+s.Total+","+s.Remaining+","+s.Mask);var ratios=new List<string>();foreach(var e in creator.FillRatios)ratios.Add(e.Key+","+e.Value);var counts=new List<string>();foreach(var e in creator.CreatedCounts)counts.Add(e.Key+","+e.Value);output.Add(result+":"+string.Join("/",state)+":"+string.Join("/",ratios)+":"+string.Join("/",counts)+":"+string.Join("/",Trace));}Console.WriteLine(string.Join("|",output));}}
 private class Context:IOriginalFillSetGenerationContext
 {public bool IsTuningEnabled=>(H[0]&1)!=0;public int GetTunedItem(OriginalLevelSetSelection s,OriginalFillingCell c){R("tune,"+s.Id);return 777;}public int GetLimitCount(int g){R("limit,"+g);return H[5];}public int GetGoalTargetCount(int g){R("goal,"+g);return H[6];}public void ReportRandomColorFallback(int n){R("fallback,"+n);}}
}
namespace Whitebox.Foundation
{
 public class OriginalFillingCell{public int FillType;}
 public class OriginalLevelRandomSource{public int DifficultyNext(int a,int b){G.R("random,"+a+","+b);if(b<a)throw new ArgumentOutOfRangeException();return G.H[3]!=0?Math.Max(a,b-1):a;}}
 public static class OriginalGoalTypes{public static int FromItemType(int t)=>t+1000;}
 public class OriginalLevelSetSelection
 {
  public int Id,CreateRatio,CanFall,Priority,CanPriority,Match,Output,Mask,Column,Total,Remaining;public int[] Ratios,Types;public bool IsAvailableForFill;
  public OriginalLevelSetSelection(int id,int[] v){Id=id;CreateRatio=v[0];CanFall=v[1];Priority=v[2];CanPriority=v[3];Match=v[4];Output=v[5];Column=v[6];Ratios=new[]{v[7],v[8],v[9],v[10]};Types=new int[v.Length-11];Array.Copy(v,11,Types,0,Types.Length);}
  public bool ShouldPrioritize{get{G.R("priority,"+Id);return Priority!=0;}}
  public void InitFillAvailability(){G.R("init,"+Id);IsAvailableForFill=CanFall!=0;Mask=IsAvailableForFill?(1<<Types.Length)-1:0;Total=0;Remaining=0;}
  public void CalculateFillRatioForItems(){G.R("calculate,"+Id);Total=0;for(int i=0;i<Types.Length;i++)if((Mask&(1<<i))!=0)Total++;}
  public bool HasAnyTargetColumn(){G.R("anycolumn,"+Id);return Column>=0;}
  public int GetFillRatio(int f){G.R("ratio,"+Id+","+f);return f>=1&&f<=4?Ratios[f-1]:0;}
  public bool CanGetPrioritizedItem(){G.R("canpriority,"+Id);return CanPriority!=0&&IsAvailableForFill;}
  public int GetRandomItem(){G.R("item,"+Id);return Mask==0?0:Output;}
  public void ConsumePrioritizedItem(){Remaining=unchecked(Remaining-1);}
  public bool HasTargetColumn(OriginalFillingCell c,int column){G.R("hascolumn,"+Id+","+column);return Column==column;}
  public bool ContainsMatchItem(){G.R("match,"+Id);return Match!=0;}
  public bool DisableFirstElementByItemType(int t){for(int i=0;i<Types.Length;i++)if(Types[i]==t){Mask&=~(1<<i);return true;}return false;}
  public void UpdateFillAvailability(){G.R("update,"+Id);if(Mask==0)IsAvailableForFill=false;}
 }
 public class OriginalTargetColumnSetContainer
 {
  private List<OriginalLevelSetSelection> Sets;public OriginalTargetColumnSetContainer(List<OriginalLevelSetSelection> s){Sets=s;}
  public bool HasTarget(int c)=>true;
  private List<OriginalLevelSetSelection> List(int c){var r=new List<OriginalLevelSetSelection>();foreach(var s in Sets)if(s.Column==c)r.Add(s);return r;}
  public IReadOnlyList<OriginalLevelSetSelection> GetSetsFromColumn(int f,int c,List<OriginalLevelSetSelection>b){G.R("columnsets,"+f+","+c);return List(c);}
  public int GetTotalRatioForColumn(int f,int c,List<OriginalLevelSetSelection>b){G.R("columntotal,"+f+","+c);int n=0;foreach(var s in List(c))if(s.IsAvailableForFill&&s.Priority==0)n=unchecked(n+(f>=1&&f<=4?s.Ratios[f-1]:0));return n;}
 }
}
