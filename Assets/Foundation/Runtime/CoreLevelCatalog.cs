using System;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // The original 100 authored boards repeat while player progress keeps increasing.
    // Only resource selection wraps; session, UI and server levels stay unchanged.
    [CreateAssetMenu(menuName = "Whitebox/Core level catalog")]
    public sealed class CoreLevelCatalog : ScriptableObject
    {
        public const int SupportedLevelCount = 100;
        [Serializable]
        public sealed class Entry
        {
            public int number;
            public string originalName, resourcePath;
        }
        [SerializeField] private Entry[] levels;
        public int Count => levels == null ? 0 : levels.Length;
        public static int GetResourceLevelNumber(int number)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number), "Level numbers must be positive.");
            return (number - 1) % SupportedLevelCount + 1;
        }
        public Entry GetEntry(int number)
        {
            int resourceNumber = GetResourceLevelNumber(number);
            if (Count != SupportedLevelCount)
                throw new InvalidOperationException("The core catalog must contain the original100 levels.");
            var entry = levels[resourceNumber - 1];
            if (entry == null || entry.number != resourceNumber || string.IsNullOrEmpty(entry.originalName) || string.IsNullOrEmpty(entry.resourcePath))
                throw new InvalidOperationException("Invalid core level catalog entry: " + resourceNumber);
            return entry;
        }
        public FTiledLevel Load(int number)
        {
            var entry = GetEntry(number);
            var level = OriginalLevelReader.Load(entry.resourcePath);
            if (level.Name != entry.originalName)
                throw new InvalidOperationException("Original level identity does not match its catalog entry.");
            return level;
        }
    }
}
