using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalGrassViewAssets : ScriptableObject
    {
        [SerializeField] private string[] lightBasePaths, darkBasePaths, patchPaths;
        [SerializeField] private string shadowPath;
        [SerializeField] private float[] shadowAlphas;
        [NonSerialized] private Sprite[] lightBases, darkBases, patches;
        [NonSerialized] private Sprite shadow;

        public Sprite GetBase(int layer, IOriginalLevelSeedRandom random)
        {
            if (lightBases == null)
            {
                if (lightBasePaths.Length != 2 || darkBasePaths.Length != 2 ||
                    patchPaths.Length != 3 || shadowAlphas.Length != 2)
                    throw new InvalidOperationException("Grass view asset slots do not match the original configuration.");
                var light = Load(lightBasePaths);
                darkBases = Load(darkBasePaths);
                patches = Load(patchPaths);
                shadow = Load(shadowPath);
                lightBases = light;
            }
            var values = layer < 2 ? lightBases : darkBases;
            // Native ShuffleArray 0x260e574. Reverse Fisher-Yates, Next(count).
            for (int count = values.Length; count > 1; count--)
            {
                int selected = random.Next(0, count);
                var last = values[count - 1];
                values[count - 1] = values[selected];
                values[selected] = last;
            }
            return values[0];
        }
        public Sprite GetPatch(int layer) => patches[layer];
        public Sprite Shadow => shadow;
        public float GetShadowAlpha(int layer) => shadowAlphas[layer - 1];

        private static Sprite[] Load(string[] paths)
        {
            var values = new Sprite[paths.Length];
            for (int i = 0; i < paths.Length; i++) values[i] = Load(paths[i]);
            return values;
        }

        private static Sprite Load(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Original grass sprite is missing: " + path);
            return sprite;
        }
    }
}
