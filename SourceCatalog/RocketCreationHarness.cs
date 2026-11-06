// Storage/Unity API boundaries only; production strategies and config are compiled in.
using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;
static class Trace
{
    public static readonly List<string> Calls = new List<string>();
    public static uint Bits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value),0);
    public static float Float(string value) => BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(value)),0);
    public static string Vector(UnityEngine.Vector3 v) => Bits(v.x)+":"+Bits(v.y)+":"+Bits(v.z);
}
namespace UnityEngine
{
    public class ScriptableObject { }
    public class Sprite { public int Index; }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; } }
    public struct Quaternion
    {
        public float x,y,z,w;
        public static Quaternion Euler(Vector3 v)
        {
            Trace.Calls.Add("euler:"+Trace.Bits(v.z*.0174532924f));
            return new Quaternion { z=v.z==0f?0f:.70710677f,w=v.z==0f?1f:.70710677f };
        }
    }
    public class GameObject
    {
        public readonly string Id;
        public GameObject(string id) { Id=id; }
        public void SetActive(bool value) { Trace.Calls.Add("active:"+Id+":"+(value?1:0)); }
    }
    public class Transform
    {
        public readonly string Id;
        public readonly GameObject gameObject;
        public Transform(string id) { Id=id;gameObject=new GameObject(id); }
        public Vector3 localPosition { set { Trace.Calls.Add("pos:"+Id+":"+Trace.Vector(value)); } }
        public Quaternion localRotation { set { Trace.Calls.Add("rot:"+Id+":"+Trace.Bits(value.z)+":"+Trace.Bits(value.w)); } }
    }
    public class SpriteRenderer
    {
        private readonly string id;
        public readonly Transform transform;
        public GameObject gameObject => transform.gameObject;
        public SpriteRenderer(string id) { this.id=id; transform=new Transform(id); }
        public Sprite sprite { set { Trace.Calls.Add("sprite:"+id+":"+value.Index); } }
    }
    public struct AnimatorStateInfo { public int fullPathHash; public float normalizedTime; }
    public class Animator
    {
        public AnimatorStateInfo State;
        public static int StringToHash(string value) => value.Contains("Horizontal")?12345:-98765;
        public void Play(int hash,int layer,float time) { Trace.Calls.Add("play:"+hash+":"+layer+":"+Trace.Bits(time)); }
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer) { Trace.Calls.Add("state:"+layer);return State; }
    }
    public static class Resources
    {
        public static string[] Paths;
        public static T Load<T>(string path) where T:class { return new Sprite { Index=Array.IndexOf(Paths,path) } as T; }
    }
}
namespace Whitebox.Foundation
{
    public interface IOriginalRocketViewServices { bool IsDeviceWide { get; } void PlaySfx(int type,float offset); }
    public class OriginalRocketItemSpriteView
    {
        public readonly UnityEngine.SpriteRenderer BaseRenderer=new UnityEngine.SpriteRenderer("b"),ShadowRenderer=new UnityEngine.SpriteRenderer("s"),
            FirstPart=new UnityEngine.SpriteRenderer("p1"),SecondPart=new UnityEngine.SpriteRenderer("p2");
        public readonly UnityEngine.Transform HorizontalAnimation=new UnityEngine.Transform("h"),VerticalAnimation=new UnityEngine.Transform("v");
        public readonly UnityEngine.Animator Animator=new UnityEngine.Animator();
    }
}
class RocketServices : IOriginalRocketViewServices
{
    public bool IsDeviceWide => false;
    public void PlaySfx(int type,float offset) { Trace.Calls.Add("sound:"+type+":"+Trace.Bits(offset)); }
}
class RocketCreationHarness
{
    static OriginalRocketViewPose Pose(string text)
    {
        var a=text.Split(',');int i=0;
        Func<UnityEngine.Vector3> vector=()=>new UnityEngine.Vector3(Trace.Float(a[i++]),Trace.Float(a[i++]),Trace.Float(a[i++]));
        return new OriginalRocketViewPose { BodyOffset=vector(),ShadowOffset=vector(),ShadowEuler=vector(),FirstPartOffset=vector(),SecondPartOffset=vector(),
            BodySprite=int.Parse(a[i++]),FirstPartSprite=int.Parse(a[i++]),SecondPartSprite=int.Parse(a[i++]) };
    }
    static void Main()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        var config=Console.ReadLine().Split('|');
        var assets=new OriginalRocketViewAssets { spritePaths=config[0].Split(','),horizontalCreationState=config[1],verticalCreationState=config[2],
            shadowSprite=int.Parse(config[3]),creationSound=int.Parse(config[4]),creationSoundOffset=Trace.Float(config[5]),
            horizontalPose=Pose(config[6]),verticalPose=Pose(config[7]) };
        UnityEngine.Resources.Paths=assets.spritePaths;
        string line;
        while((line=Console.ReadLine())!=null)
        {
            var a=line.Split(',');int orientation=int.Parse(a[1]);
            var view=new OriginalRocketItemSpriteView();
            OriginalRocketCreationStrategy strategy=orientation==0?(OriginalRocketCreationStrategy)new OriginalHorizontalRocketCreationStrategy(view,assets,new RocketServices()):
                orientation==1?new OriginalVerticalRocketCreationStrategy(view,assets,new RocketServices()):new OriginalWaterVerticalRocketCreationStrategy(view,assets,new RocketServices());
            Trace.Calls.Clear();string value="-";
            if(a[0]=="S") strategy.SetSprites();
            else if(a[0]=="P") strategy.PlayCreationAnimation(Trace.Float(a[2]),a[3]=="1");
            else
            {
                view.Animator.State=new UnityEngine.AnimatorStateInfo { fullPathHash=int.Parse(a[2]),normalizedTime=Trace.Float(a[3]) };
                value=strategy.IsCreationAnimationPlaying()?"1":"0";
            }
            Console.WriteLine(value+";"+string.Join("|",Trace.Calls));
        }
    }
}
