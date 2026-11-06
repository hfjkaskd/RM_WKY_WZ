namespace Whitebox.Foundation
{
    public sealed class OriginalUnityTimeSource : IOriginalUnityTimeSource
    {
        public static readonly OriginalUnityTimeSource Instance = new OriginalUnityTimeSource();
        private OriginalUnityTimeSource() { }
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float TimeScale { get => UnityEngine.Time.timeScale; set => UnityEngine.Time.timeScale = value; }
    }
}
