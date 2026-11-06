using System;
using System.Collections.Generic;
using Whitebox.Foundation;

namespace Whitebox.Foundation
{
    // Unused item-goal adapter dependencies; fail if this harness crosses them.
    public sealed class OriginalBoardItem { public int GoalType; }
    public sealed class OriginalGoalManager
    { public void DecreaseGoal(int goal) { throw new Exception(); } public void DecreaseGoalUi(int goal) { throw new Exception(); } }
    public interface IOriginalItemGoalServices
    { void DecreaseGoal(int goal); void CollectAsGoalUi(OriginalBoardItem item); }
}

sealed class Context : IOriginalViewCollectionLevelContext
{
    bool Read(string name, int index) { Verify.Trace.Add(name); return Verify.Head[index] != 0; }
    public bool IsStoryLevel => Read("S", 2);
    public bool IsFinalBoard => Read("F", 3);
    public bool IsPortalLevel => Read("P", 4);
    public bool IsWaterLevel => Read("W", 5);
    public bool IsMultipleBoardLevel => Read("M", 6);
}
sealed class Board : IOriginalMultipleBoardGoalCollection
{
    public bool IsGoalInCurrentSubGoal(int goal)
    { Verify.Trace.Add("board," + goal); return Verify.Head[7] != 0; }
}
sealed class Effect : IOriginalProgressCollectionEffect
{
    public void SetPosition(float x, float y, float z)
    {
        if (Verify.Head[11] == 0) throw new NullReferenceException();
        Verify.Trace.Add("pos," + Verify.Position(x,y,z));
    }
    public void Initialize(int goal, bool remaining) { Verify.Trace.Add("init," + goal + "," + (remaining ? 1 : 0)); }
}
sealed class Factory : IOriginalProgressCollectionFactory
{
    public IOriginalProgressCollectionEffect Spawn(int pool, bool activate)
    { Verify.Trace.Add("spawn," + pool + "," + (activate ? 1 : 0)); return Verify.Head[10] == 0 ? null : new Effect(); }
}
sealed class Claimer : IOriginalGoalTrailClaimer
{
    public int Id, Active, Want, Await, Result;
    public bool IsInCurrentBoard() { Verify.Trace.Add("a," + Id); Verify.Mutate(1,Id); return Active != 0; }
    public bool WantsGoal(int goal) { Verify.Trace.Add("w," + Id + "," + goal); Verify.Mutate(3,Id); return Want == goal; }
    public bool HasClaimAwaitingTrail(int goal) { Verify.Trace.Add("c," + Id + "," + goal); Verify.Mutate(2,Id); return Await == goal; }
    public bool TryCollectWithTrail(int goal, float x, float y, float z)
    { Verify.Trace.Add("trail," + Id + "," + goal + "," + Verify.Position(x,y,z)); return Result != 0; }
    public bool HasIncomingGoal(int goal) { throw new Exception(); }
    public bool CanRestoreGoal(int goal) { throw new Exception(); }
    public int GetOutstandingTargetForGoal(int goal) { throw new Exception(); }
    public void DecrementTarget(int goal) { throw new Exception(); }
    public void IncrementTarget(int goal) { throw new Exception(); }
    public void ItemReached(int goal) { throw new Exception(); }
}
static class Verify
{
    public static readonly List<string> Trace = new List<string>();
    public static int[] Head;
    public static OriginalGoalClaims Claims;
    static Claimer[] items;
    static bool mutated;
    static string Bits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value),0).ToString("x8");
    public static string Position(float x,float y,float z) => Bits(x)+","+Bits(y)+","+Bits(z);
    public static void Mutate(int phase, int id)
    {
        if (mutated || Head[14] != phase || id != 0) return;
        mutated = true; Trace.Add("mutate," + phase);
        if (phase == 1) Claims.Unregister(items[0]);
        if (phase == 2) Claims.Unregister(items[1]);
        if (phase == 3) Claims.Register(items[3]);
    }
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts=line.Split('#'); Head=Array.ConvertAll(parts[1].Split(','),int.Parse);
            Trace.Clear(); mutated=false; Claims=new OriginalGoalClaims(); items=new Claimer[4];
            var rows=parts[2].Split(';');
            for(int i=0;i<4;i++)
            {
                var r=Array.ConvertAll(rows[i].Split(','),int.Parse);
                items[i]=new Claimer{Id=i,Active=r[0],Want=r[1],Await=r[2],Result=r[3]};
                if((Head[13]&(1<<i))!=0) Claims.Register(items[i]);
            }
            float x=Head[15]*.25f,y=Head[16]*.5f,z=Head[17]*.125f;
            var collection=new OriginalItemViewGoalCollection(Head[12]==0?null:Claims,new Context(),()=>
            { Trace.Add("R"); return Head[8]==0?null:new Board(); },Head[9]==0?null:new Factory());
            try
            {
                if(parts[0]=="T") Trace.Add(Claims.TryCollectWithTrail(Head[0],x,y,z)?"yes":"no");
                else
                {
                    var effect=parts[0]=="V"?collection.TryCollectForStoryLevel(Head[0],x,y,z):
                        collection.TryCollectGoal(Head[0],x,y,z,Head[1]!=0);
                    Trace.Add(effect==null?"none":"effect");
                }
            }
            catch(NullReferenceException) { Trace.Add("error"); }
            Console.WriteLine(string.Join(";",Trace));
        }
    }
}
