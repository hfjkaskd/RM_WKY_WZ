using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalRocketFlightServices : IOriginalRocketViewServices
    {
        OriginalGameStateManager State { get; }
        OriginalCellGrid Grid { get; }
        OriginalBoardLayout RocketLayout { get; }
        OriginalCoroutinePlayer RocketCoroutines { get; }
        float RocketDeltaTime { get; }
        float CameraWidth { get; }
        float CameraHeight { get; }
        int ParticlesSortingLayer { get; }
        OriginalRocketItemParticles SpawnRocketParticles();
    }
    public sealed partial class OriginalPlayableBoard : IOriginalRocketFlightServices, IOriginalRocketExplosionServices
    {
        private readonly Camera gameCamera;
        private readonly OriginalGameSceneProjection gameSceneProjection;
        public OriginalBoardLayout RocketLayout => board.Layout;
        public OriginalCoroutinePlayer RocketCoroutines => board.Presentation.Coroutines;
        public float RocketDeltaTime => board.Presentation.LevelDeltaTime;
        // Flight consumes the configured game camera's actual dimensions.
        // Full original CameraManager sizing/safe-area startup remains separate.
        public float CameraHeight => gameCamera.orthographicSize * 2f;
        public float CameraWidth => CameraHeight * gameCamera.aspect;
        public bool IsDeviceWide // 0x2003628 / 0x200568c; mobile IsLandscape is false.
        {
            get
            {
                float aspect = gameCamera.aspect;
                return (aspect > 1f ? 1f / aspect : aspect) >= board.Presentation.RocketAssets.wideDeviceAspect;
            }
        }
        public bool IsInCurrentBoard(OriginalGridCell cell) => cell.BoardId == board.Layout.CurrentBoardIndex;
        public OriginalRocketItemParticles SpawnRocketParticles()
        {
            var particles = board.Presentation.SpawnRocketParticles();
            particles.BindOrientation(gameSceneProjection);
            return particles;
        }
    }
    public sealed partial class OriginalRocketItemSpriteView
    {
        public void Play(OriginalExplodeData data, OriginalGridCell cell,
            System.Collections.Generic.List<OriginalCellPoint> first, System.Collections.Generic.List<OriginalCellPoint> second) // 0x27bb0fc.
        {
            creationAnimationFinished = false;
            var flight = (IOriginalRocketFlightServices)services;
            UpdateSortingForAnimation(new OriginalSwapSorting(flight.ParticlesSortingLayer, -1, false), true);
            strategy.Play(data, cell, first, second);
        }
        public void RocketPartStart() // 0x27bbeb4.
        { var state = ((IOriginalRocketFlightServices)services).State; state.Operations.OperationStart(4); state.StartSpecialOperation(); }
        public void RocketPartEnd() // 0x27bbee8.
        { var state = ((IOriginalRocketFlightServices)services).State; state.Operations.OperationFinish(4); state.FinishSpecialOperation(); }
        public void Recycle() => services.Pool.Recycle(this); // 0x27bbf1c.
    }
}
