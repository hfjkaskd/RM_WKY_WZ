using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalLightballItemBg : MonoBehaviour, IOriginalPoolable
    {
        public SpriteRenderer colorBg, whiteBg;
        public ParticleSystem rayTipParticle;
        public Color[] spriteColors;
        private OriginalGameObjectPool pool;
        public void BindPool(OriginalGameObjectPool value) { pool = value; }
        public int GetPoolId() => 25; // 0x1f4096c.
        public void OnSpawn() { } // 0x1f40974.
        public void Initialize(int itemType, int matchType, OriginalSwapSorting sorting, OriginalLightballViewAssets assets) // 0x1f40518.
        {
            if (itemType == 1)
            {
                float colorScale = assets.colorBackgroundScale;
                colorBg.transform.localScale = new Vector3(colorScale, colorScale, 1f);
                float whiteScale = matchType == 5 ? assets.purpleWhiteBackgroundScale : assets.whiteBackgroundScale;
                whiteBg.transform.localScale = new Vector3(whiteScale, whiteScale, 1f);
                colorBg.enabled = true;
                colorBg.sprite = assets.GetBackgroundSprite(matchType);
                colorBg.color = spriteColors[OriginalLightballViewAssets.GetColorIndex(matchType)];
                colorBg.transform.localPosition = Vector3.zero;
                whiteBg.enabled = true;
                whiteBg.sprite = assets.GetBackgroundSprite(matchType);
                whiteBg.transform.localPosition = Vector3.zero;
            }
            else { colorBg.enabled = false; whiteBg.enabled = false; }
            OriginalItemSortingRules.Apply(whiteBg, new OriginalSwapSorting(sorting.Layer, sorting.Order + 1, false));
            OriginalItemSortingRules.Apply(colorBg, sorting);
        }
        public void PlayRayTipParticle() { rayTipParticle.Play(); } // 0x1f40950.
        public void OnRecycle() // 0x1f40978.
        { transform.localRotation = Quaternion.identity; transform.localScale = Vector3.one; }
        public void RecycleSelf() { pool.Recycle(this); } // 0x1f409f8.
    }
}
