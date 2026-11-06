using System;using System.Collections.Generic;using System.Globalization;
using Whitebox.Foundation;
namespace Whitebox.Foundation {
 public sealed class OriginalGameOperations {public OriginalGameOperationContainer Container;public void OperationReset(int v){Container.Mask &= ~(1<<v);}}
 public sealed class OriginalGameOperationContainer {public int Mask;}
 public sealed class OriginalGameStateManager {
  public int State=1;public OriginalGameOperations Operations=new OriginalGameOperations{Container=new OriginalGameOperationContainer()};
  public int GetGameState()=>State;public bool IsPlaying()=>State==1;public bool IsWin()=>State==2;public bool IsNoGame()=>State==0;public bool IsQuit()=>State==5;public bool IsEgo()=>State==3;public bool IsFail()=>State==4;public bool IsNoPossibleMove()=>State==6;
  public bool HasOperation(int v)=>(Operations.Container.Mask&(1<<v))!=0;public bool HasAnyOperation()=>Operations.Container.Mask!=0;
  public void SetStateToWin(){State=2;}public void SetStateToEgo(){State=3;}public void SetStateToNoPossibleMove(){State=6;}
 }
 public sealed class OriginalGoalManager {public event Action<int,int> OnGoalUpdated;public bool Complete,Checks;public bool IsAllGoalsReached()=>Complete;public bool AreAllGoalChecksShown()=>Checks;public void Raise(bool v){Complete=v;OnGoalUpdated?.Invoke(11,v?0:1);}}
 public sealed class OriginalMoveManager {public event Action<int,int> OnMoveChanged;public void Raise(int v){OnMoveChanged?.Invoke(v,0);}}
 public sealed class OriginalAutoExplodeManager {public bool IsThereAWaitingMatch;}
}
sealed class HarnessServices:IOriginalWinFailServices {
 public bool Bonus,Fast,Token;public float Time;public List<string> Trace=new List<string>();
 public bool IsBonus=>Bonus;public bool ShouldSpeedUpLevelEnd=>Fast;public bool CanUseMoveTokenInLevel=>Token;public float LevelTime=>Time;
 public void OnWin(){Trace.Add("win");}public void StartWinFlow(){Trace.Add("winflow");}public void TrySendRemainingMoves(){Trace.Add("send");}public void StartExtraMoveFlow(){Trace.Add("extra");}public void StartEgoFlow(){Trace.Add("ego");}public void StartNoPossibleMoveFlow(){Trace.Add("nomove");}public void StartFailFlow(){Trace.Add("fail");}public void SpendMovesOnLevelQuit(){Trace.Add("spend");}public void ReportOperationTimeout(int n,int state){Trace.Add("warn:"+n+":"+state);}public void StopFadeOutSoundSourceCoroutine(){Trace.Add("stopfade");}
}
static class WinFailHarness {
 static void Main(){string line;while((line=Console.ReadLine())!=null){var commands=line.Split(';');var config=commands[0].Split(',');var state=new OriginalGameStateManager{State=int.Parse(config[0])};var goals=new OriginalGoalManager();var moves=new OriginalMoveManager();var auto=new OriginalAutoExplodeManager();var services=new HarnessServices{Bonus=config[1]=="1",Fast=config[2]=="1",Token=config[3]=="1"};var manager=new OriginalWinFailManager(state,goals,moves,auto,services);manager.Bind();
  for(int i=1;i<commands.Length;i++){var v=commands[i].Split(',');switch(v[0]){case "t":services.Time=float.Parse(v[1],CultureInfo.InvariantCulture);state.Operations.Container.Mask=int.Parse(v[2]);auto.IsThereAWaitingMatch=v[3]=="1";goals.Checks=v[4]=="1";manager.ManualUpdate();break;case "m":moves.Raise(int.Parse(v[1]));break;case "g":goals.Raise(v[1]=="1");break;case "s":state.State=int.Parse(v[1]);break;case "c":manager.Clear(true);break;}}
  Console.WriteLine(state.State+","+(manager.IsEnabled?1:0)+","+(manager.DidStartWinFlow?1:0)+","+(manager.IsOutOfMoves?1:0)+","+state.Operations.Container.Mask+"|"+string.Join("/",services.Trace));manager.Release();
 }}
}
