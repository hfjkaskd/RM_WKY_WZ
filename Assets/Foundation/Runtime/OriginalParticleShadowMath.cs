using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // The native helpers rotate Y, X, Z in that order, with distinct signs.
    // Float fields preserve single-precision boundaries on Mono as on ARM64.
    public struct OriginalParticleShadowMath
    {
        private Vector3 result;
        private float radius, firstSquare, secondSquare, angle, rotation, sine, cosine;
        public Vector3 ArrangeRotation(Vector3 position, Vector3 angles)
        {
            result = position;
            if (angles.x == 0f && angles.y == 0f && angles.z == 0f) return result;
            if (angles.y > float.Epsilon)
            {
                angle = (float)Math.Atan2(result.z, result.x);
                firstSquare = result.z * result.z;
                secondSquare = result.x * result.x;
                radius = firstSquare + secondSquare;
                radius = (float)Math.Sqrt(radius);
                rotation = angles.y * 3.1415927410125732f;
                rotation /= 180f;
                angle = rotation + angle;
                sine = (float)Math.Sin(angle);
                cosine = (float)Math.Cos(angle);
                result.x = radius * cosine;
                result.z = radius * sine;
            }
            if (angles.x > float.Epsilon)
            {
                angle = (float)Math.Atan2(result.z, result.y);
                firstSquare = result.z * result.z;
                secondSquare = result.y * result.y;
                radius = secondSquare + firstSquare;
                radius = (float)Math.Sqrt(radius);
                rotation = angles.x * -3.1415927410125732f;
                rotation /= 180f;
                angle += rotation;
                sine = (float)Math.Sin(angle);
                cosine = (float)Math.Cos(angle);
                result.z = radius * sine;
                result.y = radius * cosine;
            }
            if (angles.z > float.Epsilon)
            {
                angle = (float)Math.Atan2(result.y, result.x);
                firstSquare = result.y * result.y;
                secondSquare = result.x * result.x;
                radius = secondSquare + firstSquare;
                radius = (float)Math.Sqrt(radius);
                rotation = angles.z * -3.1415927410125732f;
                rotation /= 180f;
                angle += rotation;
                sine = (float)Math.Sin(angle);
                cosine = (float)Math.Cos(angle);
                result.x = radius * cosine;
                result.y = radius * sine;
            }
            return result;
        }
        public Vector3 ArrangeScale(Vector3 position, Vector3 scale)
        {
            return new Vector3(position.x / scale.x, position.y / scale.y, position.z / scale.z);
        }
        public static Color32 UpdateColor(Color32 color, float alpha) // 0x2d74bcc.
        {
            float denominator = alpha * 255f;
            float step = color.a / denominator;
            step += 1f;
            if (color.a == 255) step = 0f;
            if (step > 10f) return color;
            float nextAlpha = step * alpha;
            if (nextAlpha > 1f) nextAlpha = 1f;
            if (nextAlpha < 0f) nextAlpha = 0f;
            float channel = nextAlpha * 255f;
            byte rounded = float.IsNaN(channel) ? (byte)0 : unchecked((byte)(int)Math.Round(channel, MidpointRounding.ToEven));
            return new Color32(0, 0, 0, rounded);
        }
    }
}
