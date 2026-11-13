using UnityEngine;
using Whitebox.Foundation;

internal sealed partial class WkyGameplayBridge : IRoyalMatchSynthesisBridge, IRoyalMatchEffectiveActionBridge
{
    public void OnEffectiveMatch()
    {
        if (loading || attemptEnded) return;
        FlowModule.ReportFirstEffectiveAction();
    }

    public void OnSynthesisResolved(int remainingTargets, Vector3 worldPosition, Camera worldCamera)
    {
        if (loading || attemptEnded)
            return;

        // Persist the effective move even if reward VFX cannot project to the UI.
        FlowModule.ReportFirstEffectiveAction();
        if (worldCamera == null) return;

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
