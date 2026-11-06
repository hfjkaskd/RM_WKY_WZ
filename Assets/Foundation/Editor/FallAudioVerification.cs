using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    public static class FallAudioVerification
    {
        [Serializable] private sealed class Report
        {
            public bool passed=true,source_playback_observed;
            public int played_type;
            public string timestamp_utc=DateTime.UtcNow.ToString("o");
            public string scope="Actual configured OriginalGameAudioHost, application Unity random, GameSfx.PlayFallHit, timestamp throttle, path-loaded original fall clips and AudioSource.PlayOneShot. Immediate second call is suppressed; actual source playback observed. Full audio preferences and player-driven fall composition remain pending.";
        }
        private static OriginalGameAudioHost host;
        private static Report report;
        private static float started;
        [MenuItem("Recovery/Verify Fall Audio (Play Mode)")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying || host!=null)throw new InvalidOperationException("Run once in Play mode.");
            host=Object.FindObjectOfType<OriginalGameAudioHost>();
            try
            {
                Require(host!=null && host.GameSfx!=null && host.SoundSource.isActiveAndEnabled,"Configured running audio host");
                Require(host.GameAssets.GetClip(146).name=="fall_hit_1" && host.GameAssets.GetClip(147).name=="fall_hit_2","Original fall clip order");
                float before1=host.Playback.GetClipLastPlayedTime(146),before2=host.Playback.GetClipLastPlayedTime(147);
                Require(Time.time-before1>.1f && Time.time-before2>.1f,"Run after the native shared throttle expires");
                host.GameSfx.PlayFallHit();
                float after1=host.Playback.GetClipLastPlayedTime(146),after2=host.Playback.GetClipLastPlayedTime(147);
                Require((after1!=before1) != (after2!=before2),"Exactly one randomly selected fall clip commits a timestamp");
                host.GameSfx.PlayFallHit();
                Require(host.Playback.GetClipLastPlayedTime(146)==after1 && host.Playback.GetClipLastPlayedTime(147)==after2,"Immediate fall hit is suppressed across both variants");
                report=new Report{played_type=after1!=before1?146:147};started=Time.realtimeSinceStartup;
                EditorApplication.update+=Tick;
            }catch{Cleanup();throw;}
        }
        private static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying){Cleanup();return;}
                if(!host.SoundSource.isPlaying)
                {
                    Require(Time.realtimeSinceStartup-started<2f,"Actual audio source starts playback within two seconds");return;
                }
                report.source_playback_observed=true;
                File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"fall_audio_unity_verification.json"),JsonUtility.ToJson(report,true));
                Cleanup();Debug.Log("Fall audio Unity verification passed.");
            }catch{Cleanup();throw;}
        }
        private static void Cleanup(){EditorApplication.update-=Tick;host=null;}
        private static void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);}
    }
}
