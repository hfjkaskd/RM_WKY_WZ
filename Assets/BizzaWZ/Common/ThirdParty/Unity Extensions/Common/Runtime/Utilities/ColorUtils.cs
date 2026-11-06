using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// ColorUtilities
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Get the perceived brightness of a LDR color (alpha is ignored).
        /// (http://alienryderflex.com/hsp.html)
        /// </summary>
        public static float GetPerceivedBrightness(Color color)
        {
            return Mathf.Sqrt(
                0.25f * color.r * color.r +
                0.68f * color.g * color.g +
                0.07f * color.b * color.b);
        }

        public static Color SetGrayscaleLDR(Color color, float grayscale)
        {
            float current = color.grayscale;
            if (current < 0.001f)
            {
                color.r = grayscale;
                color.g = grayscale;
                color.b = grayscale;
            }
            else
            {
                float scale = grayscale / current;
                if (scale <= 1f)
                {
                    color.RGBMultiply(scale);
                }
                else
                {
                    float max = color.maxColorComponent;
                    float scaledMax = max * scale;
                    if (scaledMax <= 1f)
                    {
                        color.RGBMultiply(scale);
                    }
                    else
                    {
                        scale = 1f / max;
                        color.RGBMultiply(scale);
                        current *= scale;

                        float a = color.a;
                        scale = (1f - grayscale) / (1f - current);
                        color = Color.white - (Color.white - color) * scale;
                        color.a = a;
                    }
                }
            }

            return color;
        }

        /// <summary>
        /// Convert a color value to an ARGB32 format int value.
        /// </summary>
        public static int ColorToInt(Color c)
            => (Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f) << 24)
             | (Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f) << 16)
             | (Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f) <<  8)
             | (Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f)      );

        public static uint ColorToUInt(Color c)
            => (uint)ColorToInt(c);

        /// <summary>
        /// Convert an ARGB32 format int value to a color value.
        /// </summary>
        public static Color IntToColor(int argb)
            => new Color(
                ((argb >> 16) & 0xFF) / 255f,
                ((argb >>  8) & 0xFF) / 255f,
                ((argb      ) & 0xFF) / 255f,
                ((argb >> 24) & 0xFF) / 255f);

        public static Color UIntToColor(uint argb)
            => IntToColor((int)argb);

        /// <summary>
        /// Convert a hue value to a color vlue.
        /// 0-red; 0.167-yellow; 0.333-green; 0.5-cyan; 0.667-blue; 0.833-magenta; 1-red
        /// </summary>
        public static Color HueToColor(float hue)
        {
            return new Color(
                HueToGreen(hue + 1f / 3f),
                HueToGreen(hue),
                HueToGreen(hue - 1f / 3f));

            float HueToGreen(float h)
            {
                h = ((h % 1f + 1f) % 1f) * 6f;

                if (h < 1f) return h;
                if (h < 3f) return 1f;
                if (h < 4f) return (4f - h);
                return 0f;
            }
        }

        public static void RGBMultiply(this ref Color color, float factor)
        {
            color.r *= factor;
            color.g *= factor;
            color.b *= factor;
        }

    } // struct ColorUtilities

} // namespace UnityExtensions