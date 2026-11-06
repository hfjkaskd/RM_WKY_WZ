using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMadnessMatchManager : IOriginalMadnessSessionManager
    {
        int CollectMatchType { get; }
        bool ShouldPreventStickerAnimation { get; }
    }
    public interface IOriginalMadnessMatchAnimation
    {
        void StartAnimation(OriginalSwapPoint position, int count, int delayInFrames, OriginalBoardItem createdItem);
    }
    public interface IOriginalMadnessMatchPresentation
    {
        bool IsDailyLightningRushEventValid();
        IOriginalMadnessMatchAnimation Spawn(int poolId, bool activate);
        bool IsMissing(IOriginalMadnessMatchAnimation view);
    }
    public partial class OriginalMadnessSessionData
    {
        private IOriginalMadnessMatchPresentation matchPresentation;
        public void BindMatchPresentation(IOriginalMadnessMatchPresentation value) { matchPresentation = value; }
        private IOriginalMadnessMatchManager MatchManager()
        {
            var current = Manager();
            return current as IOriginalMadnessMatchManager ?? throw new InvalidCastException("Madness match collection manager is required.");
        }
        public int MatchTypeToCollect => IsEventValid() ? MatchManager().CollectMatchType : 0; // 0x32b2f60.
        public int GetRemainingMoveReplaceItem() // 0x32b3ad8.
        { if (!IsEventValid() || Manager().EventType == 1) return 0; return Manager().EventType; }
        public void TryAddMadnessMatch(int type, OriginalSwapPoint position, int count, int difficulty,
            int delayInFrames, OriginalBoardItem createdItem) // 0x32b370c.
        {
            if (type == 0 || count == 0 || !IsEventValid() || MatchTypeToCollect != type) return;
            count = unchecked(GetMultiplierValueForDifficulty(difficulty) * count);
            AddExtra(count);
            ShouldConsume = true; // Native also sets this when the multiplied value is zero.
            var presentation = matchPresentation ?? throw new NullReferenceException();
            if (presentation.IsDailyLightningRushEventValid()) return;
            if (MatchManager().ShouldPreventStickerAnimation) return;
            var view = presentation.Spawn(107, true);
            if (presentation.IsMissing(view)) return;
            if (view == null) throw new NullReferenceException();
            view.StartAnimation(position, count, delayInFrames, createdItem);
        }
        public void TryIncrementMadnessForExploderId(int type, int exploderId) // 0x32b393c.
        {
            if (!IsEventValid() || MatchTypeToCollect != type || exploderIdMatchItemCount == null) return;
            exploderIdMatchItemCount.TryGetValue(exploderId, out int count);
            exploderIdMatchItemCount[exploderId] = unchecked(count + 1);
        }
        public int GetMadnessForExploderId(int id) // 0x32b39f8.
        { return exploderIdMatchItemCount != null && exploderIdMatchItemCount.TryGetValue(id, out var count) ? count : 0; }
    }
    public sealed partial class OriginalActiveLevelData
    {
        private OriginalMadnessSessionData MadnessMatchData()
        {
            if (AnimationValues == null || AnimationValues.StartSnapshots == null) throw new NullReferenceException();
            return AnimationValues.StartSnapshots.Madness as OriginalMadnessSessionData ?? throw new NullReferenceException();
        }
        public void TryCollectMadness(int type, OriginalSwapPoint position, int count, int delayInFrames,
            OriginalBoardItem createdItem, bool remainingMoves = false) // 0x32b9ab0.
        {
            if (State != 2 && !remainingMoves) return;
            MadnessMatchData().TryAddMadnessMatch(type, position, count, Difficulty, delayInFrames, createdItem);
        }
        public void IncrementMadnessMatchItemCount(int type, int exploderId) // 0x32b9bbc.
        {
            if (State == 2) MadnessMatchData().TryIncrementMadnessForExploderId(type, exploderId);
        }
        public void TryCollectMadnessForExploder(OriginalExplodeData data, OriginalSwapPoint position, int delay) // 0x32b9afc.
        {
            if (State != 2) return;
            var madness = MadnessMatchData();
            int count = madness.GetMadnessForExploderId(data.Id);
            madness.TryAddMadnessMatch(madness.MatchTypeToCollect, position, count, Difficulty, delay, null);
        }
    }
}
