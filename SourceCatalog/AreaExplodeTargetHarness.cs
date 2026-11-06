using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public interface IOriginalLevelSeedRandom { int Next(int min, int max); }
    public struct OriginalCellPoint
    { public int X,Y; public static readonly OriginalCellPoint Default = new OriginalCellPoint(-1,-1); public OriginalCellPoint(int x,int y){X=x;Y=y;} }
    public struct OriginalExplodeData { public int Trigger; public bool CanSpreadJelly; }
    public interface IOriginalExplodeTarget { }
    public class OriginalGridCell { public int Id,X,Y,Kind,Score,Above;public bool IsNormalCell,Add,Winning,Below; }
    public interface IOriginalGridIteratorStrategy { OriginalGridCell FirstCell(); OriginalGridCell GetNext(OriginalGridCell cell); }
    public struct OriginalGridIterator
    {
        private IOriginalGridIteratorStrategy strategy;private OriginalGridCell cell;private bool first;
        public OriginalGridIterator(IOriginalGridIteratorStrategy s){strategy=s;cell=s.FirstCell();first=true;}
        public void Reset(){cell=strategy.FirstCell();first=true;}
        public OriginalGridCell MoveNext(){if(first)first=false;else if(cell!=null)cell=strategy.GetNext(cell);return cell;}
    }
}
class AreaExplodeTargetHarness : IOriginalAreaExplodeTargetServices,IOriginalLevelSeedRandom,IOriginalGridIteratorStrategy
{
    private readonly List<OriginalGridCell> cells=new List<OriginalGridCell>();
    private readonly Dictionary<long,OriginalGridCell> grid=new Dictionary<long,OriginalGridCell>();
    private readonly List<int> draws=new List<int>();private int randomValue,winningCalls;
    private static long Key(int x,int y)=>((long)x<<32)|(uint)y;
    public void RequireSupportedArea(OriginalExplodeData data){if(data.Trigger!=13||data.CanSpreadJelly)throw new Exception("Unsupported harness input");}
    public OriginalGridCell GetCell(int x,int y){grid.TryGetValue(Key(x,y),out var cell);return cell;}
    public int GetScore(OriginalGridCell cell,int guided,bool jelly)=>cell.Score;
    public bool IsUncoveredMatchOrSpecial(OriginalGridCell cell) => cell.Above == 0 && cell.Kind > 0 && cell.Kind <= 5;
    public bool ShouldPenalizeAreaLightball(OriginalGridCell cell) => cell.Kind == 5 && cell.Above != 1;
    public bool HasTargetableBelowItemUnderLightball(OriginalGridCell cell) => cell.Kind == 5 && cell.Above == 0 && cell.Below;
    public void BeginAreaScore(){}
    public bool ShouldAddAreaScore(OriginalGridCell cell)=>cell.Add;
    public bool IsAreaWinning(OriginalCellPoint point,OriginalExplodeData data)=>GetCell(point.X,point.Y).Winning;
    public IOriginalExplodeTarget AddIncomingExploder(OriginalGridCell cell,OriginalExplodeData data)=>throw new Exception("Separate model lifetime owner");
    public OriginalGridCell FirstCell()=>cells.Count==0?null:cells[0];
    public OriginalGridCell GetNext(OriginalGridCell cell){int index=cells.IndexOf(cell)+1;return index<cells.Count?cells[index]:null;}
    public int Next(int min,int max){draws.Add(max);return min+randomValue%(max-min);}
    static int[] Ints(string text)=>Array.ConvertAll(text.Split(','),int.Parse);
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('|');var h=new AreaExplodeTargetHarness{randomValue=int.Parse(p[0])};
            foreach(string entry in p[1].Split(';'))if(entry.Length>0)
            {
                int[] v=Ints(entry);var cell=new OriginalGridCell{Id=v[0],X=v[1],Y=v[2],Kind=v[3],Score=v[4],IsNormalCell=v[5]!=0,Add=v[6]!=0,Winning=v[7]!=0,Above=v[8],Below=v[9]!=0};
                h.cells.Add(cell);h.grid.Add(Key(cell.X,cell.Y),cell);
            }
            var finder=new OriginalAreaExplodeTargetFinder(h,h,h);finder.WinningComboSelected+=value=>h.winningCalls++;
            var data=new OriginalExplodeData{Trigger=13};var scores=new List<string>();
            foreach(var cell in h.cells)
            {int score=finder.CalculateScore(new OriginalCellPoint(cell.X,cell.Y),data,out bool positive);scores.Add(score+","+(positive?1:0));}
            var selected=finder.FindHighestScore(data);
            Console.WriteLine(selected.X+","+selected.Y+"|"+h.winningCalls+"|"+string.Join(",",h.draws)+"|"+string.Join(";",scores));
        }
    }
}
