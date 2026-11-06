using System;
using System.Collections.Generic;
using Whitebox.Foundation;
using UnityEngine;
public static class T
{
    public static List<string> trace=new List<string>();
    public static int calls,failure,flags,missingGame,missingTransform,neighborPresence,normal,same,grid;
    public static SpriteRenderer[] renderers;
    public static void R(string n,params object[] args){string s=n;foreach(var a in args)s+=","+a;trace.Add(s);if(++calls==failure)throw new Exception();}
    public static uint Bits(float f)=>BitConverter.ToUInt32(BitConverter.GetBytes(f),0);
    public static string State(SpriteRenderer r)=>string.Join(",",new object[]{r.on?1:0,r.fx?1:0,r.fy?1:0,Bits(r.sz.x),Bits(r.sz.y),r.go.active?1:0,Bits(r.tr.pos.x),Bits(r.tr.pos.y),Bits(r.tr.pos.z),Bits(r.tr.rot.x),Bits(r.tr.rot.y),Bits(r.tr.rot.z),Bits(r.tr.rot.w),r.sp==null?0:r.sp.id});
    public static void Main(){string line;while((line=Console.ReadLine())!=null){
        var p=line.Split('#');var h=Array.ConvertAll(p[0].Split(','),int.Parse);
        neighborPresence=h[0];normal=h[1];same=h[2];grid=h[3];failure=h[5];flags=h[6];missingGame=h[8];missingTransform=h[9];calls=0;
        renderers=new SpriteRenderer[16];for(int i=0;i<16;i++)renderers[i]=new SpriteRenderer{id=i,on=true,fx=true,fy=false,sz=new Vector2(2,3),go=new GameObject{id=i,active=true},tr=new Transform{id=i,pos=new Vector3(2,3,4),rot=new Quaternion(1,2,3,4)},sp=i==h[13]?null:new Sprite{id=100+i}};
        var neighbors=new OriginalCellNeighbors();
        for(int i=0;i<8;i++)if((neighborPresence&(1<<i))!=0)neighbors.items[i]=new OriginalGridCell{IsNormalCell=(normal&(1<<i))!=0,ShifterGridId=(same&(1<<i))!=0?grid:unchecked(grid+1)};
        var model=(flags&2)!=0?new Model{neighbors=(flags&4)!=0?neighbors:null}:null;
        var view=(flags&1)!=0?new OriginalCellView{ViewDelegate=model}:null;
        var settings=new OriginalCellViewSettings{sideBorderSize=new Vector2(1,.3f),topPosition=new Vector3(0,.5f,0),rightPosition=new Vector3(.5f,0,0),bottomPosition=new Vector3(0,-.5f,0),leftPosition=new Vector3(-.5f,0,0)};
        var border=new OriginalCellBorder{cellView=view,settings=settings,borderRenderers=ArrayPart(0,h[10],h[7]),fillRenderers=ArrayPart(8,h[11],h[7]),fadeOutMaskRenderers=ArrayPart(12,h[12],h[7])};
        border.Init(new OriginalCellAssets(),false);
        var output=new List<string>();
        foreach(var command in p[1].Split(';')){trace.Clear();string result="ok";var c=command.Split(',');try{switch(c[0]){
            case "s":border.ShowParts(h[3],h[4]!=0);break;
            case "p":border.PrepareBorder(int.Parse(c[1]),int.Parse(c[2])<0?null:renderers[int.Parse(c[2])],h[3],h[4]!=0);break;
            case "h":border.HideParts();break;case "a":border.ActivateBorders();break;case "r":border.OnRecycle();break;
        }}catch{result="error";}output.Add(result+":"+string.Join("/",Array.ConvertAll(renderers,State))+":"+string.Join("/",trace));}
        Console.WriteLine(string.Join("|",output));
    }}
    private static SpriteRenderer[] ArrayPart(int start,int length,int missing){if(length<0)return null;var a=new SpriteRenderer[length];for(int i=0;i<length;i++)a[i]=start+i==missing?null:renderers[start+i];return a;}
    private class Model:IOriginalCellViewDelegate{public OriginalCellNeighbors neighbors;public OriginalCellNeighbors Neighbors{get{R("neighbors");return neighbors;}}}
}
namespace Whitebox.Foundation
{
    public class OriginalCellView{public IOriginalCellViewDelegate ViewDelegate;}
    public interface IOriginalCellViewDelegate{OriginalCellNeighbors Neighbors{get;}}
    public class OriginalCellAssets:IOriginalCellBorderSprites{}
    public interface IOriginalCellBorderSprites{}
    public class OriginalGridCell{public bool IsNormalCell;public int ShifterGridId;}
    public class OriginalCellNeighbors{public static int[] Sides={0,1,2,3,4,5,6,7};public OriginalGridCell[] items=new OriginalGridCell[8];public OriginalGridCell Get(int i){T.R("get",i);return items[i];}}
    public static class OriginalCellBorderGeometry
    {
        private static void Side(int axis,SpriteRenderer p,Vector3 pos,bool a,bool b,bool c,bool d){T.R("side",axis,p==null?-1:p.id,T.Bits(pos.x),T.Bits(pos.y),T.Bits(pos.z),a?1:0,b?1:0,c?1:0,d?1:0);}
        public static void PrepareTopBottom(SpriteRenderer p,Vector3 pos,bool a,bool b,bool c,bool d,OriginalCellViewSettings s)=>Side(0,p,pos,a,b,c,d);
        public static void PrepareLeftRight(SpriteRenderer p,Vector3 pos,bool a,bool b,bool c,bool d,OriginalCellViewSettings s)=>Side(1,p,pos,a,b,c,d);
        public static void PrepareCorner(SpriteRenderer p,SpriteRenderer f,bool a,bool b,bool c,OriginalCellViewSettings s,IOriginalCellBorderSprites ca,IOriginalCellBorderSprites wa,bool water){T.R("corner",p==null?-1:p.id,f==null?-1:f.id,a?1:0,b?1:0,c?1:0);}
    }
}
namespace UnityEngine
{
    public class SerializeField:Attribute{}
    public class CreateAssetMenuAttribute:Attribute{public string menuName;}
    public class ScriptableObject{}
    public class MonoBehaviour{}
    public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
    public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();}
    public struct Quaternion{public float x,y,z,w;public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Quaternion identity=>new Quaternion(0,0,0,1);}
    public struct Color{public float r,g,b,a;}
    public class Sprite{public int id;public static bool operator==(Sprite a,Sprite b)=>ReferenceEquals(a,b);public static bool operator!=(Sprite a,Sprite b){T.R("nonnull",ReferenceEquals(a,null)?0:a.id);return !ReferenceEquals(a,b);}public override bool Equals(object b)=>ReferenceEquals(this,b);public override int GetHashCode()=>id;}
    public class GameObject{public int id;public bool active;public void SetActive(bool v){T.R("active",id,v?1:0);active=v;}}
    public class Transform{public int id;public Vector3 pos;public Quaternion rot;public Vector3 localPosition{set{T.R("position",id,T.Bits(value.x),T.Bits(value.y),T.Bits(value.z));pos=value;}}public Quaternion localRotation{set{T.R("rotation",id,T.Bits(value.x),T.Bits(value.y),T.Bits(value.z),T.Bits(value.w));rot=value;}}}
    public class SpriteRenderer
    {
        public int id;public bool on,fx,fy;public Vector2 sz;public GameObject go;public Transform tr;public Sprite sp;
        public GameObject gameObject{get{T.R("game",id);return id==T.missingGame?null:go;}}
        public Transform transform{get{T.R("transform",id);return id==T.missingTransform?null:tr;}}
        public bool enabled{get{T.R("enabledget",id);return on;}set{T.R("enabled",id,value?1:0);on=value;}}
        public bool flipX{set{T.R("flipx",id,value?1:0);fx=value;}}public bool flipY{set{T.R("flipy",id,value?1:0);fy=value;}}
        public Vector2 size{set{T.R("size",id,T.Bits(value.x),T.Bits(value.y));sz=value;}}
        public Sprite sprite{get{T.R("sprite",id);return sp;}}
    }
}
