using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Live first-board adapter. No fixture input or Editor/player branching.
    public sealed class OriginalRemainingMovesReplacementServices : IOriginalRemainingMovesReplacementServices, IOriginalItemReplacementContext
    {
        private readonly OriginalPlayableBoard game;
        private readonly OriginalInitialBoardView board;
        private readonly OriginalGameplayHud hud;
        private readonly OriginalGameAudioHost audio;
        public OriginalRemainingMovesReplacementServices(OriginalPlayableBoard game, OriginalInitialBoardView board,
            OriginalGameplayHud hud, OriginalGameAudioHost audio)
        { this.game = game; this.board = board; this.hud = hud; this.audio = audio; board.Presentation.PrepareRemainingMovesParticles(); }
        public int LeftMoves => game.Session.LeftMoves;
        private OriginalMadnessSessionData Madness => (OriginalMadnessSessionData)game.Session.AnimationValues.StartSnapshots.Madness;
        public int MatchTypeToCollect => Madness.MatchTypeToCollect;
        public bool ReplaceWithPropeller => Madness.GetRemainingMoveReplaceItem() == 3 || game.ShouldProgress;
        public Vector3 MovesPosition => hud.moveText.transform.position;
        public float UnityDeltaTime => Time.deltaTime;
        public float UnityRandomValue => UnityEngine.Random.value;
        public int NextRegularRandom(int min, int max) => game.NextRegularRandom(min, max);
        public void SetDisplayedMoves(int value) => hud.SetRemainingMovesDisplay(value);
        public void SetOrientationEnabled(bool value) => hud.SetOrientationEnabled(value);
        public void ResetTopSorting() => hud.ResetSorting();
        public void PlaySound(int id, float offset) => audio.GameSfx.PlaySfx(id, offset);
        public Transform SpawnFlightParticles() => game.Pool.Spawn<OriginalRemainingMovesParticles>(77, true).transform;
        public void RecycleFlightParticles(Transform particles) => game.Pool.Recycle(particles.GetComponent<OriginalRemainingMovesParticles>());
        public OriginalSpecialBoardItem ReplaceWithSpecialItem(OriginalGridCell cell, int index)
        {
            var old = cell.Mediation.CurrentItem;
            if (old == null) throw new NullReferenceException(
                "Remaining move target changed before landing: cell=" + cell.X + "," + cell.Y +
                "; current=" + (old == null ? -1 : old.ItemType) +
                "; next=" + (cell.Mediation.NextItem == null ? -1 : cell.Mediation.NextItem.ItemType) +
                "; target=" + (cell.Mediation.TargetItem == null ? -1 : cell.Mediation.TargetItem.ItemType) + ".");
            old.Mediator.ClearFromAllRegisteredCells();
            // Native calls virtual ReplaceItem(true), not a MatchItem cast or
            // a direct pool recycle. The current item can change in flight.
            old.ReplaceItem(true, this);
            int tiledId = SelectItem(); // Native0x111a508 selects AFTER ReplaceItem.
            var item = (OriginalSpecialBoardItem)game.CreateItemAtCell(tiledId, cell);
            // Native compares ItemType3, i.e. propeller, not rocket. Its coin
            // value alternates2/3 to match unconverted-move floor(n*2.5).
            if (item is OriginalPropellerBoardItem propeller)
            { game.SpecialItemCreated(item, cell, 17, 1, true); propeller.RemainingMoveCoins = 2 + (index & 1); }
            game.AddMissionEvent(item.ItemType, game.Session.Difficulty, 1);
            game.TryCollectMadness(cell, MatchTypeToCollect, 5, 12, null, true);
            var hit = board.Presentation.Particles.GetGlobalParticle<OriginalRemainingMovesHitParticles>(78);
            hit.PlayOptimized(cell.GetViewPosition(), 0);
            item.View.PlayCreationAnimation(tiledId == 40 ? .15f : 0f, false);
            return item;
        }
        private int SelectItem() // 0x111a8e8, native NextBool is Next(0,2)==0.
        {
            if (ReplaceWithPropeller) return 10;
            if (NextRegularRandom(0, 2) == 0) return 40;
            return NextRegularRandom(0, 2) == 0 ? 30 : 20;
        }
        public void RecycleReplacementView(OriginalBoardItem item)
        {
            switch (item.SwapView)
            {
                case OriginalMatchItemSpriteView match: game.Pool.Recycle(match); break;
                case OriginalRocketItemSpriteView rocket: game.Pool.Recycle(rocket); break;
                case OriginalTntItemSpriteView tnt: game.Pool.Recycle(tnt); break;
                case OriginalPropellerItemSpriteView propeller: game.Pool.Recycle(propeller); break;
                case OriginalLightballItemSpriteView lightball: game.Pool.Recycle(lightball); break;
                case OriginalBoxItemSpriteView box: game.Pool.Recycle(box); break;
                case OriginalCoinItemSpriteView coin: game.Pool.Recycle(coin); break;
                case OriginalEggItemSpriteView egg: game.Pool.Recycle(egg); break;
                default: throw new NotSupportedException("Replacement requires the current item's restored pooled view.");
            }
        }
        public void CollectReplacementMadness(int matchType, OriginalSwapPoint position) =>
            game.Session.TryCollectMadness(matchType, position, 1, 5, null);
    }

    public sealed partial class OriginalPlayableBoard
    {
        public int NextRegularRandom(int min, int max) => generalRandom.Next(min, max);
    }
}
