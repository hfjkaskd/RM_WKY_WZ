using System;
using System.Collections.Generic;
using Whitebox.Foundation;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelSeedRandom { int Next(int min, int max); }
    public struct OriginalCellPoint { public int X,Y; public OriginalCellPoint(int x,int y){X=x;Y=y;} }
    public struct OriginalExplodeData { public int Id,Trigger;public OriginalCellPoint Point; public bool CanSpreadJelly; }
    public partial class OriginalBoardItem {protected int layer=3;public int Id,ItemType,GoalType;public bool HasView;}
    public sealed partial class OriginalMatchBoardItem : OriginalBoardItem {private bool selectedByColorMixer;public int MatchType;public void Select(bool b){selectedByColorMixer=b;}}
    public abstract partial class OriginalSpecialBoardItem : OriginalBoardItem {public bool IsPreparedForSpecialUse;}
    public class StubSpecial : OriginalSpecialBoardItem {}
    public class OriginalBoxBoardItem : OriginalBoardItem {}
    public class StaticTouch {public List<int> AboveItems=new List<int>(),BelowItems=new List<int>();}
    public class Mediation {public OriginalBoardItem CurrentItem;}
    public class OriginalGridCell {public int Id,X,Y,Score,Remaining,Redirect,GuidedScoreFrame,GuidedScoreType,GuidedScore;public bool IsNormalCell=true;public StaticTouch StaticTouch=new StaticTouch();public Mediation Mediation=new Mediation();}
    public class OriginalGoalManager {public int Left;public int GetGoalLeftCount(int type)=>Left;}
    public class OriginalGoalClaims {public bool HasClaimers;}
    public interface IOriginalGridIteratorStrategy {OriginalGridCell FirstCell();OriginalGridCell GetNext(OriginalGridCell cell);}
    public struct OriginalGridIterator
    {
        private IOriginalGridIteratorStrategy strategy;private OriginalGridCell cell;private bool first;
        public OriginalGridIterator(IOriginalGridIteratorStrategy s){strategy=s;cell=s.FirstCell();first=true;}
        public void Reset(){cell=strategy.FirstCell();first=true;}
        public OriginalGridCell MoveNext(){if(first)first=false;else if(cell!=null)cell=strategy.GetNext(cell);return cell;}
    }
}
class SingleExplodeTargetHarness : IOriginalGridIteratorStrategy,IOriginalLevelSeedRandom,IOriginalSingleExplodeTargetServices
{
    public List<OriginalGridCell> Cells=new List<OriginalGridCell>();
    public Dictionary<int,OriginalGridCell> All=new Dictionary<int,OriginalGridCell>();
    public List<string> Trace=new List<string>();public int[] RandomValues;public int RandomUsed;
    public OriginalGridCell FirstCell()=>Cells.Count==0?null:Cells[0];
    public OriginalGridCell GetNext(OriginalGridCell c){int i=Cells.IndexOf(c)+1;return i<Cells.Count?Cells[i]:null;}
    public int Next(int min,int max){int value=RandomValues[RandomUsed++]%(max-min)+min;Trace.Add("N"+max+":"+value);return value;}
    public int GetScore(OriginalGridCell c,int type,bool jelly)=>c.Score;
    public int RemainingExplodeCount(OriginalGridCell c,OriginalExplodeData d){Trace.Add("R"+c.Id);return c.Remaining;}
    public OriginalGridCell TryRedirect(OriginalGridCell c,OriginalExplodeData d,List<OriginalGridCell> options)
    {Trace.Add("D"+c.Id+":"+options.Count);return c.Redirect<0?null:All[c.Redirect];}
    public OriginalBoardItem AddIncomingExploder(OriginalGridCell c,OriginalExplodeData d)=>throw new Exception("Not part of the isolated selector boundary");
    static int[] Ints(string s)=>s.Length==0?new int[0]:Array.ConvertAll(s.Split(','),int.Parse);
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            string[] p=line.Split('|');
            if(p[0]=="S")
            {
                var h=new SingleExplodeTargetHarness();var f=new OriginalSingleExplodeTargetFinder(h,h,h);
                int id=1000;foreach(int value in Ints(p[2]))h.Cells.Add(new OriginalGridCell{Id=id++,Score=value});
                f.FillScores(0,false);h.Cells.Clear();
                foreach(string entry in p[3].Split(';'))if(entry.Length>0)
                {int[] v=Ints(entry);var c=new OriginalGridCell{Id=v[0],X=v[1],Y=v[2],Score=v[3],Remaining=v[4],Redirect=v[5]};h.Cells.Add(c);h.All.Add(c.Id,c);}
                f.FillScores(0,false);h.RandomValues=Ints(p[4]);int[] args=Ints(p[1]);
                var data=new OriginalExplodeData{Trigger=8,Point=new OriginalCellPoint(args[0],args[1])};
                var previous=args[2]==0?null:new OriginalBoardItem();var results=new List<string>();
                for(int q=0;q<args[3];q++)
                {OriginalGridCell cell;bool keep=f.FindSingleTargetCell(data,previous,out cell);results.Add((keep?"1":"0")+","+(cell==null?-1:cell.Id));}
                Console.WriteLine(string.Join(";",results)+"|"+string.Join(",",h.Trace));
            }
            else if(p[0]=="R")
            {int[] v=Ints(p[1]);Console.WriteLine(OriginalExplodeTargetRules.ItemScore(v[0])+","+OriginalExplodeTargetRules.AsGuidedType(v[1])+","+(OriginalExplodeTargetRules.IsSingleExploderStrategy(v[2])?1:0)+","+(OriginalExplodeTargetRules.IsObstacle(v[0])?1:0));}
            else if(p[0]=="C")
            {
                var item=new OriginalBoardItem();var trace=new List<string>();
                foreach(string entry in p[1].Split(';'))
                {int[] v=Ints(entry);var d=new OriginalExplodeData{Id=v[1],Trigger=v[2]};if(v[0]==0)item.AddIncomingExploder(d);else item.RemoveIncomingExploder(d);
                 trace.Add(item.IncomingExploderCount+","+item.RemainingExplodeCount()+","+item.GetExtraIncomingExplodeCount(1)+","+item.GetExtraIncomingExplodeCount(2)+","+item.GetExtraIncomingExplodeCount(3));}
                Console.WriteLine(string.Join(";",trace));
            }
            else if(p[0]=="T")
            {
                int[] v=Ints(p[1]);OriginalBoardItem item;
                if(v[0]==0){var m=new OriginalMatchBoardItem{MatchType=v[2]};m.Select(v[4]!=0);item=m;}
                else if(v[0]==1)item=new StubSpecial{IsPreparedForSpecialUse=v[5]!=0};
                else item=new OriginalBoxBoardItem();
                item.ItemType=v[1];item.HasView=v[6]!=0;
                var c=new OriginalGridCell();c.Mediation.CurrentItem=item;
                var s=new OriginalFirstBoardExplodeTargets(new OriginalGoalManager{Left=v[3]},new OriginalGoalClaims(),()=>0);
                item.BindTargetScoreContext(s);
                Console.WriteLine(s.GetScore(c,0,false));
            }
        }
    }
}
