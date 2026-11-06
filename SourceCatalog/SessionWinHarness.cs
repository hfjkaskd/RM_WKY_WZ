using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;
namespace Whitebox.Foundation {
 public sealed class OriginalMoveManager{public int LeftMoves,MadeMoves,Seconds;}
 public sealed partial class OriginalActiveLevelData{
  public int Number,LeagueIndex,StoryLevel,State=2,LeftMoves=-9,MadeMoves=-8;public bool IsAdmin,IsTableGame,WonAnyLevel;public sbyte Difficulty;
  public int ExtraCoins{get;private set;}public OriginalBeforeAfterData Star,Cloche,SuperBooster,SuperDuke,MagicDuke;
  public OriginalActiveLevelAnimationValues AnimationValues;public OriginalActiveLevelData(int extra){ExtraCoins=extra;}
 }
 public sealed class OriginalActiveLevelAnimationValues{public OriginalBeforeAfterData Coin,CompletedNThousandLevelCoin;public OriginalLevelStartSnapshots StartSnapshots;}
 public class OriginalLevelStartSnapshots{
  public OriginalBeforeAfterData LeagueCrown,KingsCup,TeamBattle,RoyalPass,WeeklyPass,PinataParty,TeamTreasure,SpaceMission,BalloonRise,SkyRace,HiddenTemple,DragonNest,TeamTournament,MagicCauldron,ArcheryArena,DukesFortune,TrainJourney,OceanOdyssey,PuzzleBreak,RobotRivals,RoyalWorldCup,MergeSmith,ShortExpeditionEvent;
 }
 public class OriginalRoyalPassLevelData:OriginalBeforeAfterData{public bool IsEventValid(){throw new InvalidOperationException();}}
 public class OriginalPinataPartyLevelData:OriginalBeforeAfterData{public bool IsEventValid(){throw new InvalidOperationException();}}
}
public static class Harness{
 public static readonly List<string> Trace=new List<string>();
 private sealed class Coin:OriginalBeforeAfterData{public override void Init(int n){Trace.Add("init,"+n);base.Init(n);}}
 private sealed class Context:IOriginalSessionWinContext,IOriginalWinSuperBooster,IOriginalWinSuperDuke,IOriginalWinMagicDuke,IOriginalWinInventory{
  public int[] V;public bool Flag(int n){Trace.Add("q"+n);return (V[14]&(1<<n))!=0;}
  private void Check(int actor){if(V[22]==actor)throw new NullReferenceException();}
  public IOriginalWinInventory GetInventory(){Trace.Add("inventory-owner");return V[22]==-1?null:this;}
  public int Coin{get{Trace.Add("coin");return V[10];}}
  public int PersistentLevelNumber{get{Check(-3);Trace.Add("persistent");return V[12];}}
  public int GetNThousandLevelCoin(int level){Trace.Add("milestone,"+level);Check(-2);return V[11];}
  public bool IsWaterLevel{get{Trace.Add("water");return V[13]!=0;}}
  public bool KingsCupIsInGroup{get{Check(7);return Flag(0);}}
  public bool TeamBattleIsInGroup{get{Check(8);return Flag(1);}}
  public bool WeeklyPassShouldCollect{get{Check(65);return Flag(3);}}
  public bool TeamTreasureShouldProgress{get{Check(16);return Flag(5);}}
  public bool SpaceMissionShouldProgress{get{Check(26);return Flag(6);}}
  public bool BalloonRiseShouldProgress{get{Check(57);return Flag(7);}}
  public bool SkyRaceIsContinue{get{Check(13);return Flag(8);}}
  public bool SkyRaceIsInGroup{get{Check(13);return Flag(9);}}
  public bool HiddenTempleShouldIncrease{get{Check(35);return Flag(10);}}
  public bool DragonNestShouldIncrease{get{Check(41);return Flag(11);}}
  public bool TeamTournamentShouldProgress{get{Check(49);return Flag(12);}}
  public bool MagicCauldronShouldIncrease{get{Check(38);return Flag(13);}}
  public bool ArcheryArenaShouldProgress{get{Check(45);return Flag(14);}}
  public bool DukesFortuneShouldIncrease{get{Check(46);return Flag(15);}}
  public bool TrainJourneyShouldIncrease{get{Check(58);return Flag(16);}}
  public bool OceanOdysseyShouldIncrease{get{Check(59);return Flag(17);}}
  public bool PuzzleBreakShouldCollect{get{Check(64);return Flag(18);}}
  public bool RobotRivalsShouldIncrease{get{Check(72);return Flag(19);}}
  public bool RoyalWorldCupShouldProgress{get{Check(77);return Flag(20);}}
  public bool MergeSmithShouldCollect{get{Check(73);return Flag(21);}}
  public bool ShortExpeditionShouldCollect{get{Check(80);return Flag(22);}}
  public int GetArcheryCurrentMultiplier(){Trace.Add("archery-mult");return V[15];}
  public int GetRoyalWorldCupQualifierMultiplier(){Trace.Add("world-mult");return V[16];}
  public void UpdateCloche(OriginalBeforeAfterData data){Check(31);Trace.Add("cloche,"+(data==null?-999:data.After));}
  public IOriginalWinSuperBooster GetSuperBooster(){Trace.Add("get-super");return V[22]==29?null:this;}
  public IOriginalWinSuperDuke GetSuperDuke(){Trace.Add("get-duke");return V[22]==50?null:this;}
  public IOriginalWinMagicDuke GetMagicDuke(){Trace.Add("get-magic");return V[22]==63?null:this;}
  public bool HasShowedTutorial=>Flag(23);public bool HasShowedTagTutorial=>Flag(25);public bool HasShowedHeaderTutorial=>Flag(26);
  public int GetUnlockLevel(bool shown){Trace.Add("unlock,"+(shown?1:0));return shown?V[18]:V[17];}
  public bool IsFeatureActive()=>Flag(24);
  void IOriginalWinSuperBooster.IncrementWinCount(){Trace.Add("super-win");}
  public bool CanUpdateProgress(bool win){Trace.Add("duke-can,"+(win?1:0));return (V[14]&(1<<27))!=0;}
  void IOriginalWinSuperDuke.IncrementWinCount(){Trace.Add("duke-win");}
  public bool IsFeatureReady()=>Flag(28);
  public int MaxProgress{get{Trace.Add("magic-max");return V[19];}}
  public void UpdateProgress(int progress){Trace.Add("magic-progress,"+progress);}
 }
 private sealed class Rewards:IOriginalSessionWinRewards{
  public Context Context;
  public bool IsRoyalPassEventValid(){if(Context.V[23]==8)throw new NullReferenceException();return Context.Flag(2);}
  public bool IsPinataEventValid(){if(Context.V[23]==9)throw new NullReferenceException();return Context.Flag(4);}
  public void AddLeagueCrown(sbyte difficulty){Trace.Add("r0"+","+difficulty);}
  public void AddKingsCup(sbyte difficulty){Trace.Add("r1"+","+difficulty);}
  public void AddTeamBattle(sbyte difficulty){Trace.Add("r2"+","+difficulty);}
  public void AddRoyalPass(sbyte difficulty){Trace.Add("r3"+","+difficulty);}
  public void AddWeeklyPass(sbyte difficulty){Trace.Add("r4"+","+difficulty);}
  public void AddPinata(sbyte difficulty){Trace.Add("r5"+","+difficulty);}
  public void AddTeamTreasure(sbyte difficulty){Trace.Add("r6"+","+difficulty);}
  public void AddSpaceMission(){Trace.Add("r7");}
  public void AddBalloonRise(sbyte difficulty){Trace.Add("r8"+","+difficulty);}
  public void AddSkyRace(){Trace.Add("r9");}
  public void AddHiddenTemple(sbyte difficulty){Trace.Add("r10"+","+difficulty);}
  public void AddDragonNest(sbyte difficulty){Trace.Add("r11"+","+difficulty);}
  public void AddTeamTournament(sbyte difficulty){Trace.Add("r12"+","+difficulty);}
  public void AddMagicCauldron(sbyte difficulty){Trace.Add("r13"+","+difficulty);}
  public void AddArcheryArena(sbyte difficulty,int multiplier){Trace.Add("r14"+","+difficulty+","+multiplier);}
  public void AddDukesFortune(sbyte difficulty){Trace.Add("r15"+","+difficulty);}
  public void AddTrainJourney(sbyte difficulty){Trace.Add("r16"+","+difficulty);}
  public void AddOceanOdyssey(sbyte difficulty){Trace.Add("r17"+","+difficulty);}
  public void AddPuzzleBreak(sbyte difficulty){Trace.Add("r18"+","+difficulty);}
  public void AddRobotRivals(){Trace.Add("r19");}
  public void AddRoyalWorldCup(sbyte difficulty,int multiplier){Trace.Add("r20"+","+difficulty+","+multiplier);}
  public void AddMergeSmith(sbyte difficulty){Trace.Add("r21"+","+difficulty);}
  public void AddShortExpedition(sbyte difficulty){Trace.Add("r22"+","+difficulty);}
 }
 private sealed class KernelContext:IOriginalWinAnimationContext,IOriginalWinTeamTreasure,IOriginalWinDukesFortune{
  public int Multiplier;public bool Missing;
  public IOriginalWinTeamTreasure GetTeamTreasure()=>Missing?null:this;
  public IOriginalWinDukesFortune GetDukesFortune()=>Missing?null:this;
  public void WheelCollected(){Trace.Add("wheel");}
  public int StreakMultiplier{get{Trace.Add("streak");return Multiplier;}}
 }
 public static void Main(){CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;string line;
  while((line=Console.ReadLine())!=null){Trace.Clear();var p=line.Split(',');var v=new int[p.Length-1];for(int i=1;i<p.Length;i++)v[i-1]=int.Parse(p[i]);bool fault=false;string state;
   if(p[0]=="w"){
    var snapshots=new OriginalBeforeAfterData[]{new Coin{Before=31,After=32,ShouldConsume=true},new OriginalBeforeAfterData{Before=7,After=9},
     new OriginalBeforeAfterData{Before=v[25],After=v[26]},new OriginalBeforeAfterData{Before=0,After=4},new OriginalBeforeAfterData{Before=0,After=5},new OriginalBeforeAfterData{Before=v[20],After=v[21]}};
    var a=new OriginalActiveLevelAnimationValues{Coin=v[23]==2?null:snapshots[0],CompletedNThousandLevelCoin=v[23]==3?null:snapshots[1]};
    var level=new OriginalActiveLevelData(v[24]){Number=v[0],LeagueIndex=v[1],StoryLevel=v[2],IsTableGame=v[3]!=0,IsAdmin=v[4]!=0,Difficulty=(sbyte)v[5],AnimationValues=v[23]==1?null:a,
     Star=v[23]==4?null:snapshots[2],SuperBooster=v[23]==5?null:snapshots[3],SuperDuke=v[23]==6?null:snapshots[4],MagicDuke=v[23]==7?null:snapshots[5],Cloche=new OriginalBeforeAfterData{After=17}};
    var context=new Context{V=v};var moves=v[23]==10?null:new OriginalMoveManager{LeftMoves=v[6],MadeMoves=v[7],Seconds=v[8]};
    try{level.Win(moves,v[9],context,new Rewards{Context=context});}catch(NullReferenceException){fault=true;}
    state=level.State+","+level.LeftMoves+","+level.MadeMoves+","+(level.WonAnyLevel?1:0)+","+level.ExtraCoins;
    foreach(var data in snapshots)state+=","+data.Before+","+data.After+","+(data.ShouldConsume?1:0);
   }else if(p[0]=="k"){
    var data=new OriginalBeforeAfterData{Before=13,After=v[3],ShouldConsume=v[4]!=0};var values=new OriginalLevelStartSnapshots();
    var selected=v[5]!=0?null:data;
    switch(v[0]){
     case 0:values.LeagueCrown=selected;break;
     case 1:values.KingsCup=selected;break;
     case 2:values.TeamBattle=selected;break;
     case 3:values.RoyalPass=selected;break;
     case 4:values.WeeklyPass=selected;break;
     case 5:values.PinataParty=selected;break;
     case 6:values.TeamTreasure=selected;break;
     case 7:values.SpaceMission=selected;break;
     case 8:values.BalloonRise=selected;break;
     case 9:values.SkyRace=selected;break;
     case 10:values.HiddenTemple=selected;break;
     case 11:values.DragonNest=selected;break;
     case 12:values.TeamTournament=selected;break;
     case 13:values.MagicCauldron=selected;break;
     case 14:values.ArcheryArena=selected;break;
     case 15:values.DukesFortune=selected;break;
     case 16:values.TrainJourney=selected;break;
     case 17:values.OceanOdyssey=selected;break;
     case 18:values.PuzzleBreak=selected;break;
     case 19:values.RobotRivals=selected;break;
     case 20:values.RoyalWorldCup=selected;break;
     case 21:values.MergeSmith=selected;break;
     case 22:values.ShortExpeditionEvent=selected;break;
    }
    var rewards=new OriginalSessionWinRewards(new OriginalActiveLevelAnimationValues{StartSnapshots=values},new KernelContext{Multiplier=v[2],Missing=v[6]!=0});
    try{switch(v[0]){
     case 0:rewards.AddLeagueCrown((sbyte)v[1]);break;
     case 1:rewards.AddKingsCup((sbyte)v[1]);break;
     case 2:rewards.AddTeamBattle((sbyte)v[1]);break;
     case 3:rewards.AddRoyalPass((sbyte)v[1]);break;
     case 4:rewards.AddWeeklyPass((sbyte)v[1]);break;
     case 5:rewards.AddPinata((sbyte)v[1]);break;
     case 6:rewards.AddTeamTreasure((sbyte)v[1]);break;
     case 7:rewards.AddSpaceMission();break;
     case 8:rewards.AddBalloonRise((sbyte)v[1]);break;
     case 9:rewards.AddSkyRace();break;
     case 10:rewards.AddHiddenTemple((sbyte)v[1]);break;
     case 11:rewards.AddDragonNest((sbyte)v[1]);break;
     case 12:rewards.AddTeamTournament((sbyte)v[1]);break;
     case 13:rewards.AddMagicCauldron((sbyte)v[1]);break;
     case 14:rewards.AddArcheryArena((sbyte)v[1],v[2]);break;
     case 15:rewards.AddDukesFortune((sbyte)v[1]);break;
     case 16:rewards.AddTrainJourney((sbyte)v[1]);break;
     case 17:rewards.AddOceanOdyssey((sbyte)v[1]);break;
     case 18:rewards.AddPuzzleBreak((sbyte)v[1]);break;
     case 19:rewards.AddRobotRivals();break;
     case 20:rewards.AddRoyalWorldCup((sbyte)v[1],v[2]);break;
     case 21:rewards.AddMergeSmith((sbyte)v[1]);break;
     case 22:rewards.AddShortExpedition((sbyte)v[1]);break;
    }}catch(NullReferenceException){fault=true;}
    state=data.Before+","+data.After+","+(data.ShouldConsume?1:0);
   }else throw new InvalidOperationException();
   Console.WriteLine((fault?1:0)+"#"+state+"#"+string.Join(";",Trace));
  }
 }
}
