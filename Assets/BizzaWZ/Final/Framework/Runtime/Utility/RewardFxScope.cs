#if BIZZA_REAL_WITHDRAW
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RewardFxScope : MonoBehaviour
{
    private int version;
    private UIPageBase page;

    internal readonly struct Stamp
    {
        private readonly RewardFxScope scope;
        private readonly int version;
        private readonly bool watched;
        internal Stamp(RewardFxScope scope)
        {
            this.scope = scope;
            version = scope != null ? scope.version : 0;
            watched = true;
        }
        internal bool IsValid => !watched || (scope != null && scope.isActiveAndEnabled && scope.version == version &&
            (scope.page == null || !scope.page.IsClosing));
    }

    internal static Stamp Capture(Object owner)
    {
        if (!ReferenceEquals(owner, null) && owner == null) return new Stamp(null);
        GameObject go = owner is Component component ? component.gameObject : owner as GameObject;
        if (go == null) return default;
        UIPageBase page = go.GetComponentInParent<UIPageBase>(true);
        if (page != null) go = page.gameObject;
        RewardFxScope scope = go.GetComponent<RewardFxScope>();
        if (scope == null) scope = go.AddComponent<RewardFxScope>();
        scope.page = page;
        return new Stamp(scope);
    }

    internal static void Invalidate(GameObject root)
    {
        if (root == null) return;
        RewardFxScope scope = root.GetComponent<RewardFxScope>();
        if (scope != null) scope.version++;
    }

    private void OnDisable() { version++; }
}
#endif
