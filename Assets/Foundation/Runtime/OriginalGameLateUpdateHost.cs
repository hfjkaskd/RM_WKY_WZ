using UnityEngine;

namespace Whitebox.Foundation
{
    // GameContextController.LateUpdate (0x1e3a270) dispatches at order -70.
    public sealed class OriginalGameLateUpdateHost : MonoBehaviour
    {
        [SerializeField] private OriginalContextUpdateHost context;

        private void LateUpdate() => context.DispatchGameLateUpdate();
    }
}
