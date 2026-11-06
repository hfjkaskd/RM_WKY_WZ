using System;

namespace Whitebox.Foundation
{
    // The tutorial hand uses open cubic paths, percentage position, and a
    // single tangent follower. The authored update order determines when this
    // world-space constraint runs. Unsupported layouts fail during loading.
    public sealed class OriginalSingleBonePath
    {
        private readonly OriginalKingPose pose;
        private readonly OriginalKingRigData.PathConstraint data;
        private readonly int index;
        private readonly float[] world, curves, segments = new float[10];

        public OriginalSingleBonePath(OriginalKingPose pose, OriginalKingRigData.PathConstraint data, int index)
        {
            if (data.PositionMode != 1 || data.SpacingMode != 0 || data.RotateMode != 0 || data.Bones.Length != 1)
                throw new InvalidOperationException("The rig needs a different path follower layout.");
            this.pose = pose;
            this.data = data;
            this.index = index;
            int capacity = 0;
            foreach (var attachment in pose.Data.Attachments)
            {
                if (attachment.Slot != data.Target || attachment.Kind != 3) continue;
                if (attachment.PathClosed || !attachment.PathConstantSpeed || attachment.WorldLength < 12 || attachment.WorldLength % 6 != 0)
                    throw new InvalidOperationException("The rig needs a different cubic path layout.");
                capacity = Math.Max(capacity, attachment.WorldLength);
            }
            if (capacity == 0) throw new InvalidOperationException("The path attachment is missing.");
            world = new float[capacity];
            curves = new float[(capacity - 6) / 6];
        }

        public void Apply()
        {
            int attachment = pose.Attachment[data.Target];
            if (attachment < 0 || pose.Data.Attachments[attachment].Kind != 3) return;
            var values = pose.PathValues[index];
            float rotate = values[3], translate = values[4];
            if (rotate <= 0f && translate <= 0f) return;
            pose.WorldVertices(attachment, world);
            int count = (pose.Data.Attachments[attachment].WorldLength - 6) / 6;
            float total = 0f;
            for (int curve = 0; curve < count; curve++)
            {
                total += Measure(2 + curve * 6, 4, null);
                curves[curve] = total;
            }

            float distance = values[1] * total;
            float x, y, directionX, directionY;
            if (distance < 0f || distance > total)
            {
                int offset = distance < 0f ? 2 : 2 + (count - 1) * 6;
                float t = distance < 0f ? 0f : 1f;
                Evaluate(offset, t, out x, out y);
                Tangent(offset, t, out directionX, out directionY);
                float length = (float)Math.Sqrt(directionX * directionX + directionY * directionY);
                float extra = distance < 0f ? distance : distance - total;
                if (length != 0f) { x += directionX * extra / length; y += directionY * extra / length; }
            }
            else
            {
                int curve = 0;
                while (curve < count - 1 && distance > curves[curve]) curve++;
                float start = curve == 0 ? 0f : curves[curve - 1];
                float length = curves[curve] - start;
                float fraction = length == 0f ? 0f : (distance - start) / length;
                int offset = 2 + curve * 6;
                float fineLength = Measure(offset, 10, segments);
                float fineDistance = fraction * fineLength;
                int segment = 0;
                while (segment < 9 && fineDistance > segments[segment]) segment++;
                float previous = segment == 0 ? 0f : segments[segment - 1];
                float interval = segments[segment] - previous;
                float t = (segment + (interval == 0f ? 0f : (fineDistance - previous) / interval)) * .1f;
                if (float.IsNaN(t)) t = 0f;
                Evaluate(offset, t, out x, out y);
                Tangent(offset, t, out directionX, out directionY);
            }

            var bone = pose.World[data.Bones[0]];
            bone[4] += (x - bone[4]) * translate;
            bone[5] += (y - bone[5]) * translate;
            if (rotate <= 0f) return;
            var parent = pose.World[pose.Data.Slots[data.Target].Bone];
            float offsetAngle = values[0] * (parent[0] * parent[3] - parent[1] * parent[2] > 0f ? .017453292f : -.017453292f);
            float angle = (float)Math.Atan2(directionY, directionX) - (float)Math.Atan2(bone[2], bone[0]) + offsetAngle;
            if (angle > 3.1415927f) angle -= 6.2831854f;
            else if (angle < -3.1415927f) angle += 6.2831854f;
            angle *= rotate;
            float cosine = (float)Math.Cos(angle), sine = (float)Math.Sin(angle);
            float a = bone[0], b = bone[1], c = bone[2], d = bone[3];
            bone[0] = cosine * a - sine * c;
            bone[1] = cosine * b - sine * d;
            bone[2] = sine * a + cosine * c;
            bone[3] = sine * b + cosine * d;
        }

        private float Measure(int offset, int divisions, float[] cumulative)
        {
            // Forward differences use the same four-chord curve estimate and
            // ten-chord local distance table as the original constant-speed path.
            float quadratic = divisions == 4 ? .1875f : .03f;
            float cubic = divisions == 4 ? .09375f : .006f;
            float linear = divisions == 4 ? .75f : .3f;
            float qx = (world[offset] - world[offset + 2] * 2f + world[offset + 4]) * quadratic;
            float qy = (world[offset + 1] - world[offset + 3] * 2f + world[offset + 5]) * quadratic;
            float thirdX = ((world[offset + 2] - world[offset + 4]) * 3f - world[offset] + world[offset + 6]) * cubic;
            float thirdY = ((world[offset + 3] - world[offset + 5]) * 3f - world[offset + 1] + world[offset + 7]) * cubic;
            float secondX = qx * 2f + thirdX, secondY = qy * 2f + thirdY;
            float dx = (world[offset + 2] - world[offset]) * linear + qx + thirdX * .16666667f;
            float dy = (world[offset + 3] - world[offset + 1]) * linear + qy + thirdY * .16666667f;
            float length = 0f;
            for (int segment = 0; segment < divisions; segment++)
            {
                length += (float)Math.Sqrt(dx * dx + dy * dy);
                if (cumulative != null) cumulative[segment] = length;
                dx += secondX; dy += secondY;
                secondX += thirdX; secondY += thirdY;
            }
            return length;
        }

        private void Evaluate(int offset, float t, out float x, out float y)
        {
            if (t < .00001f || float.IsNaN(t)) { x = world[offset]; y = world[offset + 1]; return; }
            float u = 1f - t, ut = u * t, u2 = u * u;
            float a = u * u2, b = u * (ut * 3f), c = (ut * 3f) * t, d = t * t * t;
            x = world[offset] * a + world[offset + 2] * b + world[offset + 4] * c + world[offset + 6] * d;
            y = world[offset + 1] * a + world[offset + 3] * b + world[offset + 5] * c + world[offset + 7] * d;
        }

        private void Tangent(int offset, float t, out float x, out float y)
        {
            if (t < .001f)
            { x = world[offset + 2] - world[offset]; y = world[offset + 3] - world[offset + 1]; return; }
            Evaluate(offset, t, out float px, out float py);
            float u = 1f - t, u2 = u * u, ut = u * t, t2 = t * t;
            x = px - (world[offset] * u2 + (world[offset + 2] * ut + world[offset + 2] * ut) + world[offset + 4] * t2);
            y = py - (world[offset + 1] * u2 + (world[offset + 3] * ut + world[offset + 3] * ut) + world[offset + 5] * t2);
        }
    }
}
