using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalCupboardItemView : MonoBehaviour, IOriginalItemSwapView, IOriginalPoolable
    {
        [SerializeField] private OriginalRootTransform rootTransform;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private GameObject doorView, cupboardMask;
        [SerializeField] private GameObject[] plateViews;
        [SerializeField] private SpriteRenderer cupboardContainerLeft, cupboardContainerRight;
        [SerializeField] private Animator cupboardAnimator, platesUpAnimator, platesDownAnimator;
        [SerializeField] private string idleState, firstTapState, commonTapState;
        [SerializeField] private string[] shakeStates, plateStates, plateTapStates, alternatePlateTapStates;
        [SerializeField] private float cupboardSpeed, plateSpeed;
        private OriginalLevelPresentation presentation;
        private OriginalGameAudioHost gameAudio;
        private IOriginalLevelSeedRandom random;
        private OriginalDeltaTimeManager clock;
        private int lastTriggerId;
        private int idle, firstTap, commonTap;
        private int[] shake, plate, plateTap, alternatePlateTap;
        internal int ShakeFrame { get; private set; }
        internal int SelectedShake { get; private set; } = -1;
        public bool IsSwapping => false;
        public int GetPoolId() => 47;

        private void Awake()
        {
            idle = Animator.StringToHash(idleState);
            firstTap = Animator.StringToHash(firstTapState);
            commonTap = Animator.StringToHash(commonTapState);
            shake = System.Array.ConvertAll(shakeStates, Animator.StringToHash);
            plate = System.Array.ConvertAll(plateStates, Animator.StringToHash);
            plateTap = System.Array.ConvertAll(plateTapStates, Animator.StringToHash);
            alternatePlateTap = System.Array.ConvertAll(alternatePlateTapStates, Animator.StringToHash);
        }

        internal void Initialize(OriginalCupboardBoardItem model, Transform parent, Vector3 position,
            OriginalLevelPresentation presentation, OriginalGameAudioHost audio, IOriginalLevelSeedRandom random,
            OriginalDeltaTimeManager clock)
        {
            this.presentation = presentation;
            gameAudio = audio;
            this.random = random;
            this.clock = clock;
            doorView.SetActive(model.HasDoor);
            for (int i = 0; i < plateViews.Length; i++) plateViews[i].SetActive(i < model.LayerCount);
            platesUpAnimator.Play(plate[4], 0, 1f);
            platesDownAnimator.Play(plate[4], 0, 1f);
            cupboardAnimator.speed = cupboardSpeed;
            platesUpAnimator.speed = plateSpeed;
            platesDownAnimator.speed = plateSpeed;
            cupboardAnimator.Play(idle, 0, 0f);
            transform.SetParent(parent, false);
            transform.localPosition = position;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            rootTransform.Init();
            int itemLayer = SortingLayer.NameToID("Item");
            // CupboardItemView.IsReverseSorted 0x2bcc7c8 and ItemView.InitTransform 0x2b41e40.
            bool covered = OriginalItemFallQueries.IsUnderNonFallBlockingAboveStaticItem(model) ||
                model.Mediator.CurrentCell.Owner.StaticTouch.HasStaticItem(6);
            var sorting = covered ? OriginalItemSortingRules.Default(itemLayer) :
                OriginalItemSortingRules.AnimatedItem(itemLayer, (int)position.y - presentation.CurrentBoardStartY);
            sortingGroup.sortingLayerID = sorting.Layer;
            sortingGroup.sortingOrder = sorting.Order;
            if (cupboardMask.activeInHierarchy)
            {
                cupboardContainerLeft.maskInteraction = SpriteMaskInteraction.None;
                cupboardContainerRight.maskInteraction = SpriteMaskInteraction.None;
                cupboardMask.SetActive(false);
            }
        }

        public void RemoveDoor(int precedingShake)
        {
            gameAudio.GameSfx.PlaySfx(99, .04f);
            PlayShake(precedingShake);
            PlayPlates(10, plate, plate);
            var particles = presentation.Pool.Spawn<OriginalCupboardDoorParticles>(48, true);
            particles.transform.position = doorView.transform.position;
            particles.Play(presentation.CupboardParticleDepth);
            doorView.SetActive(false);
        }

        public void Damage(int index, int precedingShake, OriginalExplodeData trigger)
        {
            if (lastTriggerId != trigger.Id) PlayShake(precedingShake);
            lastTriggerId = trigger.Id;
            gameAudio.GameSfx.PlaySfx(100 + random.Next(0, 3), .04f);
            PlayPlates(index, plate, plate);
            var plateView = plateViews[index];
            presentation.Particles.GetGlobalParticle<OriginalCupboardBurstParticles>(49).Play(plateView.transform.position);
            plateView.SetActive(false);
        }

        public void Explode() // 0x2bcab24.
        {
            presentation.Particles.GetGlobalParticle<OriginalCupboardBurstParticles>(50).Play(doorView.transform.position);
            gameAudio.GameSfx.PlaySfx(98, .04f);
            presentation.Pool.Recycle(this);
        }

        public void PlayTap(bool hasDoor, int count)
        {
            if (hasDoor)
            {
                gameAudio.GameSfx.PlaySfx(103, .04f);
                cupboardAnimator.Play(firstTap, 0, 0f);
                return;
            }
            cupboardAnimator.Play(commonTap, 0, 0f);
            bool alternateUpper = random.Next(0, 1) != 0;
            gameAudio.GameSfx.PlaySfx(count == 1 ? 106 : count <= 5 ? 105 : 104, .04f);
            // Native tap states use opposing variants for the two rows.
            var upperStates = alternateUpper ? alternatePlateTap : plateTap;
            var lowerStates = count == 1 ? upperStates : alternateUpper ? plateTap : alternatePlateTap;
            PlayPlates(count, upperStates, lowerStates);
        }

        private void PlayPlates(int count, int[] upperStates, int[] lowerStates) // 0x2bcc488, 0x2bcb280.
        {
            if (count == 0) return;
            int upper = count / 2;
            int lower = (count + 1) / 2;
            if (upper > 0) platesUpAnimator.Play(upperStates[upper - 1], 0, 0f);
            platesDownAnimator.Play(lowerStates[lower - 1], 0, 0f);
        }

        private void PlayShake(int preceding) // 0x2bcc35c.
        {
            ShakeFrame = clock.FrameCount;
            SelectedShake = preceding >= 0 ? 1 - preceding : UnityEngine.Random.value < .5f ? 0 : 1;
            cupboardAnimator.Play(shake[SelectedShake], 0, 0f);
        }

        public void OnSpawn() { }
        public void OnRecycle() // 0x2bcc350.
        {
            ShakeFrame = 0;
            SelectedShake = -1;
            lastTriggerId = 0;
        }
    }
}
