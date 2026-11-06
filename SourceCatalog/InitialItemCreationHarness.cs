using System;using System.Collections.Generic;using Whitebox.Foundation;
public static class N {
 public static int[] H;public static List<string> Trace;public static int Calls;public static OriginalItemSettings Last;
 public static int[] Ints(string s)=>s==""?new int[0]:Array.ConvertAll(s.Split(','),int.Parse);
 public static void R(string s){Trace.Add(s);if(++Calls==H[6])throw new Exception();}
 public static string Settings(OriginalItemSettings s)=>string.Join(",",new[]{s.TiledId,s.ItemType,s.LayerCount,s.MatchType,s.GoalType,(int)s.SaveData,s.StaticItemType,s.IsExtraPropeller?1:0,s.IsCreatedByLightball?1:0,s.CellModel==null?0:1,s.PotionColors==null?0:1,s.CurtainId,s.IsDrillMaster?1:0,s.IsJellyViewActive?1:0,s.TreasureMapId,s.IsHandGiven?1:0,s.CryptexId});
 public sealed class Context:IOriginalInitialItemCreationContext {
  public int[][] PotionColors=>(H[3]!=0)?new int[0][]:null;
  public bool ContainsDrillMasterPoint(OriginalGridCell c){if((H[5]&8)!=0)throw new Exception();R("drill");return H[4]!=0;}
  public OriginalBoardItem CreateItem(OriginalItemSettings s){Last=s;R("create,"+Settings(s));return (H[5]&4)!=0?null:new OriginalBoardItem(91,H[8]);}
  public void OnNewItemCreated(int t){R("new,"+t);}
 }
 public static void Main(){string line;while((line=Console.ReadLine())!=null){var p=line.Split('#');H=Ints(p[1]);Trace=new List<string>();Calls=0;string result="ok";try{
 if(p[0]=="s")result=Settings(OriginalItemSettings.ForTiledId(H[0],unchecked((short)H[1])));
 else if(p[0]=="c"){OriginalBoardItem.StaticId=H[0];var s=new OriginalItemSettings(H[1],H[2],H[3],H[4],H[5]);s.IsHandGiven=H[6]!=0;var item=OriginalCoreItemModels.Create(s);result=string.Join(",",new[]{item.Id,OriginalBoardItem.StaticId,item.TiledId,item.ItemType,item.LayerCount,item.GoalType,item is OriginalMatchBoardItem m&&m.IsHandGiven?1:0,item.FallComponent is OriginalFallComponentState?1:0});}
 else {OriginalLevelSetSelection[] sets=null;if((H[5]&1)==0){var rows=p[2].Split('/');sets=new OriginalLevelSetSelection[rows.Length];for(int i=0;i<rows.Length;i++)sets[i]=rows[i]=="~"?null:new OriginalLevelSetSelection(Ints(rows[i]));}var creator=new OriginalInitialItemCreation(sets,(H[5]&2)!=0?null:new OriginalLevelRandomSource(),new OriginalPredefinedItemContainer(),new Context());result=p[0]=="r"?"value,"+creator.GetRandomItemTypeFromSet(H[0]):"value,"+creator.CreateItemModelForStart(H[0],H[1],H[2]!=0?new OriginalGridCell():null).Id;}
 }catch{result="error";}Console.WriteLine(result+":"+string.Join("/",Trace));}}
}
namespace Whitebox.Foundation {
 public partial class OriginalBoardItem{public int Id{get;}public virtual int ItemType{get;}protected int layer=1;public int LayerCount=>layer;public int GoalType;public object FallComponent;public OriginalBoardItem(int id,int t){Id=id;ItemType=t;}}
 public sealed partial class OriginalMatchBoardItem:OriginalBoardItem{public int MatchType{get;}}
 public sealed partial class OriginalBoxBoardItem:OriginalBoardItem{}
 public class OriginalFallComponentState{public OriginalFallComponentState(OriginalBoardItem i){}}
 public class OriginalGridCell{}
 public class OriginalLevelSetSelection{private int[] ids;public OriginalLevelSetSelection(int[] a){ids=a;}internal int ElementCount=>ids.Length;internal int GetElementId(int i)=>ids[i];}
 public class OriginalLevelRandomSource{public int DifficultyNext(int a,int b){N.R("random,"+a+","+b);if(b<a)throw new Exception();return N.H[7]!=0?Math.Max(a,b-1):a;}}
 public class OriginalPredefinedItemContainer{public void AddPredefinedItem(int c,OriginalBoardItem i){}}
 public interface IOriginalPredefinedItemCreator{OriginalBoardItem CreateItemModelForStart(int a,int b,OriginalGridCell c);void AddPredefinedItem(int c,OriginalBoardItem i);}
}
