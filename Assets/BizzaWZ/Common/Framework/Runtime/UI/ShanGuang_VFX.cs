using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShanGuang_VFX : MonoBehaviour
{
    [SerializeField] private Image normalImage;
    [SerializeField] private Image appearImage;

    [SerializeField] private float normalRotateSpeed = 18f;
    [SerializeField] private float appearRotateSpeed = -28f;
    [SerializeField] private float appearCycleSeconds = 1.45f;
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float appearMinAlpha = 0.08f;
    [SerializeField, Range(0f, 1f)] private float appearMaxAlpha = 0.9f;
    [SerializeField] private float appearMinScale = 0.94f;
    [SerializeField] private float appearMaxScale = 1.06f;
    [SerializeField, Range(0f, 1f)] private float startPhase = 0f;
    [SerializeField] private bool autoPhaseOffset = true;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform _normalRect;
    private RectTransform _appearRect;
    private Vector3 _normalBaseScale = Vector3.one;
    private Vector3 _appearBaseScale = Vector3.one;
    private float _time;

    private void Awake()
    {
        CacheReferences();
        DisableRaycasts();
        CacheBaseScale();
        ApplyNormalAlpha(); 
    }

    private void OnEnable()
    {
        var phase = autoPhaseOffset ? startPhase + GetHierarchyPhase() : startPhase;
        _time = Mathf.Max(0f, appearCycleSeconds) * Mathf.Repeat(phase, 1f);
        ApplyNormalAlpha();
    }

    private void Update()
    {
        var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (deltaTime <= 0f) return;

        _time += deltaTime;

        Rotate(_normalRect, normalRotateSpeed, deltaTime);
        Rotate(_appearRect, appearRotateSpeed, deltaTime);
        UpdateAppearLayer();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
        DisableRaycasts();
        CacheBaseScale();

        appearCycleSeconds = Mathf.Max(0.01f, appearCycleSeconds);
        appearMaxAlpha = Mathf.Max(appearMinAlpha, appearMaxAlpha);
        appearMaxScale = Mathf.Max(appearMinScale, appearMaxScale);

        ApplyNormalAlpha();
        UpdateAppearLayer();
    }
#endif

    private void CacheReferences()
    {
        if (!normalImage)
        {
            normalImage = FindChildImage("Rotate_Normal");
        }

        if (!appearImage)
        {
            appearImage = FindChildImage("Rotate_AppearIndistinctly");
        }

        _normalRect = normalImage ? normalImage.rectTransform : null;
        _appearRect = appearImage ? appearImage.rectTransform : null;
    }

    private Image FindChildImage(string childName)
    {
        var child = transform.Find(childName);
        return child ? child.GetComponent<Image>() : null;
    }

    private void DisableRaycasts()
    {
        if (normalImage) normalImage.raycastTarget = false;
        if (appearImage) appearImage.raycastTarget = false;
    }

    private void CacheBaseScale()
    {
        if (_normalRect) _normalBaseScale = _normalRect.localScale;
        if (_appearRect) _appearBaseScale = _appearRect.localScale;
    }

    private void Rotate(RectTransform rectTransform, float speed, float deltaTime)
    {
        if (!rectTransform || Mathf.Approximately(speed, 0f)) return;
        rectTransform.Rotate(0f, 0f, speed * deltaTime, Space.Self);
    }

    private void ApplyNormalAlpha()
    {
        SetAlpha(normalImage, normalAlpha);
    }

    private void UpdateAppearLayer()
    {
        if (!appearImage) return;

        var cycle = Mathf.Max(0.01f, appearCycleSeconds);
        var rawPulse = Mathf.Sin((_time / cycle) * Mathf.PI * 2f) * 0.5f + 0.5f;
        var pulse = Mathf.SmoothStep(0f, 1f, rawPulse);

        SetAlpha(appearImage, Mathf.Lerp(appearMinAlpha, appearMaxAlpha, pulse));

        if (_appearRect)
        {
            var scale = Mathf.Lerp(appearMinScale, appearMaxScale, pulse);
            _appearRect.localScale = _appearBaseScale * scale;
        }
    }

    private float GetHierarchyPhase()
    {
        var hash = 17;
        var current = transform;
        while (current)
        {
            hash = hash * 31 + current.GetSiblingIndex();
            current = current.parent;
        }

        return Mathf.Repeat(hash * 0.6180339f, 1f);
    }

    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (!graphic) return;

        var color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }
}
