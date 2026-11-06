using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // IncomingExploderContainer 0x318ce80..0x318cf78. Reservations are id based:
    // a repeated add/remove must not consume/release another pending hit.
    public sealed class OriginalIncomingExploderContainer
    {
        private readonly HashSet<int> incoming = new HashSet<int>();
        public int Count => incoming.Count;
        public void Add(int id) { incoming.Add(id); }
        public void Remove(int id) { incoming.Remove(id); }
    }

    // ExtraIncomingExploderContainer 0x318cc60 / 0x318cd10 / 0x318cdc0.
    public sealed class OriginalExtraIncomingExploderContainer
    {
        private readonly HashSet<int> incoming = new HashSet<int>();
        private int tnt, horizontalRocket, verticalRocket;
        public void Add(int id, int trigger)
        {
            if (!incoming.Add(id)) return;
            Change(trigger, 1);
        }
        public void Remove(int id, int trigger)
        {
            if (!incoming.Remove(id)) return;
            Change(trigger, -1);
        }
        private void Change(int trigger, int delta)
        {
            switch (trigger)
            {
                case 10: verticalRocket = unchecked(verticalRocket + delta); break;
                case 11: horizontalRocket = unchecked(horizontalRocket + delta); break;
                case 13: tnt = unchecked(tnt + delta); break;
            }
        }
        public int GetCount(int guidedType) => guidedType == 1 ? verticalRocket :
            guidedType == 2 ? horizontalRocket : guidedType == 3 ? tnt : 0;
    }

    public partial class OriginalBoardItem
    {
        private readonly OriginalIncomingExploderContainer incomingExploders = new OriginalIncomingExploderContainer();
        private readonly OriginalExtraIncomingExploderContainer extraIncomingExploders = new OriginalExtraIncomingExploderContainer();
        public int IncomingExploderCount => incomingExploders.Count;
        public virtual int RemainingExplodeCount() => unchecked(layer - incomingExploders.Count); // 0x2b46578 reads the field, not the virtual getter.
        public virtual bool WillTargetStayAfterIncomingExploders() => RemainingExplodeCount() > 0; // 0x2b46554.
        public virtual void AddIncomingExploder(OriginalExplodeData data) // 0x2b466e4.
        { incomingExploders.Add(data.Id); extraIncomingExploders.Add(data.Id, data.Trigger); }
        public virtual void RemoveIncomingExploder(OriginalExplodeData data) // 0x2b46728.
        { incomingExploders.Remove(data.Id); extraIncomingExploders.Remove(data.Id, data.Trigger); }
        protected void AddNonExtraIncomingExploder(int id) => incomingExploders.Add(id); // 0x2b4bd94.
        protected void RemoveNonExtraIncomingExploder(int id) => incomingExploders.Remove(id); // 0x2b4bdb4.
        protected void AddExtraIncomingExploder(OriginalExplodeData data) => extraIncomingExploders.Add(data.Id, data.Trigger);
        protected void RemoveExtraIncomingExploder(OriginalExplodeData data) => extraIncomingExploders.Remove(data.Id, data.Trigger);
        public int GetExtraIncomingExplodeCount(int guidedType) => extraIncomingExploders.GetCount(guidedType);
        public virtual bool IsValidExplodeTarget(int guidedType) => HasView; // 0x2b45e0c.
    }

    public sealed partial class OriginalMatchBoardItem
    {
        internal bool IsSelectedByColorMixer => selectedByColorMixer;
        public override bool IsValidExplodeTarget(int guidedType) => HasView && !selectedByColorMixer; // 0x21a7a60.
    }

    public abstract partial class OriginalSpecialBoardItem
    {
        public override bool IsValidExplodeTarget(int guidedType) => HasView && !IsPreparedForSpecialUse; // 0x27b3f90.
    }
}
