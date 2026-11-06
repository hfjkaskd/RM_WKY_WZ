using System;
using System.Collections.Generic;
using Whitebox.Foundation;
static class ClashSessionHarness
{
    sealed class Bundle : IOriginalSessionEventBundle
    {
        readonly Inputs f; readonly int id;
        public Bundle(Inputs f, int id) { this.f = f; this.id = id; }
        public bool LoadBundle() { f.Record("load"); return f.H[4] != 0; }
        public void UnloadBundle(bool all) { f.Record("unload," + id + "," + (all ? 1 : 0)); }
    }
    sealed class WrongBundle : IOriginalSessionBundle
    { public bool LoadBundle() => throw new Exception(); public void UnloadBundle(bool all) => throw new Exception(); }
    sealed class Data : OriginalClashOfChampionsLevelData
    {
        public Data(Inputs f) : base(() => f.Resolve("manager") ? f : null, () => f.Resolve("owner") ? f : null,
            () => f.Resolve("pools") ? f : null, f.LoadAsset, f.Multiplier, f.Current) { }
        public bool CanScore => CanIncreaseScore;
        public void ClearBundle() { gameplayBundle = null; }
        public void Dirty(Inputs f)
        {
            ShouldConsume = true; Before = 101; After = 202; manager = f; helper = f; gameplayBundle = f.Old;
            scoreMultiplier = -7; isScoreLockedForLevel = true;
            matchItemScore = -101; propellerItemScore = -102; rocketItemScore = -103; tntItemScore = -104; lightballItemScore = -105;
            propellerPropellerComboScore = -106; propellerRocketComboScore = -107; propellerTntComboScore = -108;
            rocketRocketComboScore = -109; rocketTntComboScore = -110; tntTntComboScore = -111; lightballLightballComboMultiplier = -112;
            remainingMoveScore = -113; remainingSpecialItemsMultiplier = -114;
        }
        public string State(Inputs f)
        {
            return string.Join(",",new object[] { ShouldConsume ? 1 : 0, Before, After, manager == null ? 0 : 1, helper == null ? 0 : 1,
                gameplayBundle == null ? 0 : ReferenceEquals(gameplayBundle,f.Old) ? 1 : 2, scoreMultiplier, isScoreLockedForLevel ? 1 : 0,
                matchItemScore,propellerItemScore,rocketItemScore,tntItemScore,lightballItemScore,propellerPropellerComboScore,
                propellerRocketComboScore,propellerTntComboScore,rocketRocketComboScore,rocketTntComboScore,tntTntComboScore,
                lightballLightballComboMultiplier,remainingMoveScore,remainingSpecialItemsMultiplier,TotalLevelScoreForEvt });
        }
    }
    sealed class Inputs : IOriginalClashSessionManager, IOriginalClashSessionHelper, IOriginalClashScoreConfig,
        IOriginalClashSessionPools, IOriginalSessionBundleManager, IOriginalClashGameplayAsset, IOriginalClashCollectViewPrefab,
        IOriginalThreeColorLevelNames
    {
        public long[] H; public int Resolves, CurrentReads;
        public readonly List<string> Trace = new List<string>();
        public Bundle Old, New, Dialog;
        public readonly IOriginalSessionBundle Wrong = new WrongBundle();
        public OriginalActiveLevelData Active;
        public Data Data;
        public void Record(string label) { Trace.Add(label); if (Trace.Count == H[1]) throw new InvalidOperationException(); }
        public bool Resolve(string label) { Record(label); return ++Resolves != H[0]; }
        public IOriginalClashSessionHelper Helper => H[9] != 0 ? null : this;
        public IOriginalClashScoreConfig Config => H[10] != 0 ? null : this;
        public bool IsInEvent { get { Record("in_event"); return H[2] != 0; } }
        public bool IsAllBundlesDownloaded() { Record("downloaded"); return H[3] != 0; }
        public bool ShouldProgress { get { Record("progress"); return H[5] != 0; } }
        public bool IsScoreLocked { get { Record("locked"); return H[6] != 0; } }
        public void UpdateLocalScore(int score, int comboType) { Record("score," + score + "," + comboType); }
        public int Multiplier() { Record("multiplier"); return (int)H[7]; }
        public IOriginalSessionBundle GetBundle(string name)
        {
            bool game = name == OriginalClashOfChampionsLevelData.GameplayBundleName;
            if (!game && name != OriginalClashOfChampionsLevelData.DialogBundleName) throw new Exception();
            if (!Resolve("bundle," + (game ? "game" : "dialog"))) return null;
            return H[13] != 0 ? Wrong : game ? New : Dialog;
        }
        public IOriginalClashGameplayAsset LoadAsset(IOriginalSessionEventBundle bundle,string name)
        { if (!ReferenceEquals(bundle,New) || name != OriginalClashOfChampionsLevelData.CollectViewAssetName) throw new Exception(); Record("asset"); return H[11] != 0 ? null : this; }
        public IOriginalClashCollectViewPrefab GetCollectViewPrefab() { Record("component"); return H[12] != 0 ? null : this; }
        public void TryCreateClashOfChampionsGameplayCollectViewPool(IOriginalClashCollectViewPrefab prefab) { Record("create," + (prefab == null ? 0 : 1)); }
        public void TryClearClashOfChampionsGameplayCollectViewPool() { Record("clear"); if (H[14] != 0) Data.ClearBundle(); }
        public OriginalActiveLevelData Current()
        { CurrentReads++; return H[15] != 0 && (H[15] <= 3 || CurrentReads == 2) ? null : Active; }
        int Score(int index) { Record("config,"+index); return unchecked((int)H[8]+index*7); }
        public int GetMatchItemScore() => Score(0);
        public int GetPropellerItemScore() => Score(1);
        public int GetRocketItemScore() => Score(2);
        public int GetTntItemScore() => Score(3);
        public int GetLightballItemScore() => Score(4);
        public int GetPropellerPropellerComboScore() => Score(5);
        public int GetPropellerRocketComboScore() => Score(6);
        public int GetPropellerTntComboScore() => Score(7);
        public int GetRocketRocketComboScore() => Score(8);
        public int GetRocketTntComboScore() => Score(9);
        public int GetTntTntComboScore() => Score(10);
        public int GetLightballLightballComboMultiplier() => Score(11);
        public int GetRemainingMoveScore() => Score(12);
        public int GetWinBonus() => Score(13);
        public bool ShouldReportThreeColorExtension(int a,int b) => throw new Exception();
        public bool TryGetVersionOverride(int a,out int b) { b=0; throw new Exception(); }
        public string GetLevelExtension() => throw new Exception();
        public void Begin() { Resolves=0; CurrentReads=0; Trace.Clear(); Active.Difficulty=(sbyte)H[16]; Active.IsMultipliedInLevel=H[17]!=0; }
    }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('#'); var f=new Inputs { H=Array.ConvertAll(p[0].Split(','),long.Parse) };
            f.Old=new Bundle(f,1); f.New=new Bundle(f,2); f.Dialog=new Bundle(f,3);
            var slb=new OriginalSlbMoveConfig(()=>throw new Exception(),()=>throw new Exception());
            f.Active=new OriginalActiveLevelData(slb,f,null); f.Data=new Data(f); var output=new List<string>();
            foreach(var command in p[1].Split(';'))
            {
                f.Begin(); var v=command.Split(','); int n=v.Length>1 && v[0]!="l" ? int.Parse(v[1]):0; string result="-";
                try
                {
                    switch(v[0])
                    {
                        case "d":f.Data.Dirty(f);break;
                        case "i":f.Data.Init((sbyte)n,v[2]=="1",v[3]=="1");break;
                        case "l":f.Data.LoadSaveData(long.Parse(v[1]));break;
                        case "g":result=f.Data.GetSaveData().ToString();break;
                        case "v":result=f.Data.CanScore?"1":"0";break;
                        case "a":result=f.Data.IncrementScoreForItem(n).ToString();break;
                        case "c":result=f.Data.IncrementScoreForCombo(n,int.Parse(v[2])).ToString();break;
                        case "m":f.Data.IncrementScoreForRemainingMoves(n);break;
                        case "r":f.Data.ResetDataOnRoundFinished();break;
                        case "h":f.H[n]=long.Parse(v[2]);break;
                    }
                }
                catch { result="error"; }
                output.Add(result+":"+f.Data.State(f)+":"+string.Join(";",f.Trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
