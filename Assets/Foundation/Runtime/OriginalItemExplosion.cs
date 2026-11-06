using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // ExploderContainer 0x2edbfd0 / 0x2eec020 / 0x2eec078 / 0x2eec0d0.
    public sealed class OriginalExploderContainer
    {
        private readonly HashSet<int> exploders = new HashSet<int>();
        public void AddToExploders(int id) { exploders.Add(id); }
        public bool ContainsExploder(int id) => exploders.Contains(id);
        public bool RemoveExploder(int id) => exploders.Remove(id);
    }

    public interface IOriginalItemGoalServices
    {
        void DecreaseGoal(int goalType);
        void CollectAsGoalUi(OriginalBoardItem item);
    }

    public partial class OriginalBoardItem
    {
        protected int layer = 1;
        private OriginalExploderContainer exploderContainer = new OriginalExploderContainer();
        private IOriginalItemGoalServices goalServices;
        public int GoalType { get; set; }
        public virtual int LayerCount => layer; // 0x2b45e1c.
        // ItemModel.GetSaveData / SetRestoredData, 0x2b48688 and 0x2b4877c.
        internal virtual short SaveRestorableData() => (short)layer;
        internal virtual void RestoreItemData(short data) => layer = data & 0x7fff;
        protected IOriginalItemGoalServices GoalServices => goalServices ?? throw new NullReferenceException();
        public OriginalBoardItem(int id, int itemType, int layer) : this(id, itemType) { this.layer = layer; }
        public void BindGoalServices(IOriginalItemGoalServices value) { goalServices = value ?? throw new ArgumentNullException(nameof(value)); }
        public void ManualSetExploderContainer(OriginalExploderContainer container) { exploderContainer = container; } // 0x2b4676c.
        protected bool ContainsExploder(int id) => exploderContainer.ContainsExploder(id);
        protected bool RegisterExploder(int id)
        {
            if (exploderContainer == null) throw new NullReferenceException();
            if (exploderContainer.ContainsExploder(id)) return false;
            exploderContainer.AddToExploders(id);
            return true;
        }
        public virtual void Explode(OriginalExplodeData data) // 0x2b468dc.
        {
            if (RegisterExploder(data.Id)) TryExplode(data);
        }
        public virtual void ExplodeAll(OriginalExplodeData data) // 0x2b46978.
        {
            if (!RegisterExploder(data.Id)) return;
            if (layer == 1) TryExplode(data);
            else if (layer > 1)
            {
                TryExplode(data);
                layer = 1;
                TryExplode(data);
            }
        }
        protected virtual void TryExplode(OriginalExplodeData data)
        {
            throw new NotSupportedException("Item-specific explosion is not restored.");
        }
        protected virtual void FinalExplodeCompleted(float freezeDuration = 0.15f) // 0x2b46aa0.
        {
            var mediation = Mediator.CurrentCell;
            if (mediation == null || mediation.Owner == null) throw new NullReferenceException();
            mediation.Owner.FreezeFallForDuration(freezeDuration);
            HasView = false;
            Mediator.ClearFromAllRegisteredCells();
            CollectAsGoal(true);
            InvokeExplodeEvent();
        }
        protected virtual void CollectAsGoal(bool updateUi = true) // 0x2b46c28.
        {
            GoalServices.DecreaseGoal(GoalType);
            if (updateUi) GoalServices.CollectAsGoalUi(this);
        }
    }
}
