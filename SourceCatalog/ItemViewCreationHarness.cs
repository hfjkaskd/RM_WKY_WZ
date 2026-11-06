using System;using System.Collections.Generic;using Whitebox.Foundation;
public static class V {
 public static int[] H;public static List<string> Trace;public static int Calls;public static OriginalMatchBoardItem Model;public static View Surface;
 public static int[] Ints(string s)=>Array.ConvertAll(s.Split(','),int.Parse);
 public static void R(string t){Trace.Add(t);if(++Calls==H[2])throw new Exception();}
 public static string Point(OriginalSwapPoint p)=>((int)(p.X*4))+","+((int)(p.Y*4))+","+((int)(p.Z*4));
 public sealed class View:IOriginalMatchCreatedItemView{public bool Mask=true;public bool IsSwapping=>false;public void SetPosition(float x,float y,float z){}public void EnableFillMask(){Mask=true;}public void DisableFillMask(){R("disable");Mask=false;}}
 public sealed class Services:IOriginalMatchViewCreationServices {
  public IOriginalMatchCreatedItemView SpawnMatchView(){R("spawn,2,1");return (H[0]&2)!=0?null:Surface;}
  public void InitializeMatchView(IOriginalMatchCreatedItemView v,OriginalMatchBoardItem m,OriginalSwapPoint p){R("init,"+Point(p)+","+(ReferenceEquals(m.SwapView,v)?1:0)+","+(m.HasView?1:0));}}
 public sealed class Context:IOriginalItemViewCreationContext {
  public OriginalBoardItem CreateItem(OriginalItemSettings s){R("model,"+s.Id);return (H[0]&1)!=0?null:Model;}
  public void CreateView(OriginalBoardItem item,OriginalSwapPoint p){((OriginalMatchBoardItem)item).CreateView(p);}
  public IOriginalCreatedItemView GetItemView(OriginalBoardItem i){R("view");return (H[0]&4)!=0?null:(IOriginalCreatedItemView)i.SwapView;}}
 public static void Main(){string l;while((l=Console.ReadLine())!=null){var p=l.Split('#');H=Ints(p[1]);Trace=new List<string>();Calls=0;Surface=new View();Model=new OriginalMatchBoardItem();Model.SetHasView(H[1]!=0);Model.BindViewCreationServices((H[0]&16)!=0?null:new Services());var creation=new OriginalItemViewCreation(new Context());var point=new OriginalSwapPoint(H[3]/4f,H[4]/4f,H[5]/4f);var cell=new OriginalGridCell{X=H[6],Y=H[7],Mediation=new OriginalCellMediation()};string result="ok";try{if(p[0]=="m")Model.CreateView(point);else if(p[0]=="s")creation.CreateItemWithSettings(new OriginalItemSettings{Id=H[8]},point);else if(p[0]=="a")creation.CreateItemAt(H[8],point);else creation.CreateItemAtCell(H[8],(H[0]&8)!=0?null:cell);}catch{result="error";}Console.WriteLine(result+":"+(Model.HasView?1:0)+","+(Model.SwapView==null?0:1)+","+(Surface.Mask?1:0)+","+cell.Mediation.State+":"+string.Join("/",Trace));}}
}
namespace Whitebox.Foundation {
 public struct OriginalSwapPoint{public float X,Y,Z;public OriginalSwapPoint(float x,float y,float z){X=x;Y=y;Z=z;}}
 public struct OriginalItemSettings{public int Id;public static OriginalItemSettings ForTiledId(int id){return new OriginalItemSettings{Id=id};}}
 public interface IOriginalItemSwapView{bool IsSwapping{get;}}
 public interface IOriginalFillingItemView{void SetPosition(float x,float y,float z);void EnableFillMask();}
 public partial class OriginalBoardItem{public bool HasView{get;protected set;}public IOriginalItemSwapView SwapView{get;private set;}public void SetHasView(bool v){HasView=v;}}
 public sealed partial class OriginalMatchBoardItem:OriginalBoardItem{}
 public class OriginalCellMediation{public int State=7;public void ClearAllItems(){V.R("clear");State=0;}public void SetAllItems(OriginalBoardItem i){V.R("set");State=91;}}
 public class OriginalGridCell{public int X,Y;public OriginalCellMediation Mediation;}
 public class OriginalFillingCell:OriginalGridCell{}
 public class OriginalPredefinedItemContainer{public OriginalBoardItem GetPredefinedItem(int c){return null;}}
 public class OriginalFillSetGeneration{public bool HasTarget(int c)=>false;public int GetRandomTargetColumnItem(OriginalFillingCell c,int n)=>0;public int GetRandomFillItem(OriginalFillingCell c)=>0;public int GetRandomColor(OriginalFillingCell c)=>0;public void OnNewItemCreated(int t){}}
 public interface IOriginalFillingCreationBackend{OriginalBoardItem GetPredefinedItem(OriginalFillingCell c);bool HasTarget(int c);int GetRandomTargetColumnItem(OriginalFillingCell c,int n);int GetRandomFillItem(OriginalFillingCell c);int GetRandomColor(OriginalFillingCell c);OriginalBoardItem CreateItemAt(int i,float x,float y,float z);void OnNewItemCreated(int t);IOriginalFillingItemView GetItemView(OriginalBoardItem i);void CreateView(OriginalBoardItem i,float x,float y,float z);}
}
