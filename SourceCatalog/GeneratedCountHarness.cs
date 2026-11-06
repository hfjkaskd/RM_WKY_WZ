using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation { public interface IOriginalGameStartItemCreator { void ClearCreatedItemsCountExceptPredefined(); void UpdateCreatedItemsCountExceptPredefined(); } }
public static class GeneratedCountHarness
{
    public static int[] Head;
    public static readonly List<string> Trace = new List<string>();
    private static int calls, prerequisites, currentLoader;
    private static Dictionary<int, int> created, predefined;
    private static Set[] initialSets;
    private static IOriginalCreatedCountSet[] sets;
    private static Loader[] loaders;
    public static void Step(string label)
    {
        Trace.Add(label); calls++;
        if (calls == Head[1]) throw new InvalidOperationException();
        if (calls != Head[2]) return;
        switch (Head[3])
        {
            case 1: currentLoader = 1; break;
            case 2: sets = null; break;
            case 3: sets = new IOriginalCreatedCountSet[0]; break;
            case 4: initialSets[0].Value.Ids.Add(25); initialSets[0].Value.Available.Add(true); break;
            case 5: initialSets[0].Value = new Elements(2, new[] { 25, 1 }); break;
            case 6: created.Clear(); break;
        }
    }
    private sealed class Loader : IOriginalSavedCreatedItemCounts
    {
        public readonly int Id; public int[] Pairs;
        public Loader(int id, int[] pairs) { Id = id; Pairs = pairs; }
        public int Count { get { Step("length," + Id); return Pairs.Length / 2; } }
        public OriginalCreatedItemCountEntry Get(int i) { Step("pair," + Id + "," + i); return new OriginalCreatedItemCountEntry(Pairs[i * 2], Pairs[i * 2 + 1]); }
    }
    private sealed class Elements : IOriginalCreatedCountElements
    {
        public readonly int Id; public readonly List<int> Ids = new List<int>(); public readonly List<bool> Available = new List<bool>();
        public Elements(int id, int[] ids) { Id = id; foreach (int value in ids) { Ids.Add(value); Available.Add(true); } }
        public int Count => Ids.Count;
        public int GetTiledId(int index) { Step("element," + Id + "," + index); return Ids[index]; }
        public void Disable(int index) { Step("element," + Id + "," + index); int unused = Ids[index]; Available[index] = false; }
        public bool GetAvailability(int index) { Step("element," + Id + "," + index); return Available[index]; }
    }
    private sealed class Set : IOriginalCreatedCountSet
    {
        public Elements Value; public bool Available = true; public IOriginalCreatedCountElements Elements => Value;
        public void UpdateFillAvailability()
        {
            int i = 0;
            while (true)
            {
                var list = Value ?? throw new NullReferenceException();
                if (i >= list.Count) { Available = false; return; }
                if (list.GetAvailability(i)) return;
                i++;
            }
        }
    }
    private sealed class Context : IOriginalCreatedItemCountContext
    {
        public void ClearPrerequisiteItems() { if (Head[0] == 1) throw new NullReferenceException(); Step("clear"); prerequisites = 0; }
        public Dictionary<int, int> GetPredefinedInitialCounts() { if (Head[0] == 3) throw new NullReferenceException(); return predefined; }
        public IOriginalSavedCreatedItemCounts GetCurrentRestoreLoader() { Step("restore"); return Head[0] == 4 ? null : loaders[currentLoader]; }
        public IOriginalCreatedCountSet[] GetSets() => sets;
        public bool IsFillingObstacle(int type) { Step("obstacle," + type); return type == 7 || type == 13 || type == 8; }
        public int GetLimitCount(int goal) { if (Head[0] == 1) throw new NullReferenceException(); Step("limit," + goal); return Head[4]; }
        public int GetGoalTargetCount(int goal) { if (Head[0] == 1) throw new NullReferenceException(); Step("target," + goal); return Head[5]; }
        public void CalculateRandomFactors() { Step("calculate"); }
    }
    private static Dictionary<int, int> Dict(string value)
    {
        var result = new Dictionary<int, int>(); if (value.Length == 0) return result;
        var data = Array.ConvertAll(value.Split(','), int.Parse); for (int i = 0; i < data.Length; i += 2) result.Add(data[i], data[i + 1]); return result;
    }
    private static string State()
    {
        var result = new List<string>(); foreach (var pair in created) result.Add(pair.Key + "," + pair.Value);
        var flags = new List<string>(); foreach (var set in initialSets) { string mask = ""; if (set.Value != null) foreach (bool available in set.Value.Available) mask += available ? "1" : "0"; flags.Add((set.Value == null ? -1 : set.Value.Id) + "," + mask + "," + (set.Available ? 1 : 0)); }
        return prerequisites + "#" + string.Join("/", result) + "#" + string.Join("/", flags);
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var p = line.Split('|'); Head = Array.ConvertAll(p[0].Split(','), int.Parse); created = Dict(p[1]); predefined = Dict(p[2]);
            loaders = new[] { new Loader(0, p[3].Length == 0 ? new int[0] : Array.ConvertAll(p[3].Split(','), int.Parse)), new Loader(1, new[] { 7, 3, 13, int.MinValue, 8, 0, 7, int.MaxValue }) };
            initialSets = new[] { new Set { Value = new Elements(0, new[] { 25, 1, 25, 41 }) }, new Set { Value = new Elements(1, new[] { 13, 25 }) } }; sets = initialSets;
            if (Head[0] == 5) sets = null; else if (Head[0] == 6) sets = new IOriginalCreatedCountSet[] { null }; else if (Head[0] == 7) initialSets[0].Value = null;
            calls = 0; prerequisites = 9; currentLoader = 0; Trace.Clear();
            var owner = new OriginalGeneratedItemCountState(Head[0] == 2 ? null : created, new Context()); bool failed = false;
            try { foreach (char command in p[4]) { if (command == 'C') owner.ClearCreatedItemsCountExceptPredefined(); else owner.UpdateCreatedItemsCountExceptPredefined(); } } catch (Exception) { failed = true; }
            Console.WriteLine((failed ? 1 : 0) + "#" + State() + "#" + string.Join(";", Trace));
        }
    }
}
