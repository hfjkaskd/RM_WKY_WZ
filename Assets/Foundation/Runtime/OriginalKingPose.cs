using System;

namespace Whitebox.Foundation
{
    // Analytical evaluation of the King rig. A track can apply several clips
    // to the same local pose before evaluating the bones and constraints.
    public sealed class OriginalKingPose
    {
        public readonly OriginalKingRigData Data;
        // Local layout x,y,rotation,scaleX,scaleY,shearX,shearY.
        // World layout a,b,c,d,x,y. Buffers are allocated once.
        public readonly float[][] Local, World, Colors, Deforms;
        public readonly int[] Attachment, DeformLength, DrawOrder;
        public readonly int[][][] ClipPolygons;
        private readonly byte[] attachmentKeys;
        public readonly float[][] ConstraintValues, PathValues;
        private readonly OriginalSingleBonePath[] paths;
        public OriginalKingPose(OriginalKingRigData data)
        {
            Data=data;Local=new float[data.Bones.Length][];World=new float[Local.Length][];
            for(int i=0;i<Local.Length;i++){Local[i]=new float[7];World[i]=new float[6];}
            int count=data.Slots.Length;Colors=new float[count][];Deforms=new float[count][];
            Attachment=new int[count];DeformLength=new int[count];DrawOrder=new int[count];
            attachmentKeys = new byte[count];
            ClipPolygons=new int[count][][];
            var maximum=new int[count];
            foreach(var c in data.Clips)foreach(var t in c.Timelines)if(t.Kind==6)foreach(var frame in t.Deforms)maximum[t.Target]=Math.Max(maximum[t.Target],frame.Length);
            for(int i=0;i<count;i++){Colors[i]=new float[4];Deforms[i]=new float[maximum[i]];}
            ConstraintValues=new float[data.Constraints.Length][];
            for(int i=0;i<ConstraintValues.Length;i++)ConstraintValues[i]=new float[10];
            PathValues=new float[data.Paths.Length][];paths=new OriginalSingleBonePath[data.Paths.Length];
            for(int i=0;i<paths.Length;i++){PathValues[i]=new float[5];paths[i]=new OriginalSingleBonePath(this,data.Paths[i],i);}
            Reset();UpdateWorld();
        }
        public void Reset()
        {
            for(int i=0;i<ConstraintValues.Length;i++)Array.Copy(Data.Constraints[i].Values,ConstraintValues[i],10);
            for(int i=0;i<PathValues.Length;i++)Array.Copy(Data.Paths[i].Values,PathValues[i],5);
            for(int i=0;i<Local.Length;i++)Array.Copy(Data.Bones[i].Setup,Local[i],7);
            for(int i=0;i<Attachment.Length;i++)
            { Array.Copy(Data.Slots[i].Color,Colors[i],4);Attachment[i]=Data.Slots[i].Attachment;DeformLength[i]=0;DrawOrder[i]=i;ClipPolygons[i]=Attachment[i]<0?null:Data.Attachments[Attachment[i]].ClipPolygons; }
        }
        private static int Next(float[] frames,float time,int stride)
        {
            int lo=0,hi=frames.Length/stride;
            while(lo<hi){int mid=(lo+hi)>>1;if(frames[mid*stride]<=time)lo=mid+1;else hi=mid;}
            return lo*stride;
        }
        private static float Wrap(float angle) => angle-(16384-(int)(16384.499999999996-angle/360))*360;
        private static float Clamp(float f) => f<0?0:f>1?1:f;
        public static float Curve(float[] values,int frame,float percent)
        {
            percent=Clamp(percent);int start=frame*19;
            if(values[start]==0)return percent;if(values[start]==1)return 0;
            int end=start+19;float x=0;
            for(int i=start+1;i<end;i+=2)
            {
                x=values[i];if(x<percent)continue;
                if(i==start+1)return values[i+1]*percent/x;
                float px=values[i-2],py=values[i-1];return py+(values[i+1]-py)*(percent-px)/(x-px);
            }
            float y=values[end-1];return y+(1-y)*(percent-x)/(1-x);
        }
        public void Sample(int clipIndex,float time,bool loop)
        {
            Reset();var clip=Data.Clips[clipIndex];if(loop&&clip.Duration!=0)time%=clip.Duration;
            foreach(var t in clip.Timelines)
            {
                float[] f=t.Frames;if(time<f[0])continue;
                FindKeys(t, time, out int prev, out int next, out float percent);
                bool last = next == f.Length;
                int target=t.Target;
                if(t.Kind<=4)
                {
                    float x=f[prev+1];if(!last)x+=(t.Kind==0?Wrap(f[next+1]-x):f[next+1]-x)*percent;
                    if(t.Kind==0){Local[target][2]=Data.Bones[target].Setup[2]+(last?x:Wrap(x));continue;}
                    float y=f[prev+2];if(!last)y+=(f[next+2]-y)*percent;
                    if(t.Kind==4)
                    {
                        Colors[target][0]=Clamp(x);Colors[target][1]=Clamp(y);
                        for(int ch=2;ch<4;ch++){float value=f[prev+1+ch];if(!last)value+=(f[next+1+ch]-value)*percent;Colors[target][ch]=Clamp(value);}continue;
                    }
                    int offset=t.Kind==1?0:t.Kind==2?3:5;var setup=Data.Bones[target].Setup;
                    Local[target][offset]=t.Kind==2?x*setup[offset]:setup[offset]+x;
                    Local[target][offset+1]=t.Kind==2?y*setup[offset+1]:setup[offset+1]+y;
                }
                else if(t.Kind==5)
                { int a=t.Attachments[prev];if(a!=Attachment[target]){Attachment[target]=a;DeformLength[target]=0;ClipPolygons[target]=a<0?null:Data.Attachments[a].ClipPolygons;} }
                else if(t.Kind==6)
                {
                    int a=Attachment[target];if(a<0||Data.Attachments[a].Kind==0||Data.Attachments[a].DeformAttachment!=t.Attachment)continue;
                    var from=t.Deforms[prev];var to=last?from:t.Deforms[next];var dest=Deforms[target];DeformLength[target]=from.Length;
                    for(int v=0;v<from.Length;v++)dest[v]=last?from[v]:from[v]+(to[v]-from[v])*percent;
                    if(Data.Attachments[a].Kind==2)ClipPolygons[target]=t.ClipPolygons;
                }
                else if(t.Kind==7)
                { var order=t.Orders[prev];for(int i=0;i<DrawOrder.Length;i++)DrawOrder[i]=order.Length==0?i:order[i]; }
                else
                {
                    var values=t.Kind==11?ConstraintValues[target]:PathValues[target];
                    int first=t.Kind==8?1:t.Kind==9?2:t.Kind==10?3:0;
                    int channels=t.Kind<=9?1:t.Kind==10?2:4;
                    for(int ch=0;ch<channels;ch++)
                    {
                        float value=f[prev+1+ch];
                        if(!last)value+=(f[next+1+ch]-value)*percent;
                        values[first+ch]=value;
                    }
                }
            }
            UpdateWorld();
        }

        private static void FindKeys(OriginalKingRigData.Timeline timeline, float time, out int previous, out int next, out float percent)
        {
            float[] frames = timeline.Frames;
            int kind = timeline.Kind;
            int stride = kind == 0 || kind == 8 || kind == 9 ? 2 : kind <= 3 || kind == 10 ? 3 : kind == 4 || kind == 11 ? 5 : 1;
            bool last = time >= frames[frames.Length - stride];
            next = last ? frames.Length : Next(frames, time, stride);
            previous = next - stride;
            percent = last || timeline.Curves.Length == 0 ? 0f :
                Curve(timeline.Curves, next / stride - 1, 1 - (time - frames[next]) / (frames[previous] - frames[next]));
        }

        internal void BeginTrackPose() => Array.Clear(attachmentKeys, 0, attachmentKeys.Length);

        internal void FinishTrackPose()
        {
            for (int i = 0; i < attachmentKeys.Length; i++)
                if (attachmentKeys[i] == 1) SetAttachment(i, Data.Slots[i].Attachment);
            UpdateWorld();
        }

        internal void SetAttachment(int slot, int attachment)
        {
            if (Attachment[slot] == attachment) return;
            Attachment[slot] = attachment;
            DeformLength[slot] = 0;
            ClipPolygons[slot] = attachment < 0 ? null : Data.Attachments[attachment].ClipPolygons;
        }

        internal void ApplyTrackTimeline(OriginalKingRigData.Timeline timeline, float time, float alpha,
            bool fromSetup, bool outgoing, float[] rotation, int angleIndex, bool firstRotation, bool replaceTrack)
        {
            int slot = timeline.Target;
            bool beforeFirst = time < timeline.Frames[0];
            if (replaceTrack && !fromSetup && beforeFirst && timeline.Kind < 5)
            {
                if (timeline.Kind == 0 && firstRotation) rotation[angleIndex] = 0f;
                return;
            }
            int previous = 0, next = timeline.Frames.Length;
            float weight = 0f;
            if (!beforeFirst) FindKeys(timeline, time, out previous, out next, out weight);
            if (timeline.Kind == 5)
            {
                SetAttachment(slot, beforeFirst ? Data.Slots[slot].Attachment : timeline.Attachments[previous]);
                if (!outgoing) attachmentKeys[slot] = 2;
                else if (attachmentKeys[slot] == 0) attachmentKeys[slot] = 1;
                return;
            }
            if (timeline.Kind == 6)
            {
                ApplyTrackDeform(timeline, previous, next, weight, alpha, fromSetup, beforeFirst);
                return;
            }
            if (timeline.Kind == 7)
            {
                if (outgoing && !fromSetup) return;
                int[] order = beforeFirst || outgoing ? null : timeline.Orders[previous];
                for (int i = 0; i < DrawOrder.Length; i++)
                    DrawOrder[i] = order == null || order.Length == 0 ? i : order[i];
                return;
            }
            if (timeline.Kind == 4)
            {
                var color = Colors[slot];
                var setupColor = Data.Slots[slot].Color;
                for (int ch = 0; ch < 4; ch++)
                {
                    float value = beforeFirst ? setupColor[ch] : KeyValue(timeline.Frames, previous, next, ch + 1, weight);
                    float start = fromSetup ? setupColor[ch] : color[ch];
                    color[ch] = Clamp(alpha == 1f ? value : start + (value - start) * alpha);
                }
                return;
            }
            var local = Local[slot];
            var setup = Data.Bones[slot].Setup;
            if (timeline.Kind == 0)
            {
                ApplyTrackRotation(timeline.Frames, local, setup[2], previous, next, weight, alpha,
                    fromSetup, beforeFirst, rotation, angleIndex, firstRotation);
                return;
            }
            bool scale = timeline.Kind == 2;
            int offset = timeline.Kind == 1 ? 0 : scale ? 3 : 5;
            for (int axis = 0; axis < 2; axis++)
            {
                int index = offset + axis;
                float basis = setup[index];
                float start = fromSetup ? basis : local[index];
                float value = basis;
                if (!beforeFirst)
                {
                    float key = KeyValue(timeline.Frames, previous, next, axis + 1, weight);
                    value = scale ? key * basis : basis + key;
                    if (scale && alpha != 1f)
                    {
                        if (outgoing) value = Math.Abs(value) * Math.Sign(start);
                        else start = Math.Abs(start) * Math.Sign(value);
                    }
                }
                local[index] = alpha == 1f ? value : start + (value - start) * alpha;
            }
        }

        private static float KeyValue(float[] frames, int previous, int next, int offset, float weight)
        {
            float value = frames[previous + offset];
            return next == frames.Length ? value : value + (frames[next + offset] - value) * weight;
        }

        private static void ApplyTrackRotation(float[] keys, float[] local, float setup, int previous, int next, float weight,
            float alpha, bool fromSetup, bool beforeFirst, float[] angles, int index, bool firstRotation)
        {
            if (firstRotation) angles[index] = 0f;
            if (beforeFirst && fromSetup)
            {
                local[2] = setup;
                return;
            }
            bool last = next == keys.Length;
            float key = beforeFirst ? 0f : keys[previous + 1];
            if (!beforeFirst && !last) key += Wrap(keys[next + 1] - key) * weight;
            float start = fromSetup ? setup : local[2];
            if (alpha == 1f)
            {
                local[2] = fromSetup ? setup + (last ? key : Wrap(key)) : start + Wrap(key + (setup - start));
                return;
            }
            float target = setup + key;
            if (!last && !beforeFirst) target = Wrap(key + setup);
            float difference = Wrap(target - start);
            float total = angles[index];
            if (difference != 0f)
            {
                float previousTotal = firstRotation ? 0f : total;
                float previousDifference = firstRotation ? difference : angles[index + 1];
                bool positive = difference > 0f;
                bool direction = previousTotal >= 0f;
                if (Math.Sign(previousDifference) != Math.Sign(difference) && Math.Abs(previousDifference) <= 90f)
                {
                    if (Math.Abs(previousTotal) > 180f) previousTotal += 360f * Math.Sign(previousTotal);
                    direction = positive;
                }
                total = difference + previousTotal - previousTotal % 360f;
                if (direction != positive) total += 360f * Math.Sign(previousTotal);
                angles[index] = total;
            }
            angles[index + 1] = difference;
            local[2] = Wrap(start + total * alpha);
        }

        private void ApplyTrackDeform(OriginalKingRigData.Timeline timeline, int previous, int next, float weight,
            float alpha, bool fromSetup, bool beforeFirst)
        {
            int slot = timeline.Target;
            int id = Attachment[slot];
            if (id < 0) return;
            var attachment = Data.Attachments[id];
            if (attachment.Kind == 0 || attachment.DeformAttachment != timeline.Attachment) return;
            if (DeformLength[slot] == 0) fromSetup = true;
            if (beforeFirst && (fromSetup || alpha == 1f))
            {
                DeformLength[slot] = 0;
                ClipPolygons[slot] = attachment.ClipPolygons;
                return;
            }
            var first = timeline.Deforms[beforeFirst ? 0 : previous];
            var second = beforeFirst || next == timeline.Deforms.Length ? first : timeline.Deforms[next];
            var output = Deforms[slot];
            bool weighted = attachment.Bones.Length != 0;
            for (int vertex = 0; vertex < first.Length; vertex++)
            {
                float setup = weighted ? 0f : attachment.Vertices[vertex];
                float value = beforeFirst ? setup : first[vertex] + (second[vertex] - first[vertex]) * weight;
                float start = fromSetup ? setup : output[vertex];
                output[vertex] = alpha == 1f ? value : start + (value - start) * alpha;
            }
            DeformLength[slot] = first.Length;
            if (attachment.Kind == 2 && !beforeFirst) ClipPolygons[slot] = timeline.ClipPolygons;
        }
        private const float Radians=3.1415927f/180;
        private static float Cos(float deg) => (float)Math.Cos(deg*Radians);
        private static float Sin(float deg) => (float)Math.Sin(deg*Radians);
        internal void UpdateWorld()
        {
            for(int i=0;i<Data.UpdateKinds.Length;i++)
            {
                int index=Data.UpdateIndices[i];
                if(Data.UpdateKinds[i]==2){paths[index].Apply();continue;}
                if(Data.UpdateKinds[i]==1)
                {
                    var constraint=Data.Constraints[index];var p=World[constraint.Target];var v=ConstraintValues[index];
                    float x=v[5]*p[0]+v[6]*p[1]+p[4],y=v[5]*p[2]+v[6]*p[3]+p[5];
                    foreach (int bone in constraint.Bones)
                    {
                        var w = World[bone];
                        if (v[0] != 0f)
                        {
                            float offset = v[4] * (p[0] * p[3] - p[1] * p[2] > 0f ? Radians : -Radians);
                            float angle = (float)Math.Atan2(p[2], p[0]) - (float)Math.Atan2(w[2], w[0]) + offset;
                            if (angle > 3.1415927f) angle -= 6.2831854f;
                            else if (angle < -3.1415927f) angle += 6.2831854f;
                            angle *= v[0];
                            float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle);
                            float a0 = w[0], b0 = w[1], c0 = w[2], d0 = w[3];
                            w[0] = cos * a0 - sin * c0;
                            w[1] = cos * b0 - sin * d0;
                            w[2] = sin * a0 + cos * c0;
                            w[3] = sin * b0 + cos * d0;
                        }
                        w[4] += (x - w[4]) * v[1];
                        w[5] += (y - w[5]) * v[1];
                        if (v[2] > 0f)
                        {
                            float sx = (float)Math.Sqrt(w[0] * w[0] + w[2] * w[2]);
                            if (sx != 0f) sx = (sx + ((float)Math.Sqrt(p[0] * p[0] + p[2] * p[2]) - sx + v[7]) * v[2]) / sx;
                            w[0] *= sx;
                            w[2] *= sx;
                            float sy = (float)Math.Sqrt(w[1] * w[1] + w[3] * w[3]);
                            if (sy != 0f) sy = (sy + ((float)Math.Sqrt(p[1] * p[1] + p[3] * p[3]) - sy + v[8]) * v[2]) / sy;
                            w[1] *= sy;
                            w[3] *= sy;
                        }
                    }
                    continue;
                }
                if(Data.UpdateKinds[i]!=0)throw new InvalidOperationException("King update kind.");
                var l=Local[index];var world=World[index];
                float a=Cos(l[2]+l[5])*l[3],b=Cos(l[2]+90+l[6])*l[4],c=Sin(l[2]+l[5])*l[3],d=Sin(l[2]+90+l[6])*l[4];
                int parent=Data.Bones[index].Parent;
                if(parent<0){world[0]=a;world[1]=b;world[2]=c;world[3]=d;world[4]=l[0];world[5]=l[1];}
                else
                {
                    var p=World[parent];world[4]=p[0]*l[0]+p[1]*l[1]+p[4];world[5]=p[2]*l[0]+p[3]*l[1]+p[5];
                    world[0]=p[0]*a+p[1]*c;world[1]=p[0]*b+p[1]*d;world[2]=p[2]*a+p[3]*c;world[3]=p[2]*b+p[3]*d;
                }
            }
        }
        public void WorldVertices(int attachmentIndex,float[] output)
        {
            var a=Data.Attachments[attachmentIndex];int slot=a.Slot;var deform=Deforms[slot];bool hasDeform=DeformLength[slot]!=0;
            if(a.Bones.Length==0)
            {
                var vertices=hasDeform&&a.Kind!=0?deform:a.Vertices;var w=World[Data.Slots[slot].Bone];
                for(int i=0;i<a.WorldLength;i+=2){float x=vertices[i],y=vertices[i+1];output[i]=x*w[0]+y*w[1]+w[4];output[i+1]=x*w[2]+y*w[3]+w[5];}
                return;
            }
            int cursor=0,v=0,df=0;
            for(int i=0;i<a.WorldLength;i+=2)
            {
                float x=0,y=0;int influences=a.Bones[cursor++];
                for(int j=0;j<influences;j++,v+=3,df+=2)
                {
                    var w=World[a.Bones[cursor++]];float vx=a.Vertices[v],vy=a.Vertices[v+1],weight=a.Vertices[v+2];
                    if(hasDeform){vx+=deform[df];vy+=deform[df+1];}
                    x+=(vx*w[0]+vy*w[1]+w[4])*weight;y+=(vx*w[2]+vy*w[3]+w[5])*weight;
                }
                output[i]=x;output[i+1]=y;
            }
        }
    }
}

