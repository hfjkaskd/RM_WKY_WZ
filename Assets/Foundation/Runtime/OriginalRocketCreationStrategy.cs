using UnityEngine;

namespace Whitebox.Foundation
{
    // Creation/sprite portion of RocketStrategy. Explosion travel is restored
    // independently; it must not be replaced with a generic line clear.
    public abstract partial class OriginalRocketCreationStrategy
    {
        protected readonly OriginalRocketItemSpriteView view;
        protected readonly OriginalRocketViewAssets assets;
        protected readonly IOriginalRocketViewServices services;
        protected OriginalRocketCreationStrategy(OriginalRocketItemSpriteView view, OriginalRocketViewAssets assets,
            IOriginalRocketViewServices services)
        {
            this.view = view; this.assets = assets; this.services = services;
        }
        protected abstract bool IsVertical { get; }
        protected abstract Transform Animation { get; }
        protected abstract OriginalRocketViewPose Pose { get; }
        public void SetSprites() // Horizontal 0x27bc244 / vertical 0x27bdbb8.
        {
            Animation.gameObject.SetActive(false);
            view.BaseRenderer.gameObject.SetActive(false);
            var pose = Pose;
            view.BaseRenderer.sprite = assets.GetSprite(pose.BodySprite);
            view.BaseRenderer.transform.localPosition = pose.BodyOffset;
            Animation.localPosition = pose.BodyOffset;
            view.ShadowRenderer.sprite = assets.GetSprite(assets.ShadowSprite);
            view.ShadowRenderer.transform.localPosition = pose.ShadowOffset;
            view.ShadowRenderer.transform.localRotation = Quaternion.Euler(pose.ShadowEuler);
            view.FirstPart.sprite = assets.GetSprite(pose.FirstPartSprite);
            view.FirstPart.transform.localPosition = pose.FirstPartOffset;
            view.SecondPart.sprite = assets.GetSprite(pose.SecondPartSprite);
            view.SecondPart.transform.localPosition = pose.SecondPartOffset;
            view.FirstPart.gameObject.SetActive(false);
            view.SecondPart.gameObject.SetActive(false);
        }
        public void PlayCreationAnimation(float normalizedStartTime, bool playSound) // 0x27bc4ec / 0x27bde70.
        {
            if (playSound) services.PlaySfx(assets.CreationSound, assets.CreationSoundOffset);
            Animation.gameObject.SetActive(true);
            view.Animator.Play(assets.GetCreationState(IsVertical), 0, normalizedStartTime);
        }
        public bool IsCreationAnimationPlaying() // 0x27bc5c4 / 0x27bdf48.
        {
            var state = view.Animator.GetCurrentAnimatorStateInfo(0);
            return state.fullPathHash == assets.GetCreationState(IsVertical) && state.normalizedTime < 1f;
        }
    }
    public sealed partial class OriginalHorizontalRocketCreationStrategy : OriginalRocketCreationStrategy
    {
        private readonly bool isWideOrLandscapeDevice;
        public OriginalHorizontalRocketCreationStrategy(OriginalRocketItemSpriteView view, OriginalRocketViewAssets assets,
            IOriginalRocketViewServices services) : base(view, assets, services)
        { isWideOrLandscapeDevice = services.IsDeviceWide; } // 0x27bb8cc, mobile IsLandscape is false.
        protected override bool IsVertical => false;
        protected override Transform Animation => view.HorizontalAnimation;
        protected override OriginalRocketViewPose Pose => assets.HorizontalPose;
    }
    public partial class OriginalVerticalRocketCreationStrategy : OriginalRocketCreationStrategy
    {
        public OriginalVerticalRocketCreationStrategy(OriginalRocketItemSpriteView view, OriginalRocketViewAssets assets,
            IOriginalRocketViewServices services) : base(view, assets, services) { }
        protected override bool IsVertical => true;
        protected override Transform Animation => view.VerticalAnimation;
        protected override OriginalRocketViewPose Pose => assets.VerticalPose;
    }
    // The original water strategy inherits all creation behavior unchanged.
    public sealed partial class OriginalWaterVerticalRocketCreationStrategy : OriginalVerticalRocketCreationStrategy
    {
        public OriginalWaterVerticalRocketCreationStrategy(OriginalRocketItemSpriteView view, OriginalRocketViewAssets assets,
            IOriginalRocketViewServices services) : base(view, assets, services) { }
    }
}
