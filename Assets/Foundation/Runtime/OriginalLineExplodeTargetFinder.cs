using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalLineExplodeTargetServices
    {
        int Width { get; }
        int BoardStartY { get; }
        int BoardEndY { get; }
        void RequireSupportedLines(OriginalExplodeData data);
        OriginalGridCell GetCell(int x,int y);
        int GetScore(OriginalGridCell cell,int guidedType,bool jelly);
        bool ShouldAddLineScore(OriginalGridCell cell);
        void BeginLineScore();
        bool IsUncoveredLightball(OriginalGridCell cell);
        bool HasTargetableBelowItemUnderLightball(OriginalGridCell cell);
        bool IsLineWinning(bool column,int index,OriginalExplodeData data);
        bool IsLineTargetValid(OriginalGridCell cell,int guidedType);
        IOriginalExplodeTarget AddIncomingExploder(OriginalGridCell cell,OriginalExplodeData data);
    }
    // ExplodeTargetFinder 0x1c96b94/0x1c97208/0x1c99380/0x1c9a728.
    // Normal no-jelly/no-helper lines. Native separate line scoring, winning
    // priority, useful-negative fallback and shuffled target-cell selection.
    public sealed class OriginalLineExplodeTargetFinder
    {
        private readonly IOriginalLineExplodeTargetServices targets;
        private readonly IOriginalLevelSeedRandom random;
        private int[] columns,rows;
        private bool[] positiveColumns,positiveRows;
        private readonly List<int> winningLines=new List<int>(),highestIndexes=new List<int>();
        private readonly List<OriginalGridCell> lineCells=new List<OriginalGridCell>(14);
        public event Action<bool> WinningComboSelected;
        public OriginalLineExplodeTargetFinder(IOriginalLineExplodeTargetServices targets,IOriginalLevelSeedRandom random)
        {this.targets=targets??throw new ArgumentNullException(nameof(targets));this.random=random??throw new ArgumentNullException(nameof(random));}
        public IOriginalExplodeTarget FindForRocket(OriginalExplodeData data)
        {
            var point=FindHighestScore(data);var cell=targets.GetCell(point.X,point.Y);
            return cell==null?null:targets.AddIncomingExploder(cell,data);
        }
        public int CalculateScore(bool column,int index,OriginalExplodeData data,out bool positive)
        {
            targets.RequireSupportedLines(data);targets.BeginLineScore();positive=false;int total=0;
            int count=column?targets.BoardEndY-targets.BoardStartY+1:targets.Width;
            for(int i=0;i<count;i++)
            {
                var cell=targets.GetCell(column?index:i,column?targets.BoardStartY+i:index);
                if(cell==null)throw new InvalidOperationException("Native line scoring requires allocated grid cells.");
                int score=targets.GetScore(cell,column?1:2,false);
                if(score!=0&&!targets.ShouldAddLineScore(cell))score=0;
                positive |= score > 0 || targets.HasTargetableBelowItemUnderLightball(cell);
                total = unchecked(total + score);
                if (score > 0 && targets.IsUncoveredLightball(cell))
                    total = unchecked(total + (int)0xffe7985d);
            }
            return total;
        }
        public OriginalCellPoint FindHighestScore(OriginalExplodeData data,bool isUserAction=true)
        {
            targets.RequireSupportedLines(data);bool column=data.Trigger==10;
            int height=targets.BoardEndY-targets.BoardStartY+1;
            if(columns==null||columns.Length!=targets.Width) {columns=new int[targets.Width];positiveColumns=new bool[targets.Width];}
            if(rows==null||rows.Length!=height) {rows=new int[height];positiveRows=new bool[height];}
            var scores=column?columns:rows;var flags=column?positiveColumns:positiveRows;winningLines.Clear();
            for(int i=0;i<scores.Length;i++)
            {
                int index=column?i:targets.BoardStartY+i;
                scores[i]=CalculateScore(column,index,data,out flags[i]);
                if(!targets.IsLineWinning(column,index,data))continue;
                WinningComboSelected?.Invoke(isUserAction);winningLines.Add(i);
            }
            int selected;
            if(winningLines.Count>0)selected=winningLines[random.Next(0,winningLines.Count)];
            else
            {
                selected=FindHighestIndex(scores);
                if(scores[selected]<=0)
                {
                    int top=int.MinValue;
                    for(int i=0;i<scores.Length;i++)if(flags[i]&&scores[i]>top){top=scores[i];selected=i;}
                }
            }
            lineCells.Clear();
            int length=column?height:targets.Width;
            for(int i=0;i<length;i++)lineCells.Add(targets.GetCell(column?selected:i,column?targets.BoardStartY+i:targets.BoardStartY+selected));
            for(int i=0;i<lineCells.Count-1;i++)
            {int j=random.Next(i,lineCells.Count);var value=lineCells[j];lineCells[j]=lineCells[i];lineCells[i]=value;}
            int guided=column?1:2;
            for(int i=0;i<lineCells.Count;i++)
            {
                var cell=lineCells[i];
                if (cell != null && (targets.HasTargetableBelowItemUnderLightball(cell) || targets.GetScore(cell, guided, false) > 0))
                    return new OriginalCellPoint(cell.X, cell.Y);
            }
            for(int i=0;i<lineCells.Count;i++)
            {
                var cell=lineCells[i];
                if(cell!=null&&cell.CellType!=0&&targets.IsLineTargetValid(cell,guided))return new OriginalCellPoint(cell.X,cell.Y);
            }
            return OriginalCellPoint.Default;
        }
        private int FindHighestIndex(int[] scores) // 0x1c98b58/0x1c98c30, no exclusions.
        {
            highestIndexes.Clear();int top=int.MinValue;
            for(int i=0;i<scores.Length;i++)
            {
                if(scores[i]>top){top=scores[i];highestIndexes.Clear();highestIndexes.Add(i);}
                else if(scores[i]==top)highestIndexes.Add(i);
            }
            return highestIndexes[random.Next(0,highestIndexes.Count)];
        }
    }
}
