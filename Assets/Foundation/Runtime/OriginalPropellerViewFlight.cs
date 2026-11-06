using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalPropellerViewServices : IOriginalRocketViewServices
    {
        int UiSortingLayer { get; }
        int NextPropellerSortingOffset();
        void PlaySfxInLoop(int type);
        void StopSoundInLoop(int type);
        void PlayPropellerTakeoffParticles(Vector3 position);
        void PlayPropellerTargetParticles(Vector3 position);
        void PlayPropellerRocketParticles(Vector3 position);
    }

    public sealed partial class OriginalPropellerItemSpriteView : IOriginalPropellerPilotView
    {
        public string tapStartFlyState, swipeStartFlyState;
        public int flySound, hitSound, takeoffSound;
        public float hitSoundOffset, takeoffSoundOffset;
        private bool didStartFlyAnimation;
        private Action onTargetReached;
        private IOriginalGuidedExploderPilot pilot;
        private IOriginalPropellerViewServices FlightServices => (IOriginalPropellerViewServices)services;
        public Transform PilotTransform => transform;
        public Transform PilotAnimatorTransform => animatorTransform;
        public Transform PilotFanTransform => fanTransform;
        public Transform PilotShadowTransform => shadow.transform;
        public OriginalGuidedExploderFlyState FlyState => pilot == null ? OriginalGuidedExploderFlyState.Stable : pilot.FlyState;
        public override void SwapAnimationStarted(bool valid, int otherItem) // 0x27c0a30.
        {
            base.SwapAnimationStarted(valid, 0);
            if (valid && otherItem != 5 && otherItem != 31) PlayStartFlyAnimation(true, true);
        }
        public void PlayStartFlyAnimation(bool isSwipe, bool shouldPlayAudio) // 0x27b1e14.
        {
            if (didStartFlyAnimation) return;
            didStartFlyAnimation = true;
            if (shouldPlayAudio) services.PlaySfx(takeoffSound, takeoffSoundOffset);
            shadow.gameObject.SetActive(true);
            animator.Play(Animator.StringToHash(isSwipe ? swipeStartFlyState : tapStartFlyState), 0, 0f);
        }
        public void MoveToTarget(bool particles, Action targetReached) // 0x27b2010.
        {
            onTargetReached = targetReached;
            if (pilot == null) pilot = model.GetNewPilot();
            if (pilot.FlyState != OriginalGuidedExploderFlyState.Stable) return;
            // The first pilot tick can recycle this view before the native takeoff effect.
            var flightServices = FlightServices;
            if (pilot.ShouldPlaySfxInLoop) flightServices.PlaySfxInLoop(flySound);
            pilot.StartFlying(this, model);
            if (particles) flightServices.PlayPropellerTakeoffParticles(transform.position);
        }
        public void CancelMove() => pilot.TargetChanged(); // 0x27b2288.
        private void Update() // 0x27b26a4.
        { if (pilot != null && pilot.FlyState != OriginalGuidedExploderFlyState.Stable) pilot.Move(); }
        public void UpdatePilotSorting(bool scaleDown, bool sortEverything)
        {
            // Sorting 0x12d9f74 / 0x12d9ff4, normal propeller, no combo offset.
            int offset = scaleDown ? 0 : FlightServices.NextPropellerSortingOffset();
            UpdateSorting(new OriginalSwapSorting(FlightServices.UiSortingLayer, unchecked((sortEverything ? 150 : 100) + offset), true));
        }
        public void TriggerTargetReached() // 0x27b2510.
        { services.PlaySfx(hitSound, hitSoundOffset); onTargetReached?.Invoke(); }
        public void TriggerFakeTargetReached() // 0x27b25a4.
        { services.PlaySfx(hitSound, hitSoundOffset); model.FakeTargetReached(); }
        public void ExplodeSelf(bool propellerRocketCombo) // 0x27b232c.
        {
            if(propellerRocketCombo)FlightServices.PlayPropellerRocketParticles(transform.position);
            else FlightServices.PlayPropellerTargetParticles(transform.position);
            FlightServices.StopSoundInLoop(flySound);
            services.Pool.Recycle(this);
        }
    }
}
