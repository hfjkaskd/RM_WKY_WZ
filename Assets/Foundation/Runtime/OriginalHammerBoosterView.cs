using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalHammerBoosterView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem explodeParticles;
        [SerializeField] private SpriteRenderer baseView;
        [SerializeField] private Transform rootView;
        [SerializeField] private OriginalTntShakeConfig shake;
        [SerializeField] private float flightBaseDuration, flightDistanceFactor, animationSpeed, animationLeadTime;
        [SerializeField] private float impactDelay, postImpactFlightFraction, exitSoundDelay, freezeFallSeconds;
        [SerializeField] private Vector3 launchOffset, exitOffset;

        private const int HammerOperation = 7;
        private OriginalPlayableBoard game;
        private OriginalInitialBoardView board;
        private OriginalGridCell target;
        private bool active;

        public void Play(OriginalPlayableBoard game, OriginalInitialBoardView board, OriginalGridCell cell, Vector3 launchFramePosition)
        {
            this.game = game;
            this.board = board;
            target = cell;
            active = true;
            game.State.Operations.OperationStart(HammerOperation);
            // HammerBooster.GetStartPosition 0x3190198 applies a world-space offset.
            StartCoroutine(Strike(launchFramePosition + launchOffset));
        }

        private IEnumerator Strike(Vector3 launchPosition)
        {
            Vector3 end = target.GetViewPosition();
            Vector3 start = launchPosition - animator.transform.localPosition;
            animator.enabled = false;
            rootView.localScale = Vector3.one / baseView.transform.localScale.x;
            int originalLayer = baseView.sortingLayerID;
            int originalOrder = baseView.sortingOrder;
            if (end.y - launchPosition.y < 3f)
            {
                baseView.sortingLayerID = SortingLayer.NameToID("Ui");
                baseView.sortingOrder = 150;
            }
            float flight = flightBaseDuration + Vector3.Distance(start, end) * flightDistanceFactor;
            StartCoroutine(Fly(transform, start, end, flight, true));
            yield return game.Wait(flight - animationLeadTime);
            game.PlaySfx(1317, 0f);
            animator.speed = animationSpeed;
            animator.enabled = true;
            yield return game.Wait(impactDelay);
            transform.SetParent(board.BoardRoot, true);
            baseView.sortingLayerID = originalLayer;
            baseView.sortingOrder = originalOrder;
            game.PlaySfx(1318, 0f);
            explodeParticles.Play();
            target.FreezeFallForDuration(freezeFallSeconds);
            target.ExplodeCurrentItem(OriginalExploder.Next(18));
            board.ShakeGrid(shake);
            float exitDuration = flight * postImpactFlightFraction;
            yield return game.Wait(exitDuration);
            Vector3 exitPosition = launchPosition + animator.transform.localPosition + exitOffset;
            StartCoroutine(Fly(transform, transform.position, exitPosition, flight, false));
            StartCoroutine(Shrink(flight));
            yield return game.Wait(exitSoundDelay);
            game.PlaySfx(1319, 0f);
            yield return game.Wait(exitDuration - exitSoundDelay);
            Finish();
            Destroy(gameObject);
        }

        private IEnumerator Fly(Transform moved, Vector3 start, Vector3 end, float duration, bool easeOut)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += board.Clock.DeltaTime)
            {
                float t = elapsed / duration;
                float u = 1f - t;
                moved.position = Vector3.LerpUnclamped(start, end, easeOut ? 1f - u * u * u : t * t * t);
                yield return null;
            }
            moved.position = end;
        }

        private IEnumerator Shrink(float duration)
        {
            Vector3 from = rootView.localScale;
            Vector3 to = Vector3.one / baseView.transform.localScale.x;
            for (float elapsed = 0f; elapsed < duration; elapsed += board.Clock.DeltaTime)
            {
                float t = elapsed / duration;
                rootView.localScale = Vector3.LerpUnclamped(from, to, t * t * t);
                yield return null;
            }
            rootView.localScale = to;
        }

        internal void Cancel()
        {
            StopAllCoroutines();
            Finish();
            Destroy(gameObject);
        }

        private void Finish()
        {
            if (!active) return;
            active = false;
            if (game.State.HasOperation(HammerOperation)) game.State.Operations.OperationFinish(HammerOperation);
        }

        private void OnDestroy() => Finish();
    }
}
