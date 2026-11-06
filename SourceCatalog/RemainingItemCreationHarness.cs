using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public sealed class OriginalLevelRandomSource{public int DifficultyNext(int a,int b){return 0;}}
    public sealed partial class OriginalFillSetGeneration
    {
        private OriginalLevelSetSelection[] sets;
        public OriginalFillSetGeneration(OriginalLevelSetSelection[] values){sets=values;}
    }
    public sealed partial class OriginalLevelSetSelection
    {
        public bool ContainsMatchItem()=>OriginalLevelRules.ToMatchType(ids[0])!=0;
        public string Snapshot()
        {var flags=new int[available.Length];for(int i=0;i<flags.Length;i++)flags[i]=available[i]?1:0;return string.Join(",",ids)+"/"+string.Join(",",weights)+"/"+string.Join(",",flags)+"/"+(IsAvailableForFill?1:0)+","+TotalCreateRatio+","+RemainingItemCount;}
    }
}
public static class Harness
{
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('|');int mode=int.Parse(p[0]);var elements=new List<Whitebox.Foundation.ConfigData.FTiledSetElement>();
            if(p[2]!="")foreach(var e in p[2].Split(',')){var v=e.Split(':');elements.Add(new Whitebox.Foundation.ConfigData.FTiledSetElement{Id=int.Parse(v[0]),CreateRatio=int.Parse(v[1])});}
            var parsed=new OriginalParsedLevelSet(new Whitebox.Foundation.ConfigData.FTiledSet{CanFall=p[1]=="1",Elements=elements.ToArray()},0,0);
            var set=new OriginalLevelSetSelection(parsed,new OriginalLevelRandomSource());bool fault=false;
            try{if(mode==0){set.FillFourColors();set.FillFourColors();}else{var generator=new OriginalFillSetGeneration(new[]{set});generator.RemainingMovesStarted();generator.RemainingMovesStarted();}}
            catch(IndexOutOfRangeException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+set.Snapshot()+"#"+(set.ContainsSpecialItem()?1:0));
        }
    }
}
