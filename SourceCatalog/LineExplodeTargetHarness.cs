using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public interface IOriginalLevelSeedRandom{int Next(int min,int max);}
    public struct OriginalCellPoint{public int X,Y;public OriginalCellPoint(int x,int y){X=x;Y=y;}public static readonly OriginalCellPoint Default=new OriginalCellPoint(-1,-1);}
    public struct OriginalExplodeData{public int Trigger;public bool CanSpreadJelly;}
    public interface IOriginalExplodeTarget {}
    public class OriginalGridCell{public int Id,X,Y,CellType,Kind,Score;public bool Add,Valid,Above,Below;}
}
class LineExplodeTargetHarness:IOriginalLineExplodeTargetServices,IOriginalLevelSeedRandom
{
    OriginalGridCell[,] cells;bool[] wins;uint random;int winningCalls;
    List<string> draws=new List<string>();
    public int Width=>cells.GetLength(0);public int BoardStartY=>0;public int BoardEndY=>cells.GetLength(1)-1;
    public void RequireSupportedLines(OriginalExplodeData d){if(d.CanSpreadJelly||d.Trigger!=10&&d.Trigger!=11)throw new Exception("Unsupported fixture");}
    public OriginalGridCell GetCell(int x,int y)=>x>=0&&x<Width&&y>=0&&y<=BoardEndY?cells[x,y]:null;
    public int GetScore(OriginalGridCell c,int type,bool jelly)=>c.Score;
    public bool ShouldAddLineScore(OriginalGridCell c)=>c.Add;
    public void BeginLineScore(){}
    public bool IsUncoveredLightball(OriginalGridCell c) => c.Kind == 5 && !c.Above;
    public bool HasTargetableBelowItemUnderLightball(OriginalGridCell c) => IsUncoveredLightball(c) && c.Below;
    public bool IsLineWinning(bool col,int index,OriginalExplodeData d)=>wins[index];
    public bool IsLineTargetValid(OriginalGridCell c,int type)=>c.Kind!=0&&c.Valid;
    public IOriginalExplodeTarget AddIncomingExploder(OriginalGridCell c,OriginalExplodeData d)=>throw new Exception("Separate reservation owner");
    public int Next(int min,int max){draws.Add(min+","+max);random=unchecked(random*1664525u+1013904223u);return min+(int)(random%(uint)(max-min));}
    static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var a=line.Split('|');var cfg=Array.ConvertAll(a[0].Split(','),int.Parse);var h=new LineExplodeTargetHarness{random=(uint)cfg[3]};
            h.cells=new OriginalGridCell[cfg[0],cfg[1]];h.wins=Array.ConvertAll(a[1].Split(','),s=>s=="1");
            foreach(var v in a[2].Split(';'))
            {var d=Array.ConvertAll(v.Split(','),int.Parse);h.cells[d[1],d[2]]=new OriginalGridCell{Id=d[0],X=d[1],Y=d[2],Kind=d[3],Score=d[4],Add=d[5]!=0,Valid=d[6]!=0,CellType=d[7],Above=d[8]!=0,Below=d[9]!=0};}
            var finder=new OriginalLineExplodeTargetFinder(h,h);finder.WinningComboSelected+=u=>h.winningCalls++;
            var data=new OriginalExplodeData{Trigger=cfg[2]};bool col=data.Trigger==10;var scores=new List<string>();
            for(int i=0;i<h.wins.Length;i++){int n=finder.CalculateScore(col,i,data,out bool positive);scores.Add(n+","+(positive?1:0));}
            var point=finder.FindHighestScore(data);
            Console.WriteLine(point.X+","+point.Y+"|"+h.winningCalls+"|"+string.Join(";",h.draws)+"|"+string.Join(";",scores));
        }
    }
}
