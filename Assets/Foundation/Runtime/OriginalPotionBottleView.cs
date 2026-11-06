using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalPotionBottleView : MonoBehaviour, IOriginalPoolable
    {
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private Animator potionAnimator;
        [SerializeField] private SpriteRenderer liquidA, liquidB, liquidC;
        private static readonly int Idle = Animator.StringToHash("Base Layer.PotionIdleAnimation");
        private static readonly int[] DamageAnimations = {
            Animator.StringToHash("Base Layer.PotionAnimation1"), Animator.StringToHash("Base Layer.PotionAnimation2"),
            Animator.StringToHash("Base Layer.PotionAnimation3"), Animator.StringToHash("Base Layer.PotionAnimation4")
        };
        private static readonly int[] TapAnimations = {
            Animator.StringToHash("Base Layer.PotionTapAnimation1"), Animator.StringToHash("Base Layer.PotionTapAnimation2"),
            Animator.StringToHash("Base Layer.PotionTapAnimation3"), Animator.StringToHash("Base Layer.PotionTapAnimation4")
        };
        private static readonly WaitForSeconds[] AnimationDelays = {
            new WaitForSeconds(1f / 60f), new WaitForSeconds(3f / 60f),
            new WaitForSeconds(5f / 60f), new WaitForSeconds(7f / 60f)
        };
        public int Color { get; private set; }

        public int GetPoolId() => 59;
        public void OnSpawn() { }
        public void OnRecycle() { }

        internal void Initialize(int color, OriginalPotionVisualAssets assets, Transform parent, int slot)
        {
            Color = color;
            var liquids = assets.GetLiquids(color);
            liquidA.sprite = liquids.A;
            liquidB.sprite = liquids.B;
            liquidC.sprite = liquids.C;
            transform.SetParent(parent, false);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = assets.positions[slot];
            transform.localScale = Vector3.one * assets.scales[slot];
            sortingGroup.sortingOrder = assets.sortOrders[slot];
            potionAnimator.speed = assets.idleSpeed;
            potionAnimator.Play(Idle, 0, 0f);
        }

        internal void ScheduleAnimation(int order, bool tap) => StartCoroutine(Animate(order, tap));

        private IEnumerator Animate(int order, bool tap) // Native stagger is (2 * order + 1) / 60.
        {
            yield return AnimationDelays[order];
            int variant = Mathf.Min((int)(Random.value * 4f), 3);
            potionAnimator.Play((tap ? TapAnimations : DamageAnimations)[variant], 0, 0f);
        }
    }
}
