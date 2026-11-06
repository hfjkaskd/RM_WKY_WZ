using System;
using System.Collections.Generic;
using Whitebox.Foundation;
static class CollectedSnapshotsHarness
{
    sealed class Inputs : IOriginalLevelCollectState, IOriginalMissionSuperBooster
    {
        public int Missing, ThrowAt, Calls; public bool Playing, Collect;
        public readonly List<string> Trace = new List<string>();
        public bool ShouldCollectFromLevel { get => Collect; set => Collect = value; }
        public void Record(string label) { Trace.Add(label); if (++Calls == ThrowAt) throw new InvalidOperationException(); }
        public IOriginalLevelCollectState Get(int id) { Record("get," + id); return Missing != 0 ? null : this; }
        public IOriginalMissionSuperBooster GetBooster() { Record("get,29"); return Missing != 0 ? null : this; }
        public bool IsPlayingWithSuperBooster() { Record("playing"); return Playing; }
    }
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var p = line.Split('#'); var h = Array.ConvertAll(p[0].Split(','), int.Parse);
            int kind = h[0]; var f = new Inputs { Missing = h[1], ThrowAt = h[2], Playing = h[3] != 0 };
            OriginalCollectedLevelData data = null;
            switch (kind)
            {
                case 0: data = new OriginalWeeklyPassLevelData(() => f.Get(65)); break;
                case 1: data = new OriginalMagicCauldronLevelData(() => f.Get(38)); break;
                case 2: data = new OriginalDukesFortuneLevelData(() => f.Get(46)); break;
                case 3: data = new OriginalOceanOdysseyLevelData(() => f.Get(59)); break;
                case 4: data = new OriginalPuzzleBreakLevelData(() => f.Get(64)); break;
                case 5: data = new OriginalMergeSmithLevelData(() => f.Get(73)); break;
                case 6: data = new OriginalShortExpeditionEventLevelData(() => f.Get(80)); break;
            }
            var mission = new OriginalMissionEventsStartData(f.GetBooster); var invite = new OriginalInviteFriendsStartData();
            long saved = 0; var output = new List<string>();
            foreach (var command in p[1].Split(';'))
            {
                var v = command.Split(','); string mode = v[0], result = "-"; f.Calls = 0; f.Trace.Clear();
                int n = v.Length > 1 && mode != "l" ? int.Parse(v[1]) : 0;
                try
                {
                    if (kind < 7)
                    {
                        switch (mode)
                        {
                            case "d": data.ShouldConsume = n != 0; data.Before = int.Parse(v[2]); data.After = int.Parse(v[3]); break;
                            case "s": data.SetShouldCollect(n != 0); break;
                            case "i": data.Init(n); break;
                            case "f": data.FullReset(); break;
                            case "a": data.AddExtra(n); break;
                            case "c": data.Consume(); break;
                            case "g": saved = data.GetSaveData(); result = saved.ToString(); break;
                            case "l": data.LoadSaveData(long.Parse(v[1])); break;
                        }
                    }
                    else if (kind == 7)
                    {
                        switch (mode)
                        {
                            case "i": mission.Init(n); break;
                            case "s": mission.ShouldConsume = n != 0; break;
                            case "a": mission.AddExtraForItem(n, int.Parse(v[2])); break;
                            case "t": mission.AddExtraForTiledId(n, int.Parse(v[2])); break;
                            case "o": mission.AddExtraForSlotSelectionOrder(n, int.Parse(v[2])); break;
                            case "m": mission.MultiplyValues(n); break;
                            case "c": mission.Consume(); break;
                            case "g": saved = mission.GetSaveData(); result = saved.ToString(); break;
                            case "l": mission.LoadSaveData(long.Parse(v[1])); break;
                        }
                    }
                    else if (mode == "i") invite.Init(); else if (mode == "s") invite.SetEgoPrice(n); else if (mode == "c") invite.Consume();
                }
                catch { result = "error"; }
                string state = kind < 7 ? string.Join(",", new object[] { data.ShouldConsume ? 1 : 0, data.Before, data.After, data.ShouldCollectFromLevel ? 1 : 0, f.Collect ? 1 : 0 })
                    : kind == 7 ? string.Join(",", new object[] { mission.ShouldConsume ? 1 : 0, mission.CreatedPropellerCount, mission.CreatedRocketCount, mission.CreatedTntCount, mission.CreatedLightballCount, mission.CreatedSuperLightballCount })
                    : (invite.ShouldConsume ? "1" : "0") + "," + invite.EgoPrice;
                output.Add(result + ":" + state + ":" + saved + ":" + string.Join(";", f.Trace));
            }
            Console.WriteLine(string.Join("|", output));
        }
    }
}
