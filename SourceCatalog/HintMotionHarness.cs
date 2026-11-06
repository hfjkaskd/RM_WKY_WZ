// Typed transport for compiling the complete production manager without Unity.
// Search, eligibility, resource and gameplay ports are not exercised here.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Whitebox.Foundation;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x * b, a.y * b, a.z * b);
        public static bool operator ==(Vector3 a, Vector3 b)
        {
            var d = a - b;
            return d.x * d.x + d.y * d.y + d.z * d.z < 9.99999944E-11f;
        }
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public override bool Equals(object obj) => obj is Vector3 other && this == other;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return a + (b - a) * t;
        }
    }
    public static class Mathf
    {
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }
    public static class Time { public static float time; }
    public sealed class Transform
    {
        public Vector3 position;
        public void SetParent(Transform parent, bool world) { }
    }
    public sealed class GameObject
    {
        public bool activeInHierarchy = true;
        public T GetComponent<T>() where T : class, new() => new T();
    }
    public static class Resources
    {
        public static T Load<T>(string path) where T : class, new() => new T();
    }
}

namespace Whitebox.Foundation
{
    public sealed class OriginalRootTransform
    {
        private Vector3 position, scale = Vector3.one;
        public Vector3 GetPosition() => position;
        public Vector3 GetScale() => scale;
        public void SetPosition(Vector3 value) { position = value; }
        public void SetScale(Vector3 value) { scale = value; }
    }
    public struct OriginalSwapSorting { public int Layer, Order; }
    public interface IOriginalItemFallSortingSurface
    {
        void UpdateSorting(OriginalSwapSorting sorting);
        OriginalSwapSorting GetSorting();
    }
    public class OriginalItemSwapView : IOriginalItemFallSortingSurface
    {
        public OriginalRootTransform HintRoot = new OriginalRootTransform();
        public GameObject gameObject = new GameObject();
        public int PoolUse = 1;
        public bool IsSwapping;
        private OriginalSwapSorting sorting;
        public void UpdateSorting(OriginalSwapSorting value)
        {
            if (sorting.Order == 7010 && value.Order != 7010) FixtureTrace.Events.Add("restore");
            sorting = value;
        }
        public OriginalSwapSorting GetSorting() => sorting;
    }
    public sealed class OriginalMatchItemSpriteView : OriginalItemSwapView
    {
        public float Brightness, Maximum;
        public void SetHighlightRatio(float ratio) { Brightness = ratio; }
        public float GetHighlightRatio() => Brightness;
        public float GetMaxHighlightRatio() => Maximum;
    }
    public static class OriginalItemSortingRules
    {
        public static OriginalSwapSorting Hint(int layer) => new OriginalSwapSorting { Layer = layer, Order = 7010 };
        public static OriginalSwapSorting AnimatedItem(int layer, int y) => new OriginalSwapSorting { Layer = layer, Order = 7000 - 200 * y };
    }
    public sealed class HintCurrentCell { public OriginalGridCell Owner; }
    public sealed class HintMediator
    {
        public HintCurrentCell CurrentCell;
    }
    public class OriginalBoardItem
    {
        public OriginalItemSwapView SwapView;
        public HintMediator Mediator = new HintMediator();
    }
    public class OriginalMatchBoardItem : OriginalBoardItem { public int MatchType = 1; }
    public class OriginalSpecialBoardItem : OriginalBoardItem
    {
        public IOriginalItemFallSortingSurface FallView => SwapView;
    }
    public sealed class OriginalTntBoardItem : OriginalSpecialBoardItem { }
    public sealed class OriginalRocketBoardItem : OriginalSpecialBoardItem { public int Orientation; }
    public sealed class HintMediation { public OriginalBoardItem CurrentItem; }
    public sealed class HintNeighbors { public OriginalGridCell Get(int direction) => null; }
    public sealed class OriginalGridCell
    {
        public int X, Y;
        public HintMediation Mediation = new HintMediation();
        public HintNeighbors Neighbors = new HintNeighbors();
        public Vector3 GetViewPosition() => new Vector3(X, Y, 0);
    }
    public sealed class OriginalMatchCells
    {
        private readonly List<OriginalGridCell> cells = new List<OriginalGridCell>();
        public int Count => cells.Count;
        public void Add(OriginalGridCell cell) { cells.Add(cell); }
        public OriginalGridCell Get(int i) => cells[i];
    }
    public sealed class HintSnapshot { public OriginalMatchCells Cells; }
    public sealed class OriginalResolvedMatchGroup
    {
        public int PatternType = 2;
        public OriginalMatchCells Cells = new OriginalMatchCells();
        public HintSnapshot CaptureSnapshot() => new HintSnapshot { Cells = Cells };
        public bool ContainsCell(OriginalGridCell cell) => throw new InvalidOperationException("Search is not a motion port.");
    }
    public sealed class HintClock { public int FrameCount; }
    public sealed class HintGrid { public int Width = 9, Height = 8; }
    public sealed class HintOperations
    {
        public HintOperations Container => this;
        public bool HasAny() => false;
        public bool HasMask(int mask) => false;
    }
    public sealed class HintState { public HintOperations Operations = new HintOperations(); }
    public sealed class HintPool
    {
        private int spawned;
        public bool HasPool(int id) => true;
        public void CreatePool(OriginalHintParticles prefab, int capacity) { }
        public T Spawn<T>(int id, bool active) where T : OriginalHintParticles, new() => new T { Id = spawned++ };
        public void Recycle(OriginalHintParticles value)
        {
            if (value.Id == 0) FixtureTrace.Events.Add("recycle");
        }
    }
    public sealed class HintPresentation
    {
        public float LevelDeltaTime;
        public HintPool Pool = new HintPool();
    }
    public sealed class OriginalInitialBoardView
    {
        public object MatchBlocking, Layout, SwapAvailability;
        public HintGrid Grid = new HintGrid();
        public HintClock Clock = new HintClock();
        public HintState State = new HintState();
        public HintPresentation Presentation = new HintPresentation();
        public HintPossibleMoves PossibleMoves = new HintPossibleMoves();
        public HintMatches Matches = new HintMatches();
        public Transform BoardRoot = new Transform();
    }
    public class OriginalHintParticles
    {
        public const int PoolId = 46;
        public int Id;
        public Transform transform = new Transform();
        public void Show(int adjacent) { }
        public void Hide() { if (Id == 0) FixtureTrace.Events.Add("hide"); }
    }
    public sealed class OriginalGeneralLevelRandom { public int Next(int lo, int hi) => throw new InvalidOperationException("Search random not exercised."); }
    public sealed class CoreLevelProgress
    {
        public int PowerHintTapCount;
        public void RecordPowerHintTap() { PowerHintTapCount++; }
    }
    public sealed class HintSession { public int Number = 2, ShownHintCount, UsedHintCount; }
    public sealed class HintTargets
    {
        public int CalculateScore(OriginalCellPoint p, OriginalExplodeData d, out int other) => throw new InvalidOperationException("Score not exercised.");
        public int CalculateScore(bool column, int p, OriginalExplodeData d, out int other) => throw new InvalidOperationException("Score not exercised.");
    }
    public sealed class OriginalPlayableBoard
    {
        public OriginalGeneralLevelRandom GeneralRandom = new OriginalGeneralLevelRandom();
        public HintSession Session = new HintSession();
        public int ItemSortingLayer = 1;
        public HintTargets AreaTargets = new HintTargets(), LineTargets = new HintTargets();
        public event Action<OriginalGridCell, OriginalGridCell, bool> OnSwap;
        public event Action<OriginalGridCell> OnTap;
        public event Action OnTntOrRocketTapped;
        public bool CanUserMakeMove() => true;
        public static int GetSpecialComboType(OriginalSpecialBoardItem a, OriginalSpecialBoardItem b) => throw new InvalidOperationException("Search combo not exercised.");
    }
    public sealed class OriginalBoardPossibleMoveServices
    {
        public OriginalBoardPossibleMoveServices(object blocking) { }
        public bool CanEnterPossibleMatch(OriginalGridCell cell) => false;
        public bool CanEnterPossibleMatchAcceptingChain(OriginalGridCell cell) => false;
    }
    public sealed class OriginalBoardGridAccess { public OriginalBoardGridAccess(HintGrid g, object l) { } }
    public sealed class OriginalBottomLeftHorizontalStrategy { public OriginalBottomLeftHorizontalStrategy(OriginalBoardGridAccess a) { } }
    public sealed class OriginalGridIterator
    {
        public OriginalGridIterator(OriginalBottomLeftHorizontalStrategy a) { }
        public void Reset() { }
        public OriginalGridCell MoveNext() => throw new InvalidOperationException("Search not exercised.");
    }
    public sealed class HintPossibleResult
    {
        public OriginalGridCell Top, Right, Bottom, Left, RequiredCell;
        public bool IsValid() => false;
    }
    public sealed class HintPossibleMoves
    {
        public HintPossibleResult CheckNeighborForPossibleMatches(OriginalGridCell c, int type, int dir) => throw new InvalidOperationException("Search not exercised.");
    }
    public sealed class HintMatches
    {
        public List<OriginalResolvedMatchGroup> FindMatchesInArea(int x, int y, int radius) => throw new InvalidOperationException("Search not exercised.");
    }
    public static class OriginalCellNeighbors { public static readonly int[] TouchingNeighborTypes = { 0, 1, 2, 3 }; }
    public static class OriginalCellTouchRules
    {
        public static bool CanSwapFrom(OriginalGridCell cell, object availability) => false;
        public static bool CanSwapTo(OriginalGridCell cell, object availability) => false;
    }
    public static class OriginalCellTap { public static bool CanTapCurrentItem(OriginalGridCell c, out int a) { a = 0; return false; } }
    public static class OriginalSwapDecision { public static void SetAllItems(OriginalGridCell c, OriginalBoardItem item) => throw new InvalidOperationException("Search mutation not exercised."); }
    public static class OriginalGameOperationContainer { public const int FallAndSwapMask = 6; }
    public struct OriginalCellPoint { public OriginalCellPoint(int x, int y) { } }
    public struct OriginalExplodeData { public OriginalExplodeData(int a, int b, OriginalCellPoint c, int d, bool e) { } }
}

internal static class FixtureTrace
{
    public static readonly List<string> Events = new List<string>();
    public static readonly List<OriginalMatchItemSpriteView> Views = new List<OriginalMatchItemSpriteView>();
    public static readonly OriginalRootTransform SpareRoot = new OriginalRootTransform();
    public static string Number(float v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static string Vector(Vector3 v) => "[" + Number(v.x) + "," + Number(v.y) + "," + Number(v.z) + "]";
    public static string State(int phase, int pulses, int bursts, float elapsed, float duration, float next, string kind)
    {
        var first = Views[0].HintRoot;
        var second = kind == "combo" ? Views[1].HintRoot : SpareRoot;
        var text = new StringBuilder("{\"phase\":").Append(phase).Append(",\"pulses\":").Append(pulses)
            .Append(",\"bursts\":").Append(bursts).Append(",\"elapsed\":").Append(Number(elapsed))
            .Append(",\"duration\":").Append(Number(duration)).Append(",\"next_at\":").Append(Number(next))
            .Append(",\"positions\":[").Append(Vector(first.GetPosition())).Append(',').Append(Vector(second.GetPosition()))
            .Append("],\"scales\":[").Append(Vector(first.GetScale())).Append(',').Append(Vector(second.GetScale()))
            .Append("],\"brightness\":[");
        if (kind == "match")
            for (int i = 0; i < Views.Count; i++) { if (i != 0) text.Append(','); text.Append(Number(Views[i].Brightness)); }
        text.Append("],\"events\":[");
        for (int i = 0; i < Events.Count; i++) { if (i != 0) text.Append(','); text.Append('"').Append(Events[i]).Append('"'); }
        return text.Append("]}").ToString();
    }
}

internal static class Program
{
    public static float Parse(string v) => float.Parse(v, CultureInfo.InvariantCulture);
    public static void Main()
    {
        OriginalMatchHintManager current = null;
        string kind = null, line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('|');
            if (parts[0] == "BEGIN")
            {
                kind = parts[2];
                current = OriginalMatchHintManager.FixtureCreate(kind, int.Parse(parts[3]),
                    int.Parse(parts[4]), int.Parse(parts[5]),
                    Array.ConvertAll(parts[6].Split(','), Parse), Array.ConvertAll(parts[7].Split(','), Parse));
            }
            else
            {
                FixtureTrace.Events.Clear();
                Time.time = Parse(parts[2]);
                current.FixtureStep(Parse(parts[1]), parts[3] == "1");
                Console.WriteLine(current.FixtureState(kind));
            }
        }
    }
}
