using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalContextUnit
    {
        int Id { get; }
        void Bind();
    }
    public interface IOriginalContextBehaviour : IOriginalContextUnit { void ManualUpdate(); }
    public interface IOriginalLateContextUnit { void OnRemove(); }

    // Native ContextContainer<object>. Deferred constructors are explicit typed
    // factories instead of native Activator/reflection; all mutation/bind order
    // and partial state on failure belong to this container.
    public sealed class OriginalContextContainer
    {
        private readonly IOriginalContextUnit[] units;
        private readonly List<IOriginalContextBehaviour> behaviours = new List<IOriginalContextBehaviour>();
        internal IReadOnlyList<IOriginalContextBehaviour> Behaviours => behaviours;
        public OriginalContextContainer(int unitCount) { units = new IOriginalContextUnit[unitCount]; }
        public void Add(IOriginalContextUnit unit) // 0x3f40bf8.
        {
            units[unit.Id] = unit;
            var behaviour = unit as IOriginalContextBehaviour;
            if (behaviour != null) behaviours.Add(behaviour);
        }
        public void AddToFirst(IOriginalContextUnit unit) // 0x3f40d7c.
        {
            units[unit.Id] = unit;
            var behaviour = unit as IOriginalContextBehaviour;
            if (behaviour != null) behaviours.Insert(0, behaviour);
        }
        public void Replace(IOriginalContextUnit unit) // 0x3f40eac.
        {
            // Native queries Id again at each access/comparison. Type identity
            // here does not discover or invoke reflected methods/fields.
            if (units[unit.Id] != null && units[unit.Id].GetType() == unit.GetType()) return;
            units[unit.Id] = unit;
            int index = -1;
            for (int i = 0; i < behaviours.Count; i++)
                if (behaviours[i].Id == unit.Id) { index = i; break; }
            var behaviour = unit as IOriginalContextBehaviour;
            // A plain replacement leaves the previous behaviour in the list.
            if (behaviour == null) return;
            if (index < 0) behaviours.Add(behaviour);
            else behaviours[index] = behaviour;
        }
        public IOriginalContextUnit GetLate(int id, Func<IOriginalContextUnit> create) // 0x3a139e8.
        {
            var unit = units[id];
            if (unit != null) return unit;
            unit = create();
            // Requested slot, not unit.Id. Publish before Bind to support
            // reentrant lookup; a failed Bind retains both registrations.
            units[id] = unit;
            var behaviour = unit as IOriginalContextBehaviour;
            if (behaviour != null) behaviours.Add(behaviour);
            unit.Bind();
            return unit; // Native post-bind diagnostic logging omitted.
        }
        public T Get<T>(int id) where T : class, IOriginalContextUnit // 0x3a13898.
        { return (T)units[id]; } // Native missing-unit diagnostic logging omitted.
        public bool Has(int id) => units[id] != null; // 0x3f41478.
        public IOriginalContextUnit[] GetAllUnits() => units; // 0x3f418ac, borrowed mutable array.
        public void BindAll() // 0x3f418b4: slot order, including units added during Bind.
        {
            for (int i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit != null) unit.Bind();
            }
        }
        public void ManualUpdate() // 0x3f417b0: registration order, live Count each iteration.
        {
            for (int i = 0; i < behaviours.Count; i++) behaviours[i].ManualUpdate();
        }
        public void Remove(int id) // 0x3f41308: clear slot before searching first matching behaviour.
        {
            units[id] = null;
            for (int i = 0; i < behaviours.Count; i++)
                if (behaviours[i].Id == id) { behaviours.RemoveAt(i); return; }
        }
        public void Clear() // 0x3f414b0 does not call individual unit Clear methods.
        {
            behaviours.Clear();
            for (int i = 0; i < units.Length; i++) units[i] = null;
        }
        private static readonly Predicate<IOriginalContextBehaviour> IsLateBehaviour = value => value is IOriginalLateContextUnit;
        public void RemoveLateUnits() // 0x3f41560.
        {
            behaviours.RemoveAll(IsLateBehaviour);
            for (int i = 0; i < units.Length; i++)
            {
                var late = units[i] as IOriginalLateContextUnit;
                if (late == null) continue;
                late.OnRemove();
                // Clear after callback, including a new unit installed into
                // this same slot. Failure retains the current slot as native.
                units[i] = null;
            }
        }
    }
    // Native LevelContext constructor registration protocol. Values are stable
    // unit identities/order, not artist or designer tuning parameters. No enum
    // reflection is needed to size the original 112-slot table.
    public static class OriginalLevelUnitRegistration
    {
        public const int UnitCount = 112;
        private static readonly int[] permanentOrder =
        { 21, 15, 12, 111, 11, 0, 1, 2, 14, 3, 4, 5, 18, 22, 13, 32, 33, 84, 92, 7, 8, 16, 6, 17, 51, 74, 98 };
        public static int PermanentUnitCount => permanentOrder.Length;
        public static int GetPermanentUnitId(int index) => permanentOrder[index];
        public static OriginalContextContainer CreatePermanentUnits(Func<int, IOriginalContextUnit> create)
        {
            if (create == null) throw new ArgumentNullException(nameof(create));
            var container = new OriginalContextContainer(UnitCount);
            for (int i = 0; i < permanentOrder.Length; i++) container.Add(create(permanentOrder[i]));
            return container;
        }
    }
}
