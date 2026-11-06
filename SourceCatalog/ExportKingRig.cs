// Asset authoring executable. Compiled with the recovered decoder outside
// Assets/Packages. Only data and the independent Unity mesh player ship.
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Spine;

sealed class KingExportTexture : TextureLoader
{ public void Load(AtlasPage page, string path) { } public void Unload(object texture) { } }
static class ExportKingRig
{
    static void Floats(BinaryWriter w, float[] a) { w.Write(a == null ? 0 : a.Length); if(a != null) foreach(float x in a) w.Write(x); }
    static void Ints(BinaryWriter w, int[] a) { w.Write(a == null ? 0 : a.Length); if(a != null) foreach(int x in a) w.Write(x); }
    static void Color(BinaryWriter w, float r,float g,float b,float a) { w.Write(r);w.Write(g);w.Write(b);w.Write(a); }
    static void ClipPolygons(BinaryWriter w,Skeleton skeleton,ClippingAttachment clip,int slot)
    {
        if(clip.Bones!=null)throw new NotSupportedException("Weighted King clipping");
        var xy=new float[clip.WorldVerticesLength];clip.ComputeWorldVertices(skeleton.Slots.Items[slot],0,xy.Length,xy,0,2);
        var clipping=new SkeletonClipping();clipping.ClipStart(skeleton.Slots.Items[slot],clip);w.Write(clipping.clippingPolygons.Count);
        foreach(var polygon in clipping.clippingPolygons)
        {
            var indices=new int[polygon.Count/2-1];
            for(int p=0;p<indices.Length;p++)
            {
                int found=-1;for(int k=0;k<xy.Length;k+=2)if(xy[k]==polygon.Items[p*2]&&xy[k+1]==polygon.Items[p*2+1]){found=k/2;break;}
                if(found<0)throw new InvalidDataException("Clipping vertex identity");indices[p]=found;
            }
            Ints(w,indices);
        }
    }
    static void Main(string[] args)
    {
        bool poseOnly = args[args.Length - 1] == "--pose-only";
        int argumentCount = args.Length - (poseOnly ? 1 : 0);
        var atlas=new Atlas(args[0],new KingExportTexture());
        float scale = argumentCount > 3 ? float.Parse(args[3], CultureInfo.InvariantCulture) : .007142857f;
        var data=new SkeletonBinary(new AtlasAttachmentLoader(atlas)){Scale=scale}.ReadSkeletonData(args[1]);
        var skeleton=new Skeleton(data);skeleton.UpdateWorldTransform();var attachments=new List<Attachment>(); var slots=new List<int>(); var names=new List<string>();
        var ids=new Dictionary<Attachment,int>();
        foreach(var entry in data.DefaultSkin.GetAttachments())
        { ids.Add(entry.Attachment,attachments.Count);attachments.Add(entry.Attachment);slots.Add(entry.SlotIndex);names.Add(entry.Name); }
        var movingClips = new HashSet<ClippingAttachment>();
        foreach (var animation in data.Animations)
            foreach (var timeline in animation.Timelines)
            {
                var deform = timeline as DeformTimeline;
                var clip = deform == null ? null : deform.Attachment as ClippingAttachment;
                if (clip == null || deform.Vertices.Length <= 1) continue;
                if (clip.Bones != null || clip.WorldVerticesLength != 8)
                    throw new NotSupportedException("Animated clipping requires the original unweighted quad.");
                movingClips.Add(clip);
            }
        using(var w=new BinaryWriter(File.Create(args[2])))
        {
            int format=movingClips.Count>0?5:data.PathConstraints.Count==0?3:4;
            w.Write(0x524d4b47);w.Write(format);w.Write(data.Version);
            w.Write(data.Bones.Count);
            foreach(var b in data.Bones)
            {
                if(b.TransformMode!=TransformMode.Normal || b.SkinRequired)throw new NotSupportedException("King bone mode");
                w.Write(b.Name);w.Write(b.Parent==null?-1:b.Parent.Index);
                Floats(w,new[]{b.X,b.Y,b.Rotation,b.ScaleX,b.ScaleY,b.ShearX,b.ShearY});
            }
            w.Write(data.Slots.Count);
            foreach(var s in data.Slots)
            { w.Write(s.Name);w.Write(s.BoneData.Index);Color(w,s.R,s.G,s.B,s.A);w.Write((int)s.BlendMode);w.Write(s.AttachmentName??""); }
            w.Write(attachments.Count);
            for(int i=0;i<attachments.Count;i++)
            {
                var a=attachments[i];w.Write(slots[i]);w.Write(names[i]);
                var r=a as RegionAttachment;var m=a as MeshAttachment;var c=a as ClippingAttachment;var path=a as PathAttachment;
                if(r!=null)
                {
                    w.Write(0);w.Write(i);w.Write(-1);Color(w,r.R,r.G,r.B,r.A);
                    var o=r.Offset;
                    // Original region world output is BR,BL,UL,UR; Offset is
                    // stored BL,UL,UR,BR. UVs already use output order.
                    w.Write(8);Ints(w,null);Floats(w,new[]{o[6],o[7],o[0],o[1],o[2],o[3],o[4],o[5]});Floats(w,r.UVs);Ints(w,new[]{0,1,2,2,3,0});
                    w.Write(0);
                }
                else if(m!=null || c!=null || path!=null)
                {
                    var v=(VertexAttachment)a;w.Write(m!=null?1:c!=null?2:3);w.Write(ids[v.DeformAttachment]);w.Write(c!=null&&c.EndSlot!=null?c.EndSlot.Index:-1);
                    if(m!=null)Color(w,m.R,m.G,m.B,m.A);else Color(w,1,1,1,1);
                    w.Write(v.WorldVerticesLength);Ints(w,v.Bones);Floats(w,v.Vertices);Floats(w,m!=null?m.UVs:null);Ints(w,m!=null?m.Triangles:null);
                    if(c==null || movingClips.Contains(c))w.Write(0);
                    else ClipPolygons(w,skeleton,c,slots[i]);
                    if(c!=null)Console.WriteLine("clip slot="+slots[i]+" end="+(c.EndSlot==null?-1:c.EndSlot.Index)+" vertices="+v.WorldVerticesLength+" weighted="+(v.Bones!=null));
                }
                else throw new NotSupportedException("Rig attachment "+a.GetType().Name);
                if(format>=4)
                {w.Write(path!=null&&path.Closed);w.Write(path!=null&&path.ConstantSpeed);Floats(w,path==null?null:path.Lengths);}
                if(format>=5)w.Write(c!=null&&movingClips.Contains(c));
            }
            if(data.IkConstraints.Count!=0)throw new NotSupportedException("Rig IK constraints");
            w.Write(data.TransformConstraints.Count);
            foreach(var c in data.TransformConstraints)
            {
                w.Write(c.Target.Index);w.Write(c.Local);w.Write(c.Relative);
                var bones=new int[c.Bones.Count];for(int i=0;i<bones.Length;i++)bones[i]=c.Bones.Items[i].Index;Ints(w,bones);
                Floats(w,new[]{c.RotateMix,c.TranslateMix,c.ScaleMix,c.ShearMix,c.OffsetRotation,c.OffsetX,c.OffsetY,c.OffsetScaleX,c.OffsetScaleY,c.OffsetShearY});
                Console.WriteLine("constraint target="+c.Target.Index+" bones="+string.Join(",",bones)+" local="+c.Local+" relative="+c.Relative+" mix="+c.RotateMix+","+c.TranslateMix+","+c.ScaleMix+","+c.ShearMix);
            }
            if(format>=4)
            {
                w.Write(data.PathConstraints.Count);
                foreach(var p in data.PathConstraints)
                {
                    w.Write(p.Target.Index);w.Write((int)p.PositionMode);w.Write((int)p.SpacingMode);w.Write((int)p.RotateMode);
                    var bones=new int[p.Bones.Count];for(int i=0;i<bones.Length;i++)bones[i]=p.Bones.Items[i].Index;Ints(w,bones);
                    Floats(w,new[]{p.OffsetRotation,p.Position,p.Spacing,p.RotateMix,p.TranslateMix});
                }
            }
            // Preserve the actual dependency order, including bones updated
            // again after a constraint. No runtime topology search is needed.
            w.Write(skeleton.updateCache.Count);
            foreach(var update in skeleton.updateCache)
            {
                var b=update as Bone;var c=update as TransformConstraint;var p=update as PathConstraint;
                if(b!=null){w.Write(0);w.Write(b.Data.Index);}
                else if(c!=null){w.Write(1);w.Write(data.TransformConstraints.IndexOf(c.Data));}
                else if(p!=null){w.Write(2);w.Write(data.PathConstraints.IndexOf(p.Data));}
                else throw new NotSupportedException("Rig update dependency");
            }
            var animations = new List<Animation>();
            if (argumentCount > 4)
            {
                for (int i = 4; i < argumentCount; i++)
                    {
                        var animation = data.FindAnimation(args[i]);
                        if (animation == null) throw new ArgumentException("Animation missing: " + args[i]);
                        animations.Add(animation);
                    }
            }
            else animations.AddRange(data.Animations);
            w.Write(animations.Count);
            foreach(var animation in animations)
            {
                // Explicit pose-only export leaves non-pose events with their
                // typed gameplay owner. Jester's particles are dispatched by
                // StartHatAnimation's own sequence; the mesh player owns no events.
                var timelines = new List<Timeline>();
                foreach (var timeline in animation.Timelines)
                    if (!poseOnly || !(timeline is EventTimeline)) timelines.Add(timeline);
                w.Write(animation.Name);w.Write(animation.Duration);w.Write(timelines.Count);
                foreach(var t in timelines)
                {
                    var rt=t as RotateTimeline;var tr=t as TranslateTimeline;var co=t as ColorTimeline;
                    var at=t as AttachmentTimeline;var de=t as DeformTimeline;var dr=t as DrawOrderTimeline;
                    var pp=t as PathConstraintPositionTimeline;var pm=t as PathConstraintMixTimeline;var tc=t as TransformConstraintTimeline;
                    int kind=t is PathConstraintSpacingTimeline?9:pp!=null?8:pm!=null?10:tc!=null?11:rt!=null?0:t is ScaleTimeline?2:t is ShearTimeline?3:tr!=null?1:co!=null?4:at!=null?5:de!=null?6:dr!=null?7:-1;
                    if(kind<0)throw new NotSupportedException("King timeline");w.Write(kind);
                    w.Write(rt!=null?rt.BoneIndex:tr!=null?tr.BoneIndex:co!=null?co.SlotIndex:at!=null?at.SlotIndex:de!=null?de.SlotIndex:pp!=null?pp.PathConstraintIndex:pm!=null?pm.PathConstraintIndex:tc!=null?tc.TransformConstraintIndex:-1);
                    w.Write(de==null?-1:ids[de.Attachment]);
                    Floats(w,rt!=null?rt.Frames:tr!=null?tr.Frames:co!=null?co.Frames:at!=null?at.Frames:de!=null?de.Frames:pp!=null?pp.Frames:pm!=null?pm.Frames:tc!=null?tc.Frames:dr.Frames);
                    var curve=t as CurveTimeline;Floats(w,curve==null?null:curve.curves);
                    if(at!=null){w.Write(at.AttachmentNames.Length);foreach(string name in at.AttachmentNames)w.Write(name??"");}
                    else w.Write(0);
                    if(de!=null){w.Write(de.Vertices.Length);foreach(var frame in de.Vertices)Floats(w,frame);}else w.Write(0);
                    if(dr!=null){w.Write(dr.DrawOrders.Length);foreach(var frame in dr.DrawOrders)Ints(w,frame);}else w.Write(0);
                    var clip=de==null?null:de.Attachment as ClippingAttachment;
                    if(clip==null || movingClips.Contains(clip))w.Write(0);
                    else
                    {
                        // Actual King clip timelines have a single fixed
                        // shape key. Store its original convex decomposition
                        // as authored topology, not animation mesh samples.
                        if(de.Vertices.Length!=1)throw new NotSupportedException("Continuously deforming King clipping");
                        skeleton.SetToSetupPose();var slot=skeleton.Slots.Items[de.SlotIndex];slot.Attachment=clip;
                        de.Apply(skeleton,0,de.Frames[0],null,1,MixBlend.Setup,MixDirection.In);skeleton.UpdateWorldTransform();
                        ClipPolygons(w,skeleton,clip,de.SlotIndex);skeleton.SetToSetupPose();skeleton.UpdateWorldTransform();
                    }
                }
            }
        }
        Console.WriteLine("King data bytes="+new FileInfo(args[2]).Length);
    }
}
