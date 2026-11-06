// Engine/property storage fixture. Runtime TNT view, fuse and assets compile unchanged.
using System;
using System.Collections.Generic;
using Whitebox.Foundation;
static class TntTrace
{
    public static readonly List<string> Calls=new List<string>();
    public static uint Bits(float x)=>BitConverter.ToUInt32(BitConverter.GetBytes(x),0);
    public static float Float(string x)=>BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(x)),0);
}
namespace UnityEngine
{
    public class ScriptableObject { }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static Vector3 zero=>new Vector3(0,0,0);public static Vector3 one=>new Vector3(1,1,1); }
    public struct Quaternion { public static Quaternion identity=>new Quaternion(); }
    public struct Color { public float r,g,b,a;public static Color white=>new Color{r=1,g=1,b=1,a=1}; }
    public class GameObject
    {
        public readonly string Id;public bool activeSelf=true;
        public GameObject(string id){Id=id;}
        public void SetActive(bool value){activeSelf=value;TntTrace.Calls.Add("active:"+Id+":"+(value?1:0));}
    }
    public class Transform
    {
        public readonly string Id;public readonly GameObject gameObject;
        public Transform(string id){Id=id;gameObject=new GameObject(id);}
        private Vector3 local;
        public Vector3 localPosition {get=>local;set{local=value;TntTrace.Calls.Add("local:"+Id+":"+TntTrace.Bits(value.x)+":"+TntTrace.Bits(value.y)+":"+TntTrace.Bits(value.z));}}
        public Vector3 localScale {get;set;}
        public Quaternion localRotation {get;set;}
        public Quaternion rotation {get;set;}
        public Vector3 position {get;set;}
        public void SetParent(Transform parent){}
    }
    public class Component
    {
        public Transform transform=new Transform("view");public GameObject gameObject=>transform.gameObject;
    }
    public class MonoBehaviour:Component { }
    public class Renderer:Component
    {
        private int layer,order;
        public int sortingLayerID {get=>layer;set{layer=value;TntTrace.Calls.Add("layer:"+transform.Id+":"+value);}}
        public int sortingOrder {get=>order;set{order=value;TntTrace.Calls.Add("order:"+transform.Id+":"+value);}}
    }
    public class Sprite { }
    public enum SpriteMaskInteraction {None,VisibleInsideMask,VisibleOutsideMask}
    public class SpriteRenderer:Renderer {public Sprite sprite;public Color color;public SpriteMaskInteraction maskInteraction;}
    public class SpriteMask:Component {public int backSortingLayerID,backSortingOrder,frontSortingLayerID,frontSortingOrder;}
    public class ParticleSystem:Component
    {
        public bool Playing;
        public bool isPlaying {get{TntTrace.Calls.Add("playing:"+(Playing?1:0));return Playing;}}
        public void Play(){Playing=true;TntTrace.Calls.Add("fuse-play");}
        public void Stop(){Playing=false;TntTrace.Calls.Add("fuse-stop");}
    }
    public struct AnimatorStateInfo {public int fullPathHash;public float normalizedTime;}
    public class Animator:Component
    {
        public AnimatorStateInfo State;
        public bool isActiveAndEnabled=>gameObject.activeSelf;
        public static int StringToHash(string value)=>value.Contains("Lightball")?222:111;
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer){TntTrace.Calls.Add("state:"+layer);return State;}
        public void Play(int hash,int layer,float time){TntTrace.Calls.Add("animation:"+hash+":"+layer+":"+TntTrace.Bits(time));}
    }
    public static class Resources{public static T Load<T>(string path) where T:class=>new Sprite() as T;}
}
namespace UnityEngine.Rendering {public class SortingGroup:UnityEngine.Renderer {}}
namespace Whitebox.Foundation
{
    public struct OriginalSwapSorting {public int Layer,Order;public bool SortEverything;public OriginalSwapSorting(int l,int o,bool all){Layer=l;Order=o;SortEverything=all;}}
    public static class OriginalItemSortingRules
    {
        public static OriginalSwapSorting Default(int l)=>new OriginalSwapSorting(l,1000,false);
        public static OriginalSwapSorting WithOffset(OriginalSwapSorting s,int o)=>new OriginalSwapSorting(s.Layer,unchecked(s.Order+o),s.SortEverything);
        public static void Apply(UnityEngine.Renderer r,OriginalSwapSorting s){r.sortingLayerID=s.Layer;r.sortingOrder=s.Order;}
    }
    public class OriginalItemSwapView:UnityEngine.MonoBehaviour {public void SwapAnimationEnded(){} }
    public interface IOriginalPoolable {int GetPoolId();void OnSpawn();void OnRecycle();}
    public abstract class OriginalItemParticles:UnityEngine.MonoBehaviour,IOriginalPoolable
    {
        public abstract int GetPoolId();public void OnSpawn(){}public void OnRecycle(){}public void RecycleSelf(){TntTrace.Calls.Add("recycle-fuse");}
    }
    public class OriginalGameObjectPool
    {
        public OriginalTntFuseParticles Fuse;
        public T Spawn<T>(int id,bool activate) where T:class=>Fuse as T;
    }
    public class OriginalSpecialItemCreationParticles { }
    public class OriginalSpecialCreationParticleTransfer
    {
        public OriginalSpecialCreationParticleTransfer(UnityEngine.Transform t,OriginalGameObjectPool p){}
        public void DecreaseCreationParticleSorting(){TntTrace.Calls.Add("decrease");}
        public void SetCreationParticles(OriginalSpecialItemCreationParticles p,bool a,bool u){}
    }
    public interface IOriginalSpecialCreationView { }
    public class OriginalBoardItem { }
    public class OriginalStaticTouchMediator
    {
        public bool Touch;
        public bool HasTouchBlockingItem(){TntTrace.Calls.Add("touch");return Touch;}
    }
    public class OriginalCellMediation {public OriginalBoardItem CurrentItem=new OriginalBoardItem();}
    public class OriginalGridCell
    {
        public int X,Y;
        public readonly OriginalStaticTouchMediator StaticTouch=new OriginalStaticTouchMediator();
        public readonly OriginalCellMediation Mediation=new OriginalCellMediation();
    }
}
class TntServices:IOriginalTntViewServices,IOriginalTntItemViewDelegate
{
    public readonly OriginalGridCell Cell=new OriginalGridCell();
    public bool HasCell=true,Incoming,Cross;
    public OriginalGameObjectPool Pool {get;}=new OriginalGameObjectPool();
    public int ItemSortingLayer=>231352099;public int ParticlesSortingLayer=>151353909;public int UiSortingLayer=>487648401;
    public OriginalGridCell CurrentCell {get{TntTrace.Calls.Add("cell");return HasCell?Cell:null;}}
    public bool IsUnderHoney()=>false;public bool IsUnderRoof()=>false;public bool IsUnderChain()=>false;
    public bool HasIncomingTouchBlockingItem(OriginalGridCell cell){TntTrace.Calls.Add("incoming");return Incoming;}
    public bool IsFallingCross(OriginalBoardItem item){TntTrace.Calls.Add("cross");return Cross;}
    public void PlaySfx(int type,float offset){TntTrace.Calls.Add("sound:"+type+":"+TntTrace.Bits(offset));}
}
class TntCreationHarness
{
    static void Main()
    {
        string line;while((line=Console.ReadLine())!=null)
        {
            var fields=line.Split(';');var flags=fields[0].Split(',');
            var s=new TntServices();s.Cell.Y=int.Parse(flags[0]);s.Cell.StaticTouch.Touch=flags[1]=="1";s.Incoming=flags[2]=="1";s.Cross=flags[3]=="1";
            var ps=new UnityEngine.ParticleSystem {Playing=true};
            var fuse=new OriginalTntFuseParticles {fuseParticles=ps,sorting0=new UnityEngine.Renderer{transform=new UnityEngine.Transform("f0")},sorting1=new UnityEngine.Renderer{transform=new UnityEngine.Transform("f1")},sorting2=new UnityEngine.Renderer{transform=new UnityEngine.Transform("f2")}};
            s.Pool.Fuse=fuse;
            var v=new OriginalTntItemSpriteView {baseView=new UnityEngine.SpriteRenderer{transform=new UnityEngine.Transform("b")},shadow=new UnityEngine.SpriteRenderer{transform=new UnityEngine.Transform("s")},fuseForLightball=new UnityEngine.SpriteRenderer{transform=new UnityEngine.Transform("light")},animator=new UnityEngine.Animator{transform=new UnityEngine.Transform("a")},sortingGroup=new UnityEngine.Rendering.SortingGroup{transform=new UnityEngine.Transform("g")},resetView=new UnityEngine.Transform("reset"),fuseTransform=new UnityEngine.Transform("anchor"),movingFuseParent=new UnityEngine.Transform("moving"),fuseMaskAnimated=new UnityEngine.Transform("movingMask"),fuseMask=new UnityEngine.SpriteMask{transform=new UnityEngine.Transform("mask")}};
            v.Initialize(s,new UnityEngine.Transform("parent"),UnityEngine.Vector3.zero,new OriginalTntViewAssets{spritePath="tnt",creationSound=346,creationSoundOffset=.04f,lightballCreationState="Base Layer.TntLightballCreationAnimation",boosterState="Base Layer.TntBoosterAnimation",recycledMaskOffset=new UnityEngine.Vector3(-.18f,-.09f,0f),recycledMaskScale=new UnityEngine.Vector3(50f,50f,1f)},s);
            for(int i=1;i<fields.Length;i++)
            {
                var a=fields[i].Split(',');TntTrace.Calls.Clear();string error="";
                try
                {
                    switch(a[0])
                    {
                        case "P":v.PlayCreationAnimation(TntTrace.Float(a[1]),a[2]=="1");break;
                        case "U":v.animator.State=new UnityEngine.AnimatorStateInfo{fullPathHash=int.Parse(a[1]),normalizedTime=TntTrace.Float(a[2])};v.ManualUpdate();break;
                        case "S":v.UpdateSorting(new OriginalSwapSorting(int.Parse(a[1]),int.Parse(a[2]),a[3]=="1"));break;
                        case "T":v.StopCreationAnimation();break;
                        case "C":s.HasCell=a[1]=="1";break;
                        case "N":v.SetIsUnderNonFallBlockingAboveStaticItem(a[1]=="1");break;
                        case "F":v.FallStarted();break;
                        case "E":v.FallEnded();break;
                        case "H":v.Hide();break;
                        case "V":v.Show();break;
                        case "B":v.PlayBoosterAnimation();break;
                        case "L":v.PlayLightballCreationAnimation();break;
                        case "R":v.OnRecycle();break;
                    }
                }
                catch(NullReferenceException){error="null";}
                Console.WriteLine((v.baseView.gameObject.activeSelf?1:0)+","+(v.animator.gameObject.activeSelf?1:0)+","+(ps.Playing?1:0)+","+(v.DidFixCreationSorting?1:0)+","+v.GetSorting().Layer+","+v.GetSorting().Order+","+error+";"+string.Join("|",TntTrace.Calls));
            }
        }
    }
}
