using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Whitebox.Foundation
{
    public sealed class OriginalGameBackground : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private string areaSpritePath;
        [SerializeField] private string levelSpritePath;
        [SerializeField, FormerlySerializedAs("gardenCloudsPrefabPath")] private string gardenDecorationsPrefabPath;
        private GameObject gardenDecorations;
        private Vector2 authoredSize;
        private Vector3 authoredScale;

        public void Initialize(OriginalInitialItemAmounts initialAmounts, bool hasHoleInGrid)
        {
            // Castle AreaConfig enables the custom background for grass or holes.
            // LevelManager.ShouldAreaUseGameBackground (0x1e19750) reads the static item count.
            const int grassType = 2;
            bool useLevelBackdrop = initialAmounts.IsThereStaticItemInLevel(grassType) || hasHoleInGrid;
            InitializeBackdrop(!useLevelBackdrop);
        }

        internal void InitializeArea() => InitializeBackdrop(true);

        private void InitializeBackdrop(bool useGarden)
        {
            string path = useGarden ? areaSpritePath : levelSpritePath;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Missing game background sprite: " + path);
            spriteRenderer.sprite = sprite;
            if (useGarden && gardenDecorations == null)
            {
                var prefab = Resources.Load<GameObject>(gardenDecorationsPrefabPath);
                if (prefab == null) throw new InvalidOperationException("Missing garden backdrop prefab: " + gardenDecorationsPrefabPath);
                gardenDecorations = Instantiate(prefab, transform, false);
                // CloudMover.StopClouds 0x3f6e510 is bound to GameContext.OnActivate.
                foreach (var animator in gardenDecorations.GetComponentsInChildren<Animator>())
                    animator.speed = 0f;
            }
            if (gardenDecorations != null) gardenDecorations.SetActive(useGarden);
            authoredScale = transform.localScale;
            authoredSize = spriteRenderer.bounds.size;
        }

        public void FitViewport(float width, float height, float portraitAspect)
        {
            // AreaView.GetAreaScale 0x3f6c424 and AreaBackgroundView.GetWidthScale 0x3f63efc.
            const float wideDeviceAspect = .57f;
            const float shortWideAspectLimit = .6f;
            const float shortWideWidthOffset = .045f;
            float scale = height / authoredSize.y;
            if (portraitAspect >= wideDeviceAspect)
            {
                scale = width / authoredSize.x;
                if (portraitAspect < shortWideAspectLimit) scale += shortWideWidthOffset;
            }
            transform.localScale = authoredScale * scale;
        }
    }
}
