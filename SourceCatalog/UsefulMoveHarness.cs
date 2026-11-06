using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine { public sealed class Coroutine { } }
namespace Whitebox.Foundation
{
    public interface IOriginalBoxExplosionServices { void UsefulMoveMade(); }
    public interface IOriginalManualCoroutinePlayer
    { UnityEngine.Coroutine ManualStartCoroutine(IEnumerator value, Action onKill = null); }
}
sealed class Scheduler : IOriginalManualCoroutinePlayer
{
    public IEnumerator Current;
    public UnityEngine.Coroutine ManualStartCoroutine(IEnumerator value, Action onKill = null)
    {
        Verify.Trace.Add("start,"+(Verify.Useful.IsUsefulMoveMade?1:0));
        Current=value; return new UnityEngine.Coroutine();
    }
}
sealed class Context : IOriginalKingReliefServices
{
    public bool Worried, Stopped;
    public bool IsWorried => Worried;
    public int LeftMoves { get { if(Verify.Head[9]==5)throw new NullReferenceException();return Verify.Head[1]; } }
    public int LastMoveLayerCapsCount { get { if(Verify.Head[9]==6)throw new NullReferenceException();return Verify.Head[2]; } }
    public bool IsWinPossible() { Verify.Trace.Add("win");return Verify.Head[3]!=0; }
    public bool IsPlaying()
    { if(Verify.Head[9]==7)throw new NullReferenceException();Verify.Trace.Add("playing");return Verify.Head[4]!=0; }
    public bool ShouldKingBeWorried() { Verify.Trace.Add("worry");return Verify.Head[5]!=0; }
    public void TryStopWorry(int reason) { Verify.Trace.Add("stop,"+reason);Worried=false;Stopped=true; }
}
static class Verify
{
    public static readonly List<string> Trace=new List<string>();
    public static int[] Head;
    public static OriginalUsefulMoveState Useful;
    static int Bool(bool v)=>v?1:0;
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var parts=line.Split('#');Head=Array.ConvertAll(parts[0].Split(','),int.Parse);
            var context=new Context{Worried=Head[0]!=0};var player=new Scheduler();
            int missing=Head[9];var data=new OriginalKingBoardCheckData
            {
                IsThereLightballCombo=true,IsThereSingleLightball=true,
                MatchItemCounts=missing==1?null:new int[Head[10]],
                ItemTypesForTotalLayer=missing==2?null:new Dictionary<int,HashSet<int>>{{6,new HashSet<int>{1}}},
                MatchTypesForLightball=missing==3?null:new HashSet<int>{1},
                CheckedComboTypes=missing==4?null:new HashSet<int>{2}
            };
            if(data.MatchItemCounts!=null)for(int i=0;i<data.MatchItemCounts.Length;i++)data.MatchItemCounts[i]=i+1;
            var king=new OriginalKingRelief(data,player,context);
            Useful=new OriginalUsefulMoveState(()=>
            { Trace.Add("king,"+Bool(Useful.IsUsefulMoveMade));return Head[7]==0?null:king; })
            {IsUsefulMoveMade=Head[6]!=0,UselessMoveCount=Head[11],MaxUselessMoveStreak=Head[12],CurrentUselessMoveStreak=Head[13]};
            var services=new OriginalItemUsefulMoveServices(()=>
            { Trace.Add("level");return Head[8]==0?null:Useful; });
            var output=new List<string>();
            foreach(char command in parts[1])
            {
                Trace.Clear();string result="-";
                try
                {
                    switch(command)
                    {
                        case 'u':services.UsefulMoveMade();break;
                        case 'm':Useful.UsefulMoveMade();break;
                        case 'k':king.TryKingRelief();break;
                        case 'n':if(player.Current==null)throw new NullReferenceException();result=Bool(player.Current.MoveNext()).ToString();break;
                        case 'r':Useful.ResetIsUsefulMoveMade();break;
                        case 's':Useful.ResetUselessMoveStreak();break;
                        case 'i':Useful.IncreaseUselessMoveCount();break;
                        case 'b':data.Reset();break;
                    }
                }
                catch(NullReferenceException){result="error";}
                string counts=data.MatchItemCounts==null?"n":string.Join(",",Array.ConvertAll(data.MatchItemCounts,v=>v.ToString()));
                output.Add(result+":"+Bool(Useful.IsUsefulMoveMade)+","+Useful.UselessMoveCount+","+Useful.MaxUselessMoveStreak+","+Useful.CurrentUselessMoveStreak+","+Bool(king.ReliefCoroutine!=null)+","+Bool(data.IsThereLightballCombo)+","+Bool(data.IsThereSingleLightball)+","+Bool(context.Worried)+","+Bool(context.Stopped)+":"+counts+":"+
                    (data.ItemTypesForTotalLayer==null?"n":data.ItemTypesForTotalLayer.Count.ToString())+","+
                    (data.MatchTypesForLightball==null?"n":data.MatchTypesForLightball.Count.ToString())+","+
                    (data.CheckedComboTypes==null?"n":data.CheckedComboTypes.Count.ToString())+":"+string.Join(";",Trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
