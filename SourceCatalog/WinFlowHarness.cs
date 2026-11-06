using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine
{
    public class Coroutine { }
    public class AssetBundle { }
    public class WaitForSeconds { public WaitForSeconds(float seconds){Harness.Trace.Add("wait,"+seconds.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));} }
}
namespace Whitebox.Foundation
{
    public class OriginalActiveLevelMoveData { public int Number,MoveCount,OriginalMoveCount; }
    public class OriginalSlbMoveConfig { public bool TryGetLevelNameOverride(int level,out int version,out string extension){version=0;extension=null;return false;} }
    public sealed partial class OriginalActiveLevelData
    { public bool IsUsefulMoveMade; public OriginalActiveLevelData(OriginalSlbMoveConfig s,IOriginalThreeColorLevelNames c,object animation){slb=s;threeColor=c;} }
    public abstract class OriginalFlowAction
    {
        public virtual int Type=>0;public abstract void Execute();public abstract void Cancel(OriginalFlowAction action);
        public virtual void Complete(){Harness.Trace.Add("complete");}
    }
    public sealed class OriginalFlowManager { public void Push(OriginalFlowAction action){Harness.Trace.Add("push,"+((Harness.FixtureAction)action).Id);} }
    public sealed class OriginalRemainingMovesManager
    {
        public bool Send;public bool ShouldSendMoves(){Harness.Trace.Add("should-send");return Send;}
        public void CompleteRemainingMovesImmediately(){Harness.Trace.Add("immediate");}
    }
}
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    public sealed class FixtureAction:OriginalFlowAction
    {public int Id;public override void Execute(){}public override void Cancel(OriginalFlowAction action){}}
    private sealed class Context:IOriginalWinFlowContext,IOriginalLevelEndContext,IOriginalWinLogoActionContext,IOriginalRemainingMovesActionContext,IOriginalLevelWinActionContext,IOriginalWinLogoDialog,IOriginalLevelWinDialog
    {
        public int[] V;public int SpeedReads;public IEnumerator Routine;
        private int Missing=>V[4];
        public bool IsBonus {get{if(Missing==1)throw new NullReferenceException();return V[1]!=0;}}
        public bool ShouldSpeedUpLevelEnd {get{int value=SpeedReads++==0?V[2]:V[3];Trace.Add("speed,"+value);return value!=0;}}
        public void MultiplierTokenOnStoryOrBonusLevelWin(){if(Missing==2)throw new NullReferenceException();Trace.Add("token");}
        public void KingBeHappy(){if(Missing==3)throw new NullReferenceException();Trace.Add("happy");}
        public int GetGoalLeftCount(int type,bool final){if(Missing==4)throw new NullReferenceException();Trace.Add("goal,"+type+","+(final?1:0));return V[5];}
        public void UpdateReactivationProgressAfterWin(){if(Missing==5)throw new NullReferenceException();Trace.Add("reactivation");}
        public void SessionWin(int coins){if(Missing==6)throw new NullReferenceException();Trace.Add("session,"+coins);}
        public void LevelEnd(){if(Missing==7)throw new NullReferenceException();Trace.Add("level-end");}
        private sealed class Empty:IEnumerator {public object Current=>null;public bool MoveNext()=>false;public void Reset(){}}
        public IEnumerator FadeOutSoundSource(){if(Missing==8)throw new NullReferenceException();Trace.Add("fade");return new Empty();}
        public UnityEngine.Coroutine ManualStartCoroutine(IEnumerator routine){Trace.Add("game-coroutine");return new UnityEngine.Coroutine();}
        public OriginalFlowManager GetFlowManager(){Trace.Add("flow-owner");return Missing==9?null:new OriginalFlowManager();}
        private OriginalFlowAction Create(int id,int missing){if(Missing==missing)throw new NullReferenceException();Trace.Add("new,"+id);return new FixtureAction{Id=id};}
        public OriginalFlowAction CreateWinLogoAction()=>Create(1,10);
        public OriginalFlowAction CreateRemainingMovesAction()=>Create(2,11);
        public OriginalFlowAction CreateLevelWinAction()=>Create(3,12);
        public OriginalActiveLevelData GetCurrentSession()
        {
            if(Missing==1)throw new NullReferenceException();
            var s=new OriginalActiveLevelData(new OriginalSlbMoveConfig(),null){IsAdmin=V[1]!=0,IsUsefulMoveMade=V[2]!=0,State=V[3],LeagueIndex=V[1],Difficulty=(sbyte)V[5]};return s;
        }
        public void IncreaseUselessMoveCount(OriginalActiveLevelData session){Trace.Add("useless");}
        public void TryDownloadEpisodeLevelBundle(){Trace.Add("download");}
        public void ResetPrelevelTutorial(){if(Missing==2)throw new NullReferenceException();Trace.Add("tutorial");}
        public void MultiplierSelectionOnLevelWin(){if(Missing==3)throw new NullReferenceException();Trace.Add("multiplier");}
        public void SendLevelEndAnalytics(){if(Missing==4)throw new NullReferenceException();Trace.Add("analytics");}
        public void UserLevelEnd(){if(Missing==5)throw new NullReferenceException();Trace.Add("user-end");}
        public float LogoBeforeShowDelay=>.1f;public float LogoBeforePlayDelay=>.2f;
        public void DisableUiTouch(){if(Missing==1)throw new NullReferenceException();Trace.Add("disable");}
        public void EnableUiTouch(){Trace.Add("enable");}
        public UnityEngine.Coroutine StartApplicationCoroutine(IEnumerator routine){Routine=routine;return null;}
        public IOriginalWinLogoDialog InstantiateWinLogoDialog(OriginalFlowAction action)
        {if(Missing==2||Missing==3)throw new NullReferenceException();Trace.Add("instantiate-logo");return Missing==5?null:this;}
        public void PlayWinLogoAnimation(){Trace.Add("play");}
        public OriginalRemainingMovesManager GetRemainingMovesManager(){if(Missing==1)throw new NullReferenceException();Trace.Add("remaining-owner");return new OriginalRemainingMovesManager{Send=V[1]!=0};}
        public IOriginalFlowDialog InstantiateRemainingMovesDialog(OriginalFlowAction action){if(Missing==2||Missing==3)throw new NullReferenceException();Trace.Add("instantiate-remaining");return Missing==5?null:this;}
        public void ResetTopUiSorting(){if((V[0]==4&&Missing==2)||(V[0]==3&&Missing==4))throw new NullReferenceException();Trace.Add("sorting");}
        public IOriginalFlowDialog InstantiateDefaultWinDialog(OriginalFlowAction action){Trace.Add("instantiate-default");return Missing==3?null:this;}
        public IOriginalLevelWinDialog InstantiateLeagueWinDialog(OriginalFlowAction action){Trace.Add("instantiate-league");return Missing==4?null:this;}
        public bool IsHardOrExtremeLevelBundleAvailable{get{Trace.Add("hard-bundle");return V[2]!=0;}}
        public bool IsLeagueBundleDownloaded{get{if(Missing==5)throw new NullReferenceException();Trace.Add("league-downloaded");return V[3]!=0;}}
        public UnityEngine.AssetBundle TryLoadLeaguePopupBundle(){Trace.Add("popup-bundle");return null;}
        public void InjectBundle(UnityEngine.AssetBundle bundle,sbyte difficulty){Trace.Add("inject,"+difficulty);}
        public void Show(){Trace.Add("show");}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            int[] v=Array.ConvertAll(line.Split(','),int.Parse);Trace.Clear();bool fault=false;var ctx=new Context{V=v};
            try
            {
                if(v[0]==0)new OriginalWinFlowCoordinator(ctx).StartWinFlow();
                else if(v[0]==1)OriginalLevelEnd.End(ctx);
                else if(v[0]==2){new OriginalShowWinLogoDialogAction(ctx).Execute();while(ctx.Routine.MoveNext()){} }
                else if(v[0]==3)new OriginalRemainingMovesDialogAction(ctx).Execute();
                else new OriginalShowLevelWinDialogAction(ctx).Execute();
            }catch(NullReferenceException){fault=true;}
            Console.WriteLine((fault?1:0)+"#"+string.Join(";",Trace));
        }
    }
}
