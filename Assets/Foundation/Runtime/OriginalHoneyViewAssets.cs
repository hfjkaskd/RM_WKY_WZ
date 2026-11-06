using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalHoneyViewAssets : ScriptableObject
    {
        [SerializeField] private string innerFillPath, connectorPath, firstCornerPath;
        [SerializeField] private string[] cornerPaths, sidePaths, cornerSidePaths, innerCornerPaths;
        private Sprite[] corners, sides, cornerSides, innerCorners;
        private Sprite innerFill, firstCorner;
        public Sprite Connector { get; private set; }

        public void LoadSprites()
        {
            if (innerFill != null) return;
            corners = Load(cornerPaths);
            sides = Load(sidePaths);
            cornerSides = Load(cornerSidePaths);
            innerCorners = Load(innerCornerPaths);
            Connector = Load(connectorPath);
            firstCorner = Load(firstCornerPath);
            innerFill = Load(innerFillPath);
        }

        public Sprite GetSprite(int type, bool first, IOriginalLevelSeedRandom random)
        {
            return type switch
            {
                OriginalHoneyQuadrant.Side => ShuffleAndTakeFirst(sides, random),
                OriginalHoneyQuadrant.Inside => innerFill,
                OriginalHoneyQuadrant.Corner => first ? firstCorner : ShuffleAndTakeFirst(corners, random),
                OriginalHoneyQuadrant.CornerSide => ShuffleAndTakeFirst(cornerSides, random),
                OriginalHoneyQuadrant.InsideCorner => ShuffleAndTakeFirst(innerCorners, random),
                _ => null,
            };
        }

        private static Sprite ShuffleAndTakeFirst(Sprite[] values, IOriginalLevelSeedRandom random) // 0x260e574.
        {
            for (int count = values.Length; count > 1; count--)
            {
                int selected = random.Next(0, count);
                (values[count - 1], values[selected]) = (values[selected], values[count - 1]);
            }
            return values[0];
        }

        private static Sprite[] Load(string[] paths)
        {
            var result = new Sprite[paths.Length];
            for (int i = 0; i < paths.Length; i++) result[i] = Load(paths[i]);
            return result;
        }

        private static Sprite Load(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Original honey sprite is missing: " + path);
            return sprite;
        }
    }
}
