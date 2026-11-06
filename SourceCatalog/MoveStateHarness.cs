using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine { public static class Debug { public static void LogError(object value) { } } }
namespace Whitebox.Foundation { public sealed class OriginalGridCell { } }
sealed class Touch : IOriginalMoveTouchEvents
{
    Action<OriginalGridCell> tap;
    Action<OriginalGridCell,OriginalGridCell,bool> swap;
    Action<int> complete;
    public event Action<OriginalGridCell> OnTap { add { Verify.Trace.Add("bind,t");tap+=value; } remove { tap-=value; } }
    public event Action<OriginalGridCell,OriginalGridCell,bool> OnSwap { add { Verify.Trace.Add("bind,w");swap+=value; } remove { swap-=value; } }
    public event Action<int> OnMoveCompleted { add { Verify.Trace.Add("bind,c");complete+=value; } remove { complete-=value; } }
    public void Tap(){tap?.Invoke(null);}
    public void Swap(bool valid){swap?.Invoke(null,null,valid);}
    public void Complete(int type){complete?.Invoke(type);}
    public void Clear(){tap=null;swap=null;complete=null;}
}
sealed class Services : IOriginalGameStateServices
{
    public OriginalGameStateManager State;
    public void StartProfilingLevel(){Verify.Trace.Add("profile,"+State.GetGameState());}
    public void ClearFlow(bool exit){Verify.Trace.Add("flow,"+(exit?1:0)+","+State.GetGameState());}
}
static class Verify
{
    public static List<string> Trace=new List<string>();
    static int Bool(bool value)=>value?1:0;
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var parts=line.Split('#');var head=Array.ConvertAll(parts[0].Split(','),int.Parse);
            int frame=0;var services=new Services();var state=new OriginalGameStateManager(()=>frame,()=>head[2]!=0?services:null);
            services.State=state;var moves=new OriginalMoveManager();var touch=new Touch();
            Action<int,int> changed=(left,reason)=>{Trace.Add("move,"+left+","+reason+","+moves.MadeMoves+","+moves.MaxSeconds+","+moves.Seconds);if((head[3]&1)!=0)throw new NullReferenceException();};
            Action<int> seconds=value=>{Trace.Add("second,"+value+","+moves.LeftMoves+","+moves.MadeMoves);if((head[3]&2)!=0)throw new NullReferenceException();};
            Action<int> complete=value=>{Trace.Add("complete,"+value+","+moves.LeftMoves+","+moves.MadeMoves);if((head[3]&4)!=0)throw new NullReferenceException();};
            moves.OnMoveChanged+=changed;moves.OnSecondsChanged+=seconds;moves.OnMoveCompleted+=complete;
            var output=new List<string>();
            foreach(var command in parts[1].Split(';'))
            {
                var p=command.Split(',');int a=p.Length>1?int.Parse(p[1]):0,b=p.Length>2?int.Parse(p[2]):0,c=p.Length>3?int.Parse(p[3]):0;
                Trace.Clear();string result="-";
                try
                {
                    switch(p[0])
                    {
                        case "b":moves.Bind(head[0]!=0?state:null,head[1]!=0?touch:null);break;
                        case "s":moves.SetMovesForStart(a,b,c);break;
                        case "m":moves.SetMaxMoves(a);break;
                        case "i":moves.IncreaseMove(a,b);break;
                        case "r":moves.TriggerMoveChanged(a);break;
                        case "z":moves.SetMaxSeconds(a);break;
                        case "d":moves.DecreaseSecond();break;
                        case "t":touch.Tap();break;
                        case "w":touch.Swap(a!=0);break;
                        case "c":touch.Complete(a);break;
                        case "e":moves.CompleteMoveByType(a);break;
                        case "h":result=Bool(moves.HasMoves()).ToString();break;
                        case "u":result=Bool(moves.CanUserMakeMove()).ToString();break;
                        case "q":moves.Clear(a!=0);break;
                        case "l":moves.OnMoveChanged+=changed;moves.OnSecondsChanged+=seconds;moves.OnMoveCompleted+=complete;break;
                        case "x":touch.Clear();break;
                        case "f":frame=a;break;
                        case "A":state.Operations.OperationStart(a);break;
                        case "R":state.Operations.OperationReset(a);break;
                        case "P":state.StartSpecialOperation();break;
                        case "N":state.FinishSpecialOperation();break;
                        case "J":state.StartShifterSpecialOperation();break;
                        case "K":state.FinishShifterSpecialOperation();break;
                        case "V":state.SetShiftingStatus(a!=0);state.SetShouldShift(b!=0);break;
                        case "C":state.Clear(a!=0);break;
                        case "H":state.OnHomeContextActivated();break;
                        case "B":state.StartLevel();break;
                        case "S":
                            switch(a){case 0:state.SetStateToNoGame();break;case 1:state.SetStateToPlay();break;case 2:state.SetStateToWin();break;case 3:state.SetStateToEgo();break;case 4:state.SetStateToFail();break;case 5:state.SetStateToQuit();break;case 6:state.SetStateToNoPossibleMove();break;}
                            break;
                    }
                }
                catch(NullReferenceException){result="error";}
                output.Add(result+":"+moves.LeftMoves+","+moves.MadeMoves+","+moves.MaxSeconds+","+moves.Seconds+","+state.GetGameState()+","+state.GetSpecialOperationCount()+","+state.GetSpecialOperationCountForShifter()+","+Bool(state.AreCellsBeingShifted())+","+Bool(state.ShouldTryToShiftCells())+","+state.Operations.LastOperationFrame+","+state.Operations.Container.GetCount(6)+","+state.Operations.Container.GetCount(2)+","+Bool(state.IsGameStable())+":"+string.Join(";",Trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
