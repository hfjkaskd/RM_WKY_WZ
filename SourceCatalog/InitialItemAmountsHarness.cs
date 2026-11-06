using System;
using System.Collections.Generic;
using Whitebox.Foundation;
public static class Program
{
    private sealed class Conditions : IOriginalGoalConditionServices { public bool HasOperation(int x)=>false; public bool IsGoalReached(int x)=>false; }
    private static int[] Ints(string s)=>Array.ConvertAll(s.Split(','),int.Parse);
    private static Dictionary<int,int> Dict(string s)
    {
        if(s=="null")return null;var d=new Dictionary<int,int>();
        if(s!="")foreach(var entry in s.Split('/')){var a=Ints(entry);d.Add(a[0],a[1]);}return d;
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var p=line.Split('#');var h=Ints(p[0]);var trace=new List<string>();int calls=0;
            var a=new OriginalInitialItemAmounts(n=>{trace.Add("particle,"+n);if(++calls==h[5])throw new Exception();return h[3];},()=>{trace.Add("piggy");if(++calls==h[5])throw new Exception();return h[4]!=0;});
            Array.Copy(Ints(p[1]),a.ItemAmounts,116);Array.Copy(Ints(p[2]),a.StaticItemAmounts,20);
            string result="ok";try
            {
                switch(h[0])
                {
                    case 0:a.CalculateItemAmountsBasedOnGoalType(h[1],h[2],Dict(p[3]));break;
                    case 1:a.CalculateNonGoalItemAmounts(h[1],h[2]!=0,Dict(p[3]));break;
                    case 2:
                        var goals=new List<Dictionary<int,OriginalGoalTuple>>();
                        if(p[4]=="null")goals=null;
                        else if(p[4]!="empty")foreach(var b in p[4].Split('~'))
                        {var d=Dict(b);if(d==null){goals.Add(null);continue;}var g=new Dictionary<int,OriginalGoalTuple>();foreach(var e in d)g.Add(e.Key,new OriginalGoalTuple(e.Key,e.Value,false,new Conditions()));goals.Add(g);}
                        var counts=new List<Dictionary<int,int>>();
                        if(p[5]=="null")counts=null;else if(p[5]!="empty")foreach(var b in p[5].Split('~'))counts.Add(Dict(b));
                        var v=Ints(p[6]);a.CreateItemLists(goals,counts,v[0],v[1],v[2],v[3],v[4]!=0,v[5],v[6],v[7],v[8],v[9],v[10],v[11]);break;
                    case 3:result="value,"+a.GetItemAmount(h[1])+","+a.GetStaticItemAmount(h[2])+","+(a.IsThereNonFallBlockingAboveStaticItemInLevel()?1:0)+","+a.ItemCountDiff(h[1],h[3])+","+a.StaticItemCountDiff(h[2],h[3]);break;
                    case 4:result="value,"+OriginalInitialItemAmounts.GetParticleAmount(h[1],h[2]!=0);break;
                }
            }catch{result="error";}
            Console.WriteLine(result+":"+string.Join(",",a.ItemAmounts)+":"+string.Join(",",a.StaticItemAmounts)+":"+a.TotalGoalCountForStoryLevel+":"+string.Join("/",trace));
        }
    }
}
