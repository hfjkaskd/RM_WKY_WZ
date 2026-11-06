using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalRemainingMovesSkipView { void DestroySkipView(); void ShowSkipText(); }
    public interface IOriginalRemainingMovesDialog { void CloseRemainingMovesDialog(); }
    public interface IOriginalRemainingMovesManagerServices
    {
        OriginalGoalManager Goals { get; }
        OriginalAutoExplodeManager Auto { get; }
        bool IsBonus { get; }
        bool IsStory { get; }
        bool ShouldSpeedUpLevelEnd { get; }
        bool RewardReductionEnabled { get; }
        int Difficulty { get; }
        int LeftMoves { get; }
        int CollectedCoins { get; }
        bool HasAnySpecialItemOnBoard();
        int CountItemOnBoard(int type);
        void RemainingItemCreationStarted();
        OriginalRemainingMovesItemReplacer CreateMoveReplacer();
        OriginalRemainingMovesItemExploder CreateItemExploder();
        OriginalRemainingMovesCoinCollectAnimation CreateCoinCollection();
        void SessionRemainingMovesEnd(int notReplacedMoves,int extraCoins,Dictionary<int,int> remainingSpecials);
        void LevelRemainingMovesEnd();
    }
    // RemainingMovesManager 0x218318c..0x2183f50. Account/event/Flow owners
    // remain explicit required services; no reward or dialog outcome is faked.
    public sealed class OriginalRemainingMovesManager
    {
        private readonly IOriginalRemainingMovesManagerServices services;
        private readonly List<OriginalRemainingMovesCoinCollectAnimation> collections=new List<OriginalRemainingMovesCoinCollectAnimation>();
        private OriginalRemainingMovesItemReplacer replacer;
        private OriginalRemainingMovesItemExploder exploder;
        private IOriginalRemainingMovesDialog dialog;
        private int extraCoins;
        public bool IsRemainingMovesActive { get; private set; }
        public bool IsRemainingMovesReady { get; private set; }
        public bool IsTapToSkipClicked { get; private set; }
        public IOriginalRemainingMovesSkipView SkipView { get; set; }
        public event Action OnTapToSkip;
        public int ExtraCoinAmount=>extraCoins;
        public OriginalRemainingMovesItemReplacer Replacer=>replacer;
        public OriginalRemainingMovesItemExploder Exploder=>exploder;
        public int CollectionCount=>collections.Count;
        public static int ReducedExtremelyHardInitialCoinAmount=100; // 0x2183f58.
        public OriginalRemainingMovesManager(IOriginalRemainingMovesManagerServices services)
        {this.services=services??throw new ArgumentNullException(nameof(services));}
        public void CollectRemainingMovesCoin(OriginalGridCell cell,OriginalSpecialBoardItem item)
        {
            if(!IsRemainingMovesActive)return;
            int amount=GetCoinValue(item);extraCoins=unchecked(extraCoins+amount);
            var animation=services.CreateCoinCollection();
            animation.Play(new OriginalCellPoint(cell.X,cell.Y),cell.GetViewPosition(),amount);collections.Add(animation);
        }
        public static int GetCoinValue(OriginalSpecialBoardItem item)
        {
            if(item is OriginalPropellerBoardItem propeller)return propeller.RemainingMoveCoins;
            if(item is OriginalRocketBoardItem rocket)return rocket.RemainingMoveCoins;
            if(item is OriginalTntBoardItem tnt)return tnt.RemainingMoveCoins;
            if(item is OriginalLightballBoardItem)return 5;
            throw new NotSupportedException("Remaining coin value requires the original special model.");
        }
        public bool ShouldSendMoves()=>!IsTapToSkipClicked&&(services.HasAnySpecialItemOnBoard()||services.LeftMoves>0);
        public int GetInitialCoinAmount()
        {
            int difficulty=services.Difficulty;if(difficulty<1)return 10;
            if(services.RewardReductionEnabled)return difficulty==1?30:difficulty==2?50:ReducedExtremelyHardInitialCoinAmount;
            return difficulty==1?100:difficulty==2?150:200;
        }
        private void StartRewardGoal()
        {
            IsRemainingMovesActive=true;
            if(!services.IsBonus){extraCoins=unchecked(extraCoins+GetInitialCoinAmount());services.Goals.ReplaceGoalsWithCoin(extraCoins);}
        }
        public void StartRemainingMovesWithDialog(IOriginalRemainingMovesDialog view)
        {
            IsRemainingMovesActive=true;services.RemainingItemCreationStarted();
            if(!services.IsBonus){extraCoins=unchecked(extraCoins+GetInitialCoinAmount());services.Goals.ReplaceGoalsWithCoin(extraCoins);}
            dialog=view;replacer=services.CreateMoveReplacer();exploder=services.CreateItemExploder();
            if(services.ShouldSpeedUpLevelEnd)
            {replacer.PrepareToSendMoves(ExplodeItemsOnBoard);if(IsRemainingMovesReady)replacer.StartSendingMoves(.3f);}
            else replacer.SendMoves(.3f,ExplodeItemsOnBoard);
            OnTapToSkip+=SkipClicked;
        }
        public void TrySendMoves()
        {if(IsRemainingMovesActive)replacer.StartSendingMoves(.1f);else IsRemainingMovesReady=true;}
        public void CompleteRemainingMovesImmediately()
        {
            StartRewardGoal();replacer=services.CreateMoveReplacer();exploder=services.CreateItemExploder();
            if(!services.IsStory)replacer.SkipImmediately();FlowCompleted();
        }
        private void ExplodeItemsOnBoard()=>exploder.ExplodeItemsOnBoard(FlowCompleted);
        public void TriggerTapToSkipClick()
        {
            IsTapToSkipClicked=true;
            if(SkipView==null)throw new NullReferenceException();SkipView.DestroySkipView();SkipView=null;
            OnTapToSkip?.Invoke();OnTapToSkip=null;
        }
        public void SkipClicked()
        {replacer.Skip();exploder.SkipClicked();for(int i=0;i<collections.Count;i++)collections[i].TapToSkip();}
        private void FlowCompleted()
        {
            IsRemainingMovesActive=IsRemainingMovesReady=false;
            if(SkipView!=null){OnTapToSkip=null;SkipView.DestroySkipView();SkipView=null;}
            int originalGoalCount=services.Goals.GetGoalLeftCount(21);
            if(services.IsBonus)
            {int extra=unchecked(originalGoalCount-extraCoins-services.CollectedCoins);if(extra>0)extraCoins=unchecked(extraCoins+extra);}
            int notReplaced=replacer.StartMoves-replacer.ReplacedMoves;
            extraCoins=unchecked(extraCoins+Mathf.FloorToInt((float)(notReplaced*2.5f)));
            if(exploder!=null)extraCoins=unchecked(extraCoins+exploder.CalculateExtraCoinAmountOnBoard());
            // Native field initializer SHA256 3B51ACC4...AA6BA82 is exactly
            // the int32 sequence3,2,4,5. Preserve dictionary insertion order.
            var remaining=new Dictionary<int,int>();
            AddRemaining(remaining,3);AddRemaining(remaining,2);AddRemaining(remaining,4);AddRemaining(remaining,5);
            services.SessionRemainingMovesEnd(notReplaced,extraCoins,remaining);services.LevelRemainingMovesEnd();
            dialog?.CloseRemainingMovesDialog();
            int difference=unchecked(services.CollectedCoins-originalGoalCount);
            for(int i=0;i<difference;i++){services.Goals.IncreaseGoal(21);services.Goals.IncreaseGoalUi(21);}
            services.Auto.Disable();
        }
        private void AddRemaining(Dictionary<int,int> remaining,int type)
        {int count=services.CountItemOnBoard(type);if(count>=1)remaining[type]=count;}
        public void Clear() // Native Clear retains extraCoins/SkipView until Bind/owner cleanup.
        {
            collections.Clear();IsRemainingMovesActive=IsRemainingMovesReady=IsTapToSkipClicked=false;
            dialog=null;replacer=null;exploder=null;OnTapToSkip=null;
        }
    }
    public sealed partial class OriginalPlayableBoard
    {
        public OriginalRemainingMovesManager RemainingMoves { get; private set; }
        public void BindRemainingMovesManager(OriginalRemainingMovesManager manager){RemainingMoves=manager;}
        public void CollectRemainingMovesCoin(OriginalGridCell cell,OriginalSpecialBoardItem item)
        {RemainingMoves?.CollectRemainingMovesCoin(cell,item);} // Native LevelContext.Has(33) boundary.
    }
}
