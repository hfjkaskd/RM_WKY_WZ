using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace Whitebox.Foundation
{
    public sealed class OriginalGameStateManager
    {
        public int State=2;
        public void SetStateToNoGame(){GameExitHarness.Trace.Add("nogame");State=0;}
    }
    public interface IOriginalDialogClearer { void ClearDialogs(); }
}
public static class GameExitHarness
{
    public static readonly List<string> Trace=new List<string>();
    private static int missing;
    private sealed class Context:IOriginalGameExitUiContext,IOriginalDialogClearer
    {
        public void SetGameUiActive(bool active){if(missing==4)throw new NullReferenceException();Trace.Add("ui,"+(active?1:0));if(missing==6)throw new InvalidOperationException();}
        public void TryShowLandscapeFlags(bool show,bool animate){Trace.Add("flags,"+(show?1:0)+","+(animate?1:0));if(missing==7)throw new InvalidOperationException();}
        public void PrepareHomeBackground(){Trace.Add("background");if(missing==8)throw new InvalidOperationException();}
        public IOriginalDialogClearer GetDialogManager(){Trace.Add("dialogs");return missing==5?null:this;}
        public void ClearDialogs(){Trace.Add("clear");if(missing==9)throw new InvalidOperationException();}
        public void HideSlidingText(){Trace.Add("hide");if(missing==10)throw new InvalidOperationException();}
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            Trace.Clear();var parts=line.Split(';');var v=Array.ConvertAll(parts[0].Split(','),int.Parse);missing=v[0];int callback=v[7];
            var state=new OriginalGameStateManager();var ui=new OriginalGameExitUi(new Context());
            var decision=new OriginalSceneHomeLoadingDecision(
                ()=>{Trace.Add("story");if(missing==13)throw new NullReferenceException();return v[1]!=0;},
                ()=>{Trace.Add("table");return v[2]!=0;},()=>{Trace.Add("lava");return v[3]!=0;},
                ()=>{Trace.Add("area");if(missing==12)throw new NullReferenceException();return v[4]!=0;})
            {ShouldShowLoadingAccordingToPreviousSceneLoad=v[5]!=0,ForceLoadHomeScene=v[6]!=0};
            var manager=new OriginalGameExitManager(missing==1?null:state,missing==2?null:decision,()=>missing==3?null:ui);
            if(callback!=0)manager.OnGameExit+=()=>{Trace.Add("event,"+(manager.ShouldExitGame?1:0));if(missing==11)throw new InvalidOperationException();if(callback>=2)manager.Clear(callback==3);};
            bool failed=false;
            try{for(int i=1;i<parts.Length;i++)switch(parts[i])
            {case "X":manager.ExitGameWhenPossible();break;case "U":if(missing==3)throw new NullReferenceException();ui.PrepareUiForHomeWhenNoLoadingDisplayed();break;case "L":Trace.Add("loading,"+(decision.WillShowLoadingOnLoadHome()?1:0));break;case "F":manager.ShouldExitGame=true;break;case "C0":manager.Clear(false);break;case "C1":manager.Clear(true);break;}}
            catch(Exception){failed=true;}
            Console.WriteLine((failed?1:0)+"#"+state.State+","+(manager.ShouldExitGame?1:0)+"#"+string.Join(";",Trace));
        }
    }
}
