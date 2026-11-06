using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreDialogBackgroundVerification
    {
        public static void Run(FoundationController controller) =>
            controller.StartCoroutine(Verify(controller));

        private static IEnumerator Verify(FoundationController controller)
        {
            var owner = controller.CoreFlow;
            var camera = owner.UiCamera;
            var manager = new OriginalDialogManager(owner);
            var report = new Report();
            Require(controller.CurrentLevelNumber == 2 && !owner.BlocksBoardInput && Layers(camera).Length == 0,
                "The normal level does not allocate a backdrop before a dialog opens.");

            var lower = new Dialog(new OriginalDialogConfig
            {
                BackgroundFadeAlpha = .55f,
                BackgroundFadeInDelay = 1f,
                ShouldShowBackgroundOnClose = true
            });
            manager.ShowDialog(lower);
            var first = Layers(camera)[0].GetComponent<SpriteRenderer>();
            Require(Mathf.Approximately(first.color.a, .55f) && owner.BlocksBoardInput,
                "A zero-duration show is immediate and acquires the board-input lock.");

            var upper = new Dialog(new OriginalDialogConfig
            {
                BackgroundStartAlphaForAnimation = .2f,
                BackgroundFadeInDelay = .1f,
                BackgroundFadeInDuration = .12f,
                BackgroundFadeAlpha = .6f,
                BackgroundFadeOutDelay = .1f,
                BackgroundFadeOutDuration = .12f
            });
            manager.ShowDialog(upper);
            var layers = Layers(camera);
            Require(layers.Length == 2, "Nested dialogs have separate backdrop renderers.");
            var second = (layers[0].gameObject == first.gameObject ? layers[1] : layers[0]).GetComponent<SpriteRenderer>();
            Require(second.gameObject.activeSelf && second.sortingOrder == first.sortingOrder + 4 &&
                Mathf.Approximately(second.color.a, .2f) && Mathf.Approximately(first.color.a, .55f),
                "The upper backdrop starts at its configured alpha and higher order.");
            first.size = second.size = Vector2.one;
            UnityEngine.Object.FindObjectOfType<OriginalGameSceneProjection>().RefreshProjection();
            float height = camera.orthographicSize * 2f;
            Require(first.size == second.size && first.size == new Vector2(height * camera.aspect, height),
                "The scene projection updates the size of both backdrop renderers.");

            yield return new WaitForSeconds(.26f);
            Require(Mathf.Approximately(second.color.a, .6f), "The upper fade reaches its configured final alpha.");
            Close(manager, upper);
            Require(second.gameObject.activeSelf && first.gameObject.activeSelf &&
                Mathf.Approximately(second.color.a, .6f) && owner.BlocksBoardInput,
                "Only the upper backdrop starts hiding and the board remains blocked.");
            yield return new WaitForSeconds(.26f);
            Require(!second.gameObject.activeSelf && Mathf.Approximately(second.color.a, 0f) &&
                first.gameObject.activeSelf && Mathf.Approximately(first.color.a, .55f) && owner.BlocksBoardInput,
                "Closing the upper dialog leaves the lower backdrop and input lock in place.");
            report.stacked_layers_and_projection_verified = true;

            Close(manager, lower);
            Require(first.gameObject.activeSelf && Mathf.Approximately(first.color.a, .55f) && !owner.BlocksBoardInput,
                "Retained close releases the dialog and preserves the visible shade.");
            var closing = new Dialog(new OriginalDialogConfig
            {
                BackgroundFadeAlpha = .7f,
                BackgroundFadeOutDuration = .25f
            });
            manager.ShowDialog(closing);
            Require(Layers(camera).Length == 2 && Mathf.Approximately(first.color.a, .7f),
                "The new first dialog reuses the original backdrop.");
            Close(manager, closing);
            Require(first.gameObject.activeSelf && owner.BlocksBoardInput,
                "A pending fade holds the input lock after the dialog unregisters.");

            var replacement = new Dialog(new OriginalDialogConfig { BackgroundFadeAlpha = .36f });
            manager.ShowDialog(replacement);
            yield return new WaitForSeconds(.3f);
            Require(Layers(camera).Length == 2 && first.gameObject.activeSelf &&
                Mathf.Approximately(first.color.a, .36f) && owner.BlocksBoardInput,
                "The interrupted fade cannot clear the replacement shade or its input lock.");
            Close(manager, replacement);
            Require(!first.gameObject.activeSelf && !second.gameObject.activeSelf && !owner.BlocksBoardInput,
                "The final immediate close clears the shade and balances all input locks.");
            report.retained_and_interrupted_hides_verified = true;
            report.passed = true;
            File.WriteAllText("live_core_dialog_background_verification.json", JsonUtility.ToJson(report, true) + "\n");
        }

        private static CoreDialogBackground[] Layers(Camera camera) => camera.GetComponentsInChildren<CoreDialogBackground>(true);

        private static void Close(OriginalDialogManager manager, Dialog dialog)
        {
            Require(manager.CloseDialog(dialog), "The dialog is registered before close.");
            manager.DialogClosed(dialog);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class Dialog : OriginalUiDialog
        {
            private readonly OriginalDialogConfig config;
            public Dialog(OriginalDialogConfig config) : base(null, null) => this.config = config;
            public override OriginalDialogConfig GetConfig() => config;
            public override void SetSortingOffset(int offset) { }
            public override void SetTouchSorting(int order) { }
            protected override void OnShow(Vector3 position) { }
        }

        [Serializable] private sealed class Report
        {
            public bool passed, stacked_layers_and_projection_verified, retained_and_interrupted_hides_verified;
        }
    }
}
