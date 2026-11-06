using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    public sealed class Coroutine { internal IEnumerator Routine; internal bool Active = true; }
    public class GameObject { public void SetActive(bool value) { } }
    public class Transform { public Vector3 position; }
    public class MonoBehaviour
    {
        public readonly Transform transform = new Transform();
        public readonly GameObject gameObject = new GameObject();
        protected Coroutine StartCoroutine(IEnumerator routine) => Transport.Start(routine);
        protected void StopCoroutine(Coroutine value) { if (value != null) value.Active = false; }
        protected void Destroy(GameObject value, float delay) { }
    }
    public sealed class Camera { public readonly Transform transform = new Transform(); public float orthographicSize; }
    public sealed class Canvas { public Camera worldCamera; public int sortingOrder; }
    public sealed class MeshRenderer { public int sortingOrder; }
    public struct Rect { public float height; }
    public sealed class RectTransform { public Rect rect; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x, a.y+b.y, a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x-b.x, a.y-b.y, a.z-b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x*b, a.y*b, a.z*b);
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => new Vector3(a.x+(b.x-a.x)*t, a.y+(b.y-a.y)*t, a.z+(b.z-a.z)*t);
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Sin(float value) => (float)Math.Sin(value);
        public static float Clamp01(float value) => value < 0 ? 0 : value > 1 ? 1 : value;
    }
    public static class Time { public static float deltaTime; }
    internal static class Transport
    {
        internal static readonly List<Coroutine> Routines = new List<Coroutine>();
        internal static Coroutine Start(IEnumerator routine)
        {
            var value = new Coroutine { Routine = routine };
            value.Active = routine.MoveNext();
            if (value.Active) Routines.Add(value);
            return value;
        }
        internal static void Tick()
        {
            foreach (var value in Routines.ToArray())
                if (value.Active) value.Active = value.Routine.MoveNext();
            Routines.RemoveAll(value => !value.Active);
        }
    }
}
namespace UnityEngine.UI
{
    public sealed class Graphic { public UnityEngine.RectTransform rectTransform = new UnityEngine.RectTransform(); public bool raycastTarget = true; }
    public sealed class Click
    {
        private event Action listeners;
        public void AddListener(Action value) { listeners += value; }
        public void RemoveListener(Action value) { listeners -= value; }
        public void Invoke() { listeners?.Invoke(); }
    }
    public sealed class Button { public readonly Click onClick = new Click(); public readonly Graphic targetGraphic = new Graphic(); public bool interactable = true; }
}
namespace Whitebox.Foundation
{
    using UnityEngine;
    public struct OriginalDialogConfig { public bool ShouldCloseOnTouch, ShouldPlayCloseAnimation; }
    public sealed class OriginalFlowAction { }
    public interface IOriginalUiDialogManager { }
    public interface IOriginalUiDialogAction { }
    public sealed class OriginalUiDialogAction : IOriginalUiDialogAction
    { public OriginalUiDialogAction(OriginalFlowAction action, IOriginalUiDialogManager manager) { } }
    public interface IOriginalFlowDialog { OriginalUiDialog Lifecycle { get; } void Show(); }
    public interface IOriginalUiDialogHost
    {
        void UpdateParent(OriginalUiDialog dialog);
        Vector3 GetCameraCenter();
        void SetActive(bool active);
        void StartDialogCoroutine(IEnumerator routine);
        void DestroyDialog(float delay);
    }
    public abstract class OriginalUiDialog
    {
        private readonly IOriginalUiDialogHost host;
        private bool closing;
        protected OriginalUiDialog(IOriginalUiDialogAction action, IOriginalUiDialogHost host) { this.host = host; }
        public virtual OriginalDialogConfig GetConfig() => default(OriginalDialogConfig);
        public virtual void SetSortingOffset(int offset) { }
        public virtual void SetTouchSorting(int order) { }
        protected virtual void OnShow(Vector3 position) { }
        protected virtual IEnumerator CloseAnimation() { yield break; }
        protected virtual void OnClose() { }
        public void Show() { OnShow(host.GetCameraCenter()); }
        public void Close()
        {
            if (closing) return;
            closing = true;
            if (GetConfig().ShouldPlayCloseAnimation) host.StartDialogCoroutine(CloseRoutine());
            else OnClose();
        }
        private IEnumerator CloseRoutine()
        {
            var routine = CloseAnimation();
            while (routine.MoveNext()) yield return null;
            OnClose();
        }
    }
    public sealed class CoreGameFlow
    {
        public readonly Camera UiCamera = new Camera();
        public Vector3 SafeCenter = new Vector3(1, -.4f, 0);
        public float GameplayDeltaTime;
        public int Completed;
        public readonly List<int> Sounds = new List<int>();
        public void DisableOrientation(int reason) { if (reason != 35) throw new Exception("orientation"); }
        public void PlaySound(int id, float cooldown) { Sounds.Add(id); }
        public void CompleteOutOfMovesBanner() { Completed++; }
        public void UpdateDialogParent(Transform target, OriginalDialogConfig config) { }
        public Vector3 GetCameraCenter() => UiCamera.transform.position;
    }
    internal static class BannerMotionProgram
    {
        private static CoreOutOfMovesBanner banner;
        private static CoreGameFlow owner;
        private static float F(string value) => float.Parse(value, CultureInfo.InvariantCulture);
        private static float Bits(string value) => BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(value, CultureInfo.InvariantCulture)), 0);
        private static void State()
        {
            var p = banner.transform.position;
            Console.WriteLine(p.x.ToString("R",CultureInfo.InvariantCulture) + "," + p.y.ToString("R",CultureInfo.InvariantCulture) + "," + p.z.ToString("R",CultureInfo.InvariantCulture) + "|" + owner.Completed + "|" + (banner.SkipButton.interactable ? "1" : "0") + "|" + string.Join(",",owner.Sounds));
        }
        private static void Main()
        {
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                var a = line.Split(' ');
                if (a[0] == "I")
                {
                    Transport.Routines.Clear();
                    owner = new CoreGameFlow();
                    owner.UiCamera.transform.position = new Vector3(1,0,-10);
                    owner.UiCamera.orthographicSize = 8;
                    banner = new CoreOutOfMovesBanner();
                    banner.buttonCanvas = new Canvas();
                    banner.titleRenderer = new MeshRenderer();
                    banner.skipButton = new UnityEngine.UI.Button();
                    banner.skipButton.targetGraphic.rectTransform.rect = new Rect { height = 1.5f };
                    banner.enterDuration=F(a[1]);banner.overshootDuration=F(a[2]);banner.settleDuration=F(a[3]);banner.holdDuration=F(a[4]);
                    banner.exitDuration=F(a[5]);banner.exitPause=F(a[6]);banner.enterOffset=F(a[7]);banner.overshootOffset=F(a[8]);
                    banner.exitOvershoot=F(a[9]);banner.skipDuration=F(a[10]);
                    banner.Bind(new OriginalFlowAction(),null,owner);
                    banner.Show();
                }
                else if (a[0] == "T")
                {
                    Time.deltaTime=Bits(a[1]);owner.GameplayDeltaTime=Bits(a[2]);Transport.Tick();
                }
                else if (a[0] == "K" && banner.SkipButton.interactable && banner.SkipButton.targetGraphic.raycastTarget) banner.SkipButton.onClick.Invoke();
                State();
            }
        }
    }
}
