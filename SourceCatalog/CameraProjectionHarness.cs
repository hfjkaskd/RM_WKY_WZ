using System;
using System.Collections.Generic;
using System.Globalization;
using Whitebox.Foundation;

public static class CameraProjectionHarness
{
    private static string Hex(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value), 0).ToString("x8");
    private sealed class Camera : IOriginalProjectionCamera
    {
        public float[] Values;
        public int Policy, W, H, Reads;
        public float Current;
        public readonly List<string> Trace = new List<string>();
        public float Aspect
        {
            get
            {
                float value = Policy == 2 ? Values[Math.Min(Reads++, Values.Length - 1)] : Current;
                Trace.Add("a" + Hex(value)); return value;
            }
        }
        public int PixelWidth { get { Trace.Add("w" + W); return W; } }
        public int PixelHeight { get { Trace.Add("h" + H); return H; } }
        public void SetViewport(float x, float y, float w, float h)
        { Trace.Add("r" + Hex(x) + "," + Hex(y) + "," + Hex(w) + "," + Hex(h)); if (Policy == 1) Current *= w; }
        public void SetOrthographicSize(float size) { Trace.Add("s" + Hex(size)); }
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var p = line.Split('|'); var head = Array.ConvertAll(p[0].Split(','), int.Parse);
            var camera = new Camera { W = head[2], H = head[3], Policy = head[4], Values =
                Array.ConvertAll(p[1].Split(','), value => float.Parse(value, CultureInfo.InvariantCulture)) };
            camera.Current = camera.Values[0];
            var owner = new OriginalCameraProjection(); owner.UpdateSizeParameters(camera, head[0], head[1] != 0);
            Console.WriteLine(Hex(owner.Width) + "," + Hex(owner.Height) + "," + Hex(owner.Aspect) + "," +
                Hex(owner.SquareViewportWidth) + "," + (owner.CroppedViewport ? 1 : 0) + "," +
                (OriginalCameraProjection.IsLandscape ? 1 : 0) + "#" + string.Join(";", camera.Trace));
        }
    }
}
