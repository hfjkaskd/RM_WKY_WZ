using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Original CellAssets sprite array order. Resource paths keep its texture
    // dependencies lazy; cell pool ownership is composed separately.
    [CreateAssetMenu(menuName = "Recovery/Cell Assets")]
    public sealed class OriginalCellAssets : ScriptableObject, IOriginalCellBorderSprites
    {
        public string[] spritePaths;
        [NonSerialized] private Sprite[] sprites;
        [NonSerialized] private int loaded;
        public Sprite GetOnePixel() => Get(0); // 0x34424f8.
        public Sprite GetFillPart() => Get(1); // 0x3442520.
        public Sprite GetSidePart() => Get(2); // 0x344254c.
        public Sprite GetInnerCornerPart() => Get(3); // 0x3442578.
        public Sprite GetOuterCornerPart() => Get(4); // 0x34425a4.
        public Sprite GetCornerFadeOutMask(int type) => Get(type == 0 ? 5 : 9); // 0x34425d0.
        public Sprite GetInnerCornerFadeOutMask(int type) => Get(type == 0 ? 6 : 10); // 0x3442610.
        public Sprite GetSideFadeOutMask(int type) => Get(type == 0 ? 7 : 11); // 0x3442650.
        public Sprite GetCornerConnectFadeOutMask(int type) => Get(type == 0 ? 5 : 12); // 0x3442690.
        public Sprite GetFullFadeOutMask() => Get(8); // 0x34426d0.

        private Sprite Get(int index)
        {
            string path = spritePaths[index];
            if (sprites == null) sprites = new Sprite[spritePaths.Length];
            if ((loaded & (1 << index)) == 0)
            {
                sprites[index] = Resources.Load<Sprite>(path);
                loaded |= 1 << index;
            }
            return sprites[index];
        }
    }
}
