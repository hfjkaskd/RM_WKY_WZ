using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Native box view dependencies, supplied by the current level owners.
    public sealed class OriginalBoardBoxViewServices : IOriginalBoxViewServices
    {
        private readonly OriginalLevelPresentation presentation;
        private readonly OriginalGameAudioHost audio;
        private readonly OriginalItemViewGoalCollection collection;
        public int ItemSortingLayer { get; }
        public OriginalBoardBoxViewServices(OriginalLevelPresentation presentation, OriginalGameAudioHost audio,
            OriginalItemViewGoalCollection collection, int itemSortingLayer)
        {
            this.presentation = presentation != null ? presentation : throw new ArgumentNullException(nameof(presentation));
            this.audio = audio != null ? audio : throw new ArgumentNullException(nameof(audio));
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            ItemSortingLayer = itemSortingLayer;
        }
        public OriginalGameObjectPool Pool => presentation.Pool;
        public OriginalParticlesManager Particles => presentation.Particles;
        public float LevelDeltaTime => presentation.LevelDeltaTime;
        public int BoardStartY => presentation.CurrentBoardStartY;
        public int NextApplicationRandom(int min, int max) => audio.Random.Next(min, max);
        public void PlaySfx(int type, float delay) => audio.GameSfx.PlaySfx(type, delay);
        public void TryCollectForStoryLevel(OriginalBoxItemSpriteView view, Vector3 position) =>
            collection.TryCollectForStoryLevel(view.Model.ItemType, position.x, position.y, position.z);
    }
}
