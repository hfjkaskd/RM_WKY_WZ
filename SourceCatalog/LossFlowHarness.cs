using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine
{
    // Emulate Unity's destroyed-object equality as a separate boundary from
    // reference null, so a plain == null implementation cannot pass the oracle.
    public class AssetBundle
    {
        public bool Destroyed;
        public static bool operator ==(AssetBundle a,AssetBundle b)=>ReferenceEquals(a,b)||
            (ReferenceEquals(a,null)&&!ReferenceEquals(b,null)&&b.Destroyed)||
            (ReferenceEquals(b,null)&&!ReferenceEquals(a,null)&&a.Destroyed);
        public static bool operator !=(AssetBundle a,AssetBundle b)=>!(a==b);
        public override bool Equals(object other)=>ReferenceEquals(this,other);
        public override int GetHashCode()=>base.GetHashCode();
    }
}
namespace Whitebox.Foundation
{
    // Only dialog/owner/queue boundaries are recorded by this differential.
    // Actual FlowExecutor integration is exercised by the separate queue harness.
    public interface IOriginalFlowDialog { void Show(); }
    public abstract class OriginalFlowAction
    {
        public virtual int Type => 0;
        public abstract void Execute();
        public abstract void Cancel(OriginalFlowAction action);
        public void Complete() { Harness.Trace.Add("complete"); }
    }
    public sealed class OriginalFlowManager
    {
        public void Push(OriginalFlowAction action) { Harness.Trace.Add("push," + ((Harness.ActionFixture)action).Id); }
        public void Clear(bool alsoRunning) { Harness.Trace.Add("clear,"+(alsoRunning?1:0)); }
    }
    public class OriginalActiveLevelMoveData { public int Number,MoveCount,OriginalMoveCount; }
    public class OriginalSlbMoveConfig { public bool TryGetLevelNameOverride(int level,out int version,out string extension){version=0;extension=null;return false;} }
    public sealed partial class OriginalActiveLevelData
    { public OriginalActiveLevelData(OriginalSlbMoveConfig s,IOriginalThreeColorLevelNames c,object animation){slb=s;threeColor=c;} }
}
public static class Harness
{
    public static readonly List<string> Trace = new List<string>();
    public sealed class ActionFixture : OriginalFlowAction
    { public int Id; public override void Execute() { } public override void Cancel(OriginalFlowAction action) { } }
    private sealed class Context : IOriginalLossFlowContext,IOriginalLavaLevelEndFlow,IOriginalAncientLevelEndFlow,
        IOriginalOutOfMovesBannerContext,IOriginalLeagueOutOfMovesBanner,IOriginalEgoDialogContext,IOriginalEgoDialog,IOriginalOutOfMovesContinuationContext
    {
        public int[] V;
        public Action LavaComplete,AncientComplete;
        private int Missing => V[1];
        private void Record(string value,int missing) { if(Missing==missing)throw new NullReferenceException();Trace.Add(value); }
        public void StopGameMusic(bool pause) => Record("stop,"+(pause?1:0),1);
        public void KingBeSad() { if(Missing==7)throw new NullReferenceException();Record("sad",2); }
        public void EgoShown() => Record("ego",3);
        public void SessionNoPossibleMove() => Record("no-move-session",4);
        public void SessionFail() => Record("fail-session",4);
        public void LevelNoPossibleMove() => Record("no-move-end",5);
        public void LevelEnd() => Record("level-end",5);
        public OriginalFlowManager GetFlowManager(){Trace.Add("flow");return Missing==6?null:new OriginalFlowManager();}
        public void IncreaseTopUiMove() => Record("increase-ui",7);
        public void PlaySound(int sound,float offset) => Record("sound,"+sound+","+offset.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),1);
        public IOriginalLavaLevelEndFlow GetLavaQuest(){Trace.Add("lava-owner");return Missing==8?null:this;}
        public IOriginalAncientLevelEndFlow GetAncientAdventure(){Trace.Add("ancient-owner");return Missing==9?null:this;}
        public bool TryPushLavaLevelEndFlow(bool fail,Action callback){Trace.Add("lava,"+(fail?1:0));LavaComplete=callback;return V[2]!=0;}
        public bool TryPushAncientAdventureLevelEndFlow(Action callback,bool fail,bool quit){Trace.Add("ancient,"+(fail?1:0)+","+(quit?1:0));AncientComplete=callback;return V[3]!=0;}
        public bool ShouldShowExtremelyHardOfferAtFail(){Trace.Add("offer-gate");return V[4]!=0;}
        private OriginalFlowAction Create(int id,string value){Record(value,10);return new ActionFixture{Id=id};}
        public OriginalFlowAction CreateSuperDukeGameplayActionWithoutTutorial()=>Create(1,"new-duke,0");
        public OriginalFlowAction CreateOutOfMovesBannerAction()=>Create(2,"new-banner");
        public OriginalFlowAction CreateNoPossibleMovesDialogAction()=>Create(3,"new-no-move");
        public OriginalFlowAction CreateExtremelyHardOfferAction(int origin,bool reopen)=>Create(4,"new-offer,"+origin+","+(reopen?1:0));
        public OriginalFlowAction CreateFailDialogAction(bool clear)=>Create(5,"new-fail,"+(clear?1:0));
        public OriginalActiveLevelData GetCurrentSession()
        {if(Missing==11)throw new NullReferenceException();Trace.Add("session");return Missing==4?null:new OriginalActiveLevelData(new OriginalSlbMoveConfig(),null){LeagueIndex=V[2]};}
        public bool ShouldUpdateUiForPortalLevel(){Record("portal,"+V[3],12);return V[3]!=0;}
        public bool IsHardOrExtremeLevelBundleAvailable{get{Trace.Add("hard-bundle");return V[4]!=0;}}
        public bool IsLeagueBundleDownloaded{get{Record("league-downloaded",13);return V[5]!=0;}}
        public UnityEngine.AssetBundle TryLoadLeaguePopupBundle(){Record("load-bundle",14);return V[6]!=0?new UnityEngine.AssetBundle{Destroyed=V[6]==2}:null;}
        public IOriginalFlowDialog InstantiateDefaultBanner(OriginalFlowAction action){Record("default",15);return Missing==16?null:this;}
        public IOriginalLeagueOutOfMovesBanner InstantiateLeagueBanner(OriginalFlowAction action){Record("league-banner",15);return Missing==16?null:this;}
        public IOriginalEgoDialog InstantiateDefaultEgoDialog(OriginalFlowAction action){Record("default",15);return Missing==16?null:this;}
        public IOriginalEgoDialog InstantiateLeagueEgoDialog(OriginalFlowAction action){Record("league-banner",15);return Missing==16?null:this;}
        public void Initialize(UnityEngine.AssetBundle bundle)=>Record("initialize",17);
        public void Init(int origin,bool add,float duration)=>Record("init,"+origin+","+(add?1:0)+","+BitConverter.ToInt32(BitConverter.GetBytes(duration),0),17);
        public void Show()=>Record("show",18);
        public void EnableOrientation(int reason)=>Record("orientation,"+reason,1);
        public bool IsLandscape{get{Record("landscape",2);return V[2]!=0;}}
        public void ResetTopUiSorting()=>Record("top-sorting",3);
        public void ResetBottomUiSorting()=>Record("bottom-sorting",4);
        public OriginalFlowAction CreateEgoDialogAction(int origin,bool add,float duration)=>Create(6,"new-ego,"+origin+","+(add?1:0)+","+BitConverter.ToInt32(BitConverter.GetBytes(duration),0));
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var v=Array.ConvertAll(line.Split(','),int.Parse);Trace.Clear();var c=new Context{V=v};bool fault=false;
            try
            {
                var flow=new OriginalLossFlowCoordinator(c);
                if(v[0]==0)flow.StartExtraMoveFlow();else if(v[0]==1)flow.StartEgoFlow();
                else if(v[0]==2)flow.StartNoPossibleMoveFlow();else if(v[0]==3)flow.StartFailFlow();
                else if(v[0]==4)flow.ShowFailDialog();else if(v[0]==5){var a=new OriginalShowOutOfMovesBannerAction(c);Trace.Add("type,"+a.Type);a.Execute();}
                else if(v[0]==6){var a=new OriginalShowEgoDialogAction(c,v[7],v[8]!=0,BitConverter.ToSingle(BitConverter.GetBytes(v[9]),0));Trace.Add("type,"+a.Type);a.Execute();}
                else new OriginalOutOfMovesContinuation(c).PushEgoDialog();
                if(v[0]==3&&v[5]>0){var callback=v[2]!=0?c.LavaComplete:v[3]!=0?c.AncientComplete:null;if(callback!=null)for(int i=0;i<v[5];i++){Trace.Add("callback");callback();}}
            }catch(NullReferenceException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+string.Join(";",Trace));
        }
    }
}
