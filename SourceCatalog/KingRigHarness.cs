// Compares analytical sampling against the recovered decoder/runtime, not
// against sampled output from this implementation. Runs outside Unity.
using System;
using System.IO;
using Spine;
using Whitebox.Foundation;
sealed class KingHarnessTexture:TextureLoader
{public void Load(AtlasPage p,string path){}public void Unload(object t){}}
static class KingRigHarness
{
    static long values;static float maxError;
    static void Equal(float actual,float expected,string label)
    {values++;float error=Math.Abs(actual-expected);maxError=Math.Max(error,maxError);if(float.IsNaN(actual)||error>0.000004f)throw new Exception(label+" expected="+expected+" actual="+actual+" error="+error);}
    static void Main(string[] args)
    {
        var atlas=new Atlas(args[0],new KingHarnessTexture());var original=new SkeletonBinary(new AtlasAttachmentLoader(atlas)){Scale=.007142857f}.ReadSkeletonData(args[1]);
        OriginalKingRigData data;using(var f=File.OpenRead(args[2]))data=new OriginalKingRigData(f);
        string trackReport = KingTrackReference.Run(original, data);
        if (args.Length == 5 && args[4] == "--track-only")
        {
            Console.WriteLine(trackReport);
            return;
        }
        var pose=new OriginalKingPose(data);var mesh=new OriginalKingMesh(pose,"sol frame");var skeleton=new Skeleton(original);var random=new Random(7814);int samples=0;
        var output=new float[1024];var expected=new float[1024];
        for(int clip=0;clip<data.Clips.Length;clip++)
        {
            var c=data.Clips[clip];var animation=original.FindAnimation(c.Name);
            var times=new System.Collections.Generic.List<float>{-0.001f,0,c.Duration,c.Duration+0.001f};
            foreach(var t in c.Timelines)
            {
                int stride=t.Kind==0?2:t.Kind<=3?3:t.Kind==4?5:1;
                for(int k=0;k<t.Frames.Length;k+=stride){times.Add(t.Frames[k]);times.Add(t.Frames[k]-0.00001f);times.Add(t.Frames[k]+0.00001f);}
            }
            for(int i=0;i<150;i++)times.Add((float)(random.NextDouble()*c.Duration*1.1));
            foreach(float time in times)
            {
                bool loop=samples%2==0;samples++;skeleton.SetToSetupPose();animation.Apply(skeleton,0,time,loop,null,1,MixBlend.Setup,MixDirection.In);skeleton.UpdateWorldTransform();pose.Sample(clip,time,loop);
                for(int b=0;b<data.Bones.Length;b++)
                {
                    var bone=skeleton.Bones.Items[b];var l=pose.Local[b];var w=pose.World[b];
                    float[] local={bone.X,bone.Y,bone.Rotation,bone.ScaleX,bone.ScaleY,bone.ShearX,bone.ShearY};
                    float[] world={bone.A,bone.B,bone.C,bone.D,bone.WorldX,bone.WorldY};
                    for(int k=0;k<7;k++)Equal(l[k],local[k],c.Name+" local "+b+" ch "+k+" t "+time);
                    for(int k=0;k<6;k++)Equal(w[k],world[k],c.Name+" world "+b+" ch "+k+" t "+time);
                }
                if(samples%23==0||time==0||time==c.Duration)KingMeshReference.Compare(mesh,skeleton);
                for(int s=0;s<data.Slots.Length;s++)
                {
                    var slot=skeleton.Slots.Items[s];var a=slot.Attachment;int actual=pose.Attachment[s];
                    if((a==null)!=(actual<0))throw new Exception("attachment null");
                    if(a!=null&&data.Attachments[actual].Name!=a.Name)throw new Exception("attachment name");
                    float[] color={slot.R,slot.G,slot.B,slot.A};for(int k=0;k<4;k++)Equal(pose.Colors[s][k],color[k],"slot color");
                    if(slot.Deform.Count!=pose.DeformLength[s])throw new Exception("deform length");
                    for(int k=0;k<slot.Deform.Count;k++)Equal(pose.Deforms[s][k],slot.Deform.Items[k],"deform");
                    if(pose.DrawOrder[s]!=skeleton.DrawOrder.Items[s].Data.Index)throw new Exception("draw order");
                    if(a==null)continue;pose.WorldVertices(actual,output);var r=a as RegionAttachment;var v=a as VertexAttachment;int count;
                    if(r!=null){r.ComputeWorldVertices(slot.Bone,expected,0,2);count=8;}else{count=v.WorldVerticesLength;v.ComputeWorldVertices(slot,0,count,expected,0,2);}
                    for(int k=0;k<count;k++)Equal(output[k],expected[k],c.Name+" vertex "+s+" t "+time);
                }
            }
        }
        string json="{\"passed\":true,\"animations\":"+data.Clips.Length+",\"pose_samples\":"+samples+",\"scalar_comparisons\":"+values+",\"max_absolute_error\":"+maxError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"oracle\":\"recovered 3.8.84 reference decoder/runtime outside Unity; not native ARM execution\",\"includes_key_boundaries_loop_weighted_mesh_deform_draw_order\":true}";
        json=json.TrimEnd('}')+",\"clipped_mesh_samples\":"+KingMeshReference.Samples+",\"clipped_mesh_max_error\":"+KingMeshReference.MaxError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"}";
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++){pose.Sample(0,i*.016f,true);mesh.Build();}
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before;if(allocated!=0)throw new Exception("Per-frame managed allocation: "+allocated);
        json=json.TrimEnd('}')+",\"pose_and_mesh_managed_bytes_1000_frames\":"+allocated+",\"animation_track\":"+trackReport+"}";
        File.WriteAllText(args[3],json+"\n");Console.WriteLine(json);
    }
}
