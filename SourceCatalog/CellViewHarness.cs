using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;

public static class H
{
    public static List<string> trace = new List<string>();
    public static int calls, failure;
    public static UnityEngine.SpriteRenderer[] renderers;
    public static UnityEngine.GameObject owner;
    public static uint Bits(float v) => BitConverter.ToUInt32(BitConverter.GetBytes(v),0);
    public static float Float(string v) => BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(v)),0);
    public static void R(string name, int id, params object[] values)
    {
        string s=name+","+id;
        foreach(object v in values)s+=","+Convert.ToString(v,CultureInfo.InvariantCulture);
        trace.Add(s);if(++calls==failure)throw new InvalidOperationException();
    }
    public static string State(UnityEngine.SpriteRenderer r)
    {
        return string.Join(",",new object[]{r.layer,r.order,Bits(r.sz.x),Bits(r.sz.y),Bits(r.tr.pos.x),Bits(r.tr.pos.y),Bits(r.tr.pos.z),r.fx?1:0,r.fy?1:0,r.on?1:0,Bits(r.cl.r),Bits(r.cl.g),Bits(r.cl.b),Bits(r.cl.a),r.sp==null?0:r.sp.id});
    }
    public static void Main()
    {
        string line;
        while((line=Console.ReadLine())!=null)
        {
            string[] pieces=line.Split('#'),a=pieces[0].Split(',');
            int present=int.Parse(a[0]), flips=int.Parse(a[2]);failure=int.Parse(a[1]);calls=0;trace.Clear();
            owner=new UnityEngine.GameObject{active=true,id=9};
            renderers=new UnityEngine.SpriteRenderer[4];
            for(int i=0;i<4;i++)renderers[i]=new UnityEngine.SpriteRenderer{ id=i,layer=10+i,order=20+i,fx=(flips&(1<<(2*i)))!=0,fy=(flips&(2<<(2*i)))!=0,
                sz=new UnityEngine.Vector2(Float(a[3]),Float(a[4])),tr=new UnityEngine.Transform{id=i,pos=new UnityEngine.Vector3(Float(a[5]),Float(a[6]),Float(a[7]))},
                cl=new UnityEngine.Color(.1f,.2f,.3f,.4f),sp=new UnityEngine.Sprite{id=7+i}};
            var settings=new OriginalCellViewSettings{readMaskLayer=487648401,writeMaskLayer=1795358237,maskSize=new UnityEngine.Vector2(1.05f,1f),maskPosition=new UnityEngine.Vector3(),
                sideBorderSize=new UnityEngine.Vector2(1f,.3f),outerCornerSizeReduction=new UnityEngine.Vector2(.05714311823248863f,0f),innerCornerSizeReduction=new UnityEngine.Vector2(.5f,0f),
                outerCornerHalfOffset=.028571559116244316f,innerCornerHalfOffset=.25f,
                mainColor=new UnityEngine.Color(.6392157077789307f,.6980392336845398f,.8509804010391235f,1f),altColor=new UnityEngine.Color(.5764706134796143f,.6549019813537598f,.8509804010391235f,1f)};
            renderers[0].hasTransform=(present&4)!=0;renderers[1].hasTransform=(present&8)!=0;renderers[2].hasTransform=(present&128)!=0;
            var mask=new OriginalCellMask{readMask=(present&1)!=0?renderers[0]:null,writeMask=(present&2)!=0?renderers[1]:null,settings=settings,owner=(present&16)!=0?owner:null};
            var part=(present&32)!=0?renderers[2]:null;var fill=(present&64)!=0?renderers[3]:null;
            IOriginalCellBorderSprites normal=(present&256)!=0?new Sprites(0):null,water=(present&512)!=0?new Sprites(1):null;
            int length=a.Length>8?int.Parse(a[8]):13;
            var asset=new OriginalCellAssets{spritePaths=length<0?null:new string[length]};
            if(length>=0)for(int i=0;i<length;i++)asset.spritePaths[i]=i.ToString();
            var output=new List<string>();
            foreach(string command in pieces[1].Split(';'))
            {
                trace.Clear();string[] c=command.Split(',');string result="ok";
                try
                {
                    switch(c[0])
                    {
                        case "e":mask.Enable(c[1]=="1");break;
                        case "s":mask.SetSorting(new OriginalSwapSorting(int.Parse(c[1]),int.Parse(c[2]),c[3]=="1"));break;
                        case "w":mask.SetWriteMaskSortingOrder(new OriginalSwapSorting(int.Parse(c[1]),int.Parse(c[2]),c[3]=="1"));break;
                        case "a":OriginalCellMask.ArrangeMaskSize(mask.readMask,new UnityEngine.Vector2(Float(c[1]),Float(c[2])),new UnityEngine.Vector3(Float(c[3]),Float(c[4]),Float(c[5])));break;
                        case "t":case "l":
                            int f=int.Parse(c[1]);var p=new UnityEngine.Vector3(Float(c[2]),Float(c[3]),Float(c[4]));
                            if(c[0]=="t")OriginalCellBorderGeometry.PrepareTopBottom(part,p,(f&1)!=0,(f&2)!=0,(f&4)!=0,(f&8)!=0,settings);
                            else OriginalCellBorderGeometry.PrepareLeftRight(part,p,(f&1)!=0,(f&2)!=0,(f&4)!=0,(f&8)!=0,settings);break;
                        case "c":
                            int b=int.Parse(c[1]);OriginalCellBorderGeometry.PrepareCorner(part,fill,(b&1)!=0,(b&2)!=0,(b&4)!=0,settings,normal,water,(b&16)!=0);break;
                        case "g":
                            int type=int.Parse(c[2]);UnityEngine.Sprite s=null;
                            switch(int.Parse(c[1]))
                            {
                                case 0:s=asset.GetOnePixel();break;case 1:s=asset.GetFillPart();break;case 2:s=asset.GetSidePart();break;
                                case 3:s=asset.GetInnerCornerPart();break;case 4:s=asset.GetOuterCornerPart();break;case 5:s=asset.GetCornerFadeOutMask(type);break;
                                case 6:s=asset.GetInnerCornerFadeOutMask(type);break;case 7:s=asset.GetSideFadeOutMask(type);break;
                                case 8:s=asset.GetCornerConnectFadeOutMask(type);break;case 9:s=asset.GetFullFadeOutMask();break;
                            }
                            result="value,"+(s==null?0:s.id);break;
                    }
                }catch{result="error";}
                output.Add(result+":"+(owner.active?1:0)+":"+string.Join("/",Array.ConvertAll(renderers,State))+":"+string.Join("/",trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
    }
    private sealed class Sprites:IOriginalCellBorderSprites
    {
        private int kind;public Sprites(int k){kind=k;}
        public UnityEngine.Sprite GetInnerCornerPart(){R("inner",kind);return new UnityEngine.Sprite{id=30+kind};}
        public UnityEngine.Sprite GetOuterCornerPart(){R("outer",kind);return new UnityEngine.Sprite{id=40+kind};}
    }
}
namespace Whitebox.Foundation
{
    public struct OriginalSwapSorting
    {
        public int Layer,Order;public bool SortEverything;
        public OriginalSwapSorting(int l,int o,bool s){Layer=l;Order=o;SortEverything=s;}
    }
}
namespace UnityEngine
{
    public class SerializeField:Attribute{}
    public class CreateAssetMenuAttribute:Attribute{public string menuName;}
    public class ScriptableObject{}
    public static class Resources{public static T Load<T>(string path)where T:class{return new Sprite{id=100+int.Parse(path)}as T;}}
    public class MonoBehaviour{public GameObject owner;public GameObject gameObject{get{H.R("game",9);return owner;}}}
    public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);}
    public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
    public struct Color{public float r,g,b,a;public Color(float x,float y,float z,float w){r=x;g=y;b=z;a=w;}}
    public class Sprite{public int id;}
    public class GameObject{public int id;public bool active;public void SetActive(bool value){H.R("active",id,value?1:0);active=value;}}
    public class Transform{public int id;public Vector3 pos;public Vector3 localPosition{set{H.R("position",id,H.Bits(value.x),H.Bits(value.y),H.Bits(value.z));pos=value;}}}
    public class SpriteRenderer
    {
        public int id,layer,order;public bool fx,fy,on,hasTransform=true;public Vector2 sz;public Transform tr;public Color cl;public Sprite sp;
        public int sortingLayerID{get{H.R("layerget",id);return layer;}set{H.R("layer",id,value);layer=value;}}
        public int sortingOrder{get{H.R("orderget",id);return order;}set{H.R("order",id,value);order=value;}}
        public Vector2 size{get{H.R("sizeget",id);return sz;}set{H.R("size",id,H.Bits(value.x),H.Bits(value.y));sz=value;}}
        public Transform transform{get{H.R("transform",id);return hasTransform?tr:null;}}
        public bool flipX{get{H.R("flipxget",id);return fx;}set{H.R("flipx",id,value?1:0);fx=value;}}
        public bool flipY{get{H.R("flipyget",id);return fy;}set{H.R("flipy",id,value?1:0);fy=value;}}
        public bool enabled{set{H.R("enabled",id,value?1:0);on=value;}}
        public Color color{set{H.R("color",id,H.Bits(value.r),H.Bits(value.g),H.Bits(value.b),H.Bits(value.a));cl=value;}}
        public Sprite sprite{set{H.R("sprite",id,value==null?0:value.id);sp=value;}}
    }
}
