using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalCellViewDelegate
    {
        OriginalCellPoint Point { get; }
        OriginalCellNeighbors Neighbors { get; }
        bool IsFillingCell { get; }
    }

    public interface IOriginalCellViewFactory
    {
        Transform CellParent { get; }
        OriginalCellAssets LoadCellAssets();
        bool IsWaterLevel { get; }
        bool IsShifterLevel { get; }
        float LevelDeltaTime { get; }
    }

    public partial class OriginalGridCell : IOriginalCellViewDelegate
    {
        public OriginalCellPoint Point => new OriginalCellPoint(X, Y);
        public OriginalCellView CellView { get; internal set; }
        public Vector3 GetViewPosition() => CellView.transform.position; // 0x3191dd4.
        public Vector3 GetViewLocalPosition() => CellView.transform.localPosition; // 0x319db58.
    }
}
