using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalPotionVisualAssets : ScriptableObject
    {
        [Serializable]
        public struct Liquids
        {
            public Sprite A, B, C;
        }

        [SerializeField] private Liquids[] colors;
        public int[] bottleColors;
        public Vector3[] positions;
        public float[] scales;
        public int[] sortOrders;
        public float idleSpeed;
        public Liquids GetLiquids(int color) => colors[color - 1];
    }
}
