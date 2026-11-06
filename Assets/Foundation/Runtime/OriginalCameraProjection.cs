namespace Whitebox.Foundation
{
    public interface IOriginalProjectionCamera
    {
        float Aspect { get; }
        int PixelWidth { get; }
        int PixelHeight { get; }
        void SetViewport(float x, float y, float width, float height);
        void SetOrthographicSize(float size);
    }

    // Projection portion of CameraManager.UpdateSizeParameters, before safe
    // area, square-device decorations and debug reporting. Native constants
    // are projection rules, not designer-authored visual tuning values.
    public sealed class OriginalCameraProjection
    {
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Aspect { get; private set; }
        public float SquareViewportWidth { get; private set; }
        public bool CroppedViewport { get; private set; }
        public static bool IsLandscape => false; // 0x20022cc and 0x2002270 in this original build.

        public static float GetPortraitAspect(IOriginalProjectionCamera camera) // 0x2003628.
        {
            float aspect = camera.Aspect;
            return aspect > 1f ? 1f / aspect : aspect;
        }

        // GridPositioner.CalculateOffset 0x1e3329c, GetOffsetByAspectRatio
        // 0x1e336c8 and CameraManager.GetUiScaleFactor 0x200591c.
        public static float GetPortraitBoardOffset(float aspect)
        {
            if (aspect < .5f) return -1f;
            if (aspect < .8f) return -.75f;
            return -.9f * 1.1904762f;
        }

        public void UpdateSizeParameters(IOriginalProjectionCamera camera, int scene, bool isTableGame)
        {
            CroppedViewport = false;
            if (GetPortraitAspect(camera) >= .8f)
            {
                int height = camera.PixelHeight;
                int width = camera.PixelWidth;
                // Force the native f32 multiply boundary on Mono as well as
                // IL2CPP; Mono may otherwise retain a wider intermediate.
                float scaledHeight = (float)((double)(float)height * .7f);
                SquareViewportWidth = (float)((double)scaledHeight / (float)width);
                camera.SetViewport((1f - SquareViewportWidth) * .5f, 0f, SquareViewportWidth, 1f);
                CroppedViewport = true;
            }
            Aspect = camera.Aspect;
            float currentPortrait = GetPortraitAspect(camera);
            if (scene == 2 && currentPortrait >= .8f) Width = 15.119047164916992f;
            else if (!(camera.Aspect >= .57f)) Width = 9.3f;
            else
            {
                float portrait = GetPortraitAspect(camera);
                Width = portrait < .6f && !(portrait < .57f) ? 10.5f : 12.7f;
            }
            if (scene == 2 && isTableGame && GetPortraitAspect(camera) >= .65f)
                Width = camera.Aspect * 18.2f;
            Height = Width / Aspect;
            camera.SetOrthographicSize(Height * .5f);
        }
    }
}
