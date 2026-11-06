using UnityEngine;

namespace Whitebox.Foundation
{
    public abstract class OriginalOptionalBoosterEffect : MonoBehaviour
    {
        internal abstract void Play(OriginalPlayableBoard game, OriginalInitialBoardView board,
            Camera camera, OriginalBottomUi bottom, OriginalGridCell target);
        internal abstract void Cancel();
    }
}
