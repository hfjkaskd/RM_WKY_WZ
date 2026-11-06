using System;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // Product scope, explicitly narrowed by the user to ordinary levels1–100.
    // Entries are authored assets; only the selected level payload is loaded.
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
        public Entry GetEntry(int number)
        {
            if (number < 1 || number > SupportedLevelCount)
                throw new ArgumentOutOfRangeException(nameof(number), "Only ordinary levels1–100 are in scope.");
            if (Count != SupportedLevelCount)
                throw new InvalidOperationException("The core catalog must contain the original100 levels.");
            var entry = levels[number - 1];
            if (entry == null || entry.number != number || string.IsNullOrEmpty(entry.originalName) || string.IsNullOrEmpty(entry.resourcePath))
                throw new InvalidOperationException("Invalid core level catalog entry: " + number);
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
