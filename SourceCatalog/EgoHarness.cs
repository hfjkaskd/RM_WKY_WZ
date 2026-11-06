using System;
using System.Collections;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine
{
    public sealed class Coroutine { }
    public sealed class WaitForSeconds {public float Seconds;public WaitForSeconds(float value){Seconds=value;}}
    public abstract class CustomYieldInstruction:IEnumerator {public abstract bool keepWaiting{get;}public object Current=>null;public bool MoveNext()=>keepWaiting;public void Reset(){}}
}
namespace Whitebox.Foundation
{
    public interface IOriginalWinInventory {int Coin{get;}}
    public struct OriginalLocalRoyalPassProgress{}public struct OriginalLocalTeamOfferProgress{}
    public interface IOriginalEgoSaveState{bool ItemThrowInProgress{get;}}
    public sealed class OriginalActiveLevelData {public int EgoPurchaseCount;}
    public sealed class OriginalBeforeAfterData
    {public int Before=-99,After=-98;public void Init(int value){Harness.Trace.Add("snapshot,"+value);Before=After=value;}}
    public sealed class OriginalActiveLevelAnimationValues {public OriginalBeforeAfterData Coin=new OriginalBeforeAfterData();}
    public sealed class OriginalInviteFriendsStartData {public void SetEgoPrice(int price){Harness.Trace.Add("invite-price,"+price);}}
    public sealed class OriginalGameOperations
    {public int Count;public void OperationStart(int value){Harness.Trace.Add("op-start,"+value);Count++;}public void OperationFinish(int value){Harness.Trace.Add("op-finish,"+value);Count--;}}
    public sealed class OriginalGameStateManager
    {
        public int State;public OriginalGameOperations Operations=new OriginalGameOperations();
        public bool IsWin()=>State==2;public bool IsFail()=>State==4;public bool IsNoGame()=>State==0;public bool IsNoPossibleMove()=>State==6;
        public void SetStateToPlay(){Harness.Trace.Add("play");State=1;}public void SetStateToFail(){Harness.Trace.Add("fail");State=4;}
    }
    public sealed class OriginalMoveManager {public int Left=17;public void SetMaxMoves(int value){Harness.Trace.Add("moves,"+value);Left=value;}}
    public sealed class OriginalWinFailManager {public void Clear(bool exit){Harness.Trace.Add("clear,"+(exit?1:0));}}
    public sealed class OriginalGridCell {public int Id;}
}
public static class Harness
{
    public static readonly List<string> Trace=new List<string>();
    private sealed class Reader:IOriginalEgoRestoreReader
    {public int Count,Left,Throw;public int GetEgoPurchaseCount()=>Count;public int GetLeftMoves()=>Left;public int GetEgoPackageItemThrowInProgress()=>Throw;}
    private sealed class Context:IOriginalEgoContext,IOriginalUserBalanceContext,IOriginalHomeCoinView,IOriginalEgoExtraMoves,
        IOriginalEgoAudio,IOriginalEgoLevelSave,IOriginalEgoGrid,IOriginalEgoSpecialCreator
    {
        public int[] V;public int Missing;public bool Spent;
        public OriginalUserInventoryBalances Inventory,Other;
        public readonly OriginalActiveLevelData Session=new OriginalActiveLevelData();
        public readonly OriginalActiveLevelAnimationValues Animation=new OriginalActiveLevelAnimationValues();
        public OriginalEgoPackage[] Packages;
        public IEnumerator Routine;
        public void Require(int missing){if(Missing==missing)throw new NullReferenceException();}
        public void WriteCoin(long value){Require(1);Trace.Add("write,"+value);}
        public void WriteStar(int value){}public void WriteEventInventory(long value){}public void SetMissionProgress(int type,int amount){throw new Exception("Spend must not dispatch mission progress");}public void TrackCoinTransaction(int amount){throw new Exception("Spend must not dispatch AddCoins analytics");}
        public void OnCoinSpent(){Trace.Add("spent");Spent=true;}
        public IOriginalHomeCoinView GetHomeCoinViewIfContextExists(){Require(2);return V[13]!=0?this:null;}
        public void CoinViewForceUpdated(){Require(3);Trace.Add("home");}
        public OriginalActiveLevelAnimationValues GetCurrentAnimationData(){Require(4);return Animation;}
        public OriginalUserInventoryBalances GetCurrentInventory(){if(Spent&&V[22]!=0)return Other;Require(5);return Inventory;}
        public void TrackSpending(OriginalSpendingData spending,IOriginalInventoryPackage package,int warning){Require(7);Trace.Add("spending,"+spending.CoinAmount+","+spending.SpendingName+","+warning);}
        public OriginalEgoPackage[] GetPackages()=>Missing==8?null:Packages;
        public void ConfigureMultiplierSelectionEgoPriceValues(OriginalEgoPackage package,int index){Require(22);Trace.Add("configure,"+index);}
        public OriginalActiveLevelData GetCurrentSession(){Require(27);return Session;}
        public void SetEgoOfferStatus(bool inProgress,bool show){Require(14);Trace.Add("offer,"+(inProgress?1:0)+","+(show?1:0));}
        public void EpisodeOnEgoAccepted(){Require(13);Trace.Add("episode-accept");}
        public void EpisodeOnEgoShownFirst(){Require(23);Trace.Add("episode-first");}
        public void SetCoinShopEgoOfferPurchasedInCurrentGameSession(bool value){Require(24);Trace.Add("coin-shop,"+(value?1:0));}
        public OriginalInviteFriendsStartData GetCurrentInviteFriendsData(){Require(27);return Missing==25?null:new OriginalInviteFriendsStartData();}
        public void CalculateRemainingItems(int step){Require(27);Require(26);Trace.Add("remaining,"+step);}
        public void ChargerTryContinueThrow(){Require(15);Trace.Add("charger");}
        public IOriginalEgoLevelSave GetLevelSaveIfExists()=>V[21]!=0?this:null;
        public void SaveImmediate(){Require(17);Trace.Add("save");}
        public IOriginalEgoGrid GetGrid(){Trace.Add("grid");return Missing==18?null:this;}
        public IOriginalEgoSpecialCreator GetSpecialCreator()=>Missing==19?null:this;
        public UnityEngine.Coroutine ManualStartCoroutine(IEnumerator routine){Require(21);Trace.Add("start-coroutine");Routine=routine;return null;}
        public float GameplayDeltaTime=>.25f;
        public bool IsEgoAdditionalMoveAvailable(){Trace.Add("additional-available");return V[11]!=0;}
        public void IncrementEgo1SeenCount(){Trace.Add("seen");}
        public bool HasMovesToAdd(){Require(20);Trace.Add("has-additional");return V[11]!=0;}
        public int GetAdditionalMoveCount(){Trace.Add("additional");return V[12];}
        public void PlaySound(int id,float offset){Trace.Add("sound,"+id+","+offset.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));}
        public void ResumeGameMusic(){Trace.Add("resume");}
        public void CalculateGridCellPatternData(){Trace.Add("pattern");}
        private List<OriginalGridCell> Cells(int type)
        {Trace.Add("cells,"+type);if(Missing==28)return null;var result=new List<OriginalGridCell>();for(int i=0;i<V[18];i++)result.Add(Missing==29&&i==0?null:new OriginalGridCell{Id=i});return result;}
        public List<OriginalGridCell> GetCellsToThrowEgoRocket()=>Cells(1);public List<OriginalGridCell> GetCellsToThrowEgoTnt()=>Cells(2);public List<OriginalGridCell> GetCellsToThrowEgoLightball()=>Cells(3);
        public bool CanSpreadJelly(OriginalGridCell cell,bool first){Trace.Add("jelly,"+cell.Id+","+(first?1:0));return cell.Id%2!=0;}
        public int SelectEgoRocketType(OriginalGridCell cell,bool jelly){Trace.Add("rocket,"+cell.Id+","+(jelly?1:0));return cell.Id%2==0?10:20;}
        public void CreateSpecialItemForBooster(OriginalGridCell cell,int type){Trace.Add("create,"+(cell==null?-1:cell.Id)+","+type);}
    }
    public static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var v=Array.ConvertAll(line.Split(','),int.Parse);var c=new Context{V=v};
            c.Inventory=new OriginalUserInventoryBalances(v[2],0,0,c);c.Other=new OriginalUserInventoryBalances(777,0,0,c);
            c.Inventory.OnCoinUpdated+=()=>Trace.Add("updated");if(v[14]!=0)c.Inventory.OnCoinSpent+=c.OnCoinSpent;
            c.Session.EgoPurchaseCount=v[5];c.Packages=new OriginalEgoPackage[5];
            for(int i=0;i<c.Packages.Length;i++)c.Packages[i]=new OriginalEgoPackage(v[3],(byte)v[10],(byte)v[15],(byte)v[16],(byte)v[17]);
            var state=new OriginalGameStateManager{State=v[19]};var moves=new OriginalMoveManager();
            var ego=new OriginalEgoManager(v[1]==10?null:state,moves,v[1]==11?null:c,v[1]==12?null:c,c);
            // Public native restore initializes the desired step without reflected fields.
            ego.RestoreFromSaveData(new Reader{Count=v[6]==0?-1:v[6],Left=1,Throw=v[9]});
            if(v[6]==0)ego.EgoShown();
            if(v[8]!=0)ego.RestoreFromSaveData(new Reader{Count=v[6],Left=0});
            for(int i=0;i<v[7];i++)ego.OnExtraWarnShown();
            c.Packages=new OriginalEgoPackage[v[20]];
            for(int i=0;i<c.Packages.Length;i++)c.Packages[i]=v[1]==9?null:new OriginalEgoPackage(v[3],(byte)v[10],(byte)v[15],(byte)v[16],(byte)v[17]);
            c.Missing=v[1];if(v[1]==6)c.Animation.Coin=null;
            Trace.Clear();bool fault=false;int result=-1;
            try
            {
                switch(v[0])
                {
                    case 0:result=c.Inventory.SpendCoins(new OriginalSpendingData("fixture",v[3]),c,null,v[7])?1:0;break;
                    case 1:result=c.Inventory.HasEnoughCoins(v[3])?1:0;break;
                    case 2:result=ego.CanPlayOn()?1:0;break;
                    case 3:ego.PlayOn(v[4]!=0);break;
                    case 4:ego.EgoShown();break;
                    case 5:ego.Clear(v[4]!=0);break;
                    case 6:ego.RestoreFromSaveData(new Reader{Count=v[5],Left=v[12],Throw=v[9]});break;
                    case 7:c.Routine=ego.PutEgoItemsToGrid(c.Packages.Length==0?null:c.Packages[0]);break;
                    case 8:ego.TryThrowEgoPackageItemThrow();break;
                    case 9:ego.CancelOffer();break;
                    case 10:new OriginalEgoDialogGameActions(v[1]==31?null:ego,v[1]==30?null:new OriginalWinFailManager(),c).PaidPlayOn();break;
                    case 11:new OriginalEgoDialogGameActions(v[1]==31?null:ego,v[1]==30?null:new OriginalWinFailManager(),c).FreePlayOn();break;
                    case 12:new OriginalEgoDialogGameActions(v[1]==31?null:ego,v[1]==30?null:new OriginalWinFailManager(),c).CancelOffer();break;
                    case 13:result=ego.HasShownEgo()?1:0;break;
                }
                if(v[0]==7||v[0]==8)
                {
                    if(c.Routine!=null)for(int i=0;i<1000;i++)
                    {
                        if(!c.Routine.MoveNext())break;
                        if(c.Routine.Current is UnityEngine.WaitForSeconds wait){if(wait.Seconds!=.5f)throw new Exception("Unity delay");Trace.Add("yield,u");}
                        else if(c.Routine.Current is OriginalGameplayWait gameplay){Trace.Add("yield,g");Trace.Add("poll,"+(gameplay.keepWaiting?1:0));Trace.Add("poll,"+(gameplay.keepWaiting?1:0));}
                        else throw new Exception("Unexpected coroutine yield");
                    }
                }
            }catch(NullReferenceException){fault=true;}catch(IndexOutOfRangeException){fault=true;}
            var snapshot=c.Animation.Coin;
            Console.WriteLine((fault?1:0)+"#"+string.Join(",",new[]{result,c.Inventory.Coin,snapshot==null?-99:snapshot.Before,snapshot==null?-98:snapshot.After,ego.CurrentStep,ego.WarningStep,ego.EgoPackageItemThrowInProgress?1:0,state.State,moves.Left,c.Session.EgoPurchaseCount,state.Operations.Count})+"#"+string.Join(";",Trace));
        }
    }
}
