using System;
using System.Collections.Generic;
using Spine;
using Whitebox.Foundation;
static class KingMeshReference
{
    static readonly List<float>[] vertices={new List<float>(),new List<float>()};
    static readonly List<int>[] indices={new List<int>(),new List<int>()};
    static readonly SkeletonClipping clip=new SkeletonClipping();static readonly float[] xy=new float[1024];
    static readonly int[] regionIndices={0,1,2,2,3,0};public static int Samples;public static float MaxError;
    public static void Compare(OriginalKingMesh actual,Skeleton skeleton)
    {
        foreach(var v in vertices)v.Clear();foreach(var i in indices)i.Clear();clip.ClipEnd();int part=0;
        foreach(var slot in skeleton.DrawOrder)
        {
            if(slot.Data.Name=="sol frame")part=1;var a=slot.Attachment;var c=a as ClippingAttachment;
            if(c!=null){clip.ClipStart(slot,c);continue;}
            var r=a as RegionAttachment;var m=a as MeshAttachment;
            if(r!=null||m!=null)
            {
                int length;float[] uv;int[] triangles;int triangleCount;float red,green,blue,alpha;
                if(r!=null){length=8;r.ComputeWorldVertices(slot.Bone,xy,0,2);uv=r.UVs;triangles=regionIndices;triangleCount=6;red=r.R;green=r.G;blue=r.B;alpha=r.A;}
                else{length=m.WorldVerticesLength;m.ComputeWorldVertices(slot,0,length,xy,0,2);uv=m.UVs;triangles=m.Triangles;triangleCount=triangles.Length;red=m.R;green=m.G;blue=m.B;alpha=m.A;}
                alpha*=slot.A;red*=slot.R*alpha;green*=slot.G*alpha;blue*=slot.B*alpha;if(slot.Data.BlendMode==BlendMode.Additive)alpha=0;
                float[] points=xy;
                if(clip.IsClipping){clip.ClipTriangles(xy,length,triangles,triangleCount,uv);points=clip.ClippedVertices.Items;length=clip.ClippedVertices.Count;uv=clip.ClippedUVs.Items;triangles=clip.ClippedTriangles.Items;triangleCount=clip.ClippedTriangles.Count;}
                int start=vertices[part].Count/8;
                for(int i=0;i<length;i+=2){var v=vertices[part];v.Add(points[i]);v.Add(points[i+1]);v.Add(uv[i]);v.Add(1-uv[i+1]);v.Add(red);v.Add(green);v.Add(blue);v.Add(alpha);}
                for(int i=0;i<triangleCount;i++)indices[part].Add(start+triangles[i]);
            }
            clip.ClipEnd(slot);
        }
        clip.ClipEnd();actual.Build();Samples++;
        for(int p=0;p<2;p++)
        {
            var a=actual.Parts[p];if(a.VertexCount*8!=vertices[p].Count||a.IndexCount!=indices[p].Count)throw new Exception("King clipped topology mismatch sample="+Samples+" part="+p+" actual="+a.VertexCount+" expected="+vertices[p].Count/8);
            for(int i=0;i<vertices[p].Count;i++){float err=Math.Abs(a.Vertices[i]-vertices[p][i]);MaxError=Math.Max(MaxError,err);if(float.IsNaN(a.Vertices[i])||err>0.000004f)throw new Exception("King clipped vertex mismatch sample="+Samples+" scalar="+i+" err="+err);}
            for(int i=0;i<a.IndexCount;i++)if(a.Indices[i]!=indices[p][i])throw new Exception("King triangle mismatch");
        }
    }
}
