using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public interface IOriginalWinInventory { int Coin { get; } }
    public class OriginalActiveLevelData
    { public bool IsSideProgressionLevel, IsLeague, IsLeagueHard, IsLeagueSuperHard, IsLeagueExtremelyHard; public int Difficulty; }
}
public static class InventoryTimeResetHarness
{
    static long[] v;
    static OriginalUserInventoryBalances inventory;
    static BitArray original, other;
    static readonly List<string> trace = new List<string>();
    static int writes;
    static string Enc(string value) => value == null ? "~" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    static string Bits(BitArray value)
    {
        if (value == null) return "~";
        var text = new StringBuilder(value.Length); for (int i = 0; i < value.Length; i++) text.Append(value[i] ? '1' : '0'); return text.ToString();
    }
    static BitArray Parse(string value)
    {
        if (value == "~") return null;
        var bits = new BitArray(value.Length); for (int i = 0; i < value.Length; i++) bits[i] = value[i] == '1'; return bits;
    }
    static void Mutate(int kind)
    {
        switch (kind)
        {
            case 1: inventory.Chest = 0x1234ffff; break;
            case 2: inventory.RemainingBoosterTimes = long.MaxValue; break;
            case 3: inventory.MultiplierEndTimes = -1; break;
            case 4: inventory.Feature2 = -1; break;
            case 5: inventory.UnlimitedBoosterInventory = null; break;
            case 6: inventory.UnlimitedBoosterInventory = other; break;
            case 7: inventory.UnlimitedBoosterInventoryString = ""; break;
            case 8: inventory.Feature2 = 0; break;
            case 9: inventory.Chest = -1; break;
        }
    }
    sealed class Connection : IOriginalKeyValueConnection
    {
        public int InsertOrReplace(OriginalKeyValue row)
        {
            trace.Add(row.Key + "," + Enc(row.Value)); writes++;
            if (writes == v[9]) throw new InvalidOperationException();
            if (writes == v[10]) Mutate((int)v[11]);
            return 1;
        }
        public OriginalKeyValue Find(string key) => throw new InvalidOperationException();
        public int Delete(string key) => throw new InvalidOperationException();
        public int UpdateSynced(string key, int value) => throw new InvalidOperationException();
        public int UpdateSyncBlocked(string key, int value) => throw new InvalidOperationException();
        public void BeginTransaction() { } public void Commit() { } public void Dispose() { }
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            string[] fields = line.Split('|'); v = Array.ConvertAll(fields[0].Split(','), long.Parse); trace.Clear(); writes = 0;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone(); if (v[13] != 0) culture.NumberFormat.NegativeSign = "~";
            CultureInfo.CurrentCulture = culture;
            original = Parse(fields[1]); other = new BitArray(200, true);
            inventory = new OriginalUserInventoryBalances(0, 0, 0, null) { RocketEndTime = (int)v[2], TntEndTime = (int)v[3], LightBallEndTime = (int)v[4],
                Chest = (int)v[5], RemainingBoosterTimes = v[6], MultiplierEndTimes = v[7], Feature2 = v[8], UnlimitedBoosterInventory = original,
                UnlimitedBoosterInventoryString = fields[2] == "~" ? null : Encoding.UTF8.GetString(Convert.FromBase64String(fields[2])) };
            if (v[12] == 0) { var db = new OriginalDatabaseService(new Connection(), 61, null); inventory.BindLifeStorage(new OriginalUserKeyValue(() => db)); }
            string result = null, error = "";
            try
            {
                switch ((int)v[0])
                {
                    case 0: inventory.ResetTimes(); break;
                    case 1: inventory.UpdateRocketEndTime((int)v[1]); break;
                    case 2: inventory.UpdateTntEndTime((int)v[1]); break;
                    case 3: inventory.UpdateLightballEndTime((int)v[1]); break;
                    case 4: inventory.UpdateRemainingBoosterTimes(v[1]); break;
                    case 5: inventory.UpdateMultiplierEndTimes(v[1]); break;
                    case 6: result = OriginalUnlimitedBoosterSerialization.Serialize(inventory.UnlimitedBoosterInventory); break;
                    case 7: inventory.ResetTimes(); inventory.ResetTimes(); break;
                }
            }
            catch (NullReferenceException) { error = "null"; }
            catch (ArgumentOutOfRangeException) { error = "bounds"; }
            catch (InvalidOperationException) { error = "write"; }
            int identity = inventory.UnlimitedBoosterInventory == null ? -1 : ReferenceEquals(inventory.UnlimitedBoosterInventory, original) ? 0 : 1;
            // Invariant text is only test transport. Production KeyValue and
            // serializer above retain their native current-culture overloads.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Console.WriteLine(string.Join(",", new[] { inventory.RocketEndTime.ToString(), inventory.TntEndTime.ToString(), inventory.LightBallEndTime.ToString(), inventory.Chest.ToString(),
                inventory.RemainingBoosterTimes.ToString(), inventory.MultiplierEndTimes.ToString(), inventory.Feature2.ToString(), identity.ToString(),
                Enc(inventory.UnlimitedBoosterInventoryString), Enc(result), error }) + "|" + Bits(original) + "|" + Bits(other) + "#" + string.Join(";", trace));
        }
    }
}
