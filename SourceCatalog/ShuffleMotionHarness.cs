using System;
using System.Collections;
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
        public Vector3(float a, float b, float c) { x = a; y = b; z = c; }
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) =>
            new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    }
    public sealed class Transform { public Vector3 position; }
    public static class Mathf { public static float Min(float a, float b) => Math.Min(a, b); }
}

namespace Whitebox.Foundation
{
    internal interface IOriginalContextBehaviour { }
    internal sealed class TestClock { public float DeltaTime; public int FrameCount; }
    internal sealed class TestGrid { public int Width, Height = 1; }
    internal sealed class TestRectangle { public int Bottom; }
    internal sealed class TestLayout { public TestRectangle CurrentBoard = new TestRectangle(); }
    internal sealed class TestOperations
    {
        public TestOperations Container => this;
        public bool HasAny() => false;
        public bool HasMask(int mask) => false;
        public void OperationStart(int op) { }
        public void OperationReset(int op) { Trace.Events.Add("[\"reset\"," + op + "]"); }
    }
    internal static class OriginalGameOperationContainer { public const int FallAndSwapMask = 6; }
    internal sealed class TestState
    {
        public TestOperations Operations = new TestOperations();
        public bool ShouldTryToShiftCells() => false;
    }
    internal sealed class TestInitialShuffle { public bool OnlyLightballExists; public bool ShouldTryAgain() => false; }
    internal sealed class TestRandom { public int DifficultyNext(int lo, int hi) => lo; }
    internal sealed class TestAmounts { public bool IsThereItemInLevel(int id) => false; }
    internal sealed class TestPossible { public int CheckBoard(OriginalGridIterator iterator) => 2; }
    internal sealed class OriginalInitialBoardView
    {
        public TestClock Clock = new TestClock();
        public TestGrid Grid = new TestGrid();
        public TestLayout Layout = new TestLayout();
        public TestState State = new TestState();
        public TestInitialShuffle InitialShuffle = new TestInitialShuffle();
        public TestRandom DifficultyRandom = new TestRandom();
        public TestAmounts InitialItemAmounts = new TestAmounts();
        public TestPossible PossibleMoves = new TestPossible();
        public object MatchBlocking;
    }
    internal sealed class OriginalBoardPossibleMoveServices
    {
        public OriginalBoardPossibleMoveServices(object ignored) { }
        public bool CanEnterPossibleMatch(OriginalGridCell cell) => true;
    }
    internal sealed class OriginalBoardGridAccess { public OriginalBoardGridAccess(TestGrid a, TestLayout b) { } }
    internal sealed class OriginalBottomLeftHorizontalStrategy { public OriginalBottomLeftHorizontalStrategy(OriginalBoardGridAccess a) { } }
    internal sealed class OriginalGridIterator
    {
        public OriginalGridIterator(OriginalBottomLeftHorizontalStrategy a) { }
        public void Reset() { }
        public OriginalGridCell MoveNext() => null;
    }
    internal sealed class TestNeighbors { public OriginalGridCell Get(int direction) => null; }
    internal sealed class TestCurrentCell { public OriginalGridCell Owner; }
    internal sealed class TestMediator
    {
        public TestCurrentCell CurrentCell;
        public void ClearFromAllRegisteredCells() { }
    }
    internal class OriginalBoardItem { public TestMediator Mediator = new TestMediator(); }
    internal sealed class OriginalMatchBoardItem : OriginalBoardItem
    {
        public int MatchType = 1;
        public OriginalMatchItemSpriteView SwapView;
        public void SetShuffleMatchType(int kind) { MatchType = kind; }
    }
    internal sealed class TestMediation
    {
        public OriginalBoardItem CurrentItem;
        public void ClearAllItems() { }
        public void SetAllItems(OriginalBoardItem item) { CurrentItem = item; }
    }
    internal sealed class OriginalGridCell
    {
        public int X, Y;
        public bool IsNormalCell = true;
        public Vector3 Target;
        public TestMediation Mediation = new TestMediation();
        public TestNeighbors Neighbors = new TestNeighbors();
        public Vector3 GetViewPosition() => Target;
    }
    internal struct TestSorting { public int Layer, Order; }
    internal static class OriginalItemSortingRules
    {
        public static TestSorting AnimatedItem(int layer, int row) => new TestSorting { Layer = layer, Order = row };
    }
    internal sealed class OriginalMatchItemSpriteView
    {
        public int Id;
        public Transform transform = new Transform();
        public void SetShuffleSorting(bool changed) { Trace.Events.Add("[\"raise\"," + Id + "," + (changed ? "true" : "false") + "]"); }
        public void UpdateSortingForAnimation(TestSorting sort, bool flag) { Trace.Events.Add("[\"restore\"," + Id + "]"); }
    }
    internal sealed class TestMoves { public bool CanUserMakeMove() => true; }
    internal sealed class TestAuto { public bool IsThereAWaitingMatch; }
    internal sealed class TestSession { public int NoPossibleMoveColoredShuffleCount, NoPossibleMoveShuffleCount; }
    internal sealed class TestWinFail { public void OnNoPossibleMove() { } }
    internal sealed class TestWait { public float Duration; }
    internal sealed class OriginalPlayableBoard
    {
        public TestMoves Moves = new TestMoves();
        public TestAuto Auto = new TestAuto();
        public TestSession Session = new TestSession();
        public TestWinFail WinFail = new TestWinFail();
        public int ItemSortingLayer = 1;
        public void ManualStartCoroutine(IEnumerator routine) { }
        public object CreateGameplayWait(float duration)
        {
            Trace.Events.Add("[\"wait\"," + Trace.Number(duration) + "]");
            return new TestWait { Duration = duration };
        }
        public void SetShuffleOrientation(bool enabled) { Trace.Events.Add("[\"orientation\"," + (enabled ? "true" : "false") + "]"); }
        public OriginalMatchItemSpriteView ReplaceShuffleMatch(OriginalMatchBoardItem item, Vector3 position)
        {
            int id = item.SwapView.Id;
            Trace.Events.Add("[\"replace\"," + id + "," + Trace.Vector(position) + "]");
            item.SwapView = new OriginalMatchItemSpriteView { Id = id };
            item.SwapView.transform.position = position;
            return item.SwapView;
        }
    }
    internal static class Trace
    {
        public static readonly List<string> Events = new List<string>();
        public static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        public static string Vector(Vector3 value) => "[" + Number(value.x) + "," + Number(value.y) + "," + Number(value.z) + "]";
    }
}

internal static class Program
{
    private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#');
            int count = int.Parse(parts[1]);
            bool recolor = parts[2] == "1";
            var deltaParts = parts[3].Split(',');
            var deltas = new float[deltaParts.Length];
            for (int i = 0; i < deltas.Length; i++) deltas[i] = Parse(deltaParts[i]);
            var board = new OriginalInitialBoardView(); board.Grid.Width = count;
            var game = new OriginalPlayableBoard();
            var shuffle = new OriginalRuntimeShuffle(board, game);
            shuffle.count = count;
            shuffle.recolorOriginalIndex = recolor ? 0 : -1;
            var allCells = new OriginalGridCell[count];
            var positionParts = parts[4].Split(';');
            for (int i = 0; i < count; i++)
            {
                var p = positionParts[i].Split(',');
                var view = new OriginalMatchItemSpriteView { Id = i };
                view.transform.position = new Vector3(Parse(p[0]), Parse(p[1]), Parse(p[2]));
                var cell = new OriginalGridCell { X = i % 9, Y = i / 9,
                    Target = new Vector3(Parse(p[3]), Parse(p[4]), Parse(p[5])) };
                var item = new OriginalMatchBoardItem { SwapView = view };
                item.Mediator.CurrentCell = new TestCurrentCell { Owner = cell };
                cell.Mediation.CurrentItem = item;
                allCells[i] = shuffle.cells[i] = cell;
                shuffle.originalItems[i] = item;
            }
            var routine = shuffle.AnimateShuffle();
            var output = new StringBuilder("{\"name\":\"" + parts[0] + "\",\"states\":[");
            for (int step = 0; step < 400; step++)
            {
                board.Clock.DeltaTime = deltas[Math.Max(0, step - 1) % deltas.Length];
                Trace.Events.Clear();
                bool alive = routine.MoveNext();
                if (step != 0) output.Append(',');
                output.Append("{\"step\":").Append(step).Append(",\"alive\":").Append(alive ? "true" : "false")
                    .Append(",\"events\":[").Append(string.Join(",", Trace.Events)).Append("],\"positions\":[");
                for (int i = 0; i < count; i++)
                {
                    if (i != 0) output.Append(',');
                    output.Append(Trace.Vector(((OriginalMatchBoardItem)allCells[i].Mediation.CurrentItem).SwapView.transform.position));
                }
                output.Append("]}");
                if (!alive) break;
                if (step == 399) throw new InvalidOperationException("Unfinished test case.");
            }
            output.Append("]}");
            Console.WriteLine(output.ToString());
        }
    }
}
