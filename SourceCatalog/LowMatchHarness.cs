using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public sealed class OriginalLevelRandomSource
    {
        public int Draw;
        public int DifficultyNext(int min,int max){Harness.Trace.Add("random,"+min+","+max);if(max<min)throw new ArgumentOutOfRangeException();return Draw;}
    }
    public class OriginalGridCell
    {
        public OriginalCellNeighbors Neighbors;public OriginalCellMediation Mediation;
        public OriginalGridCell(int index){Neighbors=new OriginalCellNeighbors(index);Mediation=new OriginalCellMediation(index);}
    }
    public sealed class OriginalFillingCell:OriginalGridCell{public OriginalFillingCell(int n):base(n){}}
    public sealed class OriginalCellNeighbors
    {
        public int Index;public OriginalGridCell[] Values=new OriginalGridCell[8];
        public OriginalCellNeighbors(int index){Index=index;}
        public OriginalGridCell Get(int direction){Harness.Trace.Add("neighbor,"+Index+","+direction);return Values[direction];}
    }
    public sealed class OriginalCellMediation
    {
        public int Index;public object Item;
        public OriginalCellMediation(int index){Index=index;}
        public bool HasTargetItem(){Harness.Trace.Add("has,"+Index);return Item!=null;}
        public object TargetItem{get{Harness.Trace.Add("target,"+Index);return Item;}}
    }
    public sealed class OriginalMatchBoardItem{public int MatchType;}
    public sealed class OriginalLevelSetSelection
    {
        public int GetRandomItem(){Harness.Trace.Add("item");return 17;}
        public int GetRandomItemExcept(List<int> values){Harness.Trace.Add("except,"+string.Join(",",values));return 18;}
    }
}
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var part=line.Split('|');int[] h=Array.ConvertAll(part[0].Split(','),int.Parse);
            int[] ratios=part[1]=="null"?null:part[1]==""?new int[0]:Array.ConvertAll(part[1].Split(','),int.Parse);
            int[] types=Array.ConvertAll(part[2].Split(','),int.Parse);Trace.Clear();
            var random=new OriginalLevelRandomSource{Draw=h[1]};var tuning=new OriginalLowMatchTuning(h[3]==1?null:random);
            tuning.UpdateLowMatchRatios(ratios);if(h[4]!=0&&ratios!=null&&ratios.Length>0)ratios[0]=h[4];
            var cells=new OriginalGridCell[25];for(int i=0;i<25;i++){cells[i]=i==22?(OriginalGridCell)new OriginalFillingCell(i):new OriginalGridCell(i);cells[i].Mediation.Item=types[i]<0?null:types[i]==0?(object)new object():new OriginalMatchBoardItem{MatchType=types[i]};}
            for(int i=0;i<25;i++){int x=i%5,y=i/5;if(y>0)cells[i].Neighbors.Values[5]=cells[i-5];if(x>0)cells[i].Neighbors.Values[7]=cells[i-1];if(x<4)cells[i].Neighbors.Values[3]=cells[i+1];}
            int value=0;bool fault=false;
            try{switch(h[0]){case 0:break;case 1:value=tuning.GetLowMatchNeighborCount();break;case 2:value=tuning.GetTunedItem(h[3]==2?null:new OriginalLevelSetSelection(),h[3]==3?null:(OriginalFillingCell)cells[22]);break;case 3:value=OriginalLowMatchTuning.GetForbiddenTypeForOneNeighbor(h[3]==3?null:cells[h[2]],h[1]);break;case 4:value=OriginalLowMatchTuning.GetForbiddenTypeForTwoNeighbors(h[3]==3?null:cells[h[2]],h[1]);break;}}
            catch(NullReferenceException){fault=true;}catch(ArgumentOutOfRangeException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+(tuning.IsEnabled?1:0)+","+tuning.TotalRatio+","+value+"#"+string.Join(";",Trace));
        }
    }
}
