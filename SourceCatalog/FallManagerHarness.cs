using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
namespace Whitebox.Foundation
{
    public interface IOriginalItemFallReadiness { bool IsItemSteady { get; } }
    public interface IOriginalMatchFreezeOperations { void FreezeFall(OriginalGridCell cell); }
}
static class FallManagerHarness
{
    sealed class Fall : IOriginalFallUpdateComponent
    {
        readonly Inputs f; readonly int id;
        public Fall(Inputs f, int id) { this.f=f; this.id=id; }
        public bool IsItemSteady { get { f.Record("steady,"+id); return f.Steady[id-1]!=0; } }
        public void ManualUpdate()
        {
            f.Record("fall,"+id); f.Updates[id-1]++;
            if (f.H[7]==1) f.Items[id-1].Mediator.TargetCell?.ClearTargetItem();
            if (f.H[7]==2) f.Steady[id-1]^=1;
            if (f.H[7]==3) f.Operations.OperationStart(0);
        }
        public void SetFillingCell(OriginalFillingCell cell) { f.Record("filling,"+id+","+f.Id(cell)); }
    }
    sealed class Inputs : IOriginalFallTraversal, IOriginalBoardStartGridAccess, IOriginalFallTargetPlanning, IOriginalFillingItemCreation
    {
        public int[] H; public int[][] C;
        public OriginalGridCell[] Cells=new OriginalGridCell[9];
        public OriginalBoardItem[] Items=new OriginalBoardItem[9];
        public int[] Steady=new int[9], Updates=new int[9];
        public readonly List<string> Trace=new List<string>();
        public OriginalGameOperations Operations=new OriginalGameOperations(()=>42);
        public int Calls;
        IOriginalGridIteratorStrategy[] strategies=new IOriginalGridIteratorStrategy[6];
        public int Id(OriginalGridCell c) { if(c==null)return -1; for(int i=0;i<9;i++)if(ReferenceEquals(c,Cells[i]))return i;throw new Exception(); }
        public void Record(string s) {Trace.Add(s);if(++Calls==H[4])throw new InvalidOperationException();}
        public bool IsPortalLevel {get{Record("portal");return H[0]!=0;}}
        public int BoardStartY => H[1];
        public OriginalGridCell CurrentBoardBottomLeftCell => GetCell(0,H[1]);
        public OriginalGridCell GetCell(int x,int y) {Record("cell,"+x+","+y);return x<0||x>=3||y<0||y>=3?null:Cells[y*3+x];}
        public bool IsInCurrentBoard(OriginalGridCell c)=>c.BoardId==H[2];
        public IOriginalGridIteratorStrategy GetIteratorStrategy(int type){Record("strategy,"+type);return strategies[type];}
        public void ClearVisitCells() { }
        public void FindTargetItemOrFillingCell(OriginalGridCell c,out OriginalFillingCell filling)
        {
            filling=null;int n=Id(c);Record("plan,"+n);c.ShouldFindCross=C[n][8]!=0;
            if(C[n][5]!=0)c.Mediation.SetTargetItem(Items[C[n][5]-1]);
            if(C[n][6]!=0)filling=(OriginalFillingCell)Cells[7];
        }
        public void FindCrossItem(OriginalGridCell c){int n=Id(c);Record("cross,"+n);if(C[n][7]!=0)c.Mediation.SetTargetItem(Items[C[n][7]-1]);}
        public bool AddRemainingItemsToFillingCellQueue(int column){Record("queue,"+column);return (H[5]&(1<<(column+1)))!=0;}
        public OriginalBoardItem CreateItemForFillingCellAt(float x,float y,OriginalFillingCell cell)
        {Record("create,"+(int)x+","+(int)y+","+Id(cell));cell.AddCreatedItem(Items[8]);return Items[8];}
        public void Create(string cellText,string itemText)
        {
            C=Array.ConvertAll(cellText.Split('/'),s=>Array.ConvertAll(s.Split(','),int.Parse));
            var v=Array.ConvertAll(itemText.Split('/'),s=>Array.ConvertAll(s.Split(','),int.Parse));
            for(int i=0;i<9;i++){Items[i]=new OriginalBoardItem(i+1,1);Steady[i]=v[i][0];if(v[i][1]==0)Items[i].FallComponent=new Fall(this,i+1);}
            for(int i=0;i<9;i++){Cells[i]=i==7?new OriginalFillingCell(i%3,i/3,C[i][0],C[i][1],1,false,false,0):new OriginalGridCell(i%3,i/3,C[i][0],C[i][1]);Cells[i].ShouldFindCross=C[i][4]!=0;}
            for(int i=0;i<9;i++)
            {
                for(int d=0;d<8;d++){Cells[i].Neighbors.GetPoint(d,out int x,out int y);Cells[i].Neighbors.Set(d,x<0||x>=3||y<0||y>=3?null:Cells[y*3+x]);}
                if(C[i][2]!=0)Cells[i].Mediation.SetNextItem(Items[C[i][2]-1]);
                if(C[i][3]!=0)Cells[i].Mediation.SetTargetItem(Items[C[i][3]-1]);
            }
            strategies[0]=new OriginalBottomLeftVerticalStrategy(this);strategies[1]=new OriginalBottomLeftHorizontalStrategy(this);
            strategies[4]=new OriginalTotalBottomLeftVerticalStrategy(this);strategies[5]=new OriginalTotalBottomLeftHorizontalStrategy(this);
        }
        public string State(OriginalFallManager manager,OriginalFallRefillColumns columns)
        {
            int pending=0,disabled=0;for(int i=-1;i<=2;i++){if(columns.IsTopBoardCheckPending(i))pending|=1<<(i+1);if(columns.IsTopColumnDisabled(i))disabled|=1<<(i+1);}
            var values=new List<string>{string.Join(",",new[]{manager.IsAllItemsSteady?1:0,Operations.Container.GetCount(0),pending,disabled,((OriginalFillingCell)Cells[7]).LastCreatedItem?.Id??0})};
            foreach(var c in Cells)values.Add(string.Join(",",new[]{c.Mediation.NextItem?.Id??0,c.Mediation.TargetItem?.Id??0,c.ShouldFindCross?1:0}));
            for(int i=0;i<9;i++)values.Add(string.Join(",",new[]{Id(Items[i].Mediator.TargetCell?.Owner),Updates[i],Steady[i]}));
            return string.Join("/",values);
        }
    }
    static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var a=line.Split('#');var f=new Inputs{H=Array.ConvertAll(a[0].Split(','),int.Parse)};f.Create(a[1],a[2]);
            var columns=new OriginalFallRefillColumns();var manager=new OriginalFallManager(columns,f,f,f.H[6]!=0?null:f.Operations);manager.Enable(f.H[3]!=0);
            var output=new List<string>();
            foreach(var command in a[3].Split(';'))
            {
                f.Trace.Clear();f.Calls=0;var p=command.Split(',');int n=p.Length>1?int.Parse(p[1]):0;string result="-";
                try{switch(p[0])
                {
                    case "i":manager.InitializeGrid(f);break;
                    case "m":manager.ManualUpdate();break;
                    case "t":manager.ClearTargetItems();break;
                    case "s":manager.CalculateStraightFalls();break;
                    case "x":manager.CalculateCrossFalls();break;
                    case "u":manager.CallFallUpdates();break;
                    case "e":manager.Enable(n!=0);break;
                    case "d":columns.DisableReFillForTopColumn(n);break;
                    case "a":columns.EnableReFillForTopColumn(n);break;
                    case "c":manager.Clear(n!=0);break;
                    case "h":f.H[n]=int.Parse(p[2]);break;
                }}catch{result="error";}
                output.Add(result+":"+f.State(manager,columns)+":"+string.Join(";",f.Trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
