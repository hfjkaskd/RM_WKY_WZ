using System;

namespace Whitebox.Foundation
{
    public interface IOriginalViewCollectionLevelContext : IOriginalProgressLevelContext
    {
        bool IsWaterLevel { get; }
        bool IsMultipleBoardLevel { get; }
    }
    public interface IOriginalMultipleBoardGoalCollection
    {
        bool IsGoalInCurrentSubGoal(int goalType); // AMultipleBoardSystem slot 7.
    }
    public interface IOriginalProgressCollectionEffect
    {
        void SetPosition(float x, float y, float z);
        void Initialize(int goalType, bool remainingMoves);
    }
    public interface IOriginalProgressCollectionFactory
    {
        IOriginalProgressCollectionEffect Spawn(int poolId, bool activate);
    }
    public static class OriginalCollectionProgressRules
    {
        public static bool ShouldCollectToProgress(IOriginalViewCollectionLevelContext context) // 0x1e18c14.
        {
            if (context.IsStoryLevel || !context.IsFinalBoard || context.IsPortalLevel)
                return !context.IsWaterLevel;
            return false;
        }
    }

    // ItemView.TryCollectForStoryLevel 0x2b4b7c4 / 0x2b4b8b0.
    // Trail and progress-effect visuals are supplied by their configured pools.
    public sealed class OriginalItemViewGoalCollection
    {
        private readonly OriginalGoalClaims claims;
        private readonly IOriginalViewCollectionLevelContext context;
        private readonly Func<IOriginalMultipleBoardGoalCollection> currentBoard;
        private readonly IOriginalProgressCollectionFactory factory;
        public OriginalItemViewGoalCollection(OriginalGoalClaims claims, IOriginalViewCollectionLevelContext context,
            Func<IOriginalMultipleBoardGoalCollection> currentBoard, IOriginalProgressCollectionFactory factory)
        {
            this.claims = claims;
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.currentBoard = currentBoard ?? throw new ArgumentNullException(nameof(currentBoard));
            this.factory = factory;
        }
        public IOriginalProgressCollectionEffect TryCollectForStoryLevel(int itemType, float x, float y, float z) =>
            TryCollectGoal(OriginalGoalTypes.FromItemType(itemType), x, y, z, false);
        public IOriginalProgressCollectionEffect TryCollectGoal(int goalType, float x, float y, float z, bool remainingMoves)
        {
            if (claims == null) throw new NullReferenceException();
            if (claims.TryCollectWithTrail(goalType, x, y, z)) return null;
            if (!OriginalCollectionProgressRules.ShouldCollectToProgress(context)) return null;
            if (context.IsMultipleBoardLevel)
            {
                var board = currentBoard();
                if (board == null) throw new NullReferenceException();
                if (!board.IsGoalInCurrentSubGoal(goalType)) return null;
            }
            bool portal = context.IsPortalLevel;
            if (factory == null) throw new NullReferenceException();
            var effect = factory.Spawn(portal ? 389 : 175, true);
            if (effect == null) throw new NullReferenceException();
            effect.SetPosition(x, y, z);
            effect.Initialize(goalType, remainingMoves);
            return effect;
        }
    }
}
