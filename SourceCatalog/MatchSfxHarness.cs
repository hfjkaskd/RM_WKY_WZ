using System;
using System.Collections.Generic;
using Whitebox.Foundation;
namespace UnityEngine
{
    public class ScriptableObject { }
    public class AudioClip
    {
        public int Id;
        public static bool operator ==(AudioClip a, AudioClip b) => ReferenceEquals(a, b);
        public static bool operator !=(AudioClip a, AudioClip b) => !ReferenceEquals(a, b);
        public override bool Equals(object v) => ReferenceEquals(this,v);
        public override int GetHashCode() => Id;
    }
    public class AudioSource { public void PlayOneShot(AudioClip clip) { Verify.Trace.Add("play," + clip.Id); } }
    public static class Resources
    { public static T Load<T>(string path) where T:class => new AudioClip { Id = int.Parse(path) } as T; }
    public static class Random
    {
        public static int Returned;
        public static void InitState(int seed) { }
        public static int Range(int min,int max) { Verify.Trace.Add("int,"+min+","+max); return Returned; }
        public static float Range(float min,float max) => throw new InvalidOperationException();
    }
}
namespace Whitebox.Foundation
{ public class OriginalBoxViewAssets { public UnityEngine.AudioClip GetSfxClip(int type) => throw new InvalidOperationException(); } }
static class Verify
{
    public static List<string> Trace = new List<string>();
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            int[] v = Array.ConvertAll(line.Split(','),int.Parse); Trace.Clear();
            if (v[0] == -1)
            {
                var catalog = new OriginalGameSfxAssets { matchExplodePaths = new[]{"1","2","3","4","5","6"}, matchSwipePath = "7" };
                Console.WriteLine(catalog.GetClip(v[1]).Id); continue;
            }
            var assets = new OriginalGameSfxAssets { matchExplodePaths = (v[0]&2)!=0 ? null : new string[v[3]] };
            var audio = new OriginalSfxPlayback(() => {Trace.Add("time,3f800000");return 1f;},
                type => {Trace.Add("load,"+type);return new UnityEngine.AudioClip{Id=1};}, new UnityEngine.AudioSource(),
                type => Trace.Add("missing,"+type));
            var game = new OriginalGameSfx((v[0]&4)!=0 ? null : audio,
                (v[0]&8)!=0 ? null : new OriginalApplicationRandom(), () => 1f);
            game.BindMatchAssets((v[0]&1)!=0 ? null : assets);
            game.SetNoGameSound((v[0]&16)!=0); game.matchExplodeCounter = v[2];
            UnityEngine.Random.Returned = v[4]; string result = "-";
            try { game.PlayMatchExplode(v[1]!=0); } catch(NullReferenceException) { result="error"; }
            Console.WriteLine(result+","+game.MatchExplodeCounter+":"+string.Join(";",Trace));
        }
    }
}
