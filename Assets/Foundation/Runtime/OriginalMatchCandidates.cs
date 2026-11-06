using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public struct OriginalTempMatch
    {
        public readonly int Direction;
        public readonly OriginalGridCell First, Middle, Last, Fourth;
        public OriginalTempMatch(int direction, OriginalGridCell first, OriginalGridCell middle,
            OriginalGridCell last, OriginalGridCell fourth = null)
        {
            Direction = direction;
            First = first;
            Middle = middle;
            Last = last;
            Fourth = fourth;
        }
    }

    public struct OriginalPossibleMatch
    {
        public readonly int PatternType, MatchType, FirstTempIndex, SecondTempIndex;
        public readonly OriginalGridCell FirstIntersect, SecondIntersect;
        public OriginalPossibleMatch(int pattern, int color, int first, int second,
            OriginalGridCell intersection, OriginalGridCell secondIntersection)
        {
            PatternType = pattern;
            MatchType = color;
            FirstTempIndex = first;
            SecondTempIndex = second;
            FirstIntersect = intersection;
            SecondIntersect = secondIntersection;
        }
    }

    // Candidate-generation stage of CreateMatchesFromThrees 0x12e1bc8.
    // Final visited-cell arbitration and extensions are separate stages.
    public static class OriginalMatchCandidates
    {
        public static void Build(IReadOnlyList<OriginalTempMatch> threes, List<OriginalPossibleMatch> output)
        {
            if (threes == null || output == null) throw new ArgumentNullException();
            output.Clear();
            for (int i = 0; i < threes.Count; i++)
            {
                var a = threes[i];
                int color = Color(a.First);
                Add(output, 1, color, i, -1, null, null);
                if (a.Direction == 0)
                {
                    Add(output, 2, color, i, -1, null, null);
                    continue;
                }
                for (int j = i + 1; j < threes.Count; j++)
                {
                    var b = threes[j];
                    if (b.Direction == 0) continue;
                    if (a.Direction == b.Direction)
                    {
                        if (a.First == b.Last) Add(output, 7, color, i, j, a.First, null);
                        else if (a.Last == b.First) Add(output, 7, color, i, j, a.Last, null);
                        else if (a.First == b.Middle && a.Middle == b.Last)
                            Add(output, a.Direction == 1 ? 4 : 3, color, i, j, a.First, a.Middle);
                        else if (a.Middle == b.First && a.Last == b.Middle)
                            Add(output, a.Direction == 1 ? 4 : 3, color, i, j, a.Middle, a.Last);
                    }
                    else
                    {
                        if (a.First == b.First || a.First == b.Last)
                            Add(output, 6, color, i, j, a.First, null);
                        else if (a.Last == b.Last || a.Last == b.First)
                            Add(output, 6, color, i, j, a.Last, null);
                        else if (a.Middle == b.First || a.Middle == b.Last)
                            Add(output, 5, color, i, j, a.Middle, null);
                        else if (a.First == b.Middle || a.Last == b.Middle || a.Middle == b.Middle)
                            Add(output, 5, color, i, j, b.Middle, null);
                    }
                }
            }
        }

        // Native 0x12e2764 swaps adjacent candidates only for strictly lower
        // pattern type, preserving ties. Reused lists avoid per-search objects.
        public static void Sort(List<OriginalPossibleMatch> output)
        {
            for (int i = 0; i < output.Count; i++)
            for (int j = 0; j < output.Count - 1; j++)
                if (output[j].PatternType < output[j + 1].PatternType)
                {
                    var value = output[j];
                    output[j] = output[j + 1];
                    output[j + 1] = value;
                }
        }

        private static int Color(OriginalGridCell cell)
        {
            if (cell == null || !(cell.Mediation.CurrentItem is OriginalMatchBoardItem item))
                throw new InvalidOperationException("Temporary match has no match item.");
            return item.MatchType;
        }

        private static void Add(List<OriginalPossibleMatch> output, int pattern, int color,
            int first, int second, OriginalGridCell intersection, OriginalGridCell secondIntersection)
        {
            output.Add(new OriginalPossibleMatch(pattern, color, first, second, intersection, secondIntersection));
        }
    }
}
