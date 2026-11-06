using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace Whitebox.Foundation{
public interface IOriginalTapItem{int GetMoveType{get;}bool IsTapDisabled{get;set;}bool Tap(OriginalExplodeData d);}
public interface IOriginalLightballTapItem:IOriginalTapItem{}
public interface IOriginalBoosterTouchSelection{bool IsBoosterSelected{get;}}
public interface IOriginalMatchBlockingState{bool CanEnterToMatch(OriginalGridCell cell);}
public class OriginalBoardItem:IOriginalTapItem{
public bool Disabled;public int Move;
public bool IsTapDisabled{get{Verify.Trace.Add("disabled");return Disabled;}set{Disabled=value;}}
public int GetMoveType{get{Verify.Trace.Add("move");if(Verify.Mutate==2)Verify.Router.OnMoveCompleted-=Verify.Completed;return Move;}}
public int ItemType{get{Verify.Trace.Add("type");return Verify.Kind+20;}}
public bool Tap(OriginalExplodeData d){Verify.Trace.Add("tap:"+Verify.Data(d));return Verify.TapResult;}}
public class OriginalSpecialBoardItem:OriginalBoardItem{}
public class Lightball:OriginalSpecialBoardItem,IOriginalLightballTapItem{}
public sealed class OriginalRocketBoardItem:OriginalSpecialBoardItem{}
public sealed class OriginalTntBoardItem:OriginalSpecialBoardItem{}
public partial class OriginalGridCell{public bool Normal;public bool IsNormalCell=>Normal;public OriginalCellMediation Mediation=new OriginalCellMediation();}
public class OriginalCellMediation{public OriginalBoardItem Item;public OriginalBoardItem CurrentItem{get{Verify.Trace.Add("item");return Item;}}public bool HasCurrentItem(){Verify.Trace.Add("has");return Item!=null;}public bool HasNextItem()=>false;}
}
class Verify:IOriginalLevelTapServices{
public static List<string> Trace=new List<string>();public static int Kind,Mutate;public static bool TapResult;public static OriginalLevelTapRouter Router;public static OriginalBoardItem Item;public bool Selected;
public bool IsBoosterSelected{get{Trace.Add("booster");return Selected;}}
public void UseBooster(OriginalGridCell c){Trace.Add("use");}
public void CheckForUsefulMove(){Trace.Add("useful");}
public void ClearSinglePropellerWinCalculationData(){Trace.Add("king");}
public void IncrementTappedLightball(){Trace.Add("lightball");}
public void OnSpecialItemTapped(){Trace.Add("special");}
public void IncrementItemUse(int type,bool combo){Trace.Add("count:"+type+":"+(combo?1:0));}
public void OnTntOrRocketItemTapped(){Trace.Add("rocket-tnt");}
public static void Completed(int move){Trace.Add("completed:"+move);}
public static string Data(OriginalExplodeData d){return string.Join(",",new object[]{d.Point.X,d.Point.Y,d.Trigger,d.Id,d.MatchType,d.CanSpreadJelly?1:0,d.IsHorizontalRocket?1:0,d.ComboId,d.LastTargetItemIdentifier.ItemType,d.LastTargetItemIdentifier.StaticItemType,d.LastTargetItemIdentifier.ItemGroupId,d.ExploderIndex,d.IncomingX,d.IncomingY,d.IncomingZ});}
class Block:IOriginalStaticTouchItem{public bool IsTouchBlocking=>true;public bool IsTouchBlockingExceptChain=>true;}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');Kind=int.Parse(a[1]);Mutate=int.Parse(a[9]);TapResult=a[8]=="1";var service=new Verify{Selected=a[7]=="1"};Router=new OriginalLevelTapRouter(service);
Item=Kind==0?null:Kind==1?new OriginalBoardItem():Kind==2?new OriginalSpecialBoardItem():Kind==3?new Lightball():Kind==4?new OriginalRocketBoardItem():(OriginalBoardItem)new OriginalTntBoardItem();
if(Item!=null){Item.Disabled=a[5]=="1";Item.Move=Kind*10;}
var cell=new OriginalGridCell{Normal=a[6]=="1"};cell.Mediation.Item=Item;if(a[2]=="0")cell.DisableTouch();if(a[3]=="1")cell.StaticTouch.AboveItems.Add(new Block());if(a[4]=="1")cell.Reservation.Enable();
int listeners=int.Parse(a[10]);if((listeners&1)!=0)Router.OnTap+=c=>{Trace.Add("event");if(Mutate==1)Item.Move+=100;};if((listeners&2)!=0)Router.OnMoveCompleted+=Completed;
OriginalExploder.Reset();Trace.Clear();string result;int output=-1;
try{if(a[0]=="C"){IOriginalTapItem tap;result=OriginalCellTap.CanTapCurrentItem(cell,out tap)?"1":"0";output=tap==null?0:1;}else result=Router.TapCell(cell)?"1":"0";}catch(NullReferenceException){result="null";}
Console.WriteLine(string.Join("|",Trace)+"#"+result+","+output);
}}}
