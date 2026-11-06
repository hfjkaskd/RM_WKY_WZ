using System;
using System.Collections;

namespace Whitebox.Foundation
{
    public interface IOriginalTntExplosionServices : IOriginalSpecialItemExplosionServices
    {
        OriginalGameStateManager State { get; }
        OriginalCellGrid Grid { get; }
        void StartGameplayCoroutine(IEnumerator routine);
        object Wait(float seconds);
        void PlayTntExplosion(OriginalTntItemSpriteView view);
        void ShakeGrid(OriginalTntShakeConfig config);
        void RecycleTntView(OriginalTntItemSpriteView view);
        void UsefulMoveMade();
        void CollectTntMadness(OriginalExplodeData data, OriginalGridCell center);
    }
    public sealed partial class OriginalTntBoardItem
    {
        private IOriginalTntExplosionServices explosion;
        public void BindExplosion(IOriginalTntExplosionServices services)
        { explosion = services ?? throw new ArgumentNullException(nameof(services)); BindSpecialExplosion(services); }
        protected override bool CreatedByLightball => IsCreatedByLightball;
        public OriginalExplodeData LightballExplodeData { get; set; }
        public void RecycleView() // ItemModel 0x2b46db0; forced carried-special cleanup.
        { if (!HasView) return; HasView = false; explosion.RecycleTntView(ItemView); }
        public override void Explode(OriginalExplodeData data) // 0x27b5960: converted TNT waits for the lightball's trigger15.
        {
            if (IsCreatedByLightball && data.Trigger != 15) return;
            base.Explode(data);
        }
        protected override void TryExplode(OriginalExplodeData data) // 0x27b59a8, normal first-board branch.
        {
            explosion.State.StartSpecialOperation();
            if (data.Trigger == 14) explosion.StartGameplayCoroutine(WaitAndExplodeViews(data));
            else ExplodeViews(data);
            explosion.UsefulMoveMade();
        }
        private IEnumerator WaitAndExplodeViews(OriginalExplodeData data) // 0x27b6f4c.
        { yield return explosion.Wait(Config.ExplodeDelay); ExplodeViews(data); }
        private void ExplodeViews(OriginalExplodeData data) // 0x27b5cf4.
        {
            if (Config.Radius >= 1) explosion.StartGameplayCoroutine(ExplodeCells(data));
            else explosion.State.FinishSpecialOperation();
            if (Config.Radius == 2) explosion.PlayTntExplosion(ItemView);
            explosion.RecycleTntView(ItemView);
            if (CurrentCell != null) FinalExplodeCompleted(.15f);
        }
        private IEnumerator ExplodeCells(OriginalExplodeData incoming) // 0x27b6638.
        {
            explosion.CollectRemainingMovesCoin(CurrentCell,this);
            var center = CurrentCell ?? explosion.Grid.GetCell(incoming.Point.X, incoming.Point.Y);
            // Trigger 15 copies the wave created once by LightballTntStrategy.
            // All of its TNTs keep the same ID so overlapping targets deduplicate the hit.
            var data = incoming.Trigger == 15 ? LightballExplodeData : OriginalExploder.Next(14);
            data.Point = new OriginalCellPoint(center.X, center.Y);
            if (CurrentCell != null) center.StaticTouch.ExplodeTopMostBelowItem(data);
            else { data.CanSpreadJelly = incoming.CanSpreadJelly; center.ExplodeCurrentItem(data); center.StaticTouch.ExplodeTopMostBelowItem(data); }
            // Native 0x27b68d8/0x27b6d40: after the center hit, before the outer rings.
            explosion.ShakeGrid(Config.Shake);
            for (int ring = 1; ring <= Config.Radius; ring++)
            {
                yield return explosion.Wait(Config.RingInterval);
                ExplodeOuterCells(ring, center.X, center.Y, data);
            }
            if (data.Id != LightballExplodeData.Id) explosion.CollectTntMadness(data, center);
            explosion.State.FinishSpecialOperation();
        }
        private void ExplodeOuterCells(int ring, int x, int y, OriginalExplodeData data) // 0x27b6104, no-jelly branch.
        {
            for (int dx = -Config.Radius; dx <= Config.Radius; dx++)
                for (int dy = -Config.Radius; dy <= Config.Radius; dy++)
                {
                    if ((dx == 0 && dy == 0) || Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;
                    var cell = explosion.Grid.GetCell(x + dx, y + dy);
                    if (cell == null) continue;
                    cell.FreezeFallForDuration(.25f); // 0x27b64a0.
                    cell.ExplodeCurrentItem(data);
                }
        }
    }
}
