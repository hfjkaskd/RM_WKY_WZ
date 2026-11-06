using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnityEngine
{
    public class Coroutine { }
    public sealed class WaitForSeconds { public float Seconds; public WaitForSeconds(float seconds) { Seconds = seconds; } }
}
namespace Whitebox.Foundation
{
    public class OriginalGridCell { }
    public interface IOriginalManualCoroutinePlayer
    { UnityEngine.Coroutine ManualStartCoroutine(IEnumerator routine, Action onKill = null); }

    internal sealed class KingUiStateHarness : IOriginalManualCoroutinePlayer
    {
        private readonly List<IEnumerator> waits = new List<IEnumerator>();
        private readonly List<int> idle = new List<int>();
        private OriginalKingUiState state;
        private OriginalMoveManager moves;
        private bool improved;
        private int happy;
        public UnityEngine.Coroutine ManualStartCoroutine(IEnumerator routine, Action onKill = null)
        {
            if (!routine.MoveNext() || ((UnityEngine.WaitForSeconds)routine.Current).Seconds != 10f)
                throw new Exception("Native 10-second wait required.");
            waits.Add(routine); return new UnityEngine.Coroutine();
        }
        private void StartIdle(bool setFirst)
        { idle.Add(setFirst ? 1 : 0); state.StopAnimationsCycle = false; }
        private void Bind(int left, bool first)
        {
            moves = new OriginalMoveManager(); moves.SetMaxMoves(left);
            if (!first) waits.Clear();
            state.Bind(moves, this, improved);
        }
        public static void Main(string[] args)
        {
            var owner = new KingUiStateHarness();
            using (var output = new StreamWriter(args[1]))
                foreach (string line in File.ReadAllLines(args[0]))
                {
                    string[] c = line.Split(' ');
                    switch (c[0])
                    {
                        case "R":
                            owner.improved = c[1] == "1"; owner.happy = 0; owner.waits.Clear(); owner.idle.Clear();
                            owner.state = new OriginalKingUiState(10f, owner.StartIdle);
                            owner.Bind(int.Parse(c[2]), true); break;
                        case "H":
                            if (owner.state.TryBeHappy(owner.improved))
                            { owner.state.StopAnimationsCycle = true; if (owner.improved) owner.happy++; }
                            break;
                        case "M":
                            owner.moves.IncreaseMove(int.Parse(c[1]) - owner.moves.LeftMoves, int.Parse(c[2])); break;
                        case "Q":
                            int index = int.Parse(c[1]); if (index < owner.waits.Count) owner.waits[index].MoveNext(); break;
                        case "S": owner.state.StopAnimationsCycle = true; break;
                        case "D": owner.state.BeginSad(); break;
                        case "B":
                            var previous = owner.moves;
                            owner.improved = c[1] == "1"; owner.Bind(int.Parse(c[2]), false);
                            previous.TriggerMoveChanged(3); // Retired level sources must not affect the rebound King.
                            break;
                    }
                    output.WriteLine(string.Join(",", new int[] { owner.improved ? 1 : 0,
                        owner.state.ShouldBeHappy ? 1 : 0, owner.state.MovesLeft,
                        owner.state.StopAnimationsCycle ? 1 : 0, owner.happy, owner.waits.Count,
                        owner.idle.Count, owner.idle.Count == 0 ? -1 : owner.idle[owner.idle.Count - 1] }));
                }
        }
    }
}
