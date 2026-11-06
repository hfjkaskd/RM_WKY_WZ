using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Rocket branches of ItemModelFactory.CreateItem 0x23ec1e8 and
    // CreateItemAtCell 0x23f0154. Other item classes require their own factory.
    public static class OriginalRocketItemCreation
    {
        public static OriginalRocketBoardItem CreateAtCell(int id, int tiledId, OriginalGridCell cell,
            OriginalLevelPresentation presentation, Transform parent, Func<float> levelTime,
            IOriginalRocketItemStaticState staticState, IOriginalRocketViewServices viewServices)
        {
            if (cell == null) throw new NullReferenceException();
            if (tiledId != 20 && tiledId != 30) throw new ArgumentOutOfRangeException(nameof(tiledId));
            var model = new OriginalRocketBoardItem(id, tiledId == 30 ? 0 : 1, false, tiledId, levelTime, staticState);
            // Native CreateItemAt constructs/initializes the view before the
            // model has a current cell. Cell registration happens afterwards.
            model.CreateView(presentation, parent, new Vector3(cell.X, cell.Y, 0f), viewServices);
            cell.Mediation.ClearAllItems();
            cell.Mediation.SetAllItems(model);
            return model;
        }
    }
}
