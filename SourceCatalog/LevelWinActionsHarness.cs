using System;
using System.Collections.Generic;
using Whitebox.Foundation;

public static class LevelWinActionsHarness
{
    private static readonly List<string> trace = new List<string>();
    private sealed class Manager : IOriginalLevelWinGameManager
    {
        public string Name; public int Missing;
        public void ExitGameWhenPossible() { trace.Add("exit"+Name); if(Missing==7)throw new InvalidOperationException(); }
    }
    private sealed class Context : IOriginalLevelWinCloseContext, IOriginalLevelWinAds, IOriginalLevelWinLavaFlow, IOriginalLevelWinAncientFlow
    {
        public int Missing, Lava, Ancient, Immediate; public bool UseB;
        public Action Normal, Gate; public Action<bool> Rewarded;
        public Manager A, B;
        public IOriginalLevelWinAds GetAds() { trace.Add("ads"); return Missing==1?null:this; }
        public void EnableOrientationForStarAnimation()=>trace.Add("star");
        public void EnableBottomPanelOrientation(){if(Missing==2)throw new NullReferenceException();trace.Add("bottom");}
        public IOriginalLevelWinGameManager GetGameManager(){trace.Add("manager");return Missing==3?null:UseB?B:A;}
        public IOriginalLevelWinLavaFlow GetLavaFlow(){trace.Add("getlava");return Missing==4?null:this;}
        public IOriginalLevelWinAncientFlow GetAncientFlow(){trace.Add("getancient");return Missing==5?null:this;}
        public void CloseDialog()=>trace.Add("close");
        public void TryShowAfterLevelWin(Action completed){trace.Add("normal");Normal=completed;if(Immediate!=0)completed();}
        public void TryShowForCoin(Action<bool> completed){trace.Add("rewarded");Rewarded=completed;if(Immediate!=0)completed(Immediate==1);}
        public bool TryPushLevelEndFlow(int state,Action completed){trace.Add("lava,"+state);Gate=completed;if(Missing==8)throw new InvalidOperationException();return Lava!=0;}
        public bool TryPushLevelEndFlow(Action completed,bool didFail,bool didWin){trace.Add("ancient,"+(didFail?1:0)+","+(didWin?1:0));Gate=completed;if(Missing==9)throw new InvalidOperationException();return Ancient!=0;}
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            trace.Clear();var parts=line.Split(';');var v=Array.ConvertAll(parts[0].Split(','),int.Parse);
            var context=new Context{Missing=v[0],Lava=v[1],Ancient=v[2],Immediate=v[3],A=new Manager{Name="A",Missing=v[0]},B=new Manager{Name="B",Missing=v[0]}};
            var model=new OriginalLevelWinActions(context);bool failed=false;
            try
            {
                for(int i=1;i<parts.Length;i++)switch(parts[i])
                {
                    case "C":model.OnContinueButton();break;
                    case "R":model.OnCoinRewardedAdButtonClicked();break;
                    case "X":model.OnClosePressed();break;
                    case "T":model.TryCloseDialog();break;
                    case "N":context.Normal?.Invoke();break;
                    case "Y":context.Rewarded?.Invoke(true);break;
                    case "Z":context.Rewarded?.Invoke(false);break;
                    case "V":context.Gate?.Invoke();break;
                    case "M":context.UseB=true;break;
                }
            }
            catch(Exception){failed=true;}
            Console.WriteLine((failed?1:0)+"#"+(model.CallToActionClicked?1:0)+"#"+string.Join(";",trace));
        }
    }
}
