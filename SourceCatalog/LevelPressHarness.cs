using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace Whitebox.Foundation{
public partial class OriginalBoardItem{public object SwapView;}
public sealed partial class OriginalMatchBoardItem:OriginalBoardItem{public int MatchType=>1;}
public sealed partial class OriginalBoxBoardItem:OriginalBoardItem{}
public partial class OriginalGridCell{public OriginalCellMediation Mediation=new OriginalCellMediation();public bool IsNormalCell=>true;}
public class OriginalCellMediation{public OriginalBoardItem Item;public OriginalBoardItem CurrentItem{get{Verify.Trace.Add("item");return Item;}}public bool HasCurrentItem()=>Item!=null;public bool HasNextItem()=>false;}
public interface IOriginalMatchBlockingState{bool CanEnterToMatch(OriginalGridCell cell);}
}
class Verify:IOriginalBoosterTouchSelection,IOriginalMatchHighlightView{
public static List<string> Trace=new List<string>();public bool Selected;public int Id;
public bool IsBoosterSelected{get{Trace.Add("booster");return Selected;}}
public void Highlight(bool value){Trace.Add("highlight:"+Id+":"+(value?1:0));}
class Static:IOriginalStaticTouchItem,IOriginalStaticPressItem{
public int Id;public bool Block;public bool IsTouchBlocking=>Block;public bool IsTouchBlockingExceptChain=>Block;
public bool PlayTouchDownAnimation(){Trace.Add("static:"+Id);return true;}public void PlayInvalidMoveAnimation(){Trace.Add("invalid:"+Id);}}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');int above=int.Parse(a[1]),below=int.Parse(a[2]),kind=int.Parse(a[3]);bool enabled=a[4]=="1",block=a[5]=="1",prior=a[7]=="1";
var service=new Verify{Selected=a[6]=="1"};var router=new OriginalLevelPressRouter(service);var previous=new OriginalMatchBoardItem{SwapView=new Verify{Id=1}};
if(prior){var old=new OriginalGridCell();old.Mediation.Item=previous;router.TouchCell(old);}Trace.Clear();
var cell=new OriginalGridCell();if(!enabled)cell.DisableTouch();var current=kind==0?null:kind==1?(OriginalBoardItem)new OriginalMatchBoardItem{SwapView=new Verify{Id=0}}:new OriginalBoardItem();cell.Mediation.Item=current;
for(int i=0;i<above;i++)cell.StaticTouch.AboveItems.Add(new Static{Id=i,Block=block});for(int i=0;i<below;i++)cell.StaticTouch.BelowItems.Add(new Static{Id=10+i});
string result="";if(a[0]=="L")router.TouchCell(cell);else if(a[0]=="D")OriginalCellPress.TouchDown(cell);else if(a[0]=="I")OriginalCellPress.TryPlayInvalidMoveAnimationForSwap(cell);else if(a[0]=="M")result=OriginalCellPress.IsTouchingMatchItem(cell)?"1":"0";else if(a[0]=="K")router.ClearTouchedItemState();
int before=router.TouchedItem==null?0:ReferenceEquals(router.TouchedItem,previous)?2:1;
router.CancelTouchedItemAnimation();router.CancelTouchedItemAnimation();
Console.WriteLine(string.Join("|",Trace)+"#"+before+","+(router.TouchedItem==null?0:1)+","+result);
}}}
