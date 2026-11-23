using UnityEngine;

[DisallowMultipleComponent]
public sealed class ReskinReferenceLayout : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private Vector2 designSize = new Vector2(1510f, 2360f);
    [SerializeField] private bool fitHeight;

    private void OnEnable()
    {
        RefreshLayout();
    }

    private void Start()
    {
        RefreshLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        RefreshLayout();
    }

    private void OnTransformParentChanged()
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        if (viewport == null || content == null || designSize.x <= 0f)
            return;

        float width = viewport.rect.width;
        if (width <= 0f)
            return;

        float scale = Mathf.Min(1f, width / designSize.x);
        float height = viewport.rect.height;
        if (fitHeight && designSize.y > 0f && height > 0f)
            scale = Mathf.Min(scale, height / designSize.y);
        content.localScale = new Vector3(scale, scale, 1f);
    }
}
