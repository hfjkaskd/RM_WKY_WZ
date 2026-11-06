using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
namespace Whitebox.Foundation
{
    // Storage/CLR boundaries only. All new runtime query/callback logic compiles below.
    public interface IOriginalMatchBlockingState
    { bool HasTouchBlockingItemExceptChain(OriginalGridCell cell);bool HasIncomingTouchBlockingItem(OriginalGridCell cell); }
    public partial class OriginalBoardItem { public object FallComponent{get;set;} }
    public sealed partial class OriginalMatchBoardItem : OriginalBoardItem
    { public OriginalMatchBoardItem(int id,int type,int match):base(id,type){} }
    public abstract partial class OriginalSpecialBoardItem : OriginalBoardItem
    { protected OriginalSpecialBoardItem():base(1,1){} }
    public struct OriginalSwapSorting
    {
        public int Layer,Order; public bool SortEverything;
        public OriginalSwapSorting(int layer,int order,bool all){Layer=layer;Order=order;SortEverything=all;}
    }
    class StaticFallHarness : IOriginalItemFallViewDelegate,IOriginalItemFallSortingSurface,IOriginalItemSwapView,IOriginalExtraStaticFallExploders
    {
        class Static : IOriginalStaticFallItem
        {
            public StaticFallHarness Owner; public int Id,Type,CellIndex=-1;public bool Above,Block;
            public int StaticItemType=>Type;
            public OriginalGridCell CurrentCell{get=>CellIndex<0?null:Owner.cells[CellIndex];set=>CellIndex=value==null?-1:Array.IndexOf(Owner.cells,value);}
            public bool IsTouchBlocking{get{Owner.Record("touch,"+Id);return Above;}}
            public bool DoesBlockFall{get{Owner.Record("block,"+Id);return Block;}}
            public bool IsTouchBlockingExceptChain=>false;
            public int RemainingExplodeCount(){Owner.Record("remaining,"+Id);return Owner.h[12];}
        }
        class Roof : Static,IOriginalRoofFallItem { public void NotifyForFallUnderneath(){Owner.Record("notify,"+Id);Owner.notifies++;} }
        class Item : OriginalBoardItem
        { public StaticFallHarness Owner;public Item(StaticFallHarness owner):base(1,1){Owner=owner;}public override bool IsFallable{get{Owner.Record("fallable");return Owner.h[0]!=0;}} }
        int[] h;OriginalGridCell[] cells;Static[] items;Item item;OriginalItemFallCallbacks callbacks;OriginalSwapSorting last;
        List<string> trace=new List<string>();int calls,currentCalls,notifies;
        void Record(string s){trace.Add(s);if(++calls==h[11])throw new Exception();}
        public bool IsSwapping=>h[1]!=0;
        public OriginalGridCell CurrentCell
        {
            get
            {
                Record("current");int c=currentCalls++==0?h[5]:h[6];
                if(h[7]!=0)callbacks.BindModel(null);
                return c<0?null:cells[c];
            }
        }
        public int GetAtlasId(){Record("atlas");return -1;}
        public bool ShouldSortByCell(){Record("bycell");return false;}
        public bool IsReverseSorted(){Record("reverse");return false;}
        public OriginalSwapSorting GetItemSorting(int atlas,bool byCell,int x,int y,bool reverse)
        {if(h[8]==0)throw new NullReferenceException();Record("sorting,"+atlas+","+x+","+y);return new OriginalSwapSorting(77,x*100+y,false);}
        public void UpdateSorting(OriginalSwapSorting sorting){Record("update,"+sorting.Layer+","+sorting.Order);last=sorting;}
        public int GetExplodeCountByNeighborsPropellerCombos(OriginalGridCell cell,IOriginalStaticFallItem target){Record("extra");return h[13];}
        void Place(int c,int n)
        {
            item.Mediator.ClearFromAllRegisteredCells();if(c>=0)cells[c].Mediation.SetCurrentItem(item);if(n>=0)cells[n].Mediation.SetNextItem(item);
        }
        string Dump()
        {
            var a=cells.Select(c=>string.Join(",",Enumerable.Range(0,20).Select(t=>c.StaticTouch.GetStaticItem(t)==null?0:((Static)c.StaticTouch.GetStaticItem(t)).Id))+"/"+string.Join(",",c.StaticTouch.AboveItems.Cast<Static>().Select(x=>x.Id))+"/"+string.Join(",",c.StaticTouch.BelowItems.Cast<Static>().Select(x=>x.Id)));
            return string.Join("~",a)+"/"+string.Join(",",items.Select(x=>x.CellIndex))+"/"+(callbacks.IsUnderNonFallBlockingAboveStaticItem?1:0)+","+notifies+","+last.Layer+","+last.Order;
        }
        void Run(string line)
        {
            var all=line.Split('#');h=all[0].Split(',').Select(int.Parse).ToArray();cells=new[]{new OriginalGridCell(2,3,1),new OriginalGridCell(2,2,1),new OriginalGridCell(4,5,1)};
            cells[0].Neighbors.Set(5,cells[1]);foreach(var c in cells)c.BindExtraStaticFallExploders(this);
            items=new Static[20];for(int i=0;i<20;i++)items[i]=(i==7 && h[10]==0)?new Roof{Owner=this,Id=i+1,Type=i,Above=(h[14]&(1<<i))!=0,Block=(h[15]&(1<<i))!=0}:new Static{Owner=this,Id=i+1,Type=i,Above=(h[14]&(1<<i))!=0,Block=(h[15]&(1<<i))!=0};
            item=new Item(this);if(h[2]!=0)item.BindSwapView(this);Place(h[3],h[4]);callbacks=new OriginalItemFallCallbacks(this);if(h[9]!=0)callbacks.BindModel(this);
            var output=new List<string>();foreach(var command in all[1].Split(';'))
            {
                calls=0;currentCalls=0;trace.Clear();string result="-";var p=command.Split(',');int k=p.Length>1?int.Parse(p[1]):0;
                try
                {
                    switch(p[0])
                    {
                        case "a":cells[k].AddStaticItem(items[int.Parse(p[2])]);break;
                        case "c":cells[k].StaticTouch.ClearStaticItem(int.Parse(p[2]));break;
                        case "g":var s=cells[k].StaticTouch.GetStaticItem(int.Parse(p[2]));result=s==null?"0":((Static)s).Id.ToString();break;
                        case "h":result=cells[k].StaticTouch.HasStaticItem(int.Parse(p[2]))?"1":"0";break;
                        case "b":result=cells[k].StaticTouch.HasFallBlockingItem()?"1":"0";break;
                        case "f":result=cells[k].StaticTouch.HasFallBlockingAboveStaticItem()?"1":"0";break;
                        case "n":result=cells[k].StaticTouch.HasNonFallBlockingAboveStaticItem()?"1":"0";break;
                        case "i":result=item.IsAvailableForFall(null)?"1":"0";break;
                        case "u":result=item.IsUnderHoney()?"1":"0";break;
                        case "r":result=item.IsUnderRoof()?"1":"0";break;
                        case "s":result=OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItem(item)?"1":"0";break;
                        case "q":result=cells[k].IsFallBlocked(int.Parse(p[2])!=0,int.Parse(p[3])!=0)?"1":"0";break;
                        case "j":cells[k].HoneySpreadingCount=int.Parse(p[2]);cells[k].RoofSpreadingCount=int.Parse(p[3]);break;
                        case "e":result=cells[k].IsAboveStaticSpreading?"1":"0";break;
                        case "w":result=OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItemSpreading(item)?"1":"0";break;
                        case "k":case "z":var sorting=OriginalCrossFallSorting.Get(p[0]=="k"?item:null,k);result=sorting.Layer+","+sorting.Order+","+(sorting.SortEverything?1:0);break;
                        case "v":callbacks.FallAnimationStarted();break;
                        case "t":callbacks.SetIsUnderNonFallBlockingAboveStaticItem(k!=0);break;
                        case "p":Place(k,int.Parse(p[2]));break;
                        case "d":callbacks.BindModel(k!=0?this:null);break;
                        case "x":h[k]=int.Parse(p[2]);break;
                    }
                }catch{result="error";}
                output.Add(result+":"+Dump()+":"+string.Join(";",trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
        static void Main(){string l;while((l=Console.ReadLine())!=null)new StaticFallHarness().Run(l);}
    }
}
