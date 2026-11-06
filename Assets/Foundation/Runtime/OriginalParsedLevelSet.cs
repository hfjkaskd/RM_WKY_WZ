using System;
using System.Collections.Generic;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // Configuration stage of ParseSets 0x1c22304 and LevelSet ctor 0x190f048.
    // Runtime fill availability, board counts and weighted generation are separate stages.
    public sealed class OriginalParsedLevelSet
    {
        public string Name { get; }
        public bool CanFall { get; }
        public int CreateRatio { get; }
        public int[] ElementIds { get; }
        public int[] ElementCreateRatios { get; }
        public int[] TargetColumns { get; }
        public int[] TargetFills { get; }
        public int[] TargetFillRatios { get; }
        public Dictionary<int, int> MaxItemCounts { get; }

        public OriginalParsedLevelSet(FTiledSet source, int mustHaveColor, int replacedColor)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Name = source.Name;
            CanFall = source.CanFall;
            CreateRatio = Math.Max(1, source.CreateRatio);
            MaxItemCounts = new Dictionary<int, int>();
            if (source.MaxItemCounts != null)
                for (int i = 0; i < source.MaxItemCounts.Length; i++)
                {
                    var limit = source.MaxItemCounts[i];
                    if (limit != null) MaxItemCounts.Add(limit.Id, limit.MaxCount);
                }
            if (source.BirdMax > 0 && !MaxItemCounts.ContainsKey(16))
                MaxItemCounts.Add(16, source.BirdMax);
            if (source.FrogMax > 0 && !MaxItemCounts.ContainsKey(31))
                MaxItemCounts.Add(31, source.FrogMax);
            int count = source.Elements == null ? 0 : source.Elements.Length;
            ElementIds = new int[count];
            ElementCreateRatios = new int[count];
            for (int i = 0; i < count; i++)
            {
                var element = source.Elements[i];
                int id = element.Id;
                if (replacedColor != 0 && mustHaveColor != 0 &&
                    OriginalLevelRules.ToMatchType(id) == replacedColor)
                    id = OriginalLevelRules.ToTiledId(mustHaveColor);
                ElementIds[i] = id;
                ElementCreateRatios[i] = Math.Max(1, element.CreateRatio);
            }
            TargetColumns = Copy(source.TargetColumns);
            for (int i = 0; i < TargetColumns.Length; i++)
                TargetColumns[i] = unchecked(TargetColumns[i] - 1);
            TargetFills = Copy(source.TargetFills);
            TargetFillRatios = Copy(source.TargetFillRatios);
        }

        private static int[] Copy(int[] source) => source == null ? new int[0] : (int[])source.Clone();

        public static OriginalParsedLevelSet[] ParseSets(FTiledLevel level, int mustHaveColor,
            int replacedColor)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            int count = level.Sets == null ? 0 : level.Sets.Length;
            var sets = new OriginalParsedLevelSet[count];
            for (int i = 0; i < count; i++)
                sets[i] = new OriginalParsedLevelSet(level.Sets[i], mustHaveColor, replacedColor);
            return sets;
        }
    }
}
