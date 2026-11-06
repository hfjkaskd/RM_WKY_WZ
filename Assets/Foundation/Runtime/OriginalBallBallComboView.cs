using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public interface IOriginalBallBallViewServices
    {
        OriginalGameObjectPool Pool { get; }
        OriginalLightballViewAssets LightballAssets { get; }
        void PlaySfx(int type,float delay);
        void PlayHaptic(int id,int type,float strength,float duration,bool replay);
    }
    // BallBallComboView 0x1f4397c..0x1f44674. Native Animator and
    // prefab transforms own the merge; typed polling replaces no clip events.
    public sealed class OriginalBallBallComboView : MonoBehaviour,IOriginalPoolable
    {
        public Animator animator;
        public SortingGroup sortingGroup;
        public SpriteRenderer black;
        public Transform maskTransform;
        private IOriginalBallBallViewServices services;
        private Action onAnimationCompleted;
        private OriginalBallBallComboUseParticles useParticles;
        private bool isReady,maskExpanding;
        private float fadeElapsed,maskElapsed;
        private OriginalBallBallViewSettings settings;
        public float NormalizedTime => animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        public bool IsReady => isReady;
        public int GetPoolId()=>31;
        public void OnSpawn() { }
        public void OnRecycle() { isReady = false; }
        public void Initialize(IOriginalBallBallViewServices owner,Vector3 position,int swipe,Action complete)
        {
            services = owner; settings = owner.LightballAssets.ballBall;
            transform.position = position; onAnimationCompleted = complete;
            sortingGroup.gameObject.SetActive(true);
            sortingGroup.sortingLayerID = SortingLayer.NameToID("Front"); sortingGroup.sortingOrder = 100;
            black.sortingLayerID = SortingLayer.NameToID("Front"); black.sortingOrder = 0;
            if (owner.LightballAssets.speedIncrease) animator.Play(Animator.StringToHash(settings.fastState),0,0f);
            animator.speed = owner.LightballAssets.speedIncrease ? settings.fastAnimatorSpeed : settings.animatorSpeed;
            animator.transform.localScale = swipe == 2 ? new Vector3(-1f,1f,1f) : Vector3.one;
            isReady = true; maskExpanding = false; fadeElapsed = maskElapsed = 0f;
            useParticles = owner.Pool.Spawn<OriginalBallBallComboUseParticles>(33,true);
            useParticles.transform.SetParent(transform); useParticles.transform.localPosition = Vector3.zero; useParticles.Play();
            maskTransform.localScale = Vector3.one * settings.initialMaskScale;
            black.color = Color.clear;
            owner.PlaySfx(215,settings.soundDelay); owner.PlayHaptic(23,4,.7f,0f,false);
        }
        // TweenManager 0x38ffc34/0x38ffc88 supplies the level delta before
        // game-context updates. Animator completion remains a native Update poll.
        public bool ManualTweenUpdate(float delta)
        {
            if (!isReady) return true;
            float previousFade = fadeElapsed; fadeElapsed += delta;
            if (previousFade < settings.fadeDuration)
            {
                Color color = black.color; color.a = settings.blackAlpha * Mathf.Sin(Mathf.Clamp01(fadeElapsed/settings.fadeDuration)*Mathf.PI*.5f); black.color = color;
            }
            if (maskExpanding)
            {
                maskElapsed += delta;
                maskTransform.localScale = Vector3.one * Mathf.Lerp(settings.startMaskScale,settings.endMaskScale,maskElapsed/settings.maskDuration);
                if (maskElapsed >= settings.maskDuration) services.Pool.Recycle(this);
            }
            return !isReady;
        }
        private void Update()
        {
            if (!isReady || maskExpanding) return;
            if ((double)NormalizedTime < .98) return; // Native float promoted to double before comparing0.98.
            services.PlayHaptic(24,4,.7f,0f,false);
            var particles = services.Pool.Spawn<OriginalBallBallComboExplodeParticles>(34,true);
            particles.transform.position = transform.position; particles.Play(false);
            maskTransform.localScale = Vector3.one * settings.startMaskScale; maskExpanding = true; maskElapsed = 0f;
            onAnimationCompleted();
            sortingGroup.gameObject.SetActive(false); services.Pool.Recycle(useParticles);
        }
    }
}
