using System;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalFirstBoardExplodeTargets:IOriginalLineExplodeTargetServices
    {
        private OriginalBoardLayout lineLayout;
        public void BindLineContext(OriginalBoardLayout layout){lineLayout=layout??throw new ArgumentNullException(nameof(layout));}
        int IOriginalLineExplodeTargetServices.Width=>areaGrid.Width;
        int IOriginalLineExplodeTargetServices.BoardStartY=>lineLayout.CurrentBoard.Bottom;
        int IOriginalLineExplodeTargetServices.BoardEndY=>lineLayout.CurrentBoard.Top;
        public void RequireSupportedLines(OriginalExplodeData data)
        {
            if((data.Trigger!=10&&data.Trigger!=11)||data.CanSpreadJelly)
                throw new NotSupportedException("Line selection requires the normal rocket/no-jelly branch.");
            if(areaGrid==null||isBonusLevel==null||lineLayout==null)throw new InvalidOperationException("Line target context is not bound.");
            RequireSupportedScoring();var final=goals.GetFinalGoals();
            for(int i=0;i<final.Length;i++)if(final[i].IsSpecificWinConditionalGoal())
                throw new NotSupportedException("Line selection requires this goal's native conditional owner.");
        }
        public void BeginLineScore()=>BeginAreaScore();
        public bool ShouldAddLineScore(OriginalGridCell cell)=>ShouldAddAreaScore(cell);
        public bool IsUncoveredLightball(OriginalGridCell cell) =>
            cell.StaticTouch.AboveItems.Count == 0 && IsLightball(cell);

        public bool HasTargetableBelowItemUnderLightball(OriginalGridCell cell) =>
            IsUncoveredLightball(cell) && Grass(cell) != null; // 0x1c97054, no Jelly.

        public bool IsLineTargetValid(OriginalGridCell cell,int guidedType)
        {
            RequireSupported(cell);
            return cell.Mediation.CurrentItem?.IsValidExplodeTarget(guidedType) == true;
        }
        public bool IsLineWinning(bool column,int index,OriginalExplodeData data) // 0x1ca3cf4/0x1ca407c, normal goals.
        {
            if(goals.HasAnyPrerequisite()||isBonusLevel())return false;
            var final = BeginGoalHitScan();
            int count=column?lineLayout.CurrentBoard.Top-lineLayout.CurrentBoard.Bottom+1:areaGrid.Width;
            for(int i=0;i<count;i++)
            {
                var cell=areaGrid.GetCell(column?index:i,column?lineLayout.CurrentBoard.Bottom+i:index);if(cell==null)continue;
                CountPropellerGoalHits(cell, final, areaGoalHits);
            }
            return HasWinningGoalHits(final, areaGoalHits);
        }
    }
}
