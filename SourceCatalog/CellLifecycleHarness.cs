using System;
using System.Collections.Generic;
using UnityEngine;
using Whitebox.Foundation;
public static class L
{
    public static List<string> trace=new List<string>();
    public static int calls,failure,flags,missing,pointCalls;public static int[] head;
    public static Transform own,parent;public static OriginalCellView view;public static OriginalCellBorder border;
    public static SpriteRenderer[] renderers;public static OriginalCellFreezeCountdown countdown;
    public static uint Bits(float f)=>BitConverter.ToUInt32(BitConverter.GetBytes(f),0);
    public static float Float(int b)=>BitConverter.ToSingle(BitConverter.GetBytes(b),0);
    public static void R(string n,params object[] a){string s=n;foreach(var v in a)s+=","+v;trace.Add(s);if(++calls==failure)throw new Exception();}
    public static string Vec(Vector3 v)=>Bits(v.x)+","+Bits(v.y)+","+Bits(v.z);
    public static string State()=>((view.ViewDelegate==null)?0:1)+":"+Vec(own.pos)+","+Vec(own.scale)+","+(own.parent==null?0:1)+":"+Bits(countdown.Remaining)+","+(countdown.IsCounting?1:0)+":"+string.Join("/",Array.ConvertAll(renderers,r=>(r.on?1:0)+","+(r.go.active?1:0)+","+r.interaction+","+Bits(r.cl.r)+","+Bits(r.cl.g)+","+Bits(r.cl.b)+","+Bits(r.cl.a)));
    public static void Main(){string line;while((line=Console.ReadLine())!=null){var pieces=line.Split('#');head=Array.ConvertAll(pieces[0].Split(','),s=>unchecked((int)long.Parse(s)));flags=head[0];failure=head[1];missing=head[2];calls=0;pointCalls=0;
        own=new Transform{pos=new Vector3(.125f,-2f,Float(head[7])),scale=new Vector3(2,3,4)};parent=new Transform{pos=new Vector3(3,4,5)};
        renderers=new SpriteRenderer[14];for(int i=0;i<14;i++)renderers[i]=new SpriteRenderer{id=i,on=false,go=new GameObject{id=i,active=false},cl=new Color(.1f,.2f,.3f,.4f)};
        border=new OriginalCellBorder{borderRenderers=Part(2,head[10]),fillRenderers=Part(10,head[11])};
        var settings=new OriginalCellViewSettings{mainColor=new Color(.6392157077789307f,.6980392336845398f,.8509804010391235f,1),altColor=new Color(.5764706134796143f,.6549019813537598f,.8509804010391235f,1)};
        view=new OriginalCellView{mask=(flags&16)!=0?new OriginalCellMask():null,border=(flags&32)!=0?border:null,front=missing==0?null:renderers[0],background=missing==1?null:renderers[1],settings=settings};
        countdown=new OriginalCellFreezeCountdown(()=>R("unfreeze"));countdown.StartCountdown(1.5f);view.BindFreezeCountdown(countdown);
        var factory=(flags&2)!=0?new Factory():null;IOriginalCellViewDelegate model=(flags&4)!=0?new Model():null;
        var cell=new OriginalGridCell{CellView=view};var output=new List<string>();
        foreach(var command in pieces[1].Split(';')){trace.Clear();var c=command.Split(',');string result="ok";try{switch(c[0]){
            case "i":view.InitializeView(model,factory);break;case "v":view.ShowView(c[1]=="1");break;case "m":view.SetCellMaskState(c[1]=="1");break;
            case "e":view.EnableMaskInteraction();break;case "d":view.DisableMaskInteraction();break;case "a":view.ActivateBorders();break;
            case "r":view.OnRecycle();break;case "s":view.OnSpawn();break;case "id":result="value,"+view.GetPoolId();break;
            case "point":var p=view.CellPoint;result="point,"+p.X+","+p.Y;break;
            case "p":var pos=view.Position;result="position,"+Bits(pos.x)+","+Bits(pos.y);break;
            case "world":result="vector,"+Vec(cell.GetViewPosition());break;case "local":result="vector,"+Vec(cell.GetViewLocalPosition());break;
            case "u":view.Tick();break;
        }}catch{result="error";}output.Add(result+":"+State()+":"+string.Join("/",trace));}Console.WriteLine(string.Join("|",output));
    }}
    private static SpriteRenderer[] Part(int start,int length){if(length<0)return null;var a=new SpriteRenderer[length];for(int i=0;i<length;i++)a[i]=start+i==missing?null:renderers[start+i];return a;}
    private class Model:IOriginalCellViewDelegate
    {
        public OriginalCellPoint Point{get{R("point");bool first=pointCalls++==0;return new OriginalCellPoint(head[3],first?head[4]:head[5]);}}
        public OriginalCellNeighbors Neighbors=>null;
        public bool IsFillingCell{get{R("filling");return head[6]!=0;}}
    }
    private class Factory:IOriginalCellViewFactory
    {
        public Transform CellParent=>(flags&64)!=0?parent:null;
        public OriginalCellAssets LoadCellAssets(){if((flags&8)==0)throw new NullReferenceException();R("assets");return new OriginalCellAssets();}
        public bool IsWaterLevel=>false;public bool IsShifterLevel=>false;
        public float LevelDeltaTime{get{R("delta");return Float(head[8]);}}
    }
}
namespace Whitebox.Foundation
{
    public interface IOriginalPoolable{int GetPoolId();void OnSpawn();void OnRecycle();}
    public struct OriginalCellPoint{public int X,Y;public OriginalCellPoint(int x,int y){X=x;Y=y;}}
    public class OriginalCellNeighbors{}
    public partial class OriginalGridCell{public int X,Y;public OriginalCellNeighbors Neighbors=>null;public bool IsFillingCell=>false;public void FreezeFall(){}}
    public class OriginalCellAssets{}
    public class OriginalCellMask{public void Enable(bool v){L.R("mask",v?1:0);}}
    public class OriginalCellBorder
    {
        public SpriteRenderer[] borderRenderers,fillRenderers;
        public void Init(OriginalCellAssets a,bool water){L.R("init");}
        public void ShowParts(int grid,bool separate){L.R("show",grid,separate?1:0);}
        public void HideParts(){L.R("hide");}public void ActivateBorders(){L.R("activate");}public void OnRecycle(){L.R("recycle");}
    }
}
namespace UnityEngine
{
    public class SerializeField:Attribute{}public class CreateAssetMenuAttribute:Attribute{public string menuName;}public class ScriptableObject{}
    public enum SpriteMaskInteraction{None,VisibleInsideMask,VisibleOutsideMask}
    public class MonoBehaviour{public Transform transform{get{L.R("transform");return(L.flags&1)!=0?L.own:null;}}}
    public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
    public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new Vector3(1,1,1);public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);}
    public struct Color{public float r,g,b,a;public Color(float x,float y,float z,float w){r=x;g=y;b=z;a=w;}}
    public class GameObject{public int id;public bool active;public void SetActive(bool v){L.R("active",id,v?1:0);active=v;}}
    public class Transform
    {
        public Vector3 pos,scale;public Transform parent;
        public void SetParent(Transform p){L.R("parent",p==null?0:1);if(parent!=null){pos.x+=parent.pos.x;pos.y+=parent.pos.y;pos.z+=parent.pos.z;}if(p!=null){pos.x-=p.pos.x;pos.y-=p.pos.y;pos.z-=p.pos.z;}parent=p;}
        public Vector3 localPosition{get{L.R("localget");return pos;}set{L.R("local",L.Bits(value.x),L.Bits(value.y),L.Bits(value.z));pos=value;}}
        public Vector3 localScale{set{L.R("scale",L.Bits(value.x),L.Bits(value.y),L.Bits(value.z));scale=value;}}
        public Vector3 position{get{L.R("worldget");return new Vector3(pos.x+(parent==null?0:parent.pos.x),pos.y+(parent==null?0:parent.pos.y),pos.z+(parent==null?0:parent.pos.z));}}
    }
    public class SpriteRenderer
    {
        public int id,interaction;public bool on;public Color cl;public GameObject go;
        public GameObject gameObject{get{L.R("game",id);return id==L.head[9]?null:go;}}
        public bool enabled{set{L.R("enabled",id,value?1:0);on=value;}}
        public Color color{set{L.R("color",id,L.Bits(value.r),L.Bits(value.g),L.Bits(value.b),L.Bits(value.a));cl=value;}}
        public SpriteMaskInteraction maskInteraction{set{L.R("interaction",id,(int)value);interaction=(int)value;}}
    }
}
