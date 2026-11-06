using System;
using System.Collections;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // The item factory must supply actual special models/views. Required
    // services intentionally have no generic match/box or empty fallback.
    public interface IOriginalSpecialCreationItem
    {
        int ItemType { get; }
        OriginalGridCell CurrentCell { get; }
        IOriginalSpecialCreationView View { get; }
        void SetSpecialItemActive(bool active);
        bool IsUnderNonFallBlockingAboveStaticItem();
        bool IsUnderNonFallBlockingAboveStaticItemSpreading();
    }

    public interface IOriginalSpecialCreationView
    {
        void SetGameObjectActive(bool active);
        void UpdateSorting(OriginalSwapSorting sorting);
        void PlayCreationAnimation(float normalizedStartTime, bool playSound);
        void SetIsUnderNonFallBlockingAboveStaticItem(bool isUnder);
        void SetCreationParticles(OriginalSpecialItemCreationParticles particles, bool isAboveStaticSpreading);
    }

    public interface IOriginalSpecialItemCreationServices
    {
        IOriginalSpecialCreationItem CreateItemAtCell(int tiledId, OriginalGridCell cell);
        void ManualStartCoroutine(IEnumerator routine);
        object CreateGameplayWait(float duration);
        int ItemSortingLayer { get; }
        bool IsPlaying { get; }
        bool IsSessionStarted { get; }
        sbyte CurrentLevelDifficulty { get; }
        bool IsAboveStaticSpreading(OriginalGridCell cell);
        void IncrementItemCreation(int itemType, bool isFromAutoMatch);
        void SpecialItemCreated(IOriginalSpecialCreationItem model, OriginalGridCell cell, int animationDelayInFrames,
            int count, bool shouldShowSticker);
        void TryCollectMadness(OriginalGridCell cell, int matchType, int count, int animationDelayInFrames,
            IOriginalSpecialCreationItem model, bool remainingMoves);
        void AddMissionEvent(int itemType, sbyte difficulty, int count);
        void UsefulMoveMade();
        void ReportMissingCreationCell();
        OriginalSpecialItemCreationParticles PlayAppearParticle(IOriginalSpecialCreationView view);
    }

    // SpecialItemCreator.CreateSpecialItemForMatchGroup 0x1c91994 and the
    // complete CreateItem iterator 0x1c92df8. Booster/portal paths are separate.
    public sealed class OriginalSpecialItemCreator
    {
        public HashSet<int> LightballSelectedColors { get; } = new HashSet<int>(); // Native +0x10, shared across lightball strategies.
        private readonly IOriginalSpecialItemCreationServices services;
        private readonly float creationDelay;
        public OriginalSpecialItemCreator(IOriginalSpecialItemCreationServices services, OriginalMatchMergeSettings settings)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            creationDelay = settings.Duration * settings.ExplodeCompleteRatio; // Native .cctor 0x1c92b98.
        }

        public static int PatternToTiledId(int pattern) // 0x1c91e28 / native table 0x400dacc.
        {
            switch (pattern)
            {
                case 2: return 10;
                case 3: return 20;
                case 4: return 30;
                case 5: case 6: return 40;
                case 7: return 50;
                default: return 0;
            }
        }

        public void CreateSpecialItemForMatchGroup(OriginalMatchGroupSnapshot group, OriginalGridCell cell, bool isFromAutoMatch = false)
        {
            if (unchecked((uint)(group.PatternType - 2)) > 5U) return;
            var model = services.CreateItemAtCell(PatternToTiledId(group.PatternType), cell);
            if (model == null) throw new NullReferenceException();
            if (!(model is OriginalSpecialBoardItem)) throw new InvalidCastException();
            model.SetSpecialItemActive(false);
            var view = model.View;
            if (view == null) throw new NullReferenceException();
            view.SetGameObjectActive(false);
            for (int i = 0; i < group.Cells.Count; i++) group.Cells.Get(i).FreezeFall();
            // MatchGroup's Cell14 is copied inline into this routine. Do not
            // retain a pooled match container that a later search can reset.
            services.ManualStartCoroutine(new CreationIterator(services, model, group, creationDelay));
            if (services.IsPlaying) services.IncrementItemCreation(model.ItemType, isFromAutoMatch);
            if (services.IsSessionStarted)
            {
                services.SpecialItemCreated(model, cell, 17, 1, true);
                services.TryCollectMadness(cell, group.MatchType, group.Cells.Count, 17, model, false);
                services.AddMissionEvent(model.ItemType, services.CurrentLevelDifficulty, 1);
            }
            services.UsefulMoveMade();
        }

        public sealed class CreationIterator : IEnumerator, IDisposable
        {
            private readonly IOriginalSpecialItemCreationServices services;
            private readonly IOriginalSpecialCreationItem model;
            private readonly OriginalMatchGroupSnapshot group;
            private readonly float delay;
            private int state;
            private bool isAboveStaticSpreading;
            public object Current { get; private set; }
            public CreationIterator(IOriginalSpecialItemCreationServices services, IOriginalSpecialCreationItem model,
                OriginalMatchGroupSnapshot group, float delay)
            {
                this.services = services; this.model = model; this.group = group; this.delay = delay;
            }
            public bool MoveNext()
            {
                int previous = state;
                if (previous != 0 && previous != 1 && previous != 2) return false;
                state = -1;
                if (previous == 0)
                {
                    Current = services.CreateGameplayWait(delay);
                    state = 1;
                    return true;
                }
                if (model == null) throw new NullReferenceException();
                if (previous == 1)
                {
                    if (model.CurrentCell == null)
                    {
                        services.ReportMissingCreationCell();
                        UnFreezeCells();
                        return false;
                    }
                    var view = model.View;
                    if (view == null) throw new NullReferenceException();
                    view.SetGameObjectActive(true);
                    view = model.View;
                    if (view == null) throw new NullReferenceException();
                    view.UpdateSorting(OriginalItemSortingRules.SpecialCreation(services.ItemSortingLayer, 0));
                    view = model.View;
                    var cell = model.CurrentCell;
                    if (view == null || cell == null) throw new NullReferenceException();
                    // Native passes y and reverse=false, which selects 1000.
                    view.UpdateSorting(OriginalItemSortingRules.Default(services.ItemSortingLayer));
                    model.SetSpecialItemActive(true);
                    view = model.View;
                    if (view == null) throw new NullReferenceException();
                    view.PlayCreationAnimation(0f, true);
                    cell = model.CurrentCell;
                    if (cell == null) throw new NullReferenceException();
                    isAboveStaticSpreading = services.IsAboveStaticSpreading(cell);
                    Current = null;
                    state = 2;
                    return true;
                }
                if (model.IsUnderNonFallBlockingAboveStaticItem() || model.IsUnderNonFallBlockingAboveStaticItemSpreading())
                {
                    var view = model.View;
                    if (view == null) throw new NullReferenceException();
                    view.SetIsUnderNonFallBlockingAboveStaticItem(true);
                }
                var specialView = model.View;
                var itemView = model.View;
                if (itemView == null) throw new NullReferenceException();
                var particles = services.PlayAppearParticle(itemView);
                if (specialView == null) throw new NullReferenceException();
                specialView.SetCreationParticles(particles, isAboveStaticSpreading);
                UnFreezeCells();
                return false;
            }
            private void UnFreezeCells()
            {
                for (int i = 0; i < group.Cells.Count; i++) group.Cells.Get(i).UnFreezeFall();
            }
            public void Reset() { throw new NotSupportedException(); }
            public void Dispose() { } // Native 0x1c92df4 is empty; no implicit unfreeze on kill.
        }
    }
}
