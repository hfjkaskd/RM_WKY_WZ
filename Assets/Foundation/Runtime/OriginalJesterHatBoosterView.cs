using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalJesterHatBoosterView : OriginalOptionalBoosterEffect
    {
        [SerializeField] private OriginalJesterRigView hat;
        [SerializeField] private SpriteRenderer shade;
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private float enterDelay, particleDelay, shuffleDelay, movementSeconds, settleDelay, effectTailSeconds;
        [SerializeField] private float enterSpeed, exitSpeed, exitDelay, shadeOutDelay, exitSoundDelay;
        [SerializeField] private float enterSoundOffset, exitSoundOffset;
        [SerializeField] private float shadeAlpha, shadeInSeconds, shadeOutSeconds;

        private const int ShuffleOperation = 6;
        private const int BoosterOperation = 7;
        private const int MaxRetries = 500;
        private const int AllowStationaryAfter = 99;
        private OriginalPlayableBoard game;
        private OriginalInitialBoardView board;
        private OriginalGameTouchListener touch;
        private OriginalBoardPossibleMoveServices eligibility;
        private OriginalGridIterator iterator;
        private OriginalGridCell[] cells;
        private OriginalBoardItem[] items;
        private Dictionary<OriginalBoardItem, OriginalGridCell> originalCells;
        private OriginalItemSwapView[] views;
        private Vector3[] from, to;
        private int count, movingCount;
        private bool active;

        internal override void Play(OriginalPlayableBoard game, OriginalInitialBoardView board,
            Camera camera, OriginalBottomUi bottom, OriginalGridCell target)
        {
            this.game = game;
            this.board = board;
            var area = board.Layout.CurrentBoard;
            transform.localPosition = new Vector3((area.Left + area.Right) * .5f, (area.Bottom + area.Top) * .5f, 0f);
            var cameraCenter = camera.transform.position;
            shade.transform.position = new Vector3(cameraCenter.x, cameraCenter.y, 0f);
            float height = 2f * camera.orthographicSize;
            shade.size = new Vector2(height * camera.aspect, height);

            eligibility = new OriginalBoardPossibleMoveServices(board.MatchBlocking);
            iterator = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            int capacity = board.Grid.Width * board.Grid.Height;
            cells = new OriginalGridCell[capacity];
            items = new OriginalBoardItem[capacity];
            originalCells = new Dictionary<OriginalBoardItem, OriginalGridCell>(capacity);
            views = new OriginalItemSwapView[capacity];
            from = new Vector3[capacity];
            to = new Vector3[capacity];

            hat.Initialize();
            hat.gameObject.SetActive(false);
            touch = game.TouchListener;
            touch?.DisableTouch();
            game.State.Operations.OperationStart(ShuffleOperation);
            game.State.Operations.OperationStart(BoosterOperation);
            game.SetShuffleOrientation(false);
            active = true;
            StartCoroutine(PlayTimeline());
        }

        private IEnumerator PlayTimeline()
        {
            // The three callbacks share the sequence time in StartHatAnimation (0x318e16c).
            float elapsed = 0f;
            while (elapsed < enterDelay)
            {
                yield return null;
                elapsed += board.Clock.DeltaTime;
            }
            StartCoroutine(AnimateHat());
            while (elapsed < particleDelay)
            {
                yield return null;
                elapsed += board.Clock.DeltaTime;
            }
            particles.Play(true);
            while (elapsed < shuffleDelay)
            {
                yield return null;
                elapsed += board.Clock.DeltaTime;
            }
            StartCoroutine(ShuffleAndMove());
            while (active || elapsed < shuffleDelay + effectTailSeconds)
            {
                yield return null;
                elapsed += board.Clock.DeltaTime;
            }
            Destroy(gameObject);
        }

        private IEnumerator AnimateHat()
        {
            // PlayAnimation (0x318eeb4) moves the skeleton from staging to the world origin.
            hat.transform.position = Vector3.zero;
            hat.gameObject.SetActive(true);
            game.PlaySfx(1315, enterSoundOffset);
            hat.PlayEnter(enterSpeed);
            StartCoroutine(FadeShade(shadeAlpha, shadeInSeconds, true));

            // PlayExitAnimationDelayed (0x318f9f8) uses three sequential Unity waits.
            yield return new WaitForSeconds(exitDelay);
            hat.PlayExit(exitSpeed);
            yield return new WaitForSeconds(shadeOutDelay);
            StartCoroutine(FadeShade(0f, shadeOutSeconds, false));
            yield return new WaitForSeconds(exitSoundDelay);
            game.PlaySfx(1316, exitSoundOffset);
        }

        private IEnumerator FadeShade(float target, float duration, bool easeIn)
        {
            float start = shade.color.a;
            for (float elapsed = 0f; elapsed < duration; elapsed += board.Clock.DeltaTime)
            {
                float ratio = elapsed / duration;
                if (easeIn) ratio = 1f - Mathf.Cos(ratio * (Mathf.PI * .5f));
                SetShade(Mathf.LerpUnclamped(start, target, ratio));
                yield return null;
            }
            SetShade(target);
        }

        private void SetShade(float alpha)
        {
            var color = shade.color;
            color.a = alpha;
            shade.color = color;
        }

        private IEnumerator ShuffleAndMove()
        {
            // 0x318e5cc checks each candidate before retrying. The final candidate
            // is retained even on exhaustion. Views only move after this search.
            Prepare(true);
            Place();
            for (int retry = 0; retry < MaxRetries; retry++)
            {
                if (board.PossibleMoves.CheckBoard(iterator) != 0 &&
                    (retry >= AllowStationaryAfter || !HasStationaryItem())) break;
                Prepare(false);
                Place();
            }
            for (int i = 0; i < count; i++)
            {
                var view = (OriginalItemSwapView)cells[i].Mediation.CurrentItem.SwapView;
                views[i] = view;
                from[i] = view.transform.position;
                to[i] = cells[i].GetViewPosition();
                if (view is OriginalMatchItemSpriteView match) match.SetShuffleSorting(false);
            }
            movingCount = count;
            float elapsed = 0f;
            while (elapsed < movementSeconds)
            {
                elapsed += board.Clock.DeltaTime;
                float ratio = OriginalRuntimeShuffle.BackInOut(elapsed / movementSeconds);
                for (int i = 0; i < movingCount; i++)
                {
                    var itemTransform = views[i].transform;
                    var destination = to[i];
                    var current = itemTransform.position;
                    // MoveAllItems (0x318f0e8) leaves a view alone once its XY
                    // position is within the native component-wise tolerance.
                    const float tolerance = .001f;
                    if (current.x > destination.x - tolerance && current.x < destination.x + tolerance &&
                        current.y > destination.y - tolerance && current.y < destination.y + tolerance) continue;
                    itemTransform.position = destination * ratio + from[i] * (1f - ratio);
                }
                yield return null;
            }
            SettleViews(false);
            yield return game.CreateGameplayWait(settleDelay);
            Finish();
        }

        private void Prepare(bool first)
        {
            count = 0;
            var scan = iterator;
            scan.Reset();
            OriginalGridCell cell;
            while ((cell = scan.MoveNext()) != null)
            {
                if (!eligibility.CanEnterPossibleMatch(cell)) continue;
                var item = cell.Mediation.CurrentItem;
                if (!item.IsMatchItem && !item.IsSpecialItem) continue;
                cells[count] = cell;
                items[count++] = item;
                if (first) originalCells.Add(item, cell);
                item.Mediator.ClearFromAllRegisteredCells();
                cell.Mediation.ClearAllItems();
            }
        }

        private void Place()
        {
            int remaining = count;
            for (int i = 0; i < count; i++)
            {
                var cell = cells[i];
                int index = game.GeneralRandom.Next(0, remaining);
                for (int offset = 0; offset < remaining; offset++)
                {
                    index = (index + offset) % remaining;
                    if (originalCells[items[index]] != cell) break;
                }
                var item = items[index];
                if (originalCells[item] != cell) items[index] = items[--remaining];
                cell.Mediation.SetAllItems(item);
            }
        }

        private bool HasStationaryItem()
        {
            for (int i = 0; i < count; i++)
                if (originalCells[cells[i].Mediation.CurrentItem] == cells[i]) return true;
            return false;
        }

        private void SettleViews(bool interrupted)
        {
            for (int i = 0; i < movingCount; i++)
            {
                var view = views[i];
                if (view == null) continue;
                if (interrupted) view.transform.position = to[i];
                if (view is OriginalMatchItemSpriteView match) match.ArrangeSelfSorting();
                else if (cells[i].Mediation.CurrentItem is OriginalSpecialBoardItem special)
                    special.FallView?.ArrangeSelfSorting();
            }
            movingCount = 0;
        }

        private void Finish()
        {
            if (!active) return;
            active = false;
            if (movingCount > 0) SettleViews(true);
            if (touch != null && touch.TouchDisableCount > 0) touch.EnableTouch();
            if (game.State.HasOperation(ShuffleOperation)) game.State.Operations.OperationFinish(ShuffleOperation);
            if (game.State.HasOperation(BoosterOperation)) game.State.Operations.OperationFinish(BoosterOperation);
            game.SetShuffleOrientation(true);
        }

        internal override void Cancel()
        {
            StopAllCoroutines();
            Finish();
            Destroy(gameObject);
        }

        private void OnDestroy() => Finish();
    }
}
