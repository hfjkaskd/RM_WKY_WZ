using System;
using System.Collections.Generic;
using Whitebox.Foundation;
class Unit : IOriginalContextUnit
{
    public int Tag, Key;
    public int Id { get { Verify.Record("id,"+Tag);return Key; } }
    public void Bind() { Verify.Record("bind,"+Tag);Verify.Callback(Tag); }
}
class Behaviour : Unit, IOriginalContextBehaviour
{ public void ManualUpdate() { Verify.Record("update,"+Tag);Verify.Callback(Tag); } }
class LateUnit : Unit, IOriginalLateContextUnit
{ public void OnRemove() { Verify.Record("remove,"+Tag);Verify.Callback(Tag); } }
class LateBehaviour : Behaviour, IOriginalLateContextUnit
{ public void OnRemove() { Verify.Record("remove,"+Tag);Verify.Callback(Tag); } }
class OtherUnit : Unit { }
class OtherBehaviour : Behaviour { }
class Verify
{
    static OriginalContextContainer context;
    static Unit[] units;
    static List<string> trace=new List<string>();
    static HashSet<int> mutated=new HashSet<int>();
    static int[] row;static int calls;
    public static void Record(string text)
    { trace.Add(text);if(++calls==row[5])throw new InvalidOperationException(); }
    static Unit Ref(int tag) => tag<0?null:units[tag];
    static IOriginalContextUnit Create() { Record("create,"+row[4]);return Ref(row[4]); }
    public static void Callback(int tag)
    {
        if(tag!=row[2]||!mutated.Add(tag))return;
        switch(row[3])
        {
            case 1:context.Add(Ref(row[4]));break;
            case 2:context.AddToFirst(Ref(row[4]));break;
            case 3:context.Remove(row[4]);break;
            case 4:context.Clear();break;
            case 5:context.Replace(Ref(row[4]));break;
            case 6:context.GetLate(row[1],Create);break;
            case 7:context.RemoveLateUnits();break;
            case 8:context.GetAllUnits()[row[1]]=Ref(row[4]);break;
        }
    }
    static string Slots()
    { var v=new List<int>();foreach(var unit in context.GetAllUnits())v.Add(unit==null?-1:((Unit)unit).Tag);return string.Join(",",v); }
    static string Behaviours()
    { var v=new List<int>();foreach(var unit in context.Behaviours)v.Add(((Unit)unit).Tag);return string.Join(",",v); }
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('#');context=new OriginalContextContainer(int.Parse(p[0]));
            var cfg=p[1].Split(';');units=new Unit[cfg.Length];
            for(int i=0;i<cfg.Length;i++)
            {
                var v=Array.ConvertAll(cfg[i].Split(','),int.Parse);
                switch(v[1])
                {
                    case 0:units[i]=new Unit();break;
                    case 1:units[i]=new Behaviour();break;
                    case 2:units[i]=new LateUnit();break;
                    case 3:units[i]=new LateBehaviour();break;
                    case 4:units[i]=new OtherUnit();break;
                    default:units[i]=new OtherBehaviour();break;
                }
                units[i].Tag=i;units[i].Key=v[0];
            }
            var output=new List<string>();
            foreach(var command in p[2].Split(';'))
            {
                row=Array.ConvertAll(command.Split(','),int.Parse);trace.Clear();mutated.Clear();calls=0;string result="-";
                try
                {
                    switch(row[0])
                    {
                        case 0:context.Add(Ref(row[1]));break;
                        case 1:context.AddToFirst(Ref(row[1]));break;
                        case 2:context.Remove(row[1]);break;
                        case 3:context.BindAll();break;
                        case 4:context.ManualUpdate();break;
                        case 5:context.Clear();break;
                        case 6:result=context.Has(row[1])?"1":"0";break;
                        case 7:var unit=context.Get<Unit>(row[1]);result=unit==null?"-1":unit.Tag.ToString();break;
                        case 8:context.GetAllUnits()[row[1]]=Ref(row[4]);break;
                        case 9:result=ReferenceEquals(context.GetAllUnits(),context.GetAllUnits())?"1":"0";break;
                        case 10:context.Replace(Ref(row[1]));break;
                        case 11:var late=context.GetLate(row[1],Create);result=late==null?"-1":((Unit)late).Tag.ToString();break;
                        case 12:context.RemoveLateUnits();break;
                    }
                }
                catch(Exception){result="error";}
                output.Add(result+":"+Slots()+":"+Behaviours()+":"+string.Join("/",trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
