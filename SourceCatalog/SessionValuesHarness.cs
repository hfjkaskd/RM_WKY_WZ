using System;
using System.Collections.Generic;
using Whitebox.Foundation;
static class SessionValuesHarness
{
    sealed class Owner : IOriginalRestorable
    {
        public short[] Lengths; public string Signed;
        public short GetDataLength(int i) => i >= 0 && i < Lengths.Length ? Lengths[i] : (short)0;
        public bool GetIsDataSigned(int i) => i >= 0 && i < Signed.Length && Signed[i] == '1';
    }
    public static void Main()
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#'); string type = parts[0];
            var owner = new Owner { Lengths = Array.ConvertAll(parts[1].Split(','), short.Parse), Signed = parts[2] };
            var raw = new OriginalRestorableData(0, owner); var before = new OriginalBeforeAfterData(); var special = new OriginalSpecialItemCount();
            long saved = 0; var outputs = new List<string>();
            foreach (var step in parts[3].Split(';'))
            {
                var args = step.Split(','); int index = args.Length > 1 && !(type == "r" && args[0] == "a") ? int.Parse(args[1]) : 0;
                long v = args.Length > 2 ? long.Parse(args[2]) : 0; string result = "-";
                try
                {
                    if (type == "r")
                    {
                        switch (args[0])
                        {
                            case "c": raw = new OriginalRestorableData((short)index, owner); break;
                            case "l": raw.SetDataRaw(v); saved = v; break;
                            case "w": raw.SetData(index, v); break;
                            case "s": raw.SetDataSigned(index, v, short.Parse(args[3])); break;
                            case "u": raw.SetDataUnsigned(index, v, short.Parse(args[3])); break;
                            case "g": result = raw.GetData(index).ToString(); break;
                            case "o": result = raw.GetOffset(index).ToString(); break;
                            case "a": saved = raw.PrepareData(args.Length == 1 ? null : Array.ConvertAll(args[1].Split('/'), long.Parse)); result = saved.ToString(); break;
                        }
                    }
                    else if (type == "b")
                    {
                        switch (args[0])
                        {
                            case "d": before.ShouldConsume = index != 0; before.Before = (int)v; before.After = int.Parse(args[3]); break;
                            case "i": before.Init(index); break;
                            case "a": before.AddExtra(index); break;
                            case "u": before.Update(index); break;
                            case "c": before.Consume(); break;
                            case "f": before.FullReset(); break;
                            case "q": result = before.GetCollectedAmount().ToString(); break;
                            case "g": saved = before.GetSaveData(); result = saved.ToString(); break;
                            case "l": before.LoadSaveData(v); break;
                        }
                    }
                    else
                    {
                        if (args[0] == "d") { special.CreationCount = index; special.AutoCreationCount = (int)v; special.UseCount = int.Parse(args[3]); special.AutoTriggerCount = int.Parse(args[4]); special.LightballCreationCount = int.Parse(args[5]); }
                        else if (args[0] == "g") { saved = special.GetSaveData(); result = saved.ToString(); }
                        else if (args[0] == "l") special.LoadSaveData(v);
                    }
                }
                catch { result = "error"; }
                string fields = type == "r" ? raw.GetDataUnsigned(0, 63).ToString() + "," + raw.GetDataUnsigned(0, 64)
                    : type == "b" ? (before.ShouldConsume ? "1" : "0") + "," + before.Before + "," + before.After
                    : special.CreationCount + "," + special.AutoCreationCount + "," + special.UseCount + "," + special.AutoTriggerCount + "," + special.LightballCreationCount;
                outputs.Add(result + ":" + fields + ":" + saved);
            }
            Console.WriteLine(string.Join("|", outputs));
        }
    }
}
