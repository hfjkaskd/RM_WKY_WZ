using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Required event owners in UserActiveLevelData.RemainingMovesEnd. The
    // application supplies the real snapshots/context; no inactive fallback.
    public interface IOriginalRemainingSessionEvents
    {
        void AddMadnessRemainingMoves(int count,int difficulty);
        void AddLightningRemainingMoves(int count);
        void AddPropellerRush(int count);
        void AddClashRemainingMoves(int count);
        bool IsPlayingWithSuperLightball();
        void AddClashRemainingSpecials(Dictionary<int,int> items,bool superLightball);
        void AddLightningForLightballsOnBoard();
        bool IsMadnessSpecialIcon();
        void AddMissionEvents(int type,sbyte difficulty,int count);
    }
    public interface IOriginalRemainingSessionContext
    {
        bool IsPlayingWithSuperLightball();
        IOriginalMadnessSessionManager Madness { get; }
        int CountItemOnBoard(int type);
    }
    public sealed partial class OriginalActiveLevelData
    {
        public int ExtraCoins { get; private set; } // Native0x50.
        public void RemainingMovesEnd(int notReplacedMoves,int extraCoins,Dictionary<int,int> remainingSpecials,
            IOriginalRemainingSessionEvents events) // 0x32b9bf0.
        {
            var animation=AnimationValues??throw new NullReferenceException();
            var coin=animation.Coin??throw new NullReferenceException();
            coin.AddExtra(extraCoins);ExtraCoins=extraCoins;
            events.AddMadnessRemainingMoves(notReplacedMoves,Difficulty);
            events.AddLightningRemainingMoves(notReplacedMoves);
            events.AddPropellerRush(unchecked(notReplacedMoves*5));
            events.AddClashRemainingMoves(notReplacedMoves);
            bool superLightball=events.IsPlayingWithSuperLightball();
            events.AddClashRemainingSpecials(remainingSpecials,superLightball);
            events.AddLightningForLightballsOnBoard();
            if(events.IsMadnessSpecialIcon())events.AddMissionEvents(3,Difficulty,notReplacedMoves);
            else
            {
                int half=notReplacedMoves/2; // Native signed division truncates toward zero.
                events.AddMissionEvents(2,Difficulty,half);
                events.AddMissionEvents(4,Difficulty,half);
            }
            specialItemIds.Clear();
        }
    }
    public sealed class OriginalRemainingSessionEvents:IOriginalRemainingSessionEvents
    {
        private readonly OriginalActiveLevelData level;
        private readonly IOriginalRemainingSessionContext context;
        public OriginalRemainingSessionEvents(OriginalActiveLevelData level,IOriginalRemainingSessionContext context)
        {this.level=level??throw new ArgumentNullException(nameof(level));this.context=context??throw new ArgumentNullException(nameof(context));}
        private OriginalLevelStartSnapshots Data=>level.AnimationValues.StartSnapshots??throw new NullReferenceException();
        public void AddMadnessRemainingMoves(int count,int difficulty)
        {var data=Data.Madness as OriginalMadnessSessionData??throw new NullReferenceException();data.TryAddMadnessForRemainingMoves(count,difficulty);}
        public void AddLightningRemainingMoves(int count)
        {var data=Data.DailyLightningRush as OriginalDailyLightningRushLevelData??throw new NullReferenceException();data.AddLightningForRushRemainingMoves(count,context.IsPlayingWithSuperLightball());}
        public void AddPropellerRush(int count)
        {var data=Data.PropellerRush??throw new NullReferenceException();data.TryIncrementCount(count);}
        public void AddClashRemainingMoves(int count)
        {var data=Data.ClashOfChampions as OriginalClashOfChampionsLevelData??throw new NullReferenceException();data.IncrementScoreForRemainingMoves(count);}
        public bool IsPlayingWithSuperLightball()=>context.IsPlayingWithSuperLightball();
        public void AddClashRemainingSpecials(Dictionary<int,int> items,bool superLightball)
        {var data=Data.ClashOfChampions as OriginalClashOfChampionsLevelData??throw new NullReferenceException();data.IncrementScoreForRemainingSpecialItems(items,superLightball);}
        public void AddLightningForLightballsOnBoard() // 0x32b9df4.
        {
            var data=Data.DailyLightningRush as OriginalDailyLightningRushLevelData;
            int count=context.CountItemOnBoard(5);
            if(data==null)throw new NullReferenceException();
            data.AddLightningForRushLightballsOnBoard(count,context.IsPlayingWithSuperLightball());
        }
        public bool IsMadnessSpecialIcon()
        {var manager=context.Madness??throw new NullReferenceException();return manager.ShouldShowIcon()&&manager.EventType==3;}
        public void AddMissionEvents(int type,sbyte difficulty,int count)=>level.AnimationValues.AddMissionEvents(type,difficulty,count);
    }
}
