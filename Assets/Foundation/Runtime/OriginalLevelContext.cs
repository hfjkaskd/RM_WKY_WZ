using System;

namespace Whitebox.Foundation
{
    public interface IOriginalGameContextUnit : IOriginalContextUnit { void Clear(bool gameExit); }
    public interface IOriginalLevelRandomContextUnit : IOriginalGameContextUnit { void ResetSeed(); }
    public interface IOriginalLevelContextClock
    {
        int FrameCount { get; }
        float DeltaTime { get; }
        float Time { get; }
        float TimeScale { get; }
        void SetTimeScale(float value);
    }
    // Native inheritance tests, including the base WinFailManager test for an
    // ordinary level. Concrete manager implementations must carry these types.
    public interface IOriginalWinFailContextUnit : IOriginalGameContextUnit { }
    public interface IOriginalStoryWinFailContextUnit : IOriginalWinFailContextUnit { }
    public interface IOriginalWaterWinFailContextUnit : IOriginalWinFailContextUnit { }
    public interface IOriginalTableWinFailContextUnit : IOriginalWinFailContextUnit { }
    public enum OriginalWinFailContextKind { Normal, Story, Water, Table }
    public interface IOriginalLevelContextFactory
    {
        IOriginalGameContextUnit CreatePermanentUnit(int id);
        IOriginalLevelContextClock CreateDeltaTimeManager();
        bool IsTableGameLevel { get; }
        bool IsStoryLevel { get; }
        bool IsWaterLevel { get; }
        IOriginalGameContextUnit CreateWinFailManager(OriginalWinFailContextKind kind);
    }

    // Original LevelContext ownership/lifecycle. This does not supply dummy
    // manager implementations: a complete application factory is required.
    public sealed class OriginalLevelContext : IOriginalGameContextUnit, IOriginalContextBehaviour
    {
        public static OriginalLevelContext Current { get; private set; }
        public static bool IsActive { get; private set; }
        public int Id => 2; // 0x3206090, GameContext identity, not a LevelContextId.
        public OriginalContextContainer Container { get; }
        private readonly IOriginalLevelContextFactory factory;
        private IOriginalLevelContextClock clock;
        public static int FrameCount => Current.clock.FrameCount;
        public static float DeltaTime => Current.clock.DeltaTime;
        public static float Time => Current.clock.Time;
        public static float TimeScale => Current.clock.TimeScale;
        public static bool IsSparseUpdateReady => FrameCount % 10 == 0; // 0x3205fc4.
        public static void SetTimeScale(float value) => Current.clock.SetTimeScale(value);

        public OriginalLevelContext(IOriginalLevelContextFactory factory) // 0x3206098.
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            Container = new OriginalContextContainer(OriginalLevelUnitRegistration.UnitCount);
            Current = this; // Publish before any permanent unit constructor.
            for (int i = 0; i < OriginalLevelUnitRegistration.PermanentUnitCount; i++)
                Container.Add(factory.CreatePermanentUnit(OriginalLevelUnitRegistration.GetPermanentUnitId(i)));
        }
        public void Bind() { } // 0x3206be0 is empty; activation binds the units.
        public static T Get<T>(int id) where T : class, IOriginalContextUnit => Current.Container.Get<T>(id);
        public static IOriginalContextUnit GetLate(int id, Func<IOriginalContextUnit> create) => Current.Container.GetLate(id, create);
        public static bool Has(int id) => Current.Container.Has(id);
        public static IOriginalContextUnit[] GetAllUnits() => Current.Container.GetAllUnits();
        public void ManualUpdate() => Container.ManualUpdate(); // 0x3206e2c has no local active gate.
        public void RemoveLateUnits() => Container.RemoveLateUnits(); // 0x3206ddc.
        public void Clear(bool gameExit) // 0x3206be4.
        {
            IsActive = false;
            OriginalExploder.Reset();
            // Native reads the published Current's borrowed array once. Unit
            // callbacks can mutate later slots, which must be read live.
            var units = GetAllUnits();
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit != null) ((IOriginalGameContextUnit)unit).Clear(gameExit);
            }
            if (gameExit) RemoveLateUnits();
        }
        public void Activate(bool forNewLevel) // 0x3206e7c.
        {
            Get<IOriginalLevelRandomContextUnit>(0).ResetSeed();
            clock = factory.CreateDeltaTimeManager() ?? throw new NullReferenceException();
            if (forNewLevel)
            {
                ArrangeWinFailManager();
                Current.Container.BindAll();
            }
            IsActive = true; // Only after successful seed/clock/arrange/bind.
        }
        private void ArrangeWinFailManager() // 0x320701c.
        {
            var existing = Container.Get<IOriginalGameContextUnit>(16);
            OriginalWinFailContextKind kind;
            if (factory.IsTableGameLevel)
            {
                if (existing is IOriginalTableWinFailContextUnit) return;
                kind = OriginalWinFailContextKind.Table;
            }
            else if (factory.IsStoryLevel)
            {
                if (factory.IsWaterLevel)
                {
                    if (existing is IOriginalWaterWinFailContextUnit) return;
                    kind = OriginalWinFailContextKind.Water;
                }
                else
                {
                    if (existing is IOriginalStoryWinFailContextUnit) return;
                    kind = OriginalWinFailContextKind.Story;
                }
            }
            else
            {
                if (existing is IOriginalWinFailContextUnit) return;
                kind = OriginalWinFailContextKind.Normal;
            }
            Container.Replace(factory.CreateWinFailManager(kind));
        }
    }
}
