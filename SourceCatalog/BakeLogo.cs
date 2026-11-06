// Offline conversion tool only. Spine sources and assemblies never enter Unity Assets/Packages.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Spine;
sealed class NoTexture : TextureLoader { public void Load(AtlasPage p,string path){} public void Unload(object value){} }
static class BakeLogo {
    sealed class Part {
        public readonly List<float> vertices=new List<float>(),uvs=new List<float>(),colors=new List<float>();
        public readonly List<int> indices=new List<int>();
        public void Add(float[] xy,int length,float[] uv,int[] triangles,int count,float r,float g,float b,float a) {
            int start=vertices.Count/2;
            for(int i=0;i<length;i+=2) { vertices.Add(xy[i]);vertices.Add(xy[i+1]);uvs.Add(uv[i]);uvs.Add(1-uv[i+1]);colors.Add(r);colors.Add(g);colors.Add(b);colors.Add(a); }
            for(int i=0;i<count;i++)indices.Add(start+triangles[i]);
        }
        public void Write(BinaryWriter w) {
            w.Write(vertices.Count/2);w.Write(indices.Count);
            for(int i=0;i<vertices.Count/2;i++) {w.Write(vertices[2*i]);w.Write(vertices[2*i+1]);w.Write(uvs[2*i]);w.Write(uvs[2*i+1]);for(int c=0;c<4;c++)w.Write((byte)Math.Max(0,Math.Min(255,(int)(colors[i*4+c]*255))));}
            foreach(var i in indices)w.Write(i);
        }

        public void WriteCompact(BinaryWriter writer, Part previous)
        {
            bool reuseUvs = previous != null && uvs.SequenceEqual(previous.uvs);
            bool reuseColors = previous != null && colors.SequenceEqual(previous.colors);
            bool reuseIndices = previous != null && indices.SequenceEqual(previous.indices);
            writer.Write(vertices.Count / 2);
            writer.Write(indices.Count);
            writer.Write((byte)((reuseUvs ? 1 : 0) | (reuseColors ? 2 : 0) | (reuseIndices ? 4 : 0)));
            for (int i = 0; i < vertices.Count / 2; i++)
            {
                writer.Write(vertices[2 * i]);
                writer.Write(vertices[2 * i + 1]);
                if (!reuseUvs)
                {
                    writer.Write(uvs[2 * i]);
                    writer.Write(uvs[2 * i + 1]);
                }
                if (!reuseColors)
                    for (int c = 0; c < 4; c++)
                        writer.Write((byte)Math.Max(0, Math.Min(255, (int)(colors[i * 4 + c] * 255))));
            }
            if (!reuseIndices) foreach (int index in indices) writer.Write(index);
        }
    }
    static void Main(string[] args) {
        var atlas=new Atlas(args[0],new NoTexture());
        var data=new SkeletonBinary(new AtlasAttachmentLoader(atlas)){Scale=.01f}.ReadSkeletonData(args[1]);
        string followerOption = args[args.Length - 1];
        bool includeFollower = followerOption.StartsWith("follow:", StringComparison.Ordinal);
        int argumentCount = args.Length - (includeFollower ? 1 : 0);
        bool jester = argumentCount >= 4;
        bool mixJesterExit = argumentCount == 8 && args[3] == "exit_animation";
        var animation = data.FindAnimation(jester ? args[3] : "logo");
        if (animation == null) throw new InvalidDataException("Missing original animation.");
        var skeleton = new Skeleton(data);
        var follower = includeFollower ? skeleton.FindBone(followerOption.Substring(7)) : null;
        if (includeFollower && (follower == null || !jester))
            throw new InvalidDataException("Missing Jester follower bone.");
        var clip = new SkeletonClipping();
        int[] jesterParts = null;
        int partCount = 2;
        if (jester)
        {
            // Preserve each contiguous normal/multiply draw run in its own submesh.
            jesterParts = new int[data.Slots.Count];
            partCount = 1;
            for (int slot = 1; slot < data.Slots.Count; slot++)
            {
                if (data.Slots.Items[slot].BlendMode != data.Slots.Items[slot - 1].BlendMode) partCount++;
                jesterParts[slot] = partCount - 1;
            }
        }
        float duration = animation.Duration;
        AnimationState jesterExitState = null;
        if (mixJesterExit)
        {
            float exitAt = float.Parse(args[4], CultureInfo.InvariantCulture);
            float enterSpeed = float.Parse(args[5], CultureInfo.InvariantCulture);
            float exitSpeed = float.Parse(args[6], CultureInfo.InvariantCulture);
            float mixDuration = float.Parse(args[7], CultureInfo.InvariantCulture);
            jesterExitState = new AnimationState(new AnimationStateData(data));
            // Both native clips are set on track 1. The outgoing entry continues
            // advancing while the exit entry blends in over real playback time.
            jesterExitState.SetAnimation(1, "enter_animation", false).TimeScale = enterSpeed;
            jesterExitState.Apply(skeleton);
            jesterExitState.Update(exitAt);
            jesterExitState.Apply(skeleton);
            var exit = jesterExitState.SetAnimation(1, animation, false);
            exit.TimeScale = exitSpeed;
            exit.MixDuration = mixDuration;
            duration /= exitSpeed;
        }
        int fps=120,count=(int)Math.Ceiling(duration*fps)+1,maxVertices=0,maxIndices=0;
        float previousTime = 0f;
        var xy=new float[2048];var regionIndices=new[]{0,1,2,2,3,0};
        using(var w=new BinaryWriter(File.Create(args[2]))) {
            w.Write(0x524d4c47);w.Write(includeFollower ? 3 : jester ? 2 : 1);w.Write(fps);w.Write(count);w.Write(duration);w.Write(partCount);
            Part[] previous = null;
            for(int f=0;f<count;f++) {
                float t=Math.Min(duration,(float)f/fps);
                if (jesterExitState != null)
                {
                    jesterExitState.Update(t - previousTime);
                    jesterExitState.Apply(skeleton);
                    previousTime = t;
                }
                else
                {
                    skeleton.SetToSetupPose();
                    animation.Apply(skeleton,0,t,false,null,1,MixBlend.Setup,MixDirection.In);
                }
                skeleton.UpdateWorldTransform();
                if (follower != null)
                {
                    w.Write(follower.WorldX);
                    w.Write(follower.WorldY);
                    w.Write(follower.WorldRotationX);
                }
                var parts = new Part[partCount];
                for (int i = 0; i < partCount; i++) parts[i] = new Part();
                int part = 0, slotIndex = 0;
                foreach(var slot in skeleton.DrawOrder) {
                    if (jester)
                    {
                        if (slot.Data.Index != slotIndex++) throw new InvalidDataException("Jester draw order changed.");
                        part = jesterParts[slot.Data.Index];
                    }
                    else if (slot.Data.Name == "BoardShadow") part = 1;
                    var a=slot.Attachment;
                    if(a is ClippingAttachment) {clip.ClipStart(slot,(ClippingAttachment)a);continue;}
                    if(!slot.Bone.Active) {clip.ClipEnd(slot);continue;}
                    float[] uv=null;int[] triangles=null;int length=0;float r=1,g=1,b=1,alpha=1;
                    var region=a as RegionAttachment;var mesh=a as MeshAttachment;
                    if(region!=null) {length=8;region.ComputeWorldVertices(slot.Bone,xy,0,2);uv=region.UVs;triangles=regionIndices;r=region.R;g=region.G;b=region.B;alpha=region.A;}
                    else if(mesh!=null) {length=mesh.WorldVerticesLength;if(length>xy.Length)xy=new float[length];mesh.ComputeWorldVertices(slot,0,length,xy,0,2);uv=mesh.UVs;triangles=mesh.Triangles;r=mesh.R;g=mesh.G;b=mesh.B;alpha=mesh.A;}
                    if(length>0) {
                        if (slot.Data.BlendMode != BlendMode.Normal && slot.Data.BlendMode != BlendMode.Additive &&
                            !(jester && slot.Data.BlendMode == BlendMode.Multiply))
                            throw new NotSupportedException("Blend " + slot.Data.Name);
                        alpha*=skeleton.A*slot.A;r*=skeleton.R*slot.R*alpha;g*=skeleton.G*slot.G*alpha;b*=skeleton.B*slot.B*alpha;
                        if(slot.Data.BlendMode==BlendMode.Additive)alpha=0;
                        if(clip.IsClipping) {clip.ClipTriangles(xy,length,triangles,triangles.Length,uv);parts[part].Add(clip.ClippedVertices.Items,clip.ClippedVertices.Count,clip.ClippedUVs.Items,clip.ClippedTriangles.Items,clip.ClippedTriangles.Count,r,g,b,alpha);}
                        else parts[part].Add(xy,length,uv,triangles,triangles.Length,r,g,b,alpha);
                    }
                    clip.ClipEnd(slot);
                }
                clip.ClipEnd();
                for (int i = 0; i < parts.Length; i++)
                {
                    var partData = parts[i];
                    if (jester) partData.WriteCompact(w, previous == null ? null : previous[i]);
                    else partData.Write(w);
                    maxVertices = Math.Max(maxVertices, partData.vertices.Count / 2);
                    maxIndices = Math.Max(maxIndices, partData.indices.Count);
                }
                previous = parts;
            }
        }
        Console.WriteLine("Baked original " + data.Version + " " + animation.Name + ": frames=" + count +
            " fps=" + fps + " parts=" + partCount + " maxVertices=" + maxVertices + " maxIndices=" + maxIndices +
            " bytes=" + new FileInfo(args[2]).Length);
    }
}
