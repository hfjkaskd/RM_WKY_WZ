using System;
using System.Collections.Generic;
using Whitebox.Foundation;
// Regression composition for the production fall manager, planner, motion,
// mediation, grid, static queries and item availability. Only presentation,
// clock/audio, unrelated operation accounting, constructors, refill choice and
// the absent ice-level counter are supplied below. Special fall behavior comes
// from OriginalItemFallAvailability.cs; this does not render Unity prefabs or
// execute the full match/explosion/creation-animation lifecycle.
namespace UnityEngine { public static class Debug { public static void LogError(object x) { throw new Exception(x.ToString()); } } }
namespace Whitebox.Foundation {
public sealed class OriginalGameOperations { public void OperationStart(int n){} public void OperationFinish(int n){} public void OperationReset(int n){} }
public interface IOriginalMatchFreezeOperations { void FreezeFall(OriginalGridCell c); }
public interface IOriginalMatchBlockingState { bool HasTouchBlockingItemExceptChain(OriginalGridCell c); bool HasIncomingTouchBlockingItem(OriginalGridCell c); }
public interface IOriginalBoardEndGridAccess:IOriginalBoardStartGridAccess { OriginalGridCell CurrentBoardTopLeftCell {get;} }
public sealed class OriginalInitialItemAmounts { public bool IsThereItemInLevel(int type) { return false; } }
public static class OriginalLevelRules { public static int GetIndexFromPoint(int x,int y,int width) {return y*width+x;} public static int AsCellType(int n){return n==0?0:1;} }
public struct OriginalSwapPoint { public float X,Y,Z; public OriginalSwapPoint(float x,float y,float z){X=x;Y=y;Z=z;} }
public struct OriginalSwapSorting { public int Layer,Order;public bool SortEverything;public OriginalSwapSorting(int l,int o,bool a){Layer=l;Order=o;SortEverything=a;} }
public sealed partial class OriginalFallComponentState { public float StateChangedTime {get;set;} }
public sealed partial class OriginalMatchBoardItem:OriginalBoardItem { public OriginalMatchBoardItem(int id):base(id,1){} }
public abstract partial class OriginalSpecialBoardItem:OriginalBoardItem { protected OriginalSpecialBoardItem(int id):base(id,2){} public override bool IsSpecialItem=>true; }
}
class DiagonalFallHarness {
sealed class Special:OriginalSpecialBoardItem { public Special(int id):base(id){} }
sealed class View:IOriginalFallItemView,IOriginalItemSwapView {
 public OriginalSwapPoint Position {get;set;} public OriginalSwapPoint LocalPosition=>Position;
 public bool Swapping; public bool IsSwapping=>Swapping; public bool IsFillMaskEnabled=>false; public bool IsUnderNonFallBlockingAboveStaticItem=>false;
 public int CrossCount;
 public void DisableFillMask(){} public void ArrangeSelfSorting(){} public void UpdateSorting(OriginalSwapSorting s){CrossCount++;}
 public void UpdateFallState(int s,float p,float m){} public void FallStarted(){} public void FallEnded(){} public void FallAnimationStarted(){}
 public void SetIsUnderNonFallBlockingAboveStaticItem(bool u){}
}
sealed class Board:IOriginalFallMotionServices,IOriginalFallLevelStatus,IOriginalFillingItemCreation {
 public readonly OriginalCellGrid Grid; public readonly OriginalFallManager Fall; public readonly OriginalFallTargetPlanner Planner;
 public readonly List<OriginalBoardItem> Items=new List<OriginalBoardItem>(); public int Created,Frame;
 public bool IsPortalLevel=>false; public bool IsMultipleBoardLevel=>false; public bool IsLevelWon=>false;
 public float LevelTime=>Frame/60f;public float LevelDeltaTime=>1f/60f;public float MaxFallSpeed=>20f;
 public Board(int w,int h,bool refill){
  Grid=new OriginalCellGrid(w,h);var ops=new OriginalGameOperations();
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   OriginalGridCell c=refill&&y==h-1?(OriginalGridCell)new OriginalFillingCell(x,y,2,0,1,false,false,0):new OriginalGridCell(x,y,1);
   c.BindGameOperations(ops);Grid.SetCell(x,y,c);
  }
  Grid.CacheNeighbors();var layout=OriginalBoardLayout.Create(w,h,new int[w*h],false,false);
  var context=new OriginalBoardFallContext(Grid,layout,new OriginalInitialItemAmounts(),this,null);var columns=new OriginalFallRefillColumns();
  Planner=new OriginalFallTargetPlanner(context,columns);Fall=new OriginalFallManager(columns,Planner,this,ops);Fall.InitializeGrid(context);
 }
 public OriginalBoardItem Add(int x,int y,int type){var i=Create(type,x,y);Grid.GetCell(x,y).Mediation.SetAllItems(i);return i;}
 private OriginalBoardItem Create(int type,float x,float y){
  int id=++Created;OriginalBoardItem i=type==0?(OriginalBoardItem)new OriginalMatchBoardItem(id):type==1?(OriginalBoardItem)new Special(id):new OriginalBoxBoardItem(id,6);
  var v=new View{Position=new OriginalSwapPoint(x,y,0)};i.BindSwapView(v);i.MarkViewCreated();
  var f=new OriginalFallComponentState(i);i.FallComponent=f;f.BindMotionServices(this);Items.Add(i);return i;
 }
 public OriginalBoardItem CreateItemForFillingCellAt(float x,float y,OriginalFillingCell f){var i=Create(0,x,y);f.AddCreatedItem(i);return i;}
 public IOriginalFallItemView GetView(OriginalBoardItem i)=>(View)i.SwapView;
 public OriginalSwapPoint GetItemPosition(OriginalBoardItem i)=>GetView(i).Position;
 public OriginalSwapPoint GetCellPosition(OriginalGridCell c)=>new OriginalSwapPoint(c.X,c.Y,0);
 public bool IsInCurrentBoard(OriginalGridCell c)=>c.BoardId==0;
 public OriginalSwapSorting GetCrossSorting(OriginalBoardItem i)=>new OriginalSwapSorting(0,0,false);
 public bool HasNonFallBlockingAboveStaticItemInLevel()=>false;
 public bool IsUnderNonFallBlockingAboveStaticItem(OriginalBoardItem i)=>i.IsUnderHoney()||i.IsUnderRoof();
 public bool HasStaticItem(OriginalGridCell c,int type)=>c.StaticTouch.HasStaticItem(type);
 public void PlayFallHit(){}
 public void Run(int frames){for(int k=0;k<frames;k++){Frame++;Fall.ManualUpdate();CheckOwnership();}}
 public int Filled(int x0,int x1,int y0,int y1){int count=0;for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)if(Grid.GetCell(x,y).Mediation.CurrentItem!=null)count++;return count;}
 public int CrossItems(){int c=0;foreach(var i in Items)if(((View)i.SwapView).CrossCount>0)c++;return c;}
 public string Point(OriginalBoardItem i){var c=i.Mediator.CurrentCell?.Owner;return c==null?"none":c.X+","+c.Y;}
 public void CheckOwnership(){foreach(var i in Items){var m=i.Mediator;if(m.CurrentCell!=null&&!object.ReferenceEquals(m.CurrentCell.CurrentItem,i))throw new Exception("current ownership");if(m.NextCell!=null&&!object.ReferenceEquals(m.NextCell.NextItem,i))throw new Exception("next ownership");if(m.TargetCell!=null&&!object.ReferenceEquals(m.TargetCell.TargetItem,i))throw new Exception("target ownership");}
  for(int y=0;y<Grid.Height;y++)for(int x=0;x<Grid.Width;x++){
   var m=Grid.GetCell(x,y).Mediation;
   if(m.CurrentItem!=null&&!object.ReferenceEquals(m.CurrentItem.Mediator.CurrentCell,m))throw new Exception("reverse current ownership");
   if(m.NextItem!=null&&!object.ReferenceEquals(m.NextItem.Mediator.NextCell,m))throw new Exception("reverse next ownership");
   if(m.TargetItem!=null&&!object.ReferenceEquals(m.TargetItem.Mediator.TargetCell,m))throw new Exception("reverse target ownership");
  }
 }
}
static int failures;
static void Check(bool value,string label){Console.WriteLine((value?"PASS ":"FAIL ")+label);if(!value)failures++;}
static Board Pocket(int type,int sourceX){var b=new Board(3,3,false);for(int y=0;y<3;y++)for(int x=0;x<3;x++)if(!(x==1&&y==0))b.Add(x,y,x==sourceX&&y==1?type:2);return b;}
static void Main(){
 for(int type=0;type<=1;type++)for(int sourceX=0;sourceX<=2;sourceX+=2){
  var b=Pocket(type,sourceX);
  var item=b.Grid.GetCell(sourceX,1).Mediation.CurrentItem;b.Run(120);b.CheckOwnership();
  Check(b.Grid.GetCell(1,0).Mediation.CurrentItem==item&&b.Fall.IsAllItemsSteady,"blocked_above type="+type+" source="+sourceX+" final="+b.Point(item));
 }
 for(int special=0;special<=1;special++){
  var b=new Board(9,10,true);
  for(int y=0;y<=8;y++)for(int x=0;x<9;x++)if(y==4||x==4)b.Add(x,y,2);else if(y>=5)b.Add(x,y,special==1&&x==7&&y==5?1:0);
  var item=b.Grid.GetCell(7,5).Mediation.CurrentItem;b.Grid.GetCell(7,4).Mediation.ClearAllItems();
  b.Run(1200);b.CheckOwnership();Check(b.Filled(5,8,0,3)==15&&b.CrossItems()>0&&b.Fall.IsAllItemsSteady,"level7_opening special="+special+" filled="+b.Filled(5,8,0,3)+"/16 cross_items="+b.CrossItems()+" tracked="+b.Point(item));
  Check(b.Grid.GetCell(5,3).Mediation.CurrentItem==null&&b.Filled(0,3,0,3)==0,"closed_cells_and_left_basin_remain_empty special="+special);
  for(int y=0;y<=8;y++)Check(b.Grid.GetCell(4,y).Mediation.CurrentItem is OriginalBoxBoardItem,"center_wall_preserved y="+y+" special="+special);
  b.Grid.GetCell(6,4).Mediation.ClearAllItems();b.Run(1200);
  Check(b.Filled(5,8,0,3)==16&&b.Grid.GetCell(6,4).Mediation.CurrentItem!=null&&b.Fall.IsAllItemsSteady,"second_opening_unlocks_last_corner special="+special+" filled="+b.Filled(5,8,0,3)+"/16");
 }
 // Independent motion validates both diagonal directions and the creation-cell freeze contract.
 for(int targetX=0;targetX<=2;targetX+=2){
  var b=new Board(3,3,false);var i=b.Add(1,1,1);var source=b.Grid.GetCell(1,1);var target=b.Grid.GetCell(targetX,0);
  source.FreezeFall();target.Mediation.SetTargetItem(i);var f=(OriginalFallComponentState)i.FallComponent;
  for(int k=0;k<20;k++)f.ManualUpdate();bool held=b.Point(i)=="1,1"&&((View)i.SwapView).Position.Y==1;
  source.UnFreezeFall();for(int k=0;k<60;k++)f.ManualUpdate();b.CheckOwnership();
  Check(held&&b.Point(i)==targetX+",0"&&((View)i.SwapView).CrossCount>0&&f.IsItemSteady,"forced_motion target="+targetX+" frozen_held="+held+" final="+b.Point(i));
 }
 for(int sourceX=0;sourceX<=2;sourceX+=2)for(int type=0;type<=1;type++){
  var b=Pocket(type,sourceX);var source=b.Grid.GetCell(sourceX,1);var item=source.Mediation.CurrentItem;
  source.FreezeFall();b.Run(30);Check(b.Point(item)==sourceX+",1","source_freeze_holds type="+type+" x="+sourceX);
  source.UnFreezeFall();b.Run(120);Check(b.Point(item)=="1,0","source_unfreeze_resumes type="+type+" x="+sourceX);
  b=Pocket(type,sourceX);source=b.Grid.GetCell(sourceX,1);item=source.Mediation.CurrentItem;var target=b.Grid.GetCell(1,0);
  target.FreezeFall();b.Run(30);Check(target.Mediation.CurrentItem==null&&b.Point(item)==sourceX+",1","target_freeze_holds type="+type+" x="+sourceX);
  target.UnFreezeFall();b.Run(120);Check(target.Mediation.CurrentItem==item,"target_unfreeze_resumes type="+type+" x="+sourceX);
 }
 for(int type=0;type<=1;type++){
  var b=Pocket(type,0);var diagonal=b.Grid.GetCell(0,1).Mediation.CurrentItem;b.Grid.GetCell(1,1).Mediation.ClearAllItems();var straight=b.Add(1,1,0);
  b.Run(120);Check(b.Grid.GetCell(1,0).Mediation.CurrentItem==straight&&b.Point(diagonal)=="0,1","straight_supply_priority type="+type);
  b=Pocket(type,0);diagonal=b.Grid.GetCell(0,1).Mediation.CurrentItem;b.Grid.GetCell(0,0).Mediation.ClearAllItems();
  b.Run(120);Check(b.Point(diagonal)=="0,0"&&b.Grid.GetCell(1,0).Mediation.CurrentItem==null,"reserved_straight_source_not_stolen type="+type);
  b=Pocket(type,0);diagonal=b.Grid.GetCell(0,1).Mediation.CurrentItem;var occupied=b.Add(1,0,2);
  b.Run(120);Check(b.Grid.GetCell(1,0).Mediation.CurrentItem==occupied&&b.Point(diagonal)=="0,1","occupied_target_preserved type="+type);
  b=Pocket(type,0);diagonal=b.Grid.GetCell(0,1).Mediation.CurrentItem;((View)diagonal.SwapView).Swapping=true;
  b.Run(30);Check(b.Point(diagonal)=="0,1","swap_blocks_fall type="+type);
  ((View)diagonal.SwapView).Swapping=false;b.Run(120);Check(b.Point(diagonal)=="1,0","swap_end_resumes type="+type);
 }
 for(int leftType=0;leftType<=1;leftType++)for(int rightType=0;rightType<=1;rightType++){
  var b=Pocket(leftType,0);b.Grid.GetCell(2,1).Mediation.ClearAllItems();var right=b.Add(2,1,rightType);var left=b.Grid.GetCell(0,1).Mediation.CurrentItem;
  b.Run(120);Check(b.Grid.GetCell(1,0).Mediation.CurrentItem==(leftType==0?left:right),"original_side_preference left="+leftType+" right="+rightType);
 }
 for(int hasNext=0;hasNext<=1;hasNext++){
  var b=Pocket(1,0);var above=b.Grid.GetCell(1,1);var target=b.Grid.GetCell(1,0);var side=b.Grid.GetCell(0,1).Mediation.CurrentItem;
  above.Mediation.ClearAllItems();var incoming=b.Add(1,2,0);incoming.Mediator.ClearFromAllRegisteredCells();above.Mediation.SetTargetItem(incoming);
  if(hasNext!=0)above.Mediation.SetNextItem(incoming);
  Check(b.Planner.FindTargetItemFromCrossCell(target,target,0)==null,"incoming_current_null_reservation next="+hasNext);
  above.Mediation.ClearAllItems();Check(b.Planner.FindTargetItemFromCrossCell(target,target,0)==side,"no_incoming_target_allows_side next="+hasNext);
 }
 Console.WriteLine("RESULT failures="+failures);Environment.ExitCode=failures==0?0:1;
}
}
