// Offline inspection/export support. This tool and the existing reference
// decoder compile outside Unity Assets/Packages; no third-party runtime ships.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Spine;

sealed class InspectKingTexture : TextureLoader
{ public void Load(AtlasPage page, string path) { } public void Unload(object texture) { } }
static class InspectKingRig
{
    static string Q(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
    static string Kind(Timeline t)
    {
        if(t is RotateTimeline)return "rotate";
        if(t is ScaleTimeline)return "scale";
        if(t is ShearTimeline)return "shear";
        if(t is TranslateTimeline)return "translate";
        if(t is ColorTimeline)return "color";
        if(t is TwoColorTimeline)return "two-color";
        if(t is AttachmentTimeline)return "attachment";
        if(t is DeformTimeline)return "deform";
        if(t is DrawOrderTimeline)return "draw-order";
        if(t is EventTimeline)return "event";
        if(t is IkConstraintTimeline)return "ik";
        if(t is TransformConstraintTimeline)return "transform-constraint";
        if(t is PathConstraintSpacingTimeline)return "path-spacing";
        if(t is PathConstraintPositionTimeline)return "path-position";
        if(t is PathConstraintMixTimeline)return "path-mix";
        throw new NotSupportedException("Unknown timeline");
    }
    static void Main(string[] args)
    {
        var atlas=new Atlas(args[0],new InspectKingTexture());
        var data=new SkeletonBinary(new AtlasAttachmentLoader(atlas)){Scale=.007142857f}.ReadSkeletonData(args[1]);
        var outText=new StringBuilder();
        outText.Append("{\"version\":").Append(Q(data.Version)).Append(",\"bones\":").Append(data.Bones.Count).Append(",\"slots\":").Append(data.Slots.Count)
            .Append(",\"skins\":").Append(data.Skins.Count).Append(",\"ik\":").Append(data.IkConstraints.Count).Append(",\"transform_constraints\":").Append(data.TransformConstraints.Count)
            .Append(",\"path_constraints\":").Append(data.PathConstraints.Count).Append(",\"bone_modes\":[");
        for(int i=0;i<data.Bones.Count;i++){if(i>0)outText.Append(',');outText.Append((int)data.Bones.Items[i].TransformMode);}
        outText.Append("],\"animations\":[");
        var totals=new SortedDictionary<string,int>();int frameFloats=0,curveFloats=0;
        for(int i=0;i<data.Animations.Count;i++)
        {
            var animation=data.Animations.Items[i];if(i>0)outText.Append(',');
            outText.Append("{\"name\":").Append(Q(animation.Name)).Append(",\"duration\":").Append(animation.Duration.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(",\"timelines\":").Append(animation.Timelines.Count).Append(",\"kinds\":{");
            var counts=new SortedDictionary<string,int>();
            foreach(var timeline in animation.Timelines)
            {
                string kind=Kind(timeline);counts[kind]=counts.ContainsKey(kind)?counts[kind]+1:1;totals[kind]=totals.ContainsKey(kind)?totals[kind]+1:1;
                var curve=timeline as CurveTimeline;if(curve!=null)curveFloats+=curve.curves.Length;
                if(timeline is RotateTimeline)frameFloats+=((RotateTimeline)timeline).Frames.Length;
                else if(timeline is TranslateTimeline)frameFloats+=((TranslateTimeline)timeline).Frames.Length;
                else if(timeline is DeformTimeline){foreach(var frame in ((DeformTimeline)timeline).Vertices)frameFloats+=frame.Length;}
            }
            int n=0;foreach(var count in counts){if(n++>0)outText.Append(',');outText.Append(Q(count.Key)).Append(':').Append(count.Value);}outText.Append("}}");
        }
        int attachments=0,meshes=0,regions=0,weighted=0,clipping=0,maxVertices=0;
        foreach(var skin in data.Skins)foreach(var entry in skin.GetAttachments())
        {
            attachments++;var mesh=entry.Attachment as MeshAttachment;
            if(mesh!=null){meshes++;if(mesh.Bones!=null)weighted++;maxVertices=Math.Max(maxVertices,mesh.WorldVerticesLength/2);}
            else if(entry.Attachment is RegionAttachment)regions++;
            else if(entry.Attachment is ClippingAttachment)clipping++;
        }
        outText.Append("],\"attachments\":").Append(attachments).Append(",\"meshes\":").Append(meshes).Append(",\"regions\":").Append(regions).Append(",\"weighted_meshes\":").Append(weighted)
            .Append(",\"clipping\":").Append(clipping).Append(",\"largest_mesh_vertices\":").Append(maxVertices).Append(",\"frame_floats\":").Append(frameFloats).Append(",\"curve_floats\":").Append(curveFloats).Append('}');
        File.WriteAllText(args[2],outText.ToString(),new UTF8Encoding(false));
        Console.WriteLine("King original rig: bones="+data.Bones.Count+",slots="+data.Slots.Count+",animations="+data.Animations.Count+",attachments="+attachments+",weighted="+weighted+",clipping="+clipping);
    }
}
