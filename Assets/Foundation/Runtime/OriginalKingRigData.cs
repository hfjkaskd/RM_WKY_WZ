using System;
using System.Collections.Generic;
using System.IO;

namespace Whitebox.Foundation
{
    // Analytical keyframes/curves and bind geometry, authored from King.skel.
    // The decoder/exporter stays outside Assets. No external runtime assembly.
    public sealed class OriginalKingRigData
    {
        public sealed class Bone { public string Name; public int Parent; public float[] Setup; }
        public sealed class Slot { public string Name, AttachmentName; public int Bone, Blend, Attachment; public float[] Color; }
        public sealed class Attachment
        {
            public string Name; public int Slot, Kind, DeformAttachment, EndSlot, WorldLength;
            public float[] Color, Vertices, UV; public int[] Bones, Triangles; public int[][] ClipPolygons;
            public bool PathClosed, PathConstantSpeed, DynamicClip; public float[] PathLengths;
        }
        public sealed class Constraint { public int Target; public bool Local, Relative; public int[] Bones; public float[] Values; }
        public sealed class PathConstraint
        {
            public int Target, PositionMode, SpacingMode, RotateMode;
            public int[] Bones; public float[] Values;
        }
        public sealed class Timeline
        {
            public int Kind, Target, Attachment; public float[] Frames, Curves;
            public int[] Attachments; public float[][] Deforms; public int[][] Orders, ClipPolygons;
        }
        public sealed class Clip { public string Name; public float Duration; public Timeline[] Timelines; }
        public readonly Bone[] Bones; public readonly Slot[] Slots; public readonly Attachment[] Attachments;
        public readonly Constraint[] Constraints; public readonly PathConstraint[] Paths;
        public readonly int[] UpdateKinds, UpdateIndices; public readonly Clip[] Clips;
        private readonly Dictionary<string,int>[] attachmentsBySlot;
        private readonly Dictionary<string,int> clipsByName;
        private static int Count(BinaryReader r) { int n=r.ReadInt32(); if(n<0||n>4000000)throw new InvalidDataException("King array size."); return n; }
        private static float[] Floats(BinaryReader r) { var a=new float[Count(r)];for(int i=0;i<a.Length;i++)a[i]=r.ReadSingle();return a; }
        private static int[] Ints(BinaryReader r) { var a=new int[Count(r)];for(int i=0;i<a.Length;i++)a[i]=r.ReadInt32();return a; }
        private static float[] Color(BinaryReader r) => new[]{r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()};
        public OriginalKingRigData(Stream stream, string version = "3.8.84")
        {
            using(var r=new BinaryReader(stream,System.Text.Encoding.UTF8,true))
            {
                if(r.ReadInt32()!=0x524d4b47)throw new InvalidDataException("Rig data signature.");
                int format=r.ReadInt32();
                if((format!=3&&format!=4&&format!=5)||r.ReadString()!=version)throw new InvalidDataException("Rig data version.");
                Bones=new Bone[Count(r)];
                for(int i=0;i<Bones.Length;i++)
                {
                    var b=new Bone{Name=r.ReadString(),Parent=r.ReadInt32(),Setup=Floats(r)};Bones[i]=b;
                    if(b.Setup.Length!=7||b.Parent>=i||b.Parent< -1)throw new InvalidDataException("King bone data.");
                }
                Slots=new Slot[Count(r)];attachmentsBySlot=new Dictionary<string,int>[Slots.Length];
                for(int i=0;i<Slots.Length;i++)
                { Slots[i]=new Slot{Name=r.ReadString(),Bone=r.ReadInt32(),Color=Color(r),Blend=r.ReadInt32(),AttachmentName=r.ReadString()};attachmentsBySlot[i]=new Dictionary<string,int>(StringComparer.Ordinal); }
                Attachments=new Attachment[Count(r)];
                for(int i=0;i<Attachments.Length;i++)
                {
                    var a=new Attachment{Slot=r.ReadInt32(),Name=r.ReadString(),Kind=r.ReadInt32(),DeformAttachment=r.ReadInt32(),EndSlot=r.ReadInt32(),Color=Color(r),WorldLength=r.ReadInt32(),Bones=Ints(r),Vertices=Floats(r),UV=Floats(r),Triangles=Ints(r)};
                    a.ClipPolygons=new int[Count(r)][];for(int p=0;p<a.ClipPolygons.Length;p++)a.ClipPolygons[p]=Ints(r);
                    if(format>=4)
                    {a.PathClosed=r.ReadBoolean();a.PathConstantSpeed=r.ReadBoolean();a.PathLengths=Floats(r);}
                    else a.PathLengths=Array.Empty<float>();
                    if(format>=5)a.DynamicClip=r.ReadBoolean();
                    if(a.DynamicClip&&(a.Kind!=2||a.WorldLength!=8||a.Bones.Length!=0))
                        throw new InvalidDataException("Animated clip quad.");
                    if(a.Kind<0||a.Kind>(format>=4?3:2)||a.WorldLength<0||a.WorldLength>65534||a.WorldLength%2!=0)throw new InvalidDataException("Rig geometry.");
                    Attachments[i]=a;attachmentsBySlot[a.Slot].Add(a.Name,i);
                }
                for(int i=0;i<Slots.Length;i++)Slots[i].Attachment=FindAttachment(i,Slots[i].AttachmentName);
                Constraints=new Constraint[Count(r)];
                for(int i=0;i<Constraints.Length;i++)
                {
                    var c=new Constraint{Target=r.ReadInt32(),Local=r.ReadBoolean(),Relative=r.ReadBoolean(),Bones=Ints(r),Values=Floats(r)};Constraints[i]=c;
                    // Original King rig: absolute world translate mix -0.4.
                    // Reject other rigs instead of silently dropping channels.
                    if(c.Local||c.Relative||c.Values.Length!=10||c.Values[3]!=0)throw new InvalidDataException("King constraint mode.");
                }
                Paths=new PathConstraint[format>=4?Count(r):0];
                for(int i=0;i<Paths.Length;i++)
                {
                    Paths[i]=new PathConstraint{Target=r.ReadInt32(),PositionMode=r.ReadInt32(),SpacingMode=r.ReadInt32(),
                        RotateMode=r.ReadInt32(),Bones=Ints(r),Values=Floats(r)};
                    if(Paths[i].Values.Length!=5)throw new InvalidDataException("Rig path values.");
                }
                int updates=Count(r);UpdateKinds=new int[updates];UpdateIndices=new int[updates];
                for(int i=0;i<updates;i++){UpdateKinds[i]=r.ReadInt32();UpdateIndices[i]=r.ReadInt32();}
                Clips=new Clip[Count(r)];clipsByName=new Dictionary<string,int>(StringComparer.Ordinal);
                for(int i=0;i<Clips.Length;i++)
                {
                    var clip=new Clip{Name=r.ReadString(),Duration=r.ReadSingle(),Timelines=new Timeline[Count(r)]};Clips[i]=clip;clipsByName.Add(clip.Name,i);
                    for(int j=0;j<clip.Timelines.Length;j++)
                    {
                        var t=new Timeline{Kind=r.ReadInt32(),Target=r.ReadInt32(),Attachment=r.ReadInt32(),Frames=Floats(r),Curves=Floats(r)};
                        if(t.Kind<0||t.Kind>(format>=4?11:7)||t.Frames.Length==0)throw new InvalidDataException("Rig timeline kind.");
                        t.Attachments=new int[Count(r)];for(int k=0;k<t.Attachments.Length;k++)t.Attachments[k]=FindAttachment(t.Target,r.ReadString());
                        t.Deforms=new float[Count(r)][];for(int k=0;k<t.Deforms.Length;k++)t.Deforms[k]=Floats(r);
                        t.Orders=new int[Count(r)][];for(int k=0;k<t.Orders.Length;k++)t.Orders[k]=Ints(r);
                        t.ClipPolygons=new int[Count(r)][];for(int k=0;k<t.ClipPolygons.Length;k++)t.ClipPolygons[k]=Ints(r);
                        if(t.Kind==11)
                            for(int key=4;key<t.Frames.Length;key+=5)
                                if(t.Frames[key]!=0f)throw new InvalidDataException("Rig animated shear mix.");
                        clip.Timelines[j]=t;
                    }
                }
                if(stream.Position!=stream.Length)throw new InvalidDataException("King trailing data.");
            }
        }
        public int FindAttachment(int slot,string name)
        { if(string.IsNullOrEmpty(name))return -1;int id;if(!attachmentsBySlot[slot].TryGetValue(name,out id))throw new InvalidDataException("King attachment missing: "+name);return id; }
        public int FindClip(string name)
        { int id;if(!clipsByName.TryGetValue(name,out id))throw new InvalidDataException("King animation missing: "+name);return id; }
    }
}

