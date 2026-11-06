using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Explicit first-offer/default-catalog/close-owner fixture. The actual
    // player board, listener, move/state/WinFail, Ego, balances and SQLite run.
    // No production UI owner, commercial SDK or Editor runtime fallback.
    public static class EgoContinuationVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static Context owner;
        private static Samples samples;
        private static OriginalGridCell from,to;
        private static int previousFrame,sampleFrame,stage,acceptedBefore,leftBefore,refillsAtPurchase;
        private static float started,steadyAt;
        private static bool[,] originalBoxes;
        private static string ReportPath=>Path.Combine(Application.dataPath,"../live_ego_continuation_verification.json");
        public static void Run()
        {
            if(!EditorApplication.isPlaying||owner!=null)throw new InvalidOperationException("Fresh Play session required.");
            board=UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game=UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device=UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            Require(game!=null&&device.Listener!=null&&game.WinFail==null&&game.Moves.MadeMoves==0,"Fresh formal board and native input");
            originalBoxes=new bool[board.Grid.Width,board.Grid.Height];
            for(int y=0;y<board.Grid.Height;y++)for(int x=0;x<board.Grid.Width;x++)originalBoxes[x,y]=board.Grid.GetCell(x,y).Mediation.CurrentItem is OriginalBoxBoardItem;
            owner=new Context(game,board);owner.Ego=new OriginalEgoManager(game.State,game.Moves,owner,owner,owner);
            game.BindWinFail(owner);owner.Actions=new OriginalEgoDialogGameActions(owner.Ego,game.WinFail,owner);
            device.Unbind();samples=new Samples();listener=new OriginalGameTouchListener(samples,game);
            // Explicit remaining-budget scenario, not alternate runtime startup.
            game.Moves.SetMaxMoves(1);
            previousFrame=Time.frameCount;stage=0;started=Time.realtimeSinceStartup;
            Begin(board.Grid.GetCell(3,5),board.Grid.GetCell(3,4));
            File.WriteAllText(ReportPath,"{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update+=Tick;
        }
        private static void Begin(OriginalGridCell a,OriginalGridCell b)
        {from=a;to=b;sampleFrame=0;steadyAt=-1;acceptedBefore=game.AcceptedSwaps;leftBefore=game.Moves.LeftMoves;}
        private static void Tick()
        {
            try{Step();}catch(Exception error)
            {File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{passed=false,failure=error.ToString()},true)+"\n");Cleanup();throw;}
        }
        private static void Step()
        {
            if(!EditorApplication.isPlaying){Cleanup();return;}
            Require(Time.realtimeSinceStartup-started<75f,"Ego continuation scenario timed out");
            if(previousFrame==Time.frameCount)return;previousFrame=Time.frameCount;
            if(stage<2&&sampleFrame<3)
            {
                var point=(sampleFrame==0?from:to).GetViewPosition();samples.Position=new Vector2(point.x,point.y);
                samples.Down=sampleFrame==0;samples.Held=sampleFrame<2;samples.Up=sampleFrame==2;
                listener.ManualUpdate();sampleFrame++;return;
            }
            if(board.FirstSwapAction.IsSwapping||board.SecondSwapAction.IsSwapping||!game.Fall.IsAllItemsSteady||
                game.State.HasAnyOperation()||game.Auto.IsThereAWaitingMatch){steadyAt=-1;return;}
            if(steadyAt<0){steadyAt=Time.realtimeSinceStartup;return;}
            if(Time.realtimeSinceStartup-steadyAt<.6f)return;
            if(stage==0)
            {
                if(owner.EgoShownCalls==0)return;
                Require(game.AcceptedSwaps==1&&game.Moves.MadeMoves==1&&game.Moves.LeftMoves==0&&game.State.IsEgo()&&!game.CanUserMakeMove(),"Last legal drag enters Ego and blocks input");
                Require(owner.Account.User.Inventory.Coin==2000&&owner.Ego.CurrentStep==0&&owner.FailureCalls==0,"Ego display does not spend coins or fail the session");
                Require(owner.GetCurrentInviteFriendsData().ShouldConsume&&owner.GetCurrentInviteFriendsData().EgoPrice==900,"First Ego stores native invite-price snapshot");
                owner.Actions.PaidPlayOn();
                Require(game.State.IsPlaying()&&game.WinFail.IsEnabled&&game.CanUserMakeMove()&&game.Moves.LeftMoves==5&&game.Moves.MadeMoves==1,"Paid close restores five moves, state and WinFail without resetting made moves");
                Require(owner.Account.User.Inventory.Coin==1100&&owner.Account.StoredCoin==1100&&game.Session.EgoPurchaseCount==1&&owner.Ego.CurrentStep==1,"Actual native balance spend persists 900 coins and one accepted continuation");
                Require(!owner.Ego.EgoPackageItemThrowInProgress&&!game.State.HasOperation(8)&&owner.CoroutineStarts==1,"Zero-item first package completes its actual operation8 coroutine");
                Require(owner.SpendingCalls==1&&owner.SpendingName=="Ego-1"&&owner.SpendingAmount==900&&owner.WarningStep==0&&owner.ResumeCalls==1,"Native spending payload and resume callback");
                Require(game.Session.AnimationValues.Coin.Before==1100&&game.Session.AnimationValues.Coin.After==1100,"Coin snapshot resets from actual post-spend account");
                owner.VerifyCoins();Require(!owner.Ego.CanPlayOn(),"Next native default package costs1900; remaining1100 cannot afford it");
                refillsAtPurchase=game.CreatedRefills;stage=1;NextMove();return;
            }
            if(stage==1)
            {
                Require(game.AcceptedSwaps==acceptedBefore+1&&game.Moves.LeftMoves==leftBefore-1,"A post-purchase gesture spends exactly one restored move");
                VerifyBoard();
                if(game.Moves.LeftMoves>0){Require(game.CanUserMakeMove(),"Restored input remains playable after refill");NextMove();return;}
                if(owner.EgoShownCalls<2)return;
                Require(game.State.IsEgo()&&owner.Ego.CurrentStep==1&&owner.FailureCalls==0&&game.CreatedRefills>refillsAtPurchase,"All restored moves are playable and end at the second Ego gate");
                Require(owner.GetCurrentInviteFriendsData().ShouldConsume&&owner.GetCurrentInviteFriendsData().EgoPrice==1900,"Second Ego records next package price");
                owner.Actions.CancelOffer();Require(game.State.IsFail()&&game.WinFail.IsEnabled,"Cancel sets Fail then resets WinFail for actual dispatch");
                stage=2;steadyAt=-1;return;
            }
            if(owner.FailureCalls==0)return;
            Require(owner.FailureCalls==1&&owner.Account.User.Inventory.Coin==1100&&owner.OfferResetCalls==2,"Actual fail gate fires once and cancel does not charge again");
            owner.VerifyCoins();
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{passed=true,initial_coin=2000,persisted_coin=1100,spent_coin=900,
                accepted_swaps=game.AcceptedSwaps,made_moves=game.Moves.MadeMoves,refills_after_purchase=game.CreatedRefills-refillsAtPurchase,
                ego_shown=owner.EgoShownCalls,ego_purchase_count=game.Session.EgoPurchaseCount,fail_dispatches=owner.FailureCalls,
                coin_update_callbacks=owner.Account.CoinUpdatedCalls,coin_spent_callbacks=owner.CoinSpentCalls,
                database_reopen_verified=owner.DatabaseVerified,invite_price_snapshot_verified=true,elapsed=Time.realtimeSinceStartup-started},true)+"\n");
            Cleanup();
        }
        private static void NextMove()
        {
            Require(LivePlayerInputVerification.ChooseMove(board,1,out var a,out var b)||LivePlayerInputVerification.ChooseMove(board,2,out a,out b)||LivePlayerInputVerification.ChooseMove(board,3,out a,out b),"Read-only native shadow match search finds next gesture");
            Begin(a,b);
        }
        private static void VerifyBoard()
        {
            for(int y=0;y<board.Grid.Height;y++)for(int x=0;x<board.Grid.Width;x++)
            {
                var cell=board.Grid.GetCell(x,y);var item=cell.Mediation.CurrentItem;
                if(item==null){Require(originalBoxes[x,y],"Ordinary source slot must refill");continue;}
                Require(ReferenceEquals(item,cell.Mediation.NextItem)&&ReferenceEquals(item,cell.Mediation.TargetItem)&&!cell.IsFallFrozen&&cell.CrossFallFreezeCount==0,"Post-purchase model mediation settles without freeze");
            }
        }
        private sealed class Samples:IOriginalGameTouchInput
        {
            public Vector2 Position;public bool Down,Held,Up;public Vector2 GetMouseWorldPosition()=>Position;
            public bool GetMouseButtonDown()=>Down;public bool GetMouseButton()=>Held;public bool GetMouseButtonUp()=>Up;
        }
        private sealed class Context:IOriginalEgoContext,IOriginalEgoExtraMoves,IOriginalEgoAudio,IOriginalEgoGrid,IOriginalWinFailServices
        {
            private readonly OriginalPlayableBoard game;private readonly OriginalInitialBoardView board;
            public readonly UserEndFixtureContext Account;
            // Captured default .cctor0x1ca5f64..0x1ca6118; not a production AB owner.
            private readonly OriginalEgoPackage[] packages={new OriginalEgoPackage(900,5),new OriginalEgoPackage(1900,5,1,0,0,2),new OriginalEgoPackage(2900,5,1,1,0,2,4),new OriginalEgoPackage(3900,5,0,1,1,4,5),new OriginalEgoPackage(4900,5,0,0,2,5,5)};
            public OriginalEgoManager Ego;public OriginalEgoDialogGameActions Actions;
            public int EgoShownCalls,FailureCalls,SpendingCalls,SpendingAmount,WarningStep,ResumeCalls,CoroutineStarts,CoinSpentCalls,OfferResetCalls;
            public string SpendingName;public bool DatabaseVerified;
            public Context(OriginalPlayableBoard game,OriginalInitialBoardView board)
            {this.game=game;this.board=board;Account=new UserEndFixtureContext(game);Account.User.Inventory.OnCoinSpent+=OnCoinSpent;game.Session.AnimationValues.StartSnapshots.InviteFriends=new OriginalInviteFriendsStartData();}
            private void OnCoinSpent(){CoinSpentCalls++;}
            public OriginalEgoPackage[] GetPackages()=>packages;
            public void ConfigureMultiplierSelectionEgoPriceValues(OriginalEgoPackage package,int index){} // Explicit default, no selection AB fixture.
            public OriginalActiveLevelData GetCurrentSession()=>game.Session;
            public OriginalUserInventoryBalances GetCurrentInventory()=>Account.User.Inventory;
            public OriginalActiveLevelAnimationValues GetCurrentAnimationData()=>game.Session.AnimationValues;
            public IOriginalHomeCoinView GetHomeCoinViewIfContextExists()=>null; // Explicit game-only fixture has no HomeContext.
            public void TrackSpending(OriginalSpendingData data,IOriginalInventoryPackage package,int warning)
            {Require(package==null,"Native Ego has no inventory package payload");SpendingCalls++;SpendingName=data.SpendingName;SpendingAmount=data.CoinAmount;WarningStep=warning;}
            public void SetEgoOfferStatus(bool inProgress,bool show){Require(!inProgress&&!show,"Native offer reset flags");OfferResetCalls++;}
            public void EpisodeOnEgoAccepted(){}public void EpisodeOnEgoShownFirst(){}public void SetCoinShopEgoOfferPurchasedInCurrentGameSession(bool value){Require(!value,"Native Ego display clears coin-shop purchase flag");}
            public OriginalInviteFriendsStartData GetCurrentInviteFriendsData()=>game.Session.AnimationValues.StartSnapshots.InviteFriends;
            public void CalculateRemainingItems(int step){} // Explicit remaining-item/episode owners, not production substitutes.
            public void ChargerTryContinueThrow(){}public IOriginalEgoLevelSave GetLevelSaveIfExists()=>null;
            public IOriginalEgoGrid GetGrid()=>this;
            public IOriginalEgoSpecialCreator GetSpecialCreator()=>throw new InvalidOperationException("First package must not create special items.");
            public Coroutine ManualStartCoroutine(IEnumerator routine){CoroutineStarts++;return board.StartCoroutine(routine);}
            public float GameplayDeltaTime=>board.Clock.DeltaTime;
            public bool IsEgoAdditionalMoveAvailable()=>false;public void IncrementEgo1SeenCount()=>throw new InvalidOperationException("Default fixture has no additional move offer.");
            public bool HasMovesToAdd()=>false;public int GetAdditionalMoveCount()=>throw new InvalidOperationException("No additional moves in fixture.");
            public void PlaySound(int id,float offset)=>throw new InvalidOperationException("Paid fixture must not play the free-only sound.");
            public void ResumeGameMusic(){ResumeCalls++;} // Explicit music owner boundary; background music pipeline is not restored here.
            public void CalculateGridCellPatternData()=>throw new InvalidOperationException("First package has no specials.");
            public System.Collections.Generic.List<OriginalGridCell> GetCellsToThrowEgoRocket()=>throw new InvalidOperationException();
            public System.Collections.Generic.List<OriginalGridCell> GetCellsToThrowEgoTnt()=>throw new InvalidOperationException();
            public System.Collections.Generic.List<OriginalGridCell> GetCellsToThrowEgoLightball()=>throw new InvalidOperationException();
            public bool CanSpreadJelly(OriginalGridCell cell,bool first)=>throw new InvalidOperationException();
            public int SelectEgoRocketType(OriginalGridCell cell,bool jelly)=>throw new InvalidOperationException();
            public bool IsBonus=>game.Session.IsBonus;public bool ShouldSpeedUpLevelEnd=>!IsBonus;public bool CanUseMoveTokenInLevel=>false;public float LevelTime=>board.Clock.Time;
            public void OnWin()=>throw new InvalidOperationException("This fixture must retain box goals.");public void StartWinFlow()=>throw new InvalidOperationException("No win fixture.");
            public void TrySendRemainingMoves()=>throw new InvalidOperationException("No win fixture.");public void StartExtraMoveFlow()=>throw new InvalidOperationException("No token fixture.");
            public void StartEgoFlow(){EgoShownCalls++;Ego.EgoShown();}
            public void StartNoPossibleMoveFlow()=>throw new InvalidOperationException("No shuffle fixture.");public void StartFailFlow(){FailureCalls++;} // Failure session/account/UI owner remains unbound.
            public void SpendMovesOnLevelQuit()=>throw new InvalidOperationException("No quit fixture.");public void ReportOperationTimeout(int count,int state)=>throw new InvalidOperationException("Unexpected operation timeout.");
            public void StopFadeOutSoundSourceCoroutine(){}
            public void VerifyCoins()
            {
                Account.Database.Save();using(var connection=OriginalRuntimeStorage.Open(Account.DatabasePath))
                {
                    var database=new OriginalDatabaseService(connection,OriginalDatabaseService.UserSaveInterval,()=>{});
                    var values=new OriginalUserKeyValue(()=>database);var storage=new OriginalAccountStorage(values);
                    Require(storage.ReadCoin(long.MinValue)==1100,"Reopened SQLite stores actual continuation spend");
                    var row=connection.Find(OriginalAccountStorage.CoinsKey);Require(row.Synced==0&&row.SyncBlocked==1,"Native local spend synchronization flags");
                }
                DatabaseVerified=true;
            }
            public void Dispose(){Account.User.Inventory.OnCoinSpent-=OnCoinSpent;Account.Dispose();}
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private static void Cleanup()
        {
            EditorApplication.update-=Tick;if(game!=null)game.ReleaseWinFail();
            owner?.Dispose();owner=null;
            if(device!=null&&game!=null&&EditorApplication.isPlaying&&device.Listener==null)device.Bind(game);
        }
        [Serializable]private sealed class Report
        {
            public bool passed,database_reopen_verified,invite_price_snapshot_verified;public string failure;
            public int initial_coin,persisted_coin,spent_coin,accepted_swaps,made_moves,refills_after_purchase,ego_shown,ego_purchase_count,fail_dispatches,coin_update_callbacks,coin_spent_callbacks;
            public float elapsed;
            public string scope="Formal first-board runtime with recorded listener gestures and explicit one-move starting budget. Actual WinFail enters Ego, actual first default package spends900 from a native-loaded private SQLite account, actual OnClose paid branch restores5 moves and WinFail, and five real accepted swaps consume the restored budget with normal refill. Second Ego cannot afford1900; actual OnClose cancel sets Fail and reenables one failure dispatch. Actual invite-price setter stores900 then1900 in a fixture-created snapshot. Reopened database verifies1100 and native sync flags. Default package catalog/AB/extra moves, dialog close/render/music/home/episode/remaining snapshot/save/charger/failure session-account-UI owners remain fixtures. No physical input, production UI/application binding, later package item throwing or full/mobile/original rendered parity is proved.";
        }
    }
}
