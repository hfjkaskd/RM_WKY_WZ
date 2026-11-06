using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalHoneyBoardItem : IOriginalStaticExplosionItem, IOriginalStaticPressItem, IOriginalExplodeTarget
    {
        public const int TiledId = 33;
        public const int StaticType = 4;
        public const int HoneyGoal = 14;
        private readonly OriginalGoalManager goals;
        private readonly OriginalIncomingExploderContainer incoming = new OriginalIncomingExploderContainer();
        private readonly OriginalExtraIncomingExploderContainer extra = new OriginalExtraIncomingExploderContainer();
        private OriginalPlayableBoard game;
        private IOriginalBoxExplosionServices useful;
        private int layer = 1;

        public int Id { get; }
        public OriginalGridCell CurrentCell { get; set; }
        internal int LayerCount => layer;
        public OriginalHoneyItemView View { get; private set; }
        public bool HasView => View != null;
        public int StaticItemType => StaticType;
        public int GoalType => HoneyGoal;
        public bool IsTouchBlocking => true;
        public bool IsTouchBlockingExceptChain => true;
        public bool DoesBlockFall => false;
        public bool IsObstacle => false;
        public bool CanExplodeWithMatch => true;
        public bool CanExplodeWithNearMatch => true;
        public int IncomingExploderCount => incoming.Count;

        public OriginalHoneyBoardItem(OriginalGoalManager goals)
        {
            this.goals = goals;
            Id = OriginalBoardItem.StaticId++;
        }

        public void Bind(OriginalHoneyItemView view, OriginalPlayableBoard owner, IOriginalBoxExplosionServices usefulMoves)
        {
            View = view;
            game = owner;
            useful = usefulMoves;
        }

        public void Explode(OriginalExplodeData data) // 0x3498668, one-layer honey.
        {
            if (layer == 0) return;
            layer = 0;
            var cell = CurrentCell;
            View.Explode(data.Trigger, cell);
            if (cell.Mediation.CurrentItem is OriginalMatchBoardItem match)
                game.ManualStartCoroutine(ReserveAfterReveal(match));
            cell.StaticTouch.ClearStaticItem(StaticType);
            View = null;
            goals.DecreaseGoal(HoneyGoal);
            goals.DecreaseGoalUi(HoneyGoal);
            useful.UsefulMoveMade();
        }

        private IEnumerator ReserveAfterReveal(OriginalMatchBoardItem match) // 0x241c950.
        {
            match.Reserve();
            yield return game.CreateGameplayWait(.4f);
            match.UnReserve();
        }

        public void ExplodeAll(OriginalExplodeData data) => Explode(data);

        internal static void RevealCell(OriginalGridCell cell) // 0x34993b8.
        {
            var item = cell.Mediation.CurrentItem;
            if (item != null)
            {
                var fallView = (item as OriginalSpecialBoardItem)?.FallView ?? item.SwapView as IOriginalFallItemView;
                if (fallView != null)
                    fallView.SetIsUnderNonFallBlockingAboveStaticItem(OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItem(item));
            }
            for (int direction = 0; direction < 8; direction++)
                if (cell.Neighbors.Get(direction)?.StaticTouch.GetStaticItem(StaticType) is OriginalHoneyBoardItem honey)
                    honey.View.Refresh();
        }

        public bool PlayTouchDownAnimation()
        {
            game.PlayHaptic(25, 0, .7f, 0f, false);
            View.PlayTap();
            return true;
        }
        public void PlayInvalidMoveAnimation() { }
        public int RemainingExplodeCount() => layer - incoming.Count;
        public int GetExtraIncomingExplodeCount(int guidedType) => extra.GetCount(guidedType);
        public int GetExplodeScore(int guidedType) => 456976;
        public bool HasExplodeScore(OriginalExplodeData data) => true;
        public bool IsValidExplodeTarget(int guidedType) => HasView;
        public bool CanAddPropellerAroundExploder() => true;
        public Vector3 GetViewPosition() => View.transform.position;
        public Vector3 GetMasterViewCenterPosition() => GetViewPosition();

        public void AddIncomingExploder(OriginalExplodeData data)
        {
            incoming.Add(data.Id);
            extra.Add(data.Id, data.Trigger);
        }

        public void RemoveIncomingExploder(OriginalExplodeData data)
        {
            incoming.Remove(data.Id);
            extra.Remove(data.Id, data.Trigger);
        }
    }
}
