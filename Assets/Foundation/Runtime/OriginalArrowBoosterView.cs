using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    // ArrowBooster.Use and ArrowBoosterView.PlayArrowWithAnimator.
    public sealed class OriginalArrowBoosterView : OriginalOptionalBoosterEffect
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform arrow, target;
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private string useSortingLayer;
        [SerializeField] private int useSortingOrder;
        [SerializeField] private float offset, wideOffset, wideAspect;
        [SerializeField] private float startDelay, impactHoldDelay, soundDelay, soundReplayInterval, baseSpeed, speedIncrease;
        [SerializeField] private float hitRadius, refillDelay, finishDelay;
        [SerializeField] private float dropDelay, dropDuration, dropDistance, targetDelay, targetDuration;

        private static readonly int FallAnimation = Animator.StringToHash("ArrowFallAnimation");
        private const int BoosterOperation = 7;
        private OriginalPlayableBoard game;
        private OriginalInitialBoardView board;
        private OriginalGridCell[] cells;
        private OriginalExplodeData explode;
        private int[] freezes;
        private int cellCount, hitCount, lastHit = -1;
        private bool active, frozen, finishedFlightHits;
        private Coroutine flight;
        private Transform dropTransform;
        private Vector3 targetStart, targetEnd, dropStart;
        private float targetElapsed, dropElapsed;
        private bool targetMoving, dropMoving;

        internal override void Play(OriginalPlayableBoard game, OriginalInitialBoardView board,
            Camera camera, OriginalBottomUi bottom, OriginalGridCell selected)
        {
            this.game = game;
            this.board = board;
            transform.SetParent(board.Presentation.CellParent, false);
            int row = selected.Y;
            cells = new OriginalGridCell[board.Grid.Width];
            var cell = board.Grid.GetCell(0, row);
            while (cellCount < cells.Length && cell != null)
            {
                cells[cellCount++] = cell;
                cell = cell.Right;
            }
            freezes = new int[cellCount];
            transform.localPosition += Vector3.up * row;
            sortingGroup.sortAtRoot = true;
            sortingGroup.sortingLayerID = SortingLayer.NameToID(useSortingLayer);
            sortingGroup.sortingOrder = useSortingOrder;
            float portraitAspect = Mathf.Min(camera.aspect, 1f / camera.aspect);
            float leftOffset = portraitAspect >= wideAspect ? wideOffset : offset;
            float rightOffset = (2f * camera.orthographicSize * camera.aspect - (9 - board.Grid.Width) * .5f) /
                bottom.transform.localScale.x;
            arrow.localPosition += Vector3.left * leftOffset;
            target.localPosition += Vector3.right * rightOffset;
            target.localScale = Vector3.zero;
            explode = OriginalExploder.Next(19, new OriginalCellPoint(-1, row));
            game.State.Operations.OperationStart(BoosterOperation);
            active = frozen = true;
            particles.transform.SetParent(arrow, true);
            particles.transform.localPosition = Vector3.left;
            particles.Play();
            targetStart = target.localPosition;
            targetEnd = targetStart + Vector3.left * leftOffset;
            targetMoving = true;
            dropTransform = animator.transform;
            game.BoosterTweenUpdate += AdvanceTweens;
            flight = board.Presentation.Coroutines.ManualStartCoroutine(Flight(), Finish);
        }

        private IEnumerator Flight()
        {
            yield return game.Wait(startDelay);
            StartCoroutine(PlayLaunchSound());
            float speed = baseSpeed;
            Vector3 end = targetEnd + Vector3.left;
            while (arrow.localPosition.x < end.x)
            {
                var position = arrow.localPosition;
                position.x += speed * board.Clock.DeltaTime;
                if (position.x > end.x)
                {
                    arrow.localPosition = end;
                    animator.speed = speed;
                    animator.enabled = true;
                    particles.Stop();
                }
                else arrow.localPosition = position;
                speed += speedIncrease;
                HitCellsInRange(arrow.position.x);
                yield return null;
            }
            ExplodeRemainingCells();
            yield return game.Wait(impactHoldDelay);
            animator.speed = 1f;
            arrow.localPosition += Vector3.right;
            animator.Play(FallAnimation, 0, 0f);
            dropStart = dropTransform.localPosition;
            dropMoving = true;
            yield return game.Wait(refillDelay);
            UnfreezeCells();
            yield return game.Wait(finishDelay);
        }

        private void HitCellsInRange(float worldX) // 0x3194aa8 and 0x3194974.
        {
            if (finishedFlightHits) return;
            for (int i = 0; i < cellCount; i++)
            {
                if (Mathf.Abs(cells[i].GetViewPosition().x - worldX) < hitRadius)
                {
                    ExplodeCell(i);
                    lastHit = i;
                }
                else if (lastHit == cellCount - 1)
                {
                    finishedFlightHits = true;
                    break;
                }
            }
        }

        private void ExplodeRemainingCells() // 0x3194718.
        {
            if (hitCount == cellCount) return;
            for (int i = 0; i < cellCount; i++)
                if (freezes[i] == 0) ExplodeCell(i);
        }

        private void ExplodeCell(int index)
        {
            var cell = cells[index];
            cell.FreezeFall();
            freezes[index]++;
            hitCount++;
            cell.ExplodeCurrentItem(explode);
        }

        // PlayTargetAnimation (0x31940d8) and PlayFallAnimation (0x319452c)
        // use an interval and a tween on the same manual-update sequence.
        private void AdvanceTweens()
        {
            float delta = board.Clock.DeltaTime;
            if (targetMoving)
            {
                targetElapsed += delta;
                if (targetElapsed >= targetDelay)
                {
                    float t = Mathf.Clamp01((targetElapsed - targetDelay) / targetDuration);
                    float u = t - 1f;
                    float eased = 1f + u * u * (2.70158f * u + 1.70158f);
                    target.localPosition = Vector3.LerpUnclamped(targetStart, targetEnd, eased);
                    target.localScale = Vector3.one * eased;
                    targetMoving = t < 1f;
                }
            }
            if (dropMoving)
            {
                dropElapsed += delta;
                if (dropElapsed >= dropDelay)
                {
                    float t = Mathf.Clamp01((dropElapsed - dropDelay) / dropDuration);
                    dropTransform.localPosition = Vector3.LerpUnclamped(dropStart, Vector3.down * dropDistance, t * t);
                    dropMoving = t < 1f;
                }
            }
        }

        private IEnumerator PlayLaunchSound()
        {
            // The native Invoke is a scaled Unity wait, separate from the level clock.
            yield return new WaitForSeconds(soundDelay);
            game.PlayApplicationSfx(7, soundReplayInterval);
        }

        private void UnfreezeCells()
        {
            if (!frozen) return;
            frozen = false;
            for (int i = 0; i < cellCount; i++)
                for (int count = freezes[i]; count > 0; count--)
                    cells[i].UnFreezeFall();
        }

        internal override void Cancel()
        {
            if (flight != null) board.Presentation.Coroutines.StopCoroutine(flight);
            Finish();
        }

        // The native context coroutine runs this cleanup on completion and on kill.
        private void Finish()
        {
            if (!active) return;
            Release();
            Destroy(gameObject);
        }

        private void Release()
        {
            active = false;
            game.BoosterTweenUpdate -= AdvanceTweens;
            StopAllCoroutines();
            UnfreezeCells();
            if (game.State.HasOperation(BoosterOperation)) game.State.Operations.OperationFinish(BoosterOperation);
        }

        private void OnDestroy()
        {
            if (!active) return;
            Release();
            if (flight != null) board.Presentation.Coroutines.StopCoroutine(flight);
        }
    }
}
