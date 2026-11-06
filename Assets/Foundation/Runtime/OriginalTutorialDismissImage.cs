using UnityEngine;
using UnityEngine.UI;

namespace Whitebox.Foundation
{
    public sealed class OriginalTutorialDismissImage : Image
    {
        private OriginalDirectedTutorialView guide;
        internal void Bind(OriginalDirectedTutorialView value) => guide = value;

        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) =>
            base.IsRaycastLocationValid(screenPoint, eventCamera) && (guide == null || guide.CanDismissAt(screenPoint));
    }
}
