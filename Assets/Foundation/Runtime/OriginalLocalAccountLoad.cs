using System;
using System.Collections;

namespace Whitebox.Foundation
{
    public struct OriginalLocalRoyalPassProgress
    {
        public readonly long PassProgress, FreeProgress, GoldProgress;
        public readonly bool IsGold, HasNicknameStyle, NewThemedTutorialSeen, IsCardCollectionDisabled;
        public readonly int EventId, Step, Count, TutorialState, PreviousCount;
        public OriginalLocalRoyalPassProgress(long pass, long free, long gold) // 0x30d2110.
        {
            PassProgress=pass;FreeProgress=free;GoldProgress=gold;
            IsGold=(pass&1)!=0;HasNicknameStyle=(pass&(1L<<52))!=0;
            EventId=(int)((pass>>1)&255);Step=(int)((pass>>9)&63);Count=(int)((pass>>15)&131071);
            TutorialState=(int)((pass>>32)&3);NewThemedTutorialSeen=(pass&(1L<<54))!=0;
            PreviousCount=(int)((pass>>34)&131071);IsCardCollectionDisabled=(pass&(1L<<51))!=0;
        }
    }
    public struct OriginalLocalTeamOfferProgress
    {
        public readonly long ProgressValue;
        public readonly int EventId;
        public readonly byte AbGroupId;
        public readonly bool HasGold,HasSilver,HasBronze;
        public OriginalLocalTeamOfferProgress(long value) // 0x28dc430.
        {
            ProgressValue=value;EventId=(int)((value>>1)&255);AbGroupId=(byte)((value>>9)&3);
            HasGold=(value&(1L<<11))!=0;HasSilver=(value&(1L<<12))!=0;HasBronze=(value&(1L<<13))!=0;
        }
    }
    public sealed class OriginalLocalUserIdentity
    {
        public string Name,AppleId,GoogleId;
        public long Team,FacebookId;
    }
    public sealed class OriginalLocalLogData { public string WinMoves; }
    public sealed class OriginalLocalStoryData { public string NightmareWinMoves,TableGameWinMoves; }
    public sealed partial class OriginalLocalUserData
    {
        public OriginalLocalUserIdentity UserId;
        public OriginalBasicLevelProgress Basic;
        public OriginalUserInventoryBalances Inventory;
        public OriginalLocalLogData Log;
        public OriginalLocalStoryData Story;
    }
    public interface IOriginalLocalAccountLoadContext
    {
        OriginalLocalUserData GetCurrentUser();
        long CurrentTimeInMilliseconds { get; }
        void ParseAndSetAreaFromLocalStorage();
    }
    // SyncManager.UpdateDataFromLocalStorage0x337e714. Data is restored into the
    // existing models without balance events, clamping, reward grants or writes.
    // The home area parser remains a required owner; this is not a startup shim.
    public static class OriginalLocalAccountLoad
    {
        public const int InitialCoins=2000, InitialLevel=1, UnlimitedBoosterBitCount=192;
        public const long DefaultFeature2=6291712L; // Native PinataProgress.GetDefaultPinata0x30cc1e0.
        public const string UnlimitedBoosterKey="ByteArrayKeyValue12";
        public static void UpdateDataFromLocalStorage(IOriginalLocalAccountLoadContext context,OriginalUserKeyValue values)
        {
            var user=context.GetCurrentUser()??throw new NullReferenceException();
            FillIdentity(user.UserId??throw new NullReferenceException(),values);
            user.Basic.SyncId=values.GetInt("SyncId");
            user.Basic.Level=values.GetInt("Level",InitialLevel);
            user.Basic.LeagueLevel=values.GetInt("LeagueLevel");
            user.Basic.FullLivesTimeInMs=values.GetLong("FullLivesTimeInMs",context.CurrentTimeInMilliseconds);
            user.Basic.UnlimitedLivesEndTimeInMs=values.GetLong("UnlimitedLivesEndTimeInMs");
            // Original intentionally reads UserData through GetInt then sign-extends.
            user.Basic.UserData=values.GetInt("UserData");
            user.Basic.UserOfferSegment=values.GetInt("UserOfferSegment");
            user.Inventory.Coin=values.GetInt("Coins",InitialCoins);
            user.Inventory.Star=values.GetInt("Stars");
            user.Inventory.Inbox=values.GetInt("Inbox");
            user.Inventory.Chest=values.GetInt("Chest");
            user.Inventory.Feature1=values.GetLong("Feature1");
            var inventory=user.Inventory;
            long feature2=values.GetLong("Feature2");
            inventory.Feature2=feature2==0?DefaultFeature2:feature2;
            user.Inventory.RocketEndTime=values.GetInt("RET");
            user.Inventory.TntEndTime=values.GetInt("TET");
            user.Inventory.LightBallEndTime=values.GetInt("LET");
            inventory=user.Inventory??throw new NullReferenceException();
            inventory.UnlimitedBoosterInventoryString=values.GetString(UnlimitedBoosterKey,null);
            inventory.UnlimitedBoosterInventory=ParseUnlimitedBoosterInventory(inventory.UnlimitedBoosterInventoryString);
            inventory=user.Inventory;
            long eventInventory=values.GetLong("EventInventory");
            inventory.EventInventory=eventInventory==0?16384L:eventInventory;
            user.Inventory.InGameInventory=values.GetLong("InGameInventory");
            user.Inventory.PreLevelInventory=values.GetLong("PreLevelInventory");
            user.Inventory.RemainingBoosterTimes=values.GetLong("RemainingBoosterTimes");
            inventory=user.Inventory;
            var royalPass=new OriginalLocalRoyalPassProgress(values.GetLong("RoyalPassProgress"),values.GetLong("RoyalPassFree"),values.GetLong("RoyalPassGold"));
            if(inventory==null)throw new NullReferenceException();
            inventory.RoyalPass=royalPass;
            user.Inventory.MultiplierEndTimes=values.GetLong("MultiplierEndTimes");
            user.Log.WinMoves=values.GetString("LWM",string.Empty);
            inventory=user.Inventory;
            var teamOffer=new OriginalLocalTeamOfferProgress(values.GetLong("TeamOffer"));
            if(inventory==null)throw new NullReferenceException();
            inventory.TeamOffer=teamOffer;
            user.Inventory.DynamicDifficultyInventory=values.GetLong("DynaDiff");
            user.Inventory.Dialogs=values.GetLong("Dialogs");
            context.ParseAndSetAreaFromLocalStorage();
            user.Story.NightmareWinMoves=values.GetString("KNLWM",string.Empty);
            user.Story.TableGameWinMoves=values.GetString("KNLW",string.Empty);
        }
        public static void FillIdentity(OriginalLocalUserIdentity identity,OriginalUserKeyValue values) // 0x337ed84, diagnostic formatting omitted.
        {
            identity.Name=values.GetString("UserName",string.Empty);
            identity.Team=values.GetLong("TeamId");
            identity.FacebookId=values.GetLong("FacebookId");
            identity.AppleId=values.GetString("AppleId",string.Empty);
            identity.GoogleId=values.GetString("GoogleId",string.Empty);
        }
        public static BitArray ParseUnlimitedBoosterInventory(string value) // 0x28ea480/0x2d7b268.
        {
            if(string.IsNullOrEmpty(value))return new BitArray(UnlimitedBoosterBitCount);
            string[] fields=value.Split('\u00a7');
            var bits=new BitArray(Math.Max(UnlimitedBoosterBitCount,unchecked(fields.Length*8)));
            for(int i=0;i<fields.Length;i++)
            {
                sbyte.TryParse(fields[i],out sbyte number);
                for(int bit=0;bit<8;bit++)bits[(i*8)+bit]=((number>>bit)&1)!=0;
            }
            return bits;
        }
    }
}
