using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Standard Unity Input on every target, matching the native mouse-button
    // polling (Unity supplies mobile touch-to-mouse emulation). The game
    // context polls it after the EventSystem and before the level units.
    public sealed class OriginalGameTouchInput : MonoBehaviour, IOriginalGameTouchInput
    {
        [SerializeField] private Camera pointerCamera;
        public OriginalGameTouchListener Listener { get; private set; }
        public Camera PointerCamera => pointerCamera;
        public void Bind(IOriginalGameTouchServices services)
        {
            if (pointerCamera == null) throw new InvalidOperationException("Game touch camera is not configured.");
            if (Listener != null) throw new InvalidOperationException("Game touch context is already bound.");
            Listener = new OriginalGameTouchListener(this, services);
        }
        public void Unbind() { Listener = null; }
        public Vector2 GetMouseWorldPosition()
        {
            var screen = Input.mousePosition;
            // CameraManager.ScreenToWorldPoint 0x20057c8 explicitly passes z=0.
            var world = pointerCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            return new Vector2(world.x, world.y);
        }
        public bool GetMouseButtonDown() => Input.GetMouseButtonDown(0);
        public bool GetMouseButton() => Input.GetMouseButton(0);
        public bool GetMouseButtonUp() => Input.GetMouseButtonUp(0);
        internal void ManualUpdate()
        {
            if (isActiveAndEnabled) Listener?.ManualUpdate();
        }
    }
}
