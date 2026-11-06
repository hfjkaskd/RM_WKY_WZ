using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalEgoExtraMoves
    {
        bool IsEgoAdditionalMoveAvailable();
        void IncrementEgo1SeenCount();
        bool HasMovesToAdd();
        int GetAdditionalMoveCount();
    }
    public interface IOriginalEgoAudio
    { void PlaySound(int id, float offset); void ResumeGameMusic(); }
    public interface IOriginalEgoLevelSave { void SaveImmediate(); }
    public interface IOriginalEgoRestoreReader
    { int GetEgoPurchaseCount(); int GetLeftMoves(); int GetEgoPackageItemThrowInProgress(); }
    public interface IOriginalEgoGrid
    {
        void CalculateGridCellPatternData();
        List<OriginalGridCell> GetCellsToThrowEgoRocket();
        List<OriginalGridCell> GetCellsToThrowEgoTnt();
        List<OriginalGridCell> GetCellsToThrowEgoLightball();
        bool CanSpreadJelly(OriginalGridCell cell, bool firstLayer);
        int SelectEgoRocketType(OriginalGridCell cell, bool canSpreadJelly);
    }
    public interface IOriginalEgoSpecialCreator
    { void CreateSpecialItemForBooster(OriginalGridCell cell, int itemType); }
    public interface IOriginalEgoContext : IOriginalCoinSpendingContext
    {
        OriginalEgoPackage[] GetPackages();
        void ConfigureMultiplierSelectionEgoPriceValues(OriginalEgoPackage package, int index);
        OriginalActiveLevelData GetCurrentSession();
        void SetEgoOfferStatus(bool offerInProgress, bool showEgoOfferForCurrentProgress);
        void EpisodeOnEgoAccepted();
        void EpisodeOnEgoShownFirst();
        void SetCoinShopEgoOfferPurchasedInCurrentGameSession(bool value);
        OriginalInviteFriendsStartData GetCurrentInviteFriendsData();
        void CalculateRemainingItems(int step);
        void ChargerTryContinueThrow();
        IOriginalEgoLevelSave GetLevelSaveIfExists();
        IOriginalEgoGrid GetGrid();
        IOriginalEgoSpecialCreator GetSpecialCreator();
        Coroutine ManualStartCoroutine(IEnumerator routine);
        float GameplayDeltaTime { get; }
    }

    // Native Ego state, eligibility, paid/free continuation and item coroutine.
    // Catalog/AB, extra moves, episode/offer/charger/save and grid selection
    // remain required owners. No fabricated inactive runtime implementation.
    public sealed class OriginalEgoManager : IOriginalEgoSaveState
    {
        private readonly OriginalGameStateManager state;
        private readonly OriginalMoveManager moves;
        private readonly IOriginalEgoExtraMoves extra;
        private readonly IOriginalEgoAudio audio;
        private readonly IOriginalEgoContext context;
        private readonly Func<float> gameplayDeltaTime;
        private int currentStep = -1, warningStep;
        private bool didLoadDuringEgoFlow;
        public bool EgoPackageItemThrowInProgress { get; private set; }
        bool IOriginalEgoSaveState.ItemThrowInProgress => EgoPackageItemThrowInProgress;
        public int CurrentStep => currentStep;
        public int WarningStep => warningStep;
        public OriginalEgoManager(OriginalGameStateManager state, OriginalMoveManager moves,
            IOriginalEgoExtraMoves extra, IOriginalEgoAudio audio, IOriginalEgoContext context)
        {
            this.state = state; this.moves = moves; this.extra = extra; this.audio = audio; this.context = context;
            gameplayDeltaTime = () => (this.context ?? throw new NullReferenceException()).GameplayDeltaTime;
        }
        public void ResetEgoStep() { currentStep = -1; } // 0x1ca4d50.
        public void ResetWarningStep() { warningStep = 0; }
        public void OnExtraWarnShown() { warningStep = unchecked(warningStep + 1); }
        public bool HasShownEgo() => currentStep != -1; // 0x1ca5e80, native CMN/CSET NE.
        public void EgoShown() // 0x1ca4d64.
        {
            var owner = context ?? throw new NullReferenceException();
            if (currentStep < 0)
            {
                if (!didLoadDuringEgoFlow)
                {
                    if ((extra ?? throw new NullReferenceException()).IsEgoAdditionalMoveAvailable())
                        (extra ?? throw new NullReferenceException()).IncrementEgo1SeenCount();
                    owner.EpisodeOnEgoShownFirst();
                }
                currentStep = unchecked(currentStep + 1);
            }
            didLoadDuringEgoFlow = false;
            owner.SetCoinShopEgoOfferPurchasedInCurrentGameSession(false);
            SetInviteFriendsEgoPrice();
            if (currentStep <= 2) owner.CalculateRemainingItems(unchecked(currentStep + 1));
        }
        public void SetInviteFriendsEgoPrice() // 0x1ca4edc.
        {
            var invite = (context ?? throw new NullReferenceException()).GetCurrentInviteFriendsData();
            var package = GetEgoPackage();
            if (package == null || invite == null) throw new NullReferenceException();
            invite.SetEgoPrice(package.CoinPrice);
        }
        private int GetEgoPackageIndex(int step) // 0x1ca5020.
        {
            var packages = (context ?? throw new NullReferenceException()).GetPackages() ?? throw new NullReferenceException();
            return step < 0 ? 0 : Math.Min(step, unchecked(packages.Length - 1));
        }
        public OriginalEgoPackage GetEgoPackage() // 0x1ca4f68.
        {
            int index = GetEgoPackageIndex(currentStep);
            var packages = context.GetPackages() ?? throw new NullReferenceException();
            var package = packages[index];
            context.ConfigureMultiplierSelectionEgoPriceValues(package, index);
            return package;
        }
        public bool CanPlayOn() // 0x1ca5374.
        {
            var package = GetEgoPackage();
            var inventory = context.GetCurrentInventory();
            if (package == null || inventory == null) throw new NullReferenceException();
            return inventory.HasEnoughCoins(package.CoinPrice);
        }
        public void PlayOn(bool isFree) // 0x1ca53e0.
        {
            var package = GetEgoPackage();
            var session = context.GetCurrentSession() ?? throw new NullReferenceException();
            int count = session.EgoPurchaseCount;
            var packages = context.GetPackages() ?? throw new NullReferenceException();
            int nameIndex = Math.Min(unchecked(count + 1), packages.Length);
            var inventory = context.GetCurrentInventory();
            // ScriptString90218048: literal Ego-, plus native current-culture Int32.ToString.
            string spendingName = "Ego-" + nameIndex.ToString();
            int price = isFree ? 0 : (package ?? throw new NullReferenceException()).CoinPrice;
            var spending = new OriginalSpendingData(spendingName, price);
            if (inventory == null) throw new NullReferenceException();
            if (!inventory.SpendCoins(spending, context, null, warningStep)) return;
            context.SetEgoOfferStatus(false, false);
            session = context.GetCurrentSession() ?? throw new NullReferenceException();
            session.EgoPurchaseCount = unchecked(session.EgoPurchaseCount + 1);
            context.EpisodeOnEgoAccepted();
            (state ?? throw new NullReferenceException()).SetStateToPlay();
            if (package == null) throw new NullReferenceException();
            if (extra == null) throw new NullReferenceException();
            int amount = package.Moves;
            if (extra.HasMovesToAdd()) amount = unchecked(amount + (extra ?? throw new NullReferenceException()).GetAdditionalMoveCount());
            (moves ?? throw new NullReferenceException()).SetMaxMoves(amount);
            if (isFree) (audio ?? throw new NullReferenceException()).PlaySound(144, .04f);
            (audio ?? throw new NullReferenceException()).ResumeGameMusic();
            context.ChargerTryContinueThrow();
            EgoPackageItemThrowInProgress = true;
            currentStep = unchecked(currentStep + 1);
            context.GetLevelSaveIfExists()?.SaveImmediate();
            var routine = PutEgoItemsToGrid(package);
            context.ManualStartCoroutine(routine);
        }
        public void CancelOffer() => (state ?? throw new NullReferenceException()).SetStateToFail(); // 0x1ca5824.
        public void Clear(bool gameExit) // 0x1ca5840: gameExit itself is not tested.
        {
            var game = state ?? throw new NullReferenceException();
            if (game.IsWin() || game.IsFail() || game.IsNoGame() || game.IsNoPossibleMove())
            { currentStep = -1; warningStep = 0; }
        }
        public void RestoreFromSaveData(IOriginalEgoRestoreReader reader) // 0x1ca58b0.
        {
            if (reader == null) throw new NullReferenceException();
            int count = reader.GetEgoPurchaseCount();
            currentStep = count == 0 ? -1 : count;
            if (reader.GetLeftMoves() > 0) EgoPackageItemThrowInProgress = reader.GetEgoPackageItemThrowInProgress() == 1;
            else didLoadDuringEgoFlow = true;
        }
        public void TryThrowEgoPackageItemThrow() // 0x1ca596c, raw package without price reconfiguration.
        {
            if (!EgoPackageItemThrowInProgress) return;
            var packages = (context ?? throw new NullReferenceException()).GetPackages();
            int index = GetEgoPackageIndex(unchecked(currentStep - 1));
            if (packages == null) throw new NullReferenceException();
            var routine = PutEgoItemsToGrid(packages[index]);
            context.ManualStartCoroutine(routine);
        }
        public IEnumerator PutEgoItemsToGrid(OriginalEgoPackage package) // 0x1e29c14.
        {
            (state ?? throw new NullReferenceException()).Operations.OperationStart(8);
            var grid = (context ?? throw new NullReferenceException()).GetGrid();
            if (package == null) throw new NullReferenceException();
            if (package.GetRocketCount() != 0 || package.GetTntCount() != 0 || package.GetLightballCount() != 0)
                (grid ?? throw new NullReferenceException()).CalculateGridCellPatternData();
            byte count = package.GetRocketCount();
            if (count != 0)
            {
                var cells = (grid ?? throw new NullReferenceException()).GetCellsToThrowEgoRocket();
                for (int i = 0; i < count; i++)
                {
                    if (cells == null) throw new NullReferenceException();
                    if (i >= cells.Count) break;
                    var cell = cells[i] ?? throw new NullReferenceException();
                    bool jelly = grid.CanSpreadJelly(cell, false);
                    int type = grid.SelectEgoRocketType(cell, jelly);
                    (context.GetSpecialCreator() ?? throw new NullReferenceException()).CreateSpecialItemForBooster(cell, type);
                    yield return new WaitForSeconds(.5f);
                }
            }
            count = package.GetTntCount();
            if (count != 0)
            {
                var cells = (grid ?? throw new NullReferenceException()).GetCellsToThrowEgoTnt();
                for (int i = 0; i < count; i++)
                {
                    if (cells == null) throw new NullReferenceException();
                    if (i >= cells.Count) break;
                    var creator = context.GetSpecialCreator();
                    var cell = cells[i];
                    (creator ?? throw new NullReferenceException()).CreateSpecialItemForBooster(cell, 40);
                    yield return new OriginalGameplayWait(.5f, gameplayDeltaTime);
                }
            }
            count = package.GetLightballCount();
            if (count != 0)
            {
                var cells = (grid ?? throw new NullReferenceException()).GetCellsToThrowEgoLightball();
                for (int i = 0; i < count; i++)
                {
                    if (cells == null) throw new NullReferenceException();
                    if (i >= cells.Count) break;
                    var creator = context.GetSpecialCreator();
                    var cell = cells[i];
                    (creator ?? throw new NullReferenceException()).CreateSpecialItemForBooster(cell, 50);
                    if (i + 1 < count) yield return new OriginalGameplayWait(.5f, gameplayDeltaTime);
                }
            }
            EgoPackageItemThrowInProgress = false;
            (state ?? throw new NullReferenceException()).Operations.OperationFinish(8);
        }
    }
}
