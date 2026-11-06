using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    // CannonBooster.Use and PlayAnimation. The cross-fall hold belongs to
    // the whole column, while the projectile owns one regular freeze at a time.
    public sealed class OriginalCannonBoosterView : OriginalOptionalBoosterEffect, IOriginalLocalPositionTarget
    {
        [SerializeField] private OriginalCannonRigView cannon;
        [SerializeField] private ParticleSystem fuse, muzzleFlash;
        [SerializeField] private string ballPrefabPath;
        [SerializeField] private OriginalTntShakeConfig shake;
        [SerializeField] private float introDelay, moveSecondsPerUnit, animationSpeed, fireLead;
        [SerializeField] private float offscreenDistance, boardClearance, muzzleHeight, ballEndHeight;
        [SerializeField] private float columnHold, operationTail, audioDelay, nearDistance;
        [SerializeField] private float toolbarHiddenOffset, toolbarHideSeconds, toolbarShowSeconds, toolbarOvershoot;

        private const int BoosterOperation = 7;
        private OriginalPlayableBoard game;
        private OriginalInitialBoardView board;
        private OriginalBottomUi bottom;
        private OriginalCannonBallView ball;
        private OriginalGridCell[] frozen;
        private Coroutine sequence;
        private bool active, toolbarDisplaced;

        // MTween's world-position procedure shares the native Vector3 interpolation.
        OriginalSwapPoint IOriginalLocalPositionTarget.LocalPosition
        {
            get
            {
                Vector3 position = transform.position;
                return new OriginalSwapPoint(position.x, position.y, position.z);
            }
            set => transform.position = new Vector3(value.X, value.Y, value.Z);
        }

        internal override void Play(OriginalPlayableBoard game, OriginalInitialBoardView board,
            Camera camera, OriginalBottomUi bottom, OriginalGridCell target)
        {
            var area = board.Layout.CurrentBoard;
            var startCell = board.Grid.GetCell(target.X, area.Bottom);
            var prefab = Resources.Load<GameObject>(ballPrefabPath);
            var ballPrefab = prefab != null ? prefab.GetComponent<OriginalCannonBallView>() : null;
            if (startCell == null || ballPrefab == null) throw new InvalidOperationException("The cannon column or ball prefab is missing.");
            this.game = game;
            this.board = board;
            this.bottom = bottom;
            // CannonBooster.ctor 0x3191894 instantiates both effects as scene roots.
            // Board shake only moves the grid, so the projectile can sample world position.
            transform.SetParent(null, false);
            ball = Instantiate(ballPrefab);
            ball.gameObject.SetActive(false);
            cannon.Initialize();
            frozen = new OriginalGridCell[area.Top - area.Bottom + 1];
            for (int y = area.Bottom; y <= area.Top; y++)
            {
                var cell = board.Grid.GetCell(target.X, y);
                frozen[y - area.Bottom] = cell;
                cell?.CrossFallFreeze();
            }
            game.State.Operations.OperationStart(BoosterOperation);
            active = true;
            bottom.BeginCannonMotion();
            toolbarDisplaced = true;
            // BottomUi.HideForCannonBooster (0x13bcd7c) uses the camera height.
            float hiddenY = toolbarHiddenOffset - camera.orthographicSize;
            StartCoroutine(MoveToolbar(new Vector3(0f, hiddenY, 0f), toolbarHideSeconds, false));
            float entryHeight = camera.transform.position.y - (camera.orthographicSize + offscreenDistance);
            Vector3 end = startCell.GetViewPosition();
            end.y -= boardClearance + Mathf.Max(0, 9 - board.Grid.Height) * .5f;
            transform.position = new Vector3(end.x, entryHeight, 0f);
            sequence = board.Presentation.Coroutines.ManualStartCoroutine(Sequence(startCell, end), Retire);
        }

        private IEnumerator Sequence(OriginalGridCell startCell, Vector3 end)
        {
            yield return game.Wait(introDelay);
            game.PlaySfx(1320, audioDelay);
            cannon.PlayArrival();
            fuse.Play();
            // CannonBooster starts an independent MTween, then polls the approach
            // after yielding. MTween uses Unity's scaled delta outside replay.
            float duration = Vector3.Distance(transform.position, end) * moveSecondsPerUnit;
            duration *= 1f / animationSpeed;
            var approach = new OriginalLocalMoveTween(() => Time.deltaTime);
            approach.Configure(this, new OriginalSwapPoint(end.x, end.y, end.z), duration, null, 3);
            StartCoroutine(approach);
            do { yield return null; }
            while (Vector3.Distance(transform.position, end) > nearDistance);

            board.ShakeGrid(shake);
            cannon.PlayFire(animationSpeed, OriginalCameraProjection.IsLandscape);
            yield return game.Wait(fireLead);

            fuse.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            muzzleFlash.Play();
            game.PlaySfx(1321, audioDelay);
            Vector3 muzzle = transform.position + Vector3.up * muzzleHeight;
            ball.Launch(game, board, startCell, muzzle, ballEndHeight);
            yield return game.Wait(columnHold);
            ReleaseColumn();
            // BottomUi.ShowAfterCannonBooster (0x13bce5c) samples the current safe bottom.
            StartCoroutine(MoveToolbar(bottom.GetInsidePosition(), toolbarShowSeconds, true));
            yield return game.Wait(operationTail);
        }

        private IEnumerator MoveToolbar(Vector3 target, float duration, bool show)
        {
            Vector3 start = bottom.transform.position;
            for (float time = 0f; time < duration; time += board.Clock.DeltaTime)
            {
                if (bottom == null) yield break;
                float t = time / duration;
                float u = t - 1f;
                float overshoot = toolbarOvershoot;
                float eased = show ? 1f + u * u * ((overshoot + 1f) * u + overshoot)
                    : t * t * ((overshoot + 1f) * t - overshoot);
                bottom.transform.position = Vector3.LerpUnclamped(start, target, eased);
                yield return null;
            }
            if (show) ReleaseToolbar();
            else if (bottom != null) bottom.transform.position = target;
        }

        private void ReleaseToolbar()
        {
            if (!toolbarDisplaced) return;
            toolbarDisplaced = false;
            if (bottom != null) bottom.EndCannonMotion();
        }

        private void ReleaseColumn()
        {
            if (frozen == null) return;
            foreach (var cell in frozen) cell?.CrossFallUnFreeze();
            frozen = null;
        }

        private void Finish()
        {
            ReleaseColumn();
            ReleaseToolbar();
            if (!active) return;
            active = false;
            if (game.State.HasOperation(BoosterOperation)) game.State.Operations.OperationFinish(BoosterOperation);
        }

        // CannonBooster.Use completion (0x3192178) hides both views together.
        private void Retire()
        {
            sequence = null;
            if (!active) return;
            StopAllCoroutines();
            Finish();
            DestroyBall();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        internal override void Cancel()
        {
            if (sequence != null) board.Presentation.Coroutines.StopCoroutine(sequence);
            Retire();
        }

        private void DestroyBall()
        {
            if (ball == null) return;
            ball.Cancel();
            Destroy(ball.gameObject);
            ball = null;
        }

        private void OnDestroy()
        {
            Finish();
            if (sequence != null) board.Presentation.Coroutines.StopCoroutine(sequence);
            DestroyBall();
        }
    }
}
