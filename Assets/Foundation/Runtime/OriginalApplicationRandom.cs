using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Application RandomManager, distinct from seeded LevelRandomManager.
    public sealed class OriginalApplicationRandom : IOriginalLevelSeedRandom
    {
        internal static OriginalApplicationRandom Shared { get; } = new OriginalApplicationRandom();

        // InitContextAfterSplash 0x38fc568 adds RandomManager to ApplicationContext.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BindShared() => Shared.Bind();

        public void Bind() { UnityEngine.Random.InitState(DateTime.Now.Millisecond); } // 0x2d70a48.
        public int Next(int max) => UnityEngine.Random.Range(0, max); // 0x2d70ab8.
        public int Next(int min, int max) => UnityEngine.Random.Range(min, max); // 0x2d70ac4.
        public bool NextBool() => UnityEngine.Random.Range(0, 2) == 0; // 0x2d70ad4.
        public float Next(float min, float max) => UnityEngine.Random.Range(min, max); // 0x2d70af8.
    }
}
