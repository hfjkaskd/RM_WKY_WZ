using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace Whitebox.Foundation
{
    public struct OriginalSwapPoint
    {
        public float X,Y,Z;
        public OriginalSwapPoint(float x,float y,float z){X=x;Y=y;Z=z;}
    }
    class FallSquashHarness : IOriginalFallSquashSurface, IOriginalFallSquashContext
    {
        int[] h; float[] v; int calls; List<string> trace=new List<string>();
        OriginalSwapPoint world,local,scale; OriginalFallSquashState state;
        void Record(string s){trace.Add(s);if(++calls==h[3])throw new Exception();}
        public bool IsScroll=>h[0]!=0;
        public float DeltaTime{get{Record("dt");return v[9];}}
        public OriginalSwapPoint Position{get{Record("world");return world;}}
        public OriginalSwapPoint LocalPosition{get{Record("local");return local;}set{Record("setlocal");local=value;}}
        public OriginalSwapPoint LocalScale{get{Record("scale");return scale;}set{Record("setscale");scale=value;}}
        static float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
        static string B(float f)=>BitConverter.ToUInt32(BitConverter.GetBytes(f),0).ToString("x8");
        static IEnumerable<float> P(OriginalSwapPoint p){yield return p.X;yield return p.Y;yield return p.Z;}
        string Dump()=>string.Join(",",new[]{state.State,state.SquashType,state.IsFalling?1:0,state.IsBouncing?1:0})+"/"+
            string.Join(",",P(state.FallStartPosition).Concat(new[]{state.YPosOnStretch,state.StretchWidth,state.StretchHeight,state.StretchSpeed,state.SquashSpeed,state.BounceYPos,state.BounceElapsedDur}).Concat(P(local)).Concat(P(scale)).Select(B));
        void Run(string line)
        {
            var all=line.Split('#');h=all[0].Split(',').Select(int.Parse).ToArray();v=all[1].Split(',').Select(F).ToArray();
            state=new OriginalFallSquashState(this);
            if(h[1]!=0)state.BindContext(this);
            world=new OriginalSwapPoint(v[0],v[1],v[2]);local=new OriginalSwapPoint(v[3],v[4],v[5]);scale=new OriginalSwapPoint(v[6],v[7],v[8]);
            state.State=h[4];state.SquashType=h[5];state.IsFalling=h[6]!=0;state.IsBouncing=h[7]!=0;
            Set(v.Skip(10).ToArray()); var output=new List<string>();
            foreach(var command in all[2].Split(';'))
            {
                calls=0;trace.Clear();string result="-";var p=command.Split(',');
                try
                {
                    switch(p[0])
                    {
                        case "t":state.UpdateState(int.Parse(p[1]),F(p[2]),F(p[3]));break;
                        case "u":state.Update();break;
                        case "s":state.ScaleItemOnFall();break;
                        case "b":state.BounceItemAfterFall();break;
                        case "p":Set(p.Skip(1).Select(F).ToArray());break;
                        case "w":world.Y=F(p[1]);break;
                        case "d":v[9]=F(p[1]);break;
                        case "c":h[0]=int.Parse(p[1]);break;
                    }
                }catch{result="error";}
                output.Add(result+":"+Dump()+":"+string.Join(";",trace));
            }
            Console.WriteLine(string.Join("|",output));
        }
        void Set(float[] a)=>state.SetParameters(a[0],a[1],a[2],a[3],a[4],a[5],a[6],a[7],a[8]);
        static void Main(){string l;while((l=Console.ReadLine())!=null)new FallSquashHarness().Run(l);}
    }
}
