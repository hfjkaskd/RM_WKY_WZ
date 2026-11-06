using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public struct OriginalMatchGroupSnapshot { public int PatternType; }
    public struct OriginalExplodeData { public int Id, Trigger; }
    public static class OriginalExploder
    {
        public static int Id;
        public static OriginalExplodeData Next(int trigger)
        { Verify.Record("next,"+trigger); Id=unchecked(Id+1); return new OriginalExplodeData{Id=Id,Trigger=trigger}; }
    }
    public class OriginalResolvedMatchGroup
    {
        public int Index;
        public OriginalMatchGroupSnapshot CaptureSnapshot()
        { Verify.Record("get,"+Index); return new OriginalMatchGroupSnapshot{PatternType=Index+10}; }
    }
    public class OriginalGameStateManager
    {
        public bool HasOperation(int op) { Verify.Record("op,"+op);return Verify.Row[3]!=0; }
        public bool HasAnyOperation() { Verify.Record("any");return Verify.Row[4]!=0; }
    }
    public class OriginalMatchManager { public IReadOnlyList<OriginalResolvedMatchGroup> FindAllMatches() => throw new InvalidOperationException(); }
    public class OriginalMatchGroupExplosion { public bool ExplodeMatchGroup(OriginalMatchGroupSnapshot g,OriginalExplodeData d,bool a) => throw new InvalidOperationException(); }
}
class Verify : IOriginalAutoExplodeMatchServices
{
    public static int[] Row;
    public static List<string> Trace = new List<string>();
    public static int Calls;
    static OriginalAutoExplodeManager owner;
    static OriginalGameStateManager state = new OriginalGameStateManager();
    public static void Record(string value)
    { Trace.Add(value); Calls++; if(Calls==Row[7]) throw new NullReferenceException(); }
    static string Bits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value),0).ToString("x8");
    static float Float(string value) => BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(value,NumberStyles.HexNumber)),0);
    public IReadOnlyList<OriginalResolvedMatchGroup> FindAllMatches()
    {
        Record("find"); if((Row[2]&4)!=0)return null;
        var groups=new List<OriginalResolvedMatchGroup>();
        for(int i=0;i<Row[5];i++)groups.Add(new OriginalResolvedMatchGroup{Index=i});return groups;
    }
    public bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group,OriginalExplodeData data,bool automatic)
    {
        int index=group.PatternType-10;
        Record("explode,"+index+","+data.Trigger+","+data.Id+","+(automatic?1:0));
        if(index==0)
        {
            if(Row[8]==1)owner.Disable();
            else if(Row[8]==2)owner.Clear(false);
            else if(Row[8]==3)owner.Bind(null,this);
            else if(Row[8]==4)owner.Bind(state,null);
        }
        return (Row[6]&(1<<index))!=0;
    }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var pieces=line.Split('#');var head=pieces[0].Split(',');var service=new Verify();
            owner=new OriginalAutoExplodeManager(()=>{Record("frame");return Row[1];});
            owner.hasNoOperationSinceFrame=Float(head[0]);OriginalExploder.Id=int.Parse(head[1]);var output=new List<string>();
            foreach(var part in pieces[1].Split(';'))
            {
                Row=Array.ConvertAll(part.Split(','),int.Parse);Trace.Clear();Calls=0;string result="-";
                owner.Bind((Row[2]&1)!=0?null:state,(Row[2]&2)!=0?null:service);
                try {if(Row[0]==1)owner.Disable();else if(Row[0]==2)owner.Clear(Row[1]!=0);else owner.ManualUpdate();}
                catch(NullReferenceException){result="error";}
                output.Add(result+","+(owner.IsThereAWaitingMatch?1:0)+","+(owner.enabled?1:0)+","+Bits(owner.hasNoOperationSinceFrame)+","+OriginalExploder.Id+":"+string.Join(";",Trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
