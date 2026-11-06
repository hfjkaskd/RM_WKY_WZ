using System;
using Whitebox.Foundation;
namespace UnityEngine
{
    public class AssetBundle { }
    public static class Debug { public static void LogError(object value){throw new Exception("Unexpected operation error: "+value);} }
}
namespace Whitebox.Foundation
{
    // Board goal/auto models, UiDialog rendering, application/event owners and
    // the already native-verified DisableCounter remain explicit fixtures.
    public interface IOriginalFlowDialog { void Show(); }
    public sealed class OriginalGridCell { }
    public sealed class OriginalGoalManager
    { public event Action<int,int> OnGoalUpdated; public bool IsAllGoalsReached()=>false;public bool AreAllGoalChecksShown()=>false; }
    public sealed class OriginalAutoExplodeManager { public bool IsThereAWaitingMatch=>false; }
    public sealed class OriginalDisableCounter
    {private int count;public bool IsEnabled=>count>0;public void Enable(){count=unchecked(count+1);}public void Disable(){if(count!=0)count=unchecked(count-1);}public void Reset(){count=0;}}
    public class OriginalActiveLevelMoveData { public int Number,MoveCount,OriginalMoveCount; }
    public class OriginalSlbMoveConfig { public bool TryGetLevelNameOverride(int n,out int v,out string e){v=0;e=null;return false;} }
    public sealed partial class OriginalActiveLevelData
    {public OriginalActiveLevelData(OriginalSlbMoveConfig s,IOriginalThreeColorLevelNames c,object a){slb=s;threeColor=c;}}
}
public static class Harness
{
    private static void Require(bool value,string detail){if(!value)throw new Exception(detail);}
    private sealed class Touches:IOriginalMoveTouchEvents
    {
        public event Action<OriginalGridCell> OnTap;
        public event Action<OriginalGridCell,OriginalGridCell,bool> OnSwap;
        public event Action<int> OnMoveCompleted;
        public void AcceptSwap()=>OnSwap?.Invoke(null,null,true);
    }
    private sealed class Sentinel:OriginalFlowAction
    {public bool Executed;public override void Execute(){Executed=true;}public override void Cancel(OriginalFlowAction action){}}
    private sealed class EndDialog:OriginalFlowAction
    {public int Shows;public override int Type=>2;public override void Execute(){Shows++;}public override void Cancel(OriginalFlowAction action){}}
    private sealed class Context:IOriginalGameStateServices,IOriginalFlowExecutorContext,IOriginalWinFailServices,
        IOriginalLossFlowContext,IOriginalLavaLevelEndFlow,IOriginalAncientLevelEndFlow,
        IOriginalOutOfMovesBannerContext,IOriginalFlowDialog,IOriginalEgoDialogContext,IOriginalEgoDialog,IOriginalOutOfMovesContinuationContext
    {
        public readonly OriginalFlowManager Flow;
        public readonly OriginalLossFlowCoordinator Loss;
        public readonly OriginalGameStateManager State;
        public readonly OriginalMoveManager Moves=new OriginalMoveManager();
        public readonly OriginalWinFailManager WinFail;
        public readonly Touches Touch=new Touches();
        public readonly OriginalActiveLevelData Session=new OriginalActiveLevelData(new OriginalSlbMoveConfig(),null);
        public readonly EndDialog FailDialog=new EndDialog(),NoMovesDialog=new EndDialog();
        public float Time;
        public int EgoStarts,SessionFails,LevelEnds,NoMoveSessions,NoMoveEnds,BannerShows,EgoShows,Orientation,TopReset,BottomReset;
        public bool LavaAccept,AncientAccept,Landscape,MusicPaused;
        public int MusicStops,KingSad;
        public Action LavaComplete,AncientComplete;
        public OriginalFlowAction BannerAction,EgoAction;
        public Context()
        {
            Flow=new OriginalFlowManager(this);Loss=new OriginalLossFlowCoordinator(this);
            State=new OriginalGameStateManager(()=>1,()=>this);Moves.Bind(State,Touch);
            WinFail=new OriginalWinFailManager(State,new OriginalGoalManager(),Moves,new OriginalAutoExplodeManager(),this);
            WinFail.Bind();Moves.SetMovesForStart(1,1,0);State.StartLevel();
        }
        public void StartProfilingLevel(){}public void ClearFlow(bool exit)=>Flow.Clear(exit);
        public bool ShouldDelayAutoActions()=>false;public bool IsInDelayedActions(OriginalFlowAction a)=>false;
        public void DelayAutoAction(OriginalFlowAction a)=>throw new Exception("No auto fixture action");
        public bool IsBonus=>false;public bool ShouldSpeedUpLevelEnd=>true;public bool CanUseMoveTokenInLevel=>false;public float LevelTime=>Time;
        public void OnWin()=>throw new Exception("No victory in this fixture");public void StartWinFlow()=>throw new Exception("No victory in this fixture");
        public void TrySendRemainingMoves()=>throw new Exception("No victory in this fixture");public void StartExtraMoveFlow()=>Loss.StartExtraMoveFlow();
        public void StartEgoFlow(){EgoStarts++;Loss.StartEgoFlow();}public void StartNoPossibleMoveFlow()=>Loss.StartNoPossibleMoveFlow();public void StartFailFlow()=>Loss.StartFailFlow();
        public void SpendMovesOnLevelQuit()=>throw new Exception("No quit fixture");public void ReportOperationTimeout(int c,int s)=>throw new Exception("No timeout fixture");
        public void StopFadeOutSoundSourceCoroutine(){}
        public void StopGameMusic(bool pause){MusicStops++;MusicPaused=pause;}public void KingBeSad(){KingSad++;}public void EgoShown(){}
        public void SessionNoPossibleMove(){NoMoveSessions++;}public void SessionFail(){SessionFails++;}public void LevelNoPossibleMove(){NoMoveEnds++;}public void LevelEnd(){LevelEnds++;}
        public OriginalFlowManager GetFlowManager()=>Flow;public void IncreaseTopUiMove()=>throw new Exception("No SuperDuke fixture");
        public void PlaySound(int id,float offset){Require(id==212&&offset==.04f,"Native fail sound");}
        public IOriginalLavaLevelEndFlow GetLavaQuest()=>this;public IOriginalAncientLevelEndFlow GetAncientAdventure()=>this;
        public bool TryPushLavaLevelEndFlow(bool fail,Action callback){Require(fail,"Native lava fail flag");LavaComplete=callback;return LavaAccept;}
        public bool TryPushAncientAdventureLevelEndFlow(Action callback,bool fail,bool quit){Require(fail&&!quit,"Native ancient fail/quit flags");AncientComplete=callback;return AncientAccept;}
        public bool ShouldShowExtremelyHardOfferAtFail()=>false;
        public OriginalFlowAction CreateSuperDukeGameplayActionWithoutTutorial()=>throw new Exception("No SuperDuke fixture");
        public OriginalFlowAction CreateOutOfMovesBannerAction(){BannerAction=new OriginalShowOutOfMovesBannerAction(this);return BannerAction;}
        public OriginalFlowAction CreateNoPossibleMovesDialogAction()=>NoMovesDialog;
        public OriginalFlowAction CreateExtremelyHardOfferAction(int origin,bool reopen)=>throw new Exception("No offer fixture");
        public OriginalFlowAction CreateFailDialogAction(bool clear){Require(clear,"Native clear selection flag");return FailDialog;}
        public OriginalActiveLevelData GetCurrentSession()=>Session;public bool ShouldUpdateUiForPortalLevel()=>false;
        public bool IsHardOrExtremeLevelBundleAvailable=>false;public bool IsLeagueBundleDownloaded=>false;
        public UnityEngine.AssetBundle TryLoadLeaguePopupBundle()=>throw new Exception("No league fixture");
        public IOriginalFlowDialog InstantiateDefaultBanner(OriginalFlowAction a){Require(a==BannerAction,"Banner action identity");return this;}
        public IOriginalLeagueOutOfMovesBanner InstantiateLeagueBanner(OriginalFlowAction a)=>throw new Exception("No league fixture");
        public IOriginalEgoDialog InstantiateDefaultEgoDialog(OriginalFlowAction a){Require(a==EgoAction,"Ego action identity");return this;}
        public IOriginalEgoDialog InstantiateLeagueEgoDialog(OriginalFlowAction a)=>throw new Exception("No league fixture");
        public void Init(int origin,bool add,float fade){Require(origin==10&&!add&&fade==.2f,"Native banner-to-Ego parameters");}
        public void Show(){if(Flow.Main.GetRunningAction()==BannerAction)BannerShows++;else if(Flow.Main.GetRunningAction()==EgoAction)EgoShows++;else throw new Exception("Unexpected dialog action");}
        public void EnableOrientation(int reason){Require(reason==35,"Native orientation reason");Orientation++;}
        public bool IsLandscape=>Landscape;public void ResetTopUiSorting(){TopReset++;}public void ResetBottomUiSorting(){BottomReset++;}
        public OriginalFlowAction CreateEgoDialogAction(int origin,bool add,float fade){EgoAction=new OriginalShowEgoDialogAction(this,origin,add,fade);return EgoAction;}
    }
    public static void Main()
    {
        for(int landscape=0;landscape<2;landscape++)
        {
            var c=new Context{Landscape=landscape!=0};Require(c.Moves.CanUserMakeMove(),"Fresh move accepted");
            c.State.Operations.OperationStart(6);c.Touch.AcceptSwap();Require(c.Moves.LeftMoves==0&&c.Moves.MadeMoves==1&&!c.Moves.CanUserMakeMove(),"Actual swap spends last move and blocks input");
            c.Time=1;c.WinFail.ManualUpdate();Require(c.EgoStarts==0,"Pending board operation blocks Ego");
            c.State.Operations.OperationFinish(6);c.Time=1.19f;c.WinFail.ManualUpdate();Require(c.EgoStarts==0,"Native .2s stable gate");
            c.Time=1.21f;c.WinFail.ManualUpdate();Require(c.State.IsEgo()&&!c.WinFail.IsEnabled&&c.EgoStarts==1&&c.BannerShows==1&&c.SessionFails==0&&c.MusicPaused&&c.KingSad==1,"Out of moves enters actual banner before failure persistence");
            var stale=new Sentinel();c.Flow.Push(stale);Require(c.Flow.Main.PendingCount==1,"Pending prior action");
            new OriginalOutOfMovesContinuation(c).PushEgoDialog();
            Require(c.Flow.Main.GetRunningAction()==c.BannerAction&&c.Flow.Main.PendingCount==1&&c.EgoShows==0&&!stale.Executed,"Clear(false) discards stale queue and retains running banner; Ego waits");
            Require(c.Orientation==1&&c.TopReset==landscape&&c.BottomReset==landscape,"Orientation and landscape sorting");
            // Explicit UiDialog close completion, not a restored animation/timer.
            c.BannerAction.Complete();Require(c.EgoShows==1&&c.Flow.Main.GetRunningAction()==c.EgoAction&&!stale.Executed,"Real executor advances banner completion to native Ego action");
            c.EgoAction.Complete();Require(c.Flow.Main.GetRunningAction()==null,"Dialog owner completion releases queue");c.WinFail.Release();
        }
        for(int gate=0;gate<3;gate++)
        {
            var c=new Context{LavaAccept=gate==1,AncientAccept=gate==2};c.State.SetStateToFail();c.Time=1;c.WinFail.ManualUpdate();
            Require(c.SessionFails==1&&c.LevelEnds==1&&c.MusicStops==1&&!c.MusicPaused,"Actual fail loop dispatches session/end once");
            if(gate>0){Require(c.FailDialog.Shows==0,"Event-owned fail popup stays deferred");(gate==1?c.LavaComplete:c.AncientComplete)();}
            Require(c.FailDialog.Shows==1&&c.Flow.Main.GetRunningAction()==c.FailDialog,"Immediate or deferred fail action reaches real queue");
            c.Time=2;c.WinFail.ManualUpdate();Require(c.SessionFails==1,"Disabled manager does not repeat failure");c.WinFail.Release();
        }
        var noMove=new Context();noMove.WinFail.OnNoPossibleMove();noMove.Time=1;noMove.WinFail.ManualUpdate();
        Require(noMove.State.IsNoPossibleMove()&&noMove.NoMoveSessions==1&&noMove.NoMoveEnds==1&&noMove.NoMovesDialog.Shows==1,"No possible move uses its separate end path");
        Console.WriteLine("passed:6");
    }
}
