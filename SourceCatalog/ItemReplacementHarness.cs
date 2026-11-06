using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public struct OriginalSwapPoint
    {
        public float x,y,z;
        public OriginalSwapPoint(float a,float b,float c){x=a;y=b;z=c;}
    }
    public partial class OriginalBoardItem
    {
        private bool visible=true;
        public bool HasView{get{return visible;}protected set{visible=value;Harness.Trace.Add("visible,"+(value?1:0));}}
        protected virtual void CollectAsGoal(bool value){Harness.Trace.Add("goal,"+(value?1:0));}
    }
    public sealed partial class OriginalMatchBoardItem:OriginalBoardItem
    {
        public int MatchType,GoalType=7;
        private Harness.Services explosionServices;
        public OriginalMatchBoardItem(int type,Harness.Services services){MatchType=type;explosionServices=services;}
        public OriginalSwapPoint GetViewPosition(){Harness.Trace.Add("position");return new OriginalSwapPoint(2,3,4);}
    }
    public interface IOriginalMatchCollectManager{}
}
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    private sealed class Manager:IOriginalMatchCollectManager{}
    public sealed class Services
    {
        public int[] V;
        private bool Flag(int n){return (V[4]&(1<<n))!=0;}
        public bool IsThereItemInLevel(int n){Trace.Add("item,"+n);return Flag(n==22?0:2);}
        public bool IsThereStaticItemInLevel(int n){Trace.Add("static,"+n);return Flag(n==5?1:3);}
        public void DecrementPurpleOnBoard(){Trace.Add("purple");}
        public int GetGoalUiLeftCount(int n){if(V[5]==2)throw new NullReferenceException();Trace.Add("goals,"+n);return V[2];}
        public void CollectToGoal(OriginalMatchBoardItem item){Trace.Add("collect-goal");}
        public IOriginalMatchCollectManager GetCurtainForMatchType(int type){Trace.Add("curtain,"+type);return Flag(4)?new Manager():null;}
        public IOriginalMatchCollectManager GetDrillForMatchType(int type){Trace.Add("drill,"+type);return Flag(5)?new Manager():null;}
        public IOriginalMatchCollectManager GetCryptexForMatchType(int type){Trace.Add("cryptex,"+type);return Flag(6)?new Manager():null;}
        public void CollectToManager(OriginalMatchBoardItem item,IOriginalMatchCollectManager manager){Trace.Add("collect-manager");}
        public void MatchItemExploded(int type,int count){Trace.Add("count,"+type+","+count);}
    }
    private sealed class Context:IOriginalItemReplacementContext
    {
        public void RecycleReplacementView(OriginalBoardItem item){Trace.Add("recycle");}
        public void CollectReplacementMadness(int type,OriginalSwapPoint point){Trace.Add("madness,"+type+",1,5,0");}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            int[] v=Array.ConvertAll(line.Split(','),int.Parse);Trace.Clear();
            OriginalBoardItem item=v[0]==0?new OriginalBoardItem():new OriginalMatchBoardItem(v[1],v[5]==1?null:new Services{V=v});
            bool fault=false;try{item.ReplaceItem(v[3]!=0,new Context());}catch(NullReferenceException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+(item.HasView?1:0)+"#"+string.Join(";",Trace));
        }
    }
}
