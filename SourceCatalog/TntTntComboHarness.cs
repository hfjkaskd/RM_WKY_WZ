using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public static class ComboTrace { public static readonly List<string> Events = new List<string>(); }
    public sealed class OriginalGridCell
    {
        public string Name; public string Position;
        public void FreezeFall() { ComboTrace.Events.Add("freeze:" + Name); }
        public void UnFreezeFall() { ComboTrace.Events.Add("unfreeze:" + Name); }
        public string GetViewPosition() => Position;
    }
    public sealed class OriginalTntBoardItem
    {
        public string Name; public int Radius = 2; public OriginalGridCell CurrentCell;
        public void DisableExplosion() { Radius = 0; }
        public void IncreaseExplosion() { Radius = 4; }
        public void SetSpecialItemActive(bool value) { ComboTrace.Events.Add("active:" + Name + ":" + (value ? 1 : 0)); }
        public void Explode(OriginalExplodeData data) { ComboTrace.Events.Add("explode:" + Name + ":" + data.Id + ":" + data.Point.X + ":" + data.Point.Y + ":" + data.Trigger + ":" + data.ComboId); }
    }
    public sealed class OriginalGameStateManager
    {
        public void StartSpecialOperation() { ComboTrace.Events.Add("start"); }
        public void FinishSpecialOperation() { ComboTrace.Events.Add("finish"); }
    }
    public sealed class OriginalTntTntComboView
    {
        public Action Completion;
        public void Init(OriginalPool pool, string position, Action completion)
        { ComboTrace.Events.Add("init:" + position); Completion = completion; }
    }
    public sealed class OriginalPool
    {
        public readonly OriginalTntTntComboView View = new OriginalTntTntComboView();
        public T Spawn<T>(int id, bool active) where T : class
        { ComboTrace.Events.Add("spawn:" + id + ":" + (active ? 1 : 0)); return View as T; }
    }
    public sealed class OriginalLevelPresentation { public readonly OriginalPool Pool = new OriginalPool(); }
}
public static class VerifyTntTntCombo
{
    public static void Main()
    {
        var presentation = new OriginalLevelPresentation(); var combo = new OriginalTntTntCombo(presentation, new OriginalGameStateManager());
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var a = Array.ConvertAll(line.Split(','), int.Parse);
            var to = new OriginalTntBoardItem { Name = "to", CurrentCell = new OriginalGridCell { Name = "to", Position = a[0] + ":" + a[1] } };
            var from = new OriginalTntBoardItem { Name = "from", CurrentCell = new OriginalGridCell { Name = "from", Position = a[2] + ":" + a[3] } };
            ComboTrace.Events.Clear();
            combo.Prepare(to, from);
            var data = new OriginalExplodeData(a[5], a[4], new OriginalCellPoint(a[6], a[7]), 0, false, false, a[8]);
            combo.Explode(data);
            ComboTrace.Events.Add("state:" + (combo.IsComboActive ? 1 : 0) + ":" + to.Radius + ":" + from.Radius);
            presentation.Pool.View.Completion();
            ComboTrace.Events.Add("state:" + (combo.IsComboActive ? 1 : 0) + ":" + to.Radius + ":" + from.Radius);
            Console.WriteLine(string.Join("|", ComboTrace.Events));
        }
    }
}
