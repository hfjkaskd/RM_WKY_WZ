using System;

namespace Whitebox.Foundation
{
    // Native Unity mesh input. Float buffers keep pose/geometry verification
    // independent of Unity and are reused for every frame.
    public sealed class OriginalKingMesh
    {
        public sealed class Part
        {
            public readonly float[] Vertices; // x,y,u,v,r,g,b,a
            public readonly int[] Indices;public int VertexCount,IndexCount;
            public Part(int capacity){Vertices=new float[capacity*8];Indices=new int[capacity*3];}
        }
        public readonly Part[] Parts;
        private readonly OriginalKingPose pose;
        private readonly float[] xy,clipXY,scratchA,scratchB;
        private int[][] polygons;private int clipEnd=-1;
        private float red,green,blue,alpha;
        private readonly int separator;
        private readonly int[] blendParts;
        private readonly OriginalQuadClip dynamicClip;
        public OriginalKingMesh(OriginalKingPose pose,string separatorSlot,bool splitBlendRuns=false)
        {
            this.pose=pose;int maximum=0,triangles=0,polygonSize=0,polygonCount=1;separator=-1;
            for(int i=0;i<pose.Data.Slots.Length;i++)if(pose.Data.Slots[i].Name==separatorSlot)separator=i;
            if(separator<0 && separatorSlot!=null)throw new InvalidOperationException("Rig separator missing.");
            foreach(var a in pose.Data.Attachments){maximum=Math.Max(maximum,a.WorldLength);triangles+=a.Triangles.Length/3;foreach(var p in a.ClipPolygons){polygonSize=Math.Max(polygonSize,p.Length);polygonCount=Math.Max(polygonCount,a.ClipPolygons.Length);}}
            foreach(var c in pose.Data.Clips)foreach(var t in c.Timelines)foreach(var p in t.ClipPolygons){polygonSize=Math.Max(polygonSize,p.Length);polygonCount=Math.Max(polygonCount,t.ClipPolygons.Length);}
            foreach (var attachment in pose.Data.Attachments)
                if (attachment.DynamicClip)
                {
                    dynamicClip = new OriginalQuadClip();
                    polygonSize = Math.Max(polygonSize, 4);
                    polygonCount = Math.Max(polygonCount, 2);
                    break;
                }
            int capacity=checked(Math.Max(64,triangles*(polygonSize+3)*polygonCount));
            if (splitBlendRuns)
            {
                if (separatorSlot != null) throw new ArgumentException("Blend runs and a separator cannot be combined.");
                foreach (var clip in pose.Data.Clips)
                    foreach (var timeline in clip.Timelines)
                        if (timeline.Kind == 7) throw new InvalidOperationException("Blend partitions require fixed slot order.");
                blendParts = new int[pose.Data.Slots.Length];
                int runs = 1;
                for (int i = 1; i < blendParts.Length; i++)
                {
                    if (pose.Data.Slots[i].Blend != pose.Data.Slots[i - 1].Blend) runs++;
                    blendParts[i] = runs - 1;
                }
                var runTriangles = new int[runs];
                foreach (var attachment in pose.Data.Attachments)
                    runTriangles[blendParts[attachment.Slot]] += attachment.Triangles.Length / 3;
                Parts = new Part[runs];
                for (int i = 0; i < runs; i++)
                    Parts[i] = new Part(checked(Math.Max(64, runTriangles[i] * (polygonSize + 3) * polygonCount)));
            }
            else Parts=separator<0 ? new[]{new Part(capacity)} : new[]{new Part(capacity),new Part(capacity)};
            xy=new float[maximum];clipXY=new float[maximum];
            scratchA=new float[(polygonSize+6)*2];scratchB=new float[scratchA.Length];
        }
        private void Vertex(Part p,float x,float y,float u,float v)
        {
            int index=p.VertexCount++*8;var b=p.Vertices;
            b[index]=x;b[index+1]=y;b[index+2]=u;b[index+3]=1-v;
            b[index+4]=red;b[index+5]=green;b[index+6]=blue;b[index+7]=alpha;
        }
        private static void Triangle(Part p,int a,int b,int c)
        {p.Indices[p.IndexCount++]=a;p.Indices[p.IndexCount++]=b;p.Indices[p.IndexCount++]=c;}
        public void Build()
        {
            foreach(var p in Parts){p.VertexCount=0;p.IndexCount=0;}polygons=null;clipEnd=-1;int part=0;
            foreach(int slot in pose.DrawOrder)
            {
                if(blendParts!=null)part=blendParts[slot];else if(slot==separator)part=1;int id=pose.Attachment[slot];
                if(id>=0)
                {
                    var a=pose.Data.Attachments[id];
                    if(a.Kind==3)continue; // Paths constrain bones and never produce visible triangles.
                    if(a.Kind==2)
                    {if(polygons==null){pose.WorldVertices(id,clipXY);polygons=a.DynamicClip?dynamicClip.Build(clipXY):pose.ClipPolygons[slot];clipEnd=a.EndSlot;}continue;}
                    pose.WorldVertices(id,xy);var color=pose.Colors[slot];alpha=a.Color[3]*color[3];
                    red=a.Color[0]*color[0]*alpha;green=a.Color[1]*color[1]*alpha;blue=a.Color[2]*color[2]*alpha;
                    int blend=pose.Data.Slots[slot].Blend;if(blend==1)alpha=0;else if(blend!=0&&(blend!=2||blendParts==null))throw new InvalidOperationException("Rig blend mode.");
                    var p=Parts[part];
                    if(polygons==null)
                    {
                        int start=p.VertexCount;for(int v=0;v<a.WorldLength;v+=2)Vertex(p,xy[v],xy[v+1],a.UV[v],a.UV[v+1]);
                        for(int t=0;t<a.Triangles.Length;t+=3)Triangle(p,start+a.Triangles[t],start+a.Triangles[t+1],start+a.Triangles[t+2]);
                    }
                    else ClipAttachment(p,a);
                }
                if(slot==clipEnd){polygons=null;clipEnd=-1;}
            }
        }
        private void ClipAttachment(Part part,OriginalKingRigData.Attachment attachment)
        {
            var uv=attachment.UV;var indices=attachment.Triangles;
            for(int t=0;t<indices.Length;t+=3)
            {
                int i=indices[t]*2,j=indices[t+1]*2,k=indices[t+2]*2;
                float x1=xy[i],y1=xy[i+1],x2=xy[j],y2=xy[j+1],x3=xy[k],y3=xy[k+1];
                foreach(var poly in polygons)
                {
                    bool clipped;int count=ClipTriangle(x1,y1,x2,y2,x3,y3,poly,out clipped);if(count==0)continue;
                    int start=part.VertexCount;
                    if(!clipped)
                    {
                        Vertex(part,x1,y1,uv[i],uv[i+1]);Vertex(part,x2,y2,uv[j],uv[j+1]);Vertex(part,x3,y3,uv[k],uv[k+1]);
                        Triangle(part,start,start+1,start+2);break;
                    }
                    float d0=y2-y3,d1=x3-x2,d2=x1-x3,d4=y3-y1,inverse=1/(d0*d2+d1*(y1-y3));
                    for(int v=0;v<count;v+=2)
                    {
                        float x=scratchA[v],y=scratchA[v+1],dx=x-x3,dy=y-y3;
                        float a=(d0*dx+d1*dy)*inverse,b=(d4*dx+d2*dy)*inverse,c=1-a-b;
                        Vertex(part,x,y,uv[i]*a+uv[j]*b+uv[k]*c,uv[i+1]*a+uv[j+1]*b+uv[k+1]*c);
                    }
                    for(int v=1;v<count/2-1;v++)Triangle(part,start,start+v,start+v+1);
                }
            }
        }
        // Sutherland-Hodgman clipping against the original clockwise convex
        // pieces. Intersection UVs use barycentric interpolation above.
        private int ClipTriangle(float x1,float y1,float x2,float y2,float x3,float y3,int[] poly,out bool clipped)
        {
            scratchA[0]=x1;scratchA[1]=y1;scratchA[2]=x2;scratchA[3]=y2;scratchA[4]=x3;scratchA[5]=y3;
            float[] input=scratchA,output=scratchB;int count=6;clipped=false;
            for(int edge=0;edge<poly.Length;edge++)
            {
                int p=poly[edge]*2,q=poly[(edge+1)%poly.Length]*2;
                float ex=clipXY[p],ey=clipXY[p+1],qx=clipXY[q],qy=clipXY[q+1],dx=ex-qx,dy=ey-qy;int written=0;
                for(int v=0;v<count;v+=2)
                {
                    int next=(v+2)%count;float ax=input[v],ay=input[v+1],bx=input[next],by=input[next+1];
                    bool insideA=dx*(ay-qy)-dy*(ax-qx)>0,insideB=dx*(by-qy)-dy*(bx-qx)>0;
                    if(insideA&&insideB){output[written++]=bx;output[written++]=by;continue;}
                    clipped=true;
                    if(insideA!=insideB)
                    {
                        float c0=by-ay,c2=bx-ax,s=c0*(qx-ex)-c2*(qy-ey);
                        float u=Math.Abs(s)>0.000001f?(c2*(ey-ay)-c0*(ex-ax))/s:0;
                        output[written++]=ex+(qx-ex)*u;output[written++]=ey+(qy-ey)*u;
                    }
                    if(insideB){output[written++]=bx;output[written++]=by;}
                }
                if(written==0)return 0;count=written;var swap=input;input=output;output=swap;
            }
            if(input!=scratchA)Array.Copy(input,scratchA,count);return count;
        }
    }
}
