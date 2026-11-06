using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;
namespace UnityEngine
{
    public class ScriptableObject { }
    public class Sprite { }
    public sealed class AudioClip
    {
        public int Id;
        public static bool operator ==(AudioClip a,AudioClip b) =>
            (ReferenceEquals(a,null)||a.Id<0) ? ReferenceEquals(b,null)||b.Id<0 : !ReferenceEquals(b,null)&&a.Id==b.Id;
        public static bool operator !=(AudioClip a,AudioClip b)=>!(a==b);
        public override bool Equals(object value)=>ReferenceEquals(this,value);
        public override int GetHashCode()=>Id;
    }
    public sealed class AudioSource
    { public void PlayOneShot(AudioClip clip) { Verify.Trace.Add("play,"+clip.Id); } }
    public static class Resources
    {
        public static readonly Dictionary<string,object> Values=new Dictionary<string,object>();
        public static T Load<T>(string path)where T:class => Values.ContainsKey(path)?(T)Values[path]:null;
    }
    public static class Random
    {
        public static int Returned;
        public static float FloatReturned;
        public static void InitState(int seed){ }
        public static int Range(int min,int max) { Verify.Trace.Add("int,"+min+","+max);return Returned; }
        public static float Range(float min,float max) { Verify.Trace.Add("float,"+Verify.Bits(min)+","+Verify.Bits(max));return FloatReturned; }
    }
}
static class Verify
{
    public static List<string> Trace=new List<string>();
    public static string Bits(float v)=>BitConverter.ToUInt32(BitConverter.GetBytes(v),0).ToString("x8");
    static float Float(string text)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(text,NumberStyles.HexNumber)),0);
    static UnityEngine.AudioClip Clip(int id)=>id==0?null:new UnityEngine.AudioClip{Id=id};
    static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var parts=line.Split('#');Trace.Clear();
            if(parts[0]=="R")
            {
                var v=parts[1].Split(',');var random=new OriginalApplicationRandom();
                UnityEngine.Random.Returned=int.Parse(v[3]);UnityEngine.Random.FloatReturned=Float(v[4]);string result;
                if(v[0]=="i")result=random.Next(int.Parse(v[1]),int.Parse(v[2])).ToString();
                else if(v[0]=="x")result=random.Next(int.Parse(v[2])).ToString();
                else if(v[0]=="b")result=random.NextBool()?"1":"0";
                else result=Bits(random.Next(Float(v[1]),Float(v[2])));
                Console.WriteLine(result+":"+string.Join(";",Trace));continue;
            }
            if(parts[0]=="B")
            {
                var v=Array.ConvertAll(parts[1].Split(','),int.Parse);
                UnityEngine.Resources.Values.Clear();
                UnityEngine.Resources.Values.Add("o0",Clip(v[1]));UnityEngine.Resources.Values.Add("o1",Clip(v[2]));
                var entries=new OriginalBoxViewAssets.AlternativeClip[4];int[] types={34,35,37,38};
                for(int i=0;i<4;i++)
                {
                    entries[i]=new OriginalBoxViewAssets.AlternativeClip{Type=types[i],Path="a"+types[i]};
                    if((v[3]&(1<<i))!=0)UnityEngine.Resources.Values.Add(entries[i].Path,Clip(types[i]));
                }
                var assets=new OriginalBoxViewAssets{audioClipPaths=new[]{"o0","o1"},alternativeClips=entries};
                assets.InitializeAudio();var clip=assets.GetSfxClip(v[0]);Console.WriteLine(ReferenceEquals(clip,null)?"0":clip.Id.ToString());continue;
            }
            if(parts[0]=="F")
            {
                var v=Array.ConvertAll(parts[1].Split(','),int.Parse);UnityEngine.Resources.Values.Clear();
                UnityEngine.Resources.Values.Add("f0",Clip(v[1]));UnityEngine.Resources.Values.Add("f1",Clip(v[2]));
                var assets=new OriginalGameSfxAssets{fallHitPaths=new[]{"f0","f1"}};
                var clip=assets.GetClip(v[0]);Console.WriteLine(ReferenceEquals(clip,null)?"0":clip.Id.ToString());continue;
            }
            var head=Array.ConvertAll(parts[0].Split(','),int.Parse);float now=0;
            int resolveId=0;
            var audio=new OriginalSfxPlayback(()=>{Trace.Add("time,"+Bits(now));return now;},
                type=>{Trace.Add("load,"+type);return Clip(resolveId);},head[3]!=0?new UnityEngine.AudioSource():null,
                type=>Trace.Add("missing,"+type));
            var game=new OriginalGameSfx(head[4]!=0?audio:null,head.Length>5 && head[5]!=0?new OriginalApplicationRandom():null,
                ()=>{Trace.Add("time,"+Bits(now));return now;});var output=new List<string>();
            foreach(var row in parts[1].Split(';'))
            {
                var v=row.Split(',');int type=int.Parse(v[1]);now=Float(v[2]);float offset=Float(v[3]);resolveId=int.Parse(v[5]);
                audio.SfxMute=head[0]!=0;audio.OverrideSfxMuteForNightmare=head[1]!=0;game.SetNoGameSound(head[2]!=0);
                Trace.Clear();string result="-";
                try
                {
                    switch(v[0])
                    {
                        case "c":result=audio.CanPlayAudio(type,offset)?"1":"0";break;
                        case "p":audio.PlaySound(type,offset);break;
                        case "e":audio.PlaySound(type,Clip(int.Parse(v[4])),offset);break;
                        case "g":game.PlaySfx(type,offset);break;
                        case "h":game.PlaySfx(type,Clip(int.Parse(v[4])),offset);break;
                        case "f":UnityEngine.Random.Returned=int.Parse(v[4]);game.PlayFallHit();break;
                        case "l":result=Bits(audio.GetClipLastPlayedTime(type));break;
                    }
                }
                catch(NullReferenceException){result="error";}
                output.Add(result+":"+string.Join(";",Trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
}
