using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreLevelSettingsVerification
    {
        internal const string ReportPath = "live_core_level_settings_verification.json";
        private static FoundationController controller;
        private static CoreLevelSettingsMenu menu;
        private static CoreGameFlow flow;
        private static OriginalPlayableBoard attempt;
        private static OriginalGameAudioHost audio;
        private static Report report;
        private static int phase, frame, originalMoves;
        private static float deadline;
        private static bool originalMusicMuted, originalSoundMuted, hadMusicPreference, hadSoundPreference;

        internal static void Run(FoundationController owner)
        {
            report = new Report();
            controller = owner;
            menu = Object.FindObjectOfType<CoreLevelSettingsMenu>();
            flow = owner.CoreFlow;
            attempt = owner.Gameplay;
            audio = owner.GetComponent<OriginalGameAudioHost>();
            Require(owner.CurrentLevelNumber == 2 && menu != null && attempt.State.IsPlaying(), "Level 2 and its authored settings gear are active.");
            originalMoves = attempt.Moves.LeftMoves;
            originalMusicMuted = audio.MusicSource.mute;
            originalSoundMuted = audio.Playback.SfxMute;
            hadMusicPreference = PlayerPrefs.HasKey("CoreMusicMuted");
            hadSoundPreference = PlayerPrefs.HasKey("CoreSoundMuted");
            deadline = Time.realtimeSinceStartup + 30f;
            frame = -1;
            phase = -2;
            File.WriteAllText(ReportPath, "{\"status\":\"running\",\"passed\":false}\n");
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { Advance(); }
            catch (Exception error) { Finish(error); }
        }

        private static void Advance()
        {
            Require(Time.realtimeSinceStartup < deadline, "The settings, cancel or retry transition timed out.");
            if (phase >= 8) { AdvanceBonus(); return; }
            if (phase == -2)
            {
                flow.PressBack();
                phase++;
                return;
            }
            if (phase == -1)
            {
                var quit = flow.LevelQuitDialog;
                if (quit == null || !quit.Lifecycle.IsShowing) return;
                Require(flow.BlocksBoardInput && attempt.State.IsPlaying() && attempt.Moves.LeftMoves == originalMoves,
                    "Back from the board opens quit confirmation without changing the attempt.");
                report.back_from_board_opens_quit = true;
                flow.PressBack();
                Require(quit.Lifecycle.IsClosing || !quit.Lifecycle.IsShowing, "Back cancels the top quit confirmation.");
                report.back_cancels_quit = true;
                phase++;
                return;
            }
            if (phase == 0)
            {
                if (flow.BlocksBoardInput) return;
                Require(ReferenceEquals(controller.Gameplay, attempt) && attempt.CanUserMakeMove() &&
                    attempt.Moves.LeftMoves == originalMoves, "Back cancellation restores play with the original move budget.");
                Require(!menu.SoundButton.gameObject.activeInHierarchy, "The collapsed gear hides the three options.");
                GrassGameplayVerification.Capture(flow.UiCamera, "core_settings_closed.png");
                Click(menu.GearButton);
                Require(menu.Lifecycle.IsShowing && flow.BlocksBoardInput && attempt.State.IsPlaying() && attempt.IsUiTouching,
                    "The visible gear registers the settings dialog and owns board input.");
                phase = 1;
                return;
            }
            if (phase == 1)
            {
                if (!menu.QuitButton.interactable) return;
                float soundY = menu.SoundButton.transform.position.y;
                float musicY = menu.MusicButton.transform.position.y;
                float quitY = menu.QuitButton.transform.position.y;
                Require(Mathf.Abs(musicY - soundY - 1.84f) < .001f && Mathf.Abs(quitY - musicY - 1.84f) < .001f,
                    "Three available options settle in the recovered portrait order and spacing.");
                Click(menu.SoundButton);
                Click(menu.MusicButton);
                Require(audio.Playback.SfxMute != originalSoundMuted && audio.SoundSource.mute == audio.Playback.SfxMute &&
                    audio.MusicSource.mute != originalMusicMuted && audio.MusicSource.isPlaying != audio.MusicSource.mute,
                    "The visible sound and music buttons change the real playback sources.");
                Require((PlayerPrefs.GetInt("CoreSoundMuted") != 0) == audio.Playback.SfxMute &&
                    (PlayerPrefs.GetInt("CoreMusicMuted") != 0) == audio.MusicSource.mute,
                    "Both audio choices are persisted.");
                GrassGameplayVerification.Capture(flow.UiCamera, "core_settings_open.png");
                Click(menu.SoundButton);
                Click(menu.MusicButton);
                report.audio_toggles_verified = true;
                var shade = Object.FindObjectOfType<CoreDialogBackground>();
                var dismiss = shade != null ? shade.GetComponentInChildren<Button>() : null;
                Click(dismiss);
                Require(menu.Lifecycle.IsClosing, "The visible dimmed background is a standard dismiss Button.");
                report.background_dismiss_verified = true;
                phase = 2;
                return;
            }
            if (phase == 2 || phase == 4)
            {
                if (flow.BlocksBoardInput) return;
                Require(ReferenceEquals(controller.Gameplay, attempt) && attempt.State.IsPlaying() &&
                    attempt.Moves.LeftMoves == originalMoves && attempt.CanUserMakeMove(),
                    "Dismissing settings or cancelling quit resumes the same attempt and budget.");
                Click(menu.GearButton);
                phase++;
                return;
            }
            if (phase == 3 || phase == 5)
            {
                if (flow.LevelQuitDialog == null && !menu.Lifecycle.IsClosing && menu.QuitButton.interactable)
                {
                    if (phase == 3) flow.PressBack();
                    else Click(menu.QuitButton);
                }
                var quit = flow.LevelQuitDialog;
                if (quit == null || !quit.Lifecycle.IsShowing) return;
                Require(!menu.SoundButton.gameObject.activeInHierarchy && flow.BlocksBoardInput && attempt.State.IsPlaying(),
                    "The confirmation replaces the settings menu before changing the attempt.");
                if (phase == 3)
                {
                    report.back_from_settings_opens_quit = true;
                    var label = quit.QuitButton.GetComponentInChildren<TextMeshPro>();
                    label.ForceMeshUpdate();
                    Require(label.text == "Quit" && label.textInfo.characterCount == 4 &&
                        label.fontSharedMaterial.shader.name == "TextMeshPro/Mobile/Distance Field",
                        "The visible quit control uses the restored four-glyph label and original red SDF styling.");
                    report.quit_label_verified = true;
                    GrassGameplayVerification.Capture(flow.UiCamera, "core_settings_quit.png");
                    Click(quit.CloseButton);
                    report.cancel_verified = true;
                }
                else Click(quit.QuitButton);
                phase++;
                return;
            }
            if (phase == 6)
            {
                if (flow.FailDialog == null || !flow.FailDialog.Lifecycle.IsShowing) return;
                Require(attempt.State.IsQuit() && attempt.Session.State == 6 && attempt.Session.LeftMoves == originalMoves &&
                    controller.Progress.LevelToPlay == 2 && flow.OutOfMovesBanner == null && !audio.MusicSource.isPlaying,
                    "Explicit quit records the native quit state and goes straight to failure without spending moves.");
                GrassGameplayVerification.Capture(flow.UiCamera, "core_settings_quit_failed.png");
                Click(flow.FailDialog.RetryButton);
                report.quit_failure_verified = true;
                phase++;
                return;
            }
            var replay = controller.Gameplay;
            if (replay == null || ReferenceEquals(replay, attempt)) return;
            Require(controller.CurrentLevelNumber == 2 && replay.State.IsPlaying() && replay.Moves.LeftMoves == originalMoves &&
                menu == Object.FindObjectOfType<CoreLevelSettingsMenu>() && !flow.BlocksBoardInput,
                "Retry restores the original budget and reuses the same settings gear.");
            Click(menu.GearButton);
            Require(menu.Lifecycle.IsShowing, "The retained gear remains bound after retry.");
            report.retry_verified = true;
            controller.Progress.CompleteLevel(19);
            SceneManager.LoadSceneAsync(controller.gameObject.scene.path, LoadSceneMode.Single);
            phase++;
            deadline = Time.realtimeSinceStartup + 30f;
        }

        private static void AdvanceBonus()
        {
            if (phase == 8)
            {
                var next = Object.FindObjectOfType<FoundationController>();
                if (next == null || next == controller || next.Gameplay == null || !next.CoreFlow.IsInitialized ||
                    next.CoreFlow.BlocksBoardInput) return;
                var guide = next.CoreFlow.DirectedTutorial;
                if (guide != null && guide.IsGuiding)
                {
                    if (guide.DismissButton.interactable)
                        CoreFlowVerification.Click(guide.DismissButton, next.CoreFlow.UiCamera, false);
                    return;
                }
                controller = next;
                flow = next.CoreFlow;
                attempt = next.Gameplay;
                menu = Object.FindObjectOfType<CoreLevelSettingsMenu>();
                Require(next.CurrentLevelNumber == 20 && flow.IsBonus && attempt.Moves.LeftMoves == 20,
                    "The second scenario uses the authored bonus board.");
                Click(menu.GearButton);
                phase++;
                return;
            }
            if (phase == 9)
            {
                if (flow.LevelQuitDialog == null && !menu.Lifecycle.IsClosing && menu.QuitButton.interactable)
                    Click(menu.QuitButton);
                var quit = flow.LevelQuitDialog;
                if (quit == null || !quit.Lifecycle.IsShowing) return;
                Click(quit.QuitButton);
                Require(attempt.Session.State == 6 && flow.IsTransitioning && flow.FailDialog == null,
                    "The bonus confirmation uses the quit branch without a defeat dialog.");
                phase++;
                return;
            }
            var replay = controller.Gameplay;
            if (replay == null || ReferenceEquals(replay, attempt)) return;
            Require(controller.CurrentLevelNumber == 20 && controller.Progress.LevelToPlay == 20 &&
                replay.State.IsPlaying() && replay.Moves.LeftMoves == 20 && flow.FailDialog == null,
                "The core-only entry restores the uncompleted bonus board directly.");
            report.bonus_quit_verified = true;
            Finish(null);
        }

        private static void Click(Button button) => CoreFlowVerification.Click(button, flow.UiCamera, false);
        private static void Require(bool value, string detail)
        {
            if (!value) throw new InvalidOperationException(detail);
        }
        private static void Finish(Exception error)
        {
            EditorApplication.update -= Tick;
            RestorePreference("CoreMusicMuted", originalMusicMuted, hadMusicPreference);
            RestorePreference("CoreSoundMuted", originalSoundMuted, hadSoundPreference);
            PlayerPrefs.Save();
            report.passed = error == null;
            report.failure = error?.ToString();
            report.status = "finished";
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }
        private static void RestorePreference(string key, bool muted, bool existed)
        {
            if (existed) PlayerPrefs.SetInt(key, muted ? 1 : 0);
            else PlayerPrefs.DeleteKey(key);
        }

        [Serializable]
        private sealed class Report
        {
            public string status = "running", failure;
            public bool passed, audio_toggles_verified, background_dismiss_verified, cancel_verified, quit_failure_verified,
                retry_verified, quit_label_verified, bonus_quit_verified;
            public bool back_from_board_opens_quit, back_cancels_quit, back_from_settings_opens_quit;
        }
    }
}
