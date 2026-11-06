using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public interface IOriginalGameContextUnit { void Clear(bool gameExit); }
    public sealed class OriginalGameExitManager { public bool ShouldExitGame; }
}
public static class GameContextExitHarness
{
    private static readonly List<string> trace=new List<string>();
    private sealed class Unit:IOriginalGameContextUnit
    {
        public int Index,Count;public Context Owner;
        public void Clear(bool gameExit)
        {
            Count++;trace.Add("clear,"+Index+","+(gameExit?1:0));
            if(Index==1)Owner.Manager.ShouldExitGame=false;
            if(Index!=0)return;
            if(Owner.Missing==4||Owner.UnitMode==3)throw new InvalidOperationException();
            if(Owner.UnitMode==1)Owner.Original[1]=null;
            if(Owner.UnitMode==2)Owner.Current=Owner.Alternate;
        }
    }
    private sealed class Context:IOriginalGameContextDeactivationServices,IOriginalGameContextItemCleanup,IOriginalGameContextTouchEnabler,IOriginalGameContextSceneLoader
    {
        public int Missing,UnitMode;public bool Low,Early,Played;
        public OriginalGameContextRuntimeState State;public OriginalGameExitManager Manager;
        public IOriginalGameContextUnit[] Original,Alternate,Current;
        private void Step(string value,int fail){trace.Add(value);if(Missing==fail)throw new InvalidOperationException();}
        public void ManualUpdateGameUnits()=>trace.Add("update");
        public void RemoveOrientationChangeListener(OriginalGameContextDeactivation owner)=>Step("orientation-",24);
        public IOriginalGameContextUnit[] GetGameUnits(){if(Missing==2)throw new NullReferenceException();return Missing==3?null:Current;}
        public void KillAllTweens(bool complete)=>Step("tweens,"+(complete?1:0),23);
        public void StopAllManualTweenCoroutines(){if(Missing==5)throw new NullReferenceException();trace.Add("stop-mtween");}
        public void KillGameCoroutines(){if(Missing==6)throw new NullReferenceException();trace.Add("kill-coroutines");}
        public IOriginalGameContextItemCleanup GetItemFactory()=>Missing==7?null:this;
        public void RecycleAllSpawned()=>trace.Add("recycle");
        public void ClearPools()=>Step("clear-pools",15);
        public void StopAllSounds(){if(Missing==8)throw new NullReferenceException();trace.Add("sound");}
        public void ResetCurrentCameraOrientationLock(){if(Missing==9)throw new NullReferenceException();trace.Add("camera");}
        public void KillAllAnimatorUpdates()=>Step("animators",22);
        public IOriginalGameContextTouchEnabler GetUiTouchEnabler()=>Missing==10?null:this;
        public void EnablePinch(object owner)=>Step("pinch",11);
        public void EnableVerticalScroll(object owner)=>Step("vertical",12);
        public void EnableHorizontalScroll(object owner)=>Step("horizontal",13);
        public bool IsLowEnd{get{Step("low",14);return Low;}}
        public bool IsEarlySideProgressionLevel{get{Step("early",16);return Early;}}
        public bool IsSideProgressionLevelPlayed(){if(Missing==17)throw new NullReferenceException();trace.Add("played");return Played;}
        public void ReloadLevel(){if(Missing==18)throw new NullReferenceException();trace.Add("reload");}
        public IOriginalGameContextSceneLoader GetSceneManager()=>Missing==19?null:this;
        public void LoadHomeScene()=>trace.Add("home");public void LoadGameScene()=>trace.Add("game");
        public void SendGetMessages(object request){if(Missing==20)throw new NullReferenceException();Step("messages,null",21);}
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            trace.Clear();var parts=line.Split(';');var v=Array.ConvertAll(parts[0].Split(','),int.Parse);
            var state=new OriginalGameContextRuntimeState{Controller=new object(),IsActive=v[6]!=0};
            var manager=new OriginalGameExitManager{ShouldExitGame=v[7]!=0};var late=new OriginalGameLateUpdateDispatcher();int lateCalls=0;late.RegisterToLateUpdate(()=>lateCalls++);
            var context=new Context{Missing=v[0],Low=v[1]!=0,Early=v[2]!=0,Played=v[3]!=0,UnitMode=v[5],State=state,Manager=manager};
            var units=new[]{new Unit{Index=0,Owner=context},new Unit{Index=1,Owner=context},new Unit{Index=2,Owner=context},new Unit{Index=3,Owner=context}};
            context.Original=new IOriginalGameContextUnit[]{units[0],units[1],units[2]};context.Alternate=new IOriginalGameContextUnit[]{units[3],null,null};context.Current=context.Original;
            var model=new OriginalGameContextDeactivation(state,v[0]==1?null:manager,late,context);
            if(v[4]!=0)model.OnDeactivate+=()=>{trace.Add("event,"+(state.IsActive?1:0));if(v[4]==2)throw new InvalidOperationException();if(v[4]==3)state.IsActive=true;if(v[4]==4)state.Controller=null;if(v[4]==5)context.Current=context.Alternate;};
            bool failed=false;
            try{for(int i=1;i<parts.Length;i++)switch(parts[i])
            {case "U":model.ManualUpdate();break;case "R0":model.ResetScene(false);break;case "R1":model.ResetScene(true);break;case "K":model.KillAllAnimations();break;case "A0":state.IsActive=false;break;case "A1":state.IsActive=true;break;case "F0":manager.ShouldExitGame=false;break;case "F1":manager.ShouldExitGame=true;break;}}
            catch(Exception){failed=true;}
            late.ManualLateUpdate();
            Console.WriteLine((failed?1:0)+"#"+(state.IsActive?1:0)+","+(state.Controller==null?0:1)+","+lateCalls+","+(manager.ShouldExitGame?1:0)+","+units[0].Count+","+units[1].Count+","+units[2].Count+","+units[3].Count+"#"+string.Join(";",trace));
        }
    }
}
