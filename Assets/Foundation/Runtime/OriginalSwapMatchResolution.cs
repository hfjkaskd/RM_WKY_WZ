using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchExplosionServices
    {
        bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group, OriginalExplodeData exploder, bool isFromAutoMatch);
        void ReportPatternChanged(int previousPattern, int currentPattern);
        void ReportMatchExplosionFailed();
    }

    // SwapAction.ExplodeMatchGroups 0x1ee0a98. The required service dispatches
    // MatchManager explosion; concrete item removal is handled by that stage.
    public sealed class OriginalSwapMatchResolution
    {
        private readonly OriginalSwapActionState action;
        private readonly IOriginalMatchExplosionServices services;
        private readonly OriginalSavedSwapGroup copyBuffer = new OriginalSavedSwapGroup();

        public OriginalSwapMatchResolution(OriginalSwapActionState action, IOriginalMatchExplosionServices services)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }
        public void Resolve(OriginalBoardItem otherItem, OriginalBoardItem movingItem)
        {
            var decision = action.Decision;
            var groups = decision.MatchContext.FindExtensiveMatchesInArea(action.To, 3);
            if (groups == null) throw new NullReferenceException();
            OriginalMatchGroupSnapshot currentFrom = default(OriginalMatchGroupSnapshot);
            OriginalMatchGroupSnapshot currentTo = default(OriginalMatchGroupSnapshot);
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var item = action.To.Mediation.CurrentItem;
                if (item is OriginalMatchBoardItem && group.ContainsItem(item))
                {
                    group.CopySnapshotTo(copyBuffer);
                    currentTo = copyBuffer.Snapshot;
                    currentTo.SpecialItemCreationCell = action.To;
                    continue;
                }
                item = action.From.Mediation.CurrentItem;
                if (item is OriginalMatchBoardItem && group.ContainsItem(item))
                {
                    group.CopySnapshotTo(copyBuffer);
                    currentFrom = copyBuffer.Snapshot;
                    currentFrom.SpecialItemCreationCell = action.From;
                }
            }
            // Native handles the from group first. The other group stays frozen
            // while its counterpart is handed to MatchManager for explosion.
            ResolveOne(true, currentFrom, movingItem);
            ResolveOne(false, currentTo, otherItem);
        }
        private void ResolveOne(bool from, OriginalMatchGroupSnapshot current, OriginalBoardItem counterpart)
        {
            var decision = action.Decision;
            var saved = from ? decision.SavedFromMatch : decision.SavedToMatch;
            if (!saved.Exists) return;
            for (int i = 0; i < saved.Cells.Count; i++) saved.Cells.Get(i).UnFreezeFall(); // 0x12dfa10.

            // Compare pattern, not group ID or membership. Release old cells
            // before replacing their saved value with the newly found group.
            if (current.Exists && current.PatternType != saved.PatternType)
            {
                services.ReportPatternChanged(saved.PatternType, current.PatternType);
                saved = current;
                if (from) decision.ReplaceFromMatch(saved);
                else decision.ReplaceToMatch(saved);
            }
            if (ShouldExplodeMatchGroup(counterpart, saved))
            {
                var exploder = OriginalExploder.Next(2);
                var source = from ? action.From : action.To;
                exploder.Point = new OriginalCellPoint(source.X, source.Y);
                if (!services.ExplodeMatchGroup(saved, exploder, false)) services.ReportMatchExplosionFailed();
            }
            // Native copies MatchGroup.Null, clearing inline cells too; calling
            // MatchGroup.Reset would incorrectly retain those references.
            if (from) decision.ReplaceFromMatch(default(OriginalMatchGroupSnapshot));
            else decision.ReplaceToMatch(default(OriginalMatchGroupSnapshot));
        }
        public static bool ShouldExplodeMatchGroup(OriginalBoardItem swappedItem, OriginalMatchGroupSnapshot group) // 0x1ee4238.
        {
            return swappedItem == null || swappedItem.ItemType != 5 || group.PatternType != 1;
        }
    }
}
