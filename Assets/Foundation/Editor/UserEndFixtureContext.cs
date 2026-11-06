using System;
using System.IO;

namespace Whitebox.Foundation.Editor
{
    // Native account loading and reward/storage kernels use a private SQLite
    // database. Clock/home-area/feature/backend remain explicit fixture owners;
    // full application and mobile startup are not composed here.
    internal sealed class UserEndFixtureContext : IOriginalUserLevelEndContext, IOriginalUserBalanceContext,
        IOriginalBasicLevelProgressContext, IOriginalProgressTimedEvent, IOriginalProgressEvent, IOriginalLevelEndFeature, IOriginalLocalAccountLoadContext,
        IOriginalLifeHelperContext, IOriginalLifeClockContext
    {
        public readonly OriginalUserEndState User;
        public readonly int InitialCoin, InitialStar, InitialLevel;
        public readonly string DatabasePath;
        public readonly OriginalDatabaseService Database;
        public readonly OriginalAccountStorage Storage;
        private readonly OriginalUserKeyValue values;
        private readonly IOriginalKeyValueConnection connection;
        private readonly IOriginalKeyValueConnection appConnection;
        public readonly OriginalDatabaseService AppDatabase;
        public readonly OriginalLifeHelper Life;
        public readonly OriginalLifeClock LifeClock;
        private readonly OriginalSystemClientClock clientClock = new OriginalSystemClientClock();
        private Action lifeStarted;
        public readonly long EntryLifeTimestamp;
        public bool LifeEntryDebitVerified, WinLifeRefundVerified, LifeReopenVerified;
        public int StoredCoin=>checked((int)Storage.ReadCoin(long.MinValue));
        public int StoredStar=>Storage.ReadStar(int.MinValue);
        public int StoredLevel=>Storage.ReadLevel(int.MinValue);
        public int StoredLeagueLevel=>Storage.ReadLeagueLevel(int.MinValue);
        public long StoredEventInventory=>Storage.ReadEventInventory(long.MinValue);
        public bool DatabaseReopenVerified;
        public readonly OriginalLocalUserData LoadedUser;
        public int AreaLoadCalls;
        public bool NativeLocalReloadVerified;
        public bool RuntimeStorageFactoryVerified,DirectoryRulesVerified;
        public int SaveClearCalls,FeatureEndCalls,EpisodeWinCalls,NewUxWinCalls,StatsWinCalls,EventInfoCalls,EventWinCalls,RemainingEventCalls,RemainingMissionCalls,BackendCalls,RewardAnalyticsCalls,LevelChangedCalls,CoinUpdatedCalls,StarsUpdatedCalls;
        public UserEndFixtureContext(OriginalPlayableBoard game)
        {
            DatabasePath=Path.Combine(Path.GetTempPath(),"royal-match-account-"+Guid.NewGuid().ToString("N")+".sqlite");
            VerifyDirectoryRules();
            connection=OriginalRuntimeStorage.Open(DatabasePath);
            RuntimeStorageFactoryVerified=true;
            Database=new OriginalDatabaseService(connection,OriginalDatabaseService.UserSaveInterval,
                ()=>{}); // Explicit non-corrupt verification database; not a corruption recovery provider.
            values=new OriginalUserKeyValue(()=>Database);Storage=new OriginalAccountStorage(values);
            Database.OnApplicationResume();
            appConnection=OriginalRuntimeStorage.Open(DatabasePath+".app");
            AppDatabase=new OriginalDatabaseService(appConnection,OriginalDatabaseService.AppSaveInterval,()=>{});
            AppDatabase.OnApplicationResume();
            LifeClock=new OriginalLifeClock(new OriginalAppKeyValue(()=>AppDatabase),clientClock,this,new OriginalServerTimePublication());
            LifeClock.LoadStoredClockState();
            LoadedUser=CreateEmptyUser(this,values);
            OriginalLocalAccountLoad.UpdateDataFromLocalStorage(this,values);
            InitialCoin=LoadedUser.Inventory.Coin;InitialStar=LoadedUser.Inventory.Star;InitialLevel=LoadedUser.Basic.Level;
            if(InitialCoin!=2000||InitialStar!=0||InitialLevel!=1||Database.ShouldSave||values.HasKey(OriginalAccountStorage.CoinsKey))
                throw new InvalidOperationException("Native fresh-account load must use defaults without storing them.");
            Life=new OriginalLifeHelper(this);LifeClock.LifeHelper=new OriginalLifeClockAdapter(Life);Life.Bind();lifeStarted();
            long before=LoadedUser.Basic.FullLivesTimeInMs;
            Life.DecrementLives();EntryLifeTimestamp=LoadedUser.Basic.FullLivesTimeInMs;
            if(EntryLifeTimestamp<before+Life.RequiredTimeInMsToFillOneLife||values.GetLong("FullLivesTimeInMs")!=EntryLifeTimestamp)
                throw new InvalidOperationException("Actual entry life debit must mutate/persist its native timestamp.");
            LifeEntryDebitVerified=true;
            values.SetInt(OriginalAccountStorage.LevelTryCountKey,17);
            values.SetInt(OriginalAccountStorage.HardLevelBackgroundIndexKey,9);
            Database.Save();
            User=new OriginalUserEndState{ActiveLevel=game.Session,
                Basic=LoadedUser.Basic,Inventory=LoadedUser.Inventory};
            User.Inventory.OnCoinUpdated+=()=>CoinUpdatedCalls++;
            User.Inventory.OnStarsUpdated+=()=>StarsUpdatedCalls++;
        }
        public OriginalUserEndState GetCurrentUser()=>User;
        public void GetEventsInfoOnLevelEnd(){EventInfoCalls++;}
        public void ClearLevelSaveFile(){SaveClearCalls++;}
        public IOriginalClocheWriter GetClocheWriter()=>throw new InvalidOperationException("First board must not persist cloche.");
        public IOriginalLevelEndFeature GetSuperDuke()=>this;
        public IOriginalLevelEndFeature GetMultiplierToken()=>this;
        public void OnLevelEnd(){FeatureEndCalls++;}
        public void EpisodeOnLevelSuccess(){EpisodeWinCalls++;}
        public void NewUserUxOnLevelSuccess(OriginalActiveLevelData active){if(active!=User.ActiveLevel)throw new InvalidOperationException("Active session identity");NewUxWinCalls++;}
        public int GetWinMissionProgressFlag()=>values.GetInt(OriginalAccountStorage.LevelTryCountKey);
        public void SetMissionProgress(int type,int amount){if(type!=21)throw new InvalidOperationException("Locked first-board fixture must only post earned coin progress.");}
        public bool IsMagicLanternTutorialCompleted=>false;
        public void SetMagicLanternTutorialAsSeen(bool value)=>throw new InvalidOperationException("No lantern tutorial in the first-board fixture.");
        public void OnNightmareLevelWin()=>throw new InvalidOperationException("Not a story fixture.");
        public void OnSideProgressionLevelWin()=>throw new InvalidOperationException("Not a side-progression fixture.");
        public void EpisodeOnLevelFail()=>throw new InvalidOperationException("No failure is injected in this fixture.");
        public void FailedSideProgressionLevel()=>throw new InvalidOperationException("No failure is injected in this fixture.");
        public void QuitDetectionLevelEnded()=>throw new InvalidOperationException("No quit is injected in this fixture.");
        public void FailedLevel()=>throw new InvalidOperationException("No failure is injected in this fixture.");
        public void OnStatsLevelFail()=>throw new InvalidOperationException("No failure is injected in this fixture.");
        public void OnStatsLevelWin(){StatsWinCalls++;}
        public IOriginalLevelMilestonePackage GetNThousandLevelPackage(int level)=>throw new InvalidOperationException("No milestone package in this fixture.");
        public void AddInventoryPackage(IOriginalLevelMilestonePackage package,bool paid)=>throw new InvalidOperationException("No milestone package in this fixture.");
        public void NThousandRewardClaim(IOriginalLevelMilestonePackage package)=>throw new InvalidOperationException("No milestone package in this fixture.");
        public void MultiplierTokenPurchaseOnLevelWin(){}
        public void UpdateEventsOnLevelWin(){EventWinCalls++;}
        public void SendDataToBackend(bool force){if(force)throw new InvalidOperationException("Native end sync is not forced.");BackendCalls++;}
        public void TryMultiplyValues(bool after){if(after)throw new InvalidOperationException("Native remaining-end multiplier flag is false.");}
        public void EventsRemainingMovesEnd(OriginalActiveLevelData active){if(active!=User.ActiveLevel)throw new InvalidOperationException("Active session identity");RemainingEventCalls++;}
        public void MissionEventsRemainingMovesEnd(){RemainingMissionCalls++;}
        public IOriginalProgressTimedEvent GetKingsCup()=>this;
        public IOriginalProgressTimedEvent GetTeamBattle()=>this;
        public IOriginalProgressEvent GetTeamTreasure()=>this;
        public IOriginalProgressEvent GetWeeklyLeaderboard()=>this;
        public bool IsRemainingTimeFinished=>true;
        public bool ShouldProgress=>false;
        public int MaxLevelCount=>14700; // Explicit fixture value from the native client maximum.
        public void MarkCurrentSideProgressionLevelPlayed()=>throw new InvalidOperationException("Not a side-progression fixture.");
        public bool CanMultiplyInLevel(bool after)=>false;
        public int GetActiveAmountMultiplier()=>throw new InvalidOperationException("No multiplier in this fixture.");
        public void ClearWinProgressFlag()=>Storage.ClearLevelTryCount();
        public void ClearOtherLevelProgressFlag()=>Storage.ClearHardLevelBackgroundIndex();
        public void WriteLevel(int level)=>Storage.WriteLevel(level);
        public void WriteLeagueLevel(int level)=>Storage.WriteLeagueLevel(level);
        public void TriggerUpdateLevel(int level){LevelChangedCalls++;}
        public void UpdateStoryLevel(){}
        public void IncrementMyLevelWithEvents(int amount,bool kings,bool battle,bool treasure,bool weekly)
        {if(amount!=1||kings||battle||treasure||weekly)throw new InvalidOperationException("Native first-board progress amount1 and locked event gates");}
        public void IncrementMyLeagueScoreWithEvents(int amount,bool kings,bool battle,bool treasure,bool weekly)=>throw new InvalidOperationException("Not a league fixture.");
        public void OnCrownsWon(int amount)=>throw new InvalidOperationException("Not a league fixture.");
        public void WriteCoin(long value)=>Storage.WriteCoin(value);
        public void WriteStar(int value)=>Storage.WriteStar(value);
        public void WriteEventInventory(long value)=>Storage.WriteEventInventory(value);
        public void TrackCoinTransaction(int amount){}
        private static OriginalLocalUserData CreateEmptyUser(UserEndFixtureContext owner,OriginalUserKeyValue areaValues)
        {
            var user=new OriginalLocalUserData{UserId=new OriginalLocalUserIdentity(),Basic=new OriginalBasicLevelProgress(0,0,owner),
                Inventory=new OriginalUserInventoryBalances(0,0,0,owner),Area=new OriginalUserAreaData(areaValues),Log=new OriginalLocalLogData(),Story=new OriginalLocalStoryData()};
            user.Basic.BindLifeStorage(areaValues);user.Inventory.BindLifeStorage(areaValues);return user;
        }
        OriginalLocalUserData IOriginalLifeHelperContext.GetCurrentUser()=>LoadedUser;
        OriginalLocalUserData IOriginalLifeClockContext.GetCurrentUser()=>LoadedUser;
        public bool HasPassGoldStatus(OriginalLocalUserData user)=>user.Inventory.RoyalPass.IsGold; // Explicit weekly-pass=false fixture; not production entitlement.
        public IOriginalLifeTimeHelper GetTimeHelper()=>LifeClock;
        public void AddOnApplicationStart(Action action){lifeStarted+=action;}
        public void TrackLifeTransaction(int amount){if(amount!=-1&&amount!=1)throw new InvalidOperationException("Entry/win fixture transactions only.");}
        public bool IsUserContextActive=>true;
        public bool GetAfterPaused()=>false; // Explicit unpaused win fixture.
        public int MaxPossibleLivesToHave=>Life.MaxPossibleLivesToHave;
        public float TimeSpentOnPaused=>0;
        public IOriginalTimeManipulationTracker GetDailyRewards()=>throw new NotSupportedException("This fixture does not inject a server-time response.");
        public IOriginalWeeklyTimeOwner GetWeeklyMission()=>throw new NotSupportedException("This fixture does not inject a server-time response.");
        public bool IsThereLifeHack(long serverTime)=>throw new NotSupportedException("This fixture does not inject a server-time response.");
        public void SendPing(int reason)=>throw new NotSupportedException("No backend transport in this account fixture.");
        public void RefundWinLife(int amount)
        {
            Life.IncrementLives(amount);
            if(amount!=1||LoadedUser.Basic.FullLivesTimeInMs!=EntryLifeTimestamp-Life.RequiredTimeInMsToFillOneLife||values.GetLong("FullLivesTimeInMs")!=LoadedUser.Basic.FullLivesTimeInMs)
                throw new InvalidOperationException("Actual WinTransition must return exactly its pre-debited life through runtime storage.");
            WinLifeRefundVerified=true;
        }
        private sealed class DirectoryContext:IOriginalStorageDirectoryContext
        {
            public string NativePath,PersistentPath;public bool FailNative;public int NativeReads,PersistentReads;
            public string ReadAndroidApplicationDataPath(){NativeReads++;if(FailNative)throw new InvalidOperationException("Explicit directory fixture failure.");return NativePath;}
            public string PersistentDataPath{get{PersistentReads++;return PersistentPath;}}
        }
        private void VerifyDirectoryRules()
        {
            var context=new DirectoryContext{NativePath="relative-native-root",PersistentPath=Path.GetTempPath()};
            var paths=new OriginalDbFilePaths(context);context.NativePath="changed-after-initialization";
            if(paths.GetAppDbPath()!=Path.Combine("relative-native-root","App")||paths.GetUserDbPath(long.MinValue)!=Path.Combine("relative-native-root","U_"+long.MinValue.ToString())||context.NativeReads!=1||context.PersistentReads!=0)
                throw new InvalidOperationException("Native directory success/caching/path naming differs.");
            context=new DirectoryContext{FailNative=true,PersistentPath=Path.Combine(Path.GetTempPath(),"..")} ;
            paths=new OriginalDbFilePaths(context);
            if(paths.ApplicationDataPath!=Path.GetFullPath(context.PersistentPath)||context.NativeReads!=1||context.PersistentReads!=1)
                throw new InvalidOperationException("Native shared directory exception fallback differs.");
            var actual=OriginalRuntimeStorage.Paths;
            if(actual.ApplicationDataPath!=Path.GetFullPath(UnityEngine.Application.persistentDataPath)||actual.GetAppDbPath()!=Path.Combine(actual.ApplicationDataPath,"App"))
                throw new InvalidOperationException("Actual Windows Unity directory provider does not execute the original exception fallback.");
            DirectoryRulesVerified=true;
        }
        OriginalLocalUserData IOriginalLocalAccountLoadContext.GetCurrentUser()=>LoadedUser;
        public long CurrentTimeInMilliseconds=>clientClock.CurrentTimeInMs(); // Actual TimeUtil kernel; no server response is supplied here.
        public bool AreaDataRestored;
        public void ParseAndSetAreaFromLocalStorage()=>LoadArea(LoadedUser,values);
        private void LoadArea(OriginalLocalUserData user,OriginalUserKeyValue areaValues)
        {
            AreaLoadCalls++;
            new OriginalAreaDataParser(areaValues,new FreshAreaContext(user)).ParseAndSetFromLocalStorage();
            var area=user.Area;
            if(area.AreaId!=1||area.RealAreaId!=1||area.ChestStatus!=0||area.Tasks.Length!=6||areaValues.HasKey(OriginalAreaDataParser.AreaKey))
                throw new InvalidOperationException("Native fresh area must load its original six-task config without writing defaults.");
            for(int i=0;i<area.Tasks.Length;i++)if(area.Tasks[i].TaskId!=i+1||area.Tasks[i].IsCompleted)
                throw new InvalidOperationException("Fresh-area task identity/progress differs.");
            AreaDataRestored=true;
        }
        private sealed class FreshAreaContext:IOriginalAreaDataContext
        {
            private readonly OriginalLocalUserData user;
            public FreshAreaContext(OriginalLocalUserData user){this.user=user;}
            public OriginalUserAreaData GetCurrentAreaData()=>user.Area;
            public IOriginalAreaConfigContainer CreateConfigContainer()=>new OriginalAreaConfigContainer();
            public IOriginalAreaConfigContainer GetHomeConfigContainer()=>throw new NotSupportedException("This empty-area fixture never loads existing Home progress.");
            public int PlayableAreaCount=>throw new NotSupportedException("The fresh branch does not query playable area availability.");
            public IOriginalAreaAssetAvailability GetAssetBundleHelper()=>throw new NotSupportedException("The fresh branch does not query downloaded area bundles.");
        }
        private sealed class ReloadContext:IOriginalLocalAccountLoadContext
        {
            public readonly OriginalLocalUserData User;
            private readonly UserEndFixtureContext owner;
            private readonly OriginalUserKeyValue values;
            public ReloadContext(UserEndFixtureContext owner,OriginalUserKeyValue values){this.owner=owner;this.values=values;User=CreateEmptyUser(owner,values);}
            public OriginalLocalUserData GetCurrentUser()=>User;
            public long CurrentTimeInMilliseconds=>owner.CurrentTimeInMilliseconds;
            public void ParseAndSetAreaFromLocalStorage()=>owner.LoadArea(User,values);
        }
        public void VerifyPersisted()
        {
            Database.Save();
            using(var reopened=OriginalRuntimeStorage.Open(DatabasePath))
            {
                var db=new OriginalDatabaseService(reopened,OriginalDatabaseService.UserSaveInterval,()=>{});
                var kv=new OriginalUserKeyValue(()=>db);var account=new OriginalAccountStorage(kv);
                var reload=new ReloadContext(this,kv);
                OriginalLocalAccountLoad.UpdateDataFromLocalStorage(reload,kv);
                if(reload.User.Basic.Level!=User.Basic.Level||reload.User.Inventory.Coin!=User.Inventory.Coin||
                    reload.User.Inventory.Star!=User.Inventory.Star||reload.User.Inventory.EventInventory!=User.Inventory.EventInventory||
                    kv.GetInt(OriginalAccountStorage.LevelTryCountKey,-1)!=0||kv.GetInt(OriginalAccountStorage.HardLevelBackgroundIndexKey,-1)!=0)
                    throw new InvalidOperationException("Reopened SQLite account differs from actual win rewards/progression.");
                if(!LifeEntryDebitVerified||!WinLifeRefundVerified||reload.User.Basic.FullLivesTimeInMs!=LoadedUser.Basic.FullLivesTimeInMs||
                    reload.User.Basic.UnlimitedLivesEndTimeInMs!=LoadedUser.Basic.UnlimitedLivesEndTimeInMs)
                    throw new InvalidOperationException("Reopened SQLite account differs from actual entry debit/win life refund.");
                var lifeRow=reopened.Find("FullLivesTimeInMs");
                if(lifeRow.Synced!=0||lifeRow.SyncBlocked!=1)throw new InvalidOperationException("Native local life sync flags differ.");
                LifeReopenVerified=true;
                foreach(string key in new[]{OriginalAccountStorage.LevelKey,OriginalAccountStorage.CoinsKey,OriginalAccountStorage.StarsKey})
                {
                    var row=reopened.Find(key);
                    if(row.Synced!=0||row.SyncBlocked!=1)throw new InvalidOperationException("Native local reward sync flags differ: "+key);
                }
            }
            DatabaseReopenVerified=true;NativeLocalReloadVerified=true;
        }
        public void Dispose(){appConnection.Dispose();connection.Dispose();}
    }
}
