using UnityEngine;
using Whitebox.Foundation;

internal sealed partial class WkyGameplayBridge : IRoyalMatchSynthesisBridge
{
    public void OnSynthesisResolved(int remainingTargets, Vector3 worldPosition, Camera worldCamera)
    {
        if (loading || attemptEnded || correctionPending || worldCamera == null)
            return;

        var ui = UIModule.Instance;
        var canvas = ui != null ? ui.UICanvas : null;
        if (canvas == null)
            return;

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z <= 0f)
            return;

        // The framework reward VFX projects using its own root canvas camera.
        var root = canvas.rootCanvas;
        Vector3 rewardPosition = new Vector3(screenPoint.x, screenPoint.y, 0f);
        if (root.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            var canvasCamera = root.worldCamera != null ? root.worldCamera : ui.UICamera;
            if (canvasCamera == null || root.transform is not RectTransform rect ||
                !RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPoint, canvasCamera, out rewardPosition))
                return;
        }

        FlowModule.SynthesisLogic(remainingTargets, rewardPosition);
    }
}
