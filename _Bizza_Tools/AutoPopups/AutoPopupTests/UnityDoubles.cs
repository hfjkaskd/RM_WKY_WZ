using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class LoadingBlock
{
    public static readonly HashSet<object> Reasons = new();
    public static Task AddBlock(object reason) { Reasons.Add(reason); return Task.CompletedTask; }
    public static Task RemoveBlock(object reason) { Reasons.Remove(reason); return Task.CompletedTask; }
}

// Rendering/animation/analytics are test doubles; UIPage.cs lifecycle is compiled unchanged.
public class UnityObject { public static implicit operator bool(UnityObject obj) => obj != null; }
public class GameObject : UnityObject
{
    public bool activeInHierarchy = true;
    public void SetActive(bool value) => activeInHierarchy = value;
}
public class MonoBehaviour : UnityObject
{
    public GameObject gameObject = new();
    public Transform transform = new RectTransform();
    public string name => GetType().Name;
    public T GetComponent<T>() where T : new() => new();
    public Coroutine StartCoroutine(IEnumerator _) => new();
    public void StopCoroutine(Coroutine _) { }
}
public class Transform : UnityObject
{
    public int childCount => 0;
    public Transform GetChild(int _) => new();
    public bool TryGetComponent<T>(out T value) { value = default; return false; }
}
public class RectTransform : Transform
{
    public Vector2 anchoredPosition;
    public Vector3 localScale;
    public void SetAsLastSibling() { }
    public void SetAsFirstSibling() { }
    public void SetSiblingIndex(int _) { }
}
public struct Vector2 { public static Vector2 zero => default; }
public struct Vector3 { public static Vector3 one => default; }
public class Animation : UnityObject
{
    public void Stop() { }
    public void Play(string _) { }
    public bool IsPlaying(string _) => false;
}
public class Coroutine { }
public class CanvasGroup { public float alpha; }
public class RequireComponentAttribute : Attribute { public RequireComponentAttribute(params Type[] _) { } }
public interface IPoolElement { void PoolReset(); }
public enum E_UILayer { BaseLayer, PopupLayer, TopLayer, SystemLayer, TeachLayer }
public class UIPageRuntimeConfig
{
    public PageId PageId;
    public int ZIndex = 1000;
    public static UIPageRuntimeConfig FromPage(UIPageBase page) => new() { PageId = page.PageId };
}
public static class TransparentBlock { public static void AddBlock(object _) { } public static void RemoveBlock(object _) { } }
public static class GameUtils { public static void DelayDo(Action action, float _) => action(); }
namespace Sirenix.OdinInspector { public class FoldoutGroupAttribute : Attribute { public FoldoutGroupAttribute(string _) { } } }
namespace Unity.VisualScripting { }
namespace Bizza.GameAnalytics
{
    public static class BizzaGameAnalytics { public static void TrackPageOpen(string _) { } public static void TrackPageClose(string _) { } }
}
namespace Bizza.Channel.Analytics { public static class Manager { public static void ShowViewEvent(string _) { } } }
