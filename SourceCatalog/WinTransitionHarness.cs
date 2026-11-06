using System;
using System.Collections.Generic;
using Whitebox.Foundation;
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    private sealed class Context:IOriginalWinTransitionContext
    {
        public int[] V;
        public bool AnimationImprovementEnabled{get{Trace.Add("animation");return V[0]!=0;}}
        private void King(string name){if(V[2]==1)throw new NullReferenceException();Trace.Add(name);}
        public void KingBeHappy()=>King("happy");public void KingStopWorry()=>King("stop");
        public int[] CreateWinLowMatchRatios(){if(V[2]==2)throw new NullReferenceException();Trace.Add("ratios");return new[]{10,45,45};}
        public void UpdateLowMatchRatios(int[] ratios){if(V[2]==3)throw new NullReferenceException();Trace.Add("update,"+string.Join(",",ratios));}
        public void IncrementLives(int amount){Trace.Add("life-owner");if(V[2]==4)throw new NullReferenceException();Trace.Add("lives,"+amount);}
        public bool IsStory{get{if(V[2]==5)throw new NullReferenceException();Trace.Add("story");return V[1]!=0;}}
        public void ResetEgoOnWin(){if(V[2]==6||V[2]==7)throw new NullReferenceException();Trace.Add("ego");}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            int[] v=Array.ConvertAll(line.Split(','),int.Parse);Trace.Clear();Trace.Add("state-win");bool fault=false;
            try{OriginalWinTransition.OnWin(new Context{V=v});}catch(NullReferenceException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+string.Join(";",Trace));
        }
    }
}
