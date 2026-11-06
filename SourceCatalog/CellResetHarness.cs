using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public class OriginalCellView { }
    public class OriginalStaticTouchMediator { }
    public class OriginalItemMediation { public object TargetCell; }
    public class OriginalBoardItem
    {
        public int Id; public bool HasView=true; public OriginalItemMediation Mediator=new OriginalItemMediation();
        public event Action OnExplodeEvent;
    }
    public class OriginalCellMediation
    {
        public OriginalBoardItem TargetItem;
        public bool HasTargetItem()=>TargetItem!=null;
        public void Reset(){CellResetHarness.Step("mediation");TargetItem=null;}
    }
    public partial class OriginalGridCell
    {
        public int X,BoardId;public OriginalCellView CellView;
        public OriginalCellMediation Mediation=new OriginalCellMediation();
        public OriginalStaticTouchMediator StaticTouch=new OriginalStaticTouchMediator();
        public OriginalGridCell(int x,int y,int type,int boardId,int fill){X=x;BoardId=boardId;}
    }
}
public static class CellResetHarness
{
    static readonly List<string> trace=new List<string>();static int calls,fail,mode;static bool appended;
    static OriginalGridCell cell;static OriginalFillingCell filling;static OriginalBoardItem[] items;
    public static void Step(string label)
    {
        trace.Add(label+","+(cell.CellView==null?0:1)+","+(cell.Mediation.TargetItem==null?0:cell.Mediation.TargetItem.Id)+","+(filling==null||filling.LastCreatedItem==null?0:filling.LastCreatedItem.Id));
        if(++calls==fail)throw new InvalidOperationException();
    }
    sealed class Services:IOriginalCellResetServices
    {
        public void RecycleCellView(OriginalCellView view){Step("cell");if(mode==1)cell.Mediation.TargetItem=items[2];}
        public void RecycleItemView(OriginalBoardItem item)
        {
            if(item==null)throw new NullReferenceException();Step("item"+item.Id);
            if(mode==2)cell.Mediation.TargetItem=null;
            if(mode==3&&item.Id==2&&!appended){filling.AddToTopBoardItemQueue(items[4]);appended=true;}
        }
        public void ResetStaticItems(OriginalStaticTouchMediator owner)=>Step("static");
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var v=Array.ConvertAll(line.Split(','),int.Parse);trace.Clear();calls=0;appended=false;fail=v[6];mode=v[5];
            filling=v[0]==0?null:new OriginalFillingCell(0,0,1,0,1,false,false,0);
            cell=filling==null?new OriginalGridCell(0,0,1,0,0):filling;
            items=new OriginalBoardItem[5];for(int i=1;i<items.Length;i++)items[i]=new OriginalBoardItem{Id=i};
            cell.CellView=v[1]==0?null:new OriginalCellView();cell.Mediation.TargetItem=v[2]==0?null:items[3];
            if(filling!=null)
            {
                if(v[3]!=-1){filling.LastCreatedItem=items[1];if(v[3]==0)items[1].Mediator.TargetCell=new object();}
                for(int i=0;i<v[4];i++)filling.AddToTopBoardItemQueue(items[i+2]);
            }
            bool failed=false;try{cell.Reset(new Services());}catch(Exception){failed=true;}
            var queue=new List<int>();if(filling!=null){OriginalBoardItem item;while((item=filling.GetQueuedItemFromTopBoard())!=null)queue.Add(item.Id);}
            Console.WriteLine((failed?1:0)+"#"+(cell.CellView==null?0:1)+","+(cell.Mediation.TargetItem==null?0:cell.Mediation.TargetItem.Id)+","+(filling==null||filling.LastCreatedItem==null?0:filling.LastCreatedItem.Id)+"#"+string.Join(",",queue)+"#"+string.Join(";",trace));
        }
    }
}
