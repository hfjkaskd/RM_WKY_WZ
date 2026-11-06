using UnityEngine;

namespace Whitebox.Foundation
{
    // The native IExplodeTarget protocol is shared by ItemModel and
    // StaticItemModel. Static targets never enter the falling-item mediator.
    public interface IOriginalExplodeTarget
    {
        OriginalGridCell CurrentCell { get; }
        bool HasView { get; }
        int GoalType { get; }
        bool IsObstacle { get; }
        int RemainingExplodeCount();
        int IncomingExploderCount { get; }
        int GetExtraIncomingExplodeCount(int guidedType);
        int GetExplodeScore(int guidedType);
        bool HasExplodeScore(OriginalExplodeData data);
        bool IsValidExplodeTarget(int guidedType);
        void AddIncomingExploder(OriginalExplodeData data);
        void RemoveIncomingExploder(OriginalExplodeData data);
        bool CanAddPropellerAroundExploder();
        Vector3 GetViewPosition();
        Vector3 GetMasterViewCenterPosition();
    }
    public partial class OriginalBoardItem : IOriginalExplodeTarget
    {
        OriginalGridCell IOriginalExplodeTarget.CurrentCell => Mediator.CurrentCell?.Owner;
    }
}
