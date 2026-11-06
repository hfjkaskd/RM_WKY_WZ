using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCannonBallView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem trail;
        [SerializeField] private float speed, accelerationPerFrame, hitRadius, scaleSeconds, tailSeconds;
        [SerializeField] private Vector3 launchScale;
        private OriginalPlayableBoard game;
        private OriginalDeltaTimeManager clock;
        private OriginalCoroutinePlayer coroutines;
        private Coroutine flight;
        private OriginalGridCell lastFrozen;
        private float scaleElapsed;
        private bool scaling;

        internal void Launch(OriginalPlayableBoard game, OriginalInitialBoardView board,
            OriginalGridCell first, Vector3 start, float endHeight)
        {
            this.game = game;
            clock = board.Clock;
            coroutines = board.Presentation.Coroutines;
            transform.position = start;
            gameObject.SetActive(true);
            trail.Play();
            flight = coroutines.ManualStartCoroutine(Fly(first, endHeight), ReleaseFlight);

            // The original starts DOScale after starting the projectile routine.
            // DOTweenSettings uses Linear and the manual level tween clock.
            transform.localScale = launchScale;
            scaleElapsed = 0f;
            scaling = true;
            game.BoosterTweenUpdate += AdvanceScale;
        }

        private IEnumerator Fly(OriginalGridCell first, float endHeight)
        {
            var explode = OriginalExploder.Next(20, new OriginalCellPoint(first.X, -1));
            var projectile = transform;
            float velocity = speed;
            bool passedColumn = false;
            while (projectile.position.y < endHeight)
            {
                Vector3 position = projectile.position;
                position.y += velocity * clock.DeltaTime;
                velocity += accelerationPerFrame;
                projectile.position = position;
                // CannonBoosterBallView scans from the start cell every frame.
                // The shared exploder ID prevents repeat damage inside the hit window.
                if (!passedColumn)
                {
                    for (var cell = first; cell != null;)
                    {
                        var above = cell.Neighbors.Get(1);
                        if (Mathf.Abs(cell.GetViewPosition().y - position.y) < hitRadius)
                        {
                            if (lastFrozen != cell)
                            {
                                UnfreezeLast();
                                lastFrozen = cell;
                                cell.FreezeFall();
                            }
                            cell.ExplodeCurrentItem(explode);
                        }
                        else if (above == null && lastFrozen == cell)
                        {
                            UnfreezeLast();
                            passedColumn = true;
                        }
                        cell = above;
                    }
                }
                yield return null;
            }
            UnfreezeLast();
            // MoveToGlobalAndDestroy (0x3192fb8) yields scaled Unity time after leaving the board.
            yield return new WaitForSeconds(tailSeconds);
            gameObject.SetActive(false);
        }

        private void AdvanceScale()
        {
            scaleElapsed += clock.DeltaTime;
            float progress = Mathf.Clamp01(scaleElapsed / scaleSeconds);
            transform.localScale = Vector3.LerpUnclamped(launchScale, Vector3.one, progress);
            if (progress >= 1f) StopScale();
        }

        private void StopScale()
        {
            if (!scaling) return;
            scaling = false;
            game.BoosterTweenUpdate -= AdvanceScale;
        }

        private void UnfreezeLast()
        {
            if (lastFrozen == null) return;
            lastFrozen.UnFreezeFall();
            lastFrozen = null;
        }

        private void ReleaseFlight()
        {
            flight = null;
            StopScale();
            UnfreezeLast();
        }

        private void StopFlight()
        {
            if (flight != null) coroutines.StopCoroutine(flight);
            ReleaseFlight();
        }

        internal void Cancel()
        {
            StopFlight();
            gameObject.SetActive(false);
        }

        private void OnDestroy() => StopFlight();
    }
}
