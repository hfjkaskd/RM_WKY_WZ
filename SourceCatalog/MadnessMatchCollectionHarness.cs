using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace Whitebox.Foundation {
 public struct OriginalSwapPoint {public float X,Y,Z;public OriginalSwapPoint(float x,float y,float z){X=x;Y=y;Z=z;}}
 public class OriginalBoardItem {}
 public interface IOriginalMadnessSessionManager {}
 public partial class OriginalMadnessSessionData:OriginalBeforeAfterData {
  internal Dictionary<int,int> exploderIdMatchItemCount;
  public bool IsEventValid(){V.Record("valid");return V.H[5]!=0;}
  private IOriginalMadnessSessionManager Manager(){V.Record("manager");if((V.H[0]&128)!=0)throw new NullReferenceException();return new V.Manager();}
  public void Setup(){Before=V.H[2];After=V.H[3];ShouldConsume=V.H[4]!=0;exploderIdMatchItemCount=V.H[14]!=0?new Dictionary<int,int>():null;}
  public int ReadCount(){int n=0;exploderIdMatchItemCount?.TryGetValue(31,out n);return n;}
 }
 public class Animation {public Snapshots StartSnapshots;}
 public class Snapshots {public object Madness;}
 public sealed partial class OriginalActiveLevelData {public int State;public sbyte Difficulty;public Animation AnimationValues;}
}
public static class V {
 public static int[] H;static int calls;static List<string> trace=new List<string>();
 public static void Record(string s){trace.Add(s);if(++calls==H[1])throw new InvalidOperationException();}
 public class Manager:IOriginalMadnessMatchManager {public int CollectMatchType=>H[6];public bool ShouldPreventStickerAnimation=>H[13]!=0;}
 sealed class View:IOriginalMadnessMatchAnimation {public void StartAnimation(OriginalSwapPoint p,int count,int delay,OriginalBoardItem item){Record("anim,"+(int)(p.X*4)+","+(int)(p.Y*4)+","+(int)(p.Z*4)+","+count+","+delay+","+(item==null?0:1));}}
 sealed class Presentation:IOriginalMadnessMatchPresentation {
  public bool IsDailyLightningRushEventValid(){if((H[0]&(1|4|8|16|32|64))!=0)throw new NullReferenceException();Record("daily");return H[12]!=0;}
  public IOriginalMadnessMatchAnimation Spawn(int id,bool on){Record("factory");if((H[0]&256)!=0)throw new NullReferenceException();Record("spawn,"+id+","+(on?1:0));return (H[0]&512)!=0?null:new View();}
  public bool IsMissing(IOriginalMadnessMatchAnimation view){Record("missing");return view==null||(H[0]&1024)!=0;}
 }
 public static void Main(){string line;while((line=Console.ReadLine())!=null){H=Array.ConvertAll(line.Split(','),int.Parse);var model=new OriginalMadnessSessionData();model.Setup();model.BindMatchPresentation(new Presentation());var active=new OriginalActiveLevelData{State=H[10],Difficulty=(sbyte)H[9],AnimationValues=(H[0]&(1|32))!=0?null:new Animation{StartSnapshots=new Snapshots{Madness=(H[0]&2)!=0?null:model}}};var output=new List<string>();var point=new OriginalSwapPoint(-1.25f,2.5f,.75f);foreach(var mode in new[]{"t","a","g","i","d"}){trace.Clear();calls=0;string result="ok";try{if(mode=="t")active.TryCollectMadness(H[7],point,H[8],17,new OriginalBoardItem(),H[11]!=0);else if(mode=="a")model.TryAddMadnessMatch(H[7],point,H[8],H[9],17,new OriginalBoardItem());else if(mode=="g")result=model.MatchTypeToCollect.ToString();else if(mode=="i")active.IncrementMadnessMatchItemCount(H[7],31);else model.TryIncrementMadnessForExploderId(H[7],31);}catch{result="error";}output.Add(result+":"+model.After+","+(model.ShouldConsume?1:0)+","+model.ReadCount()+":"+string.Join("/",trace));}Console.WriteLine(string.Join("|",output));}}
}
