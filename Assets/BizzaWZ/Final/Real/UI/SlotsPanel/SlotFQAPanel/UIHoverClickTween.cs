#if BIZZA_REAL_WITHDRAW
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class UIHoverClickTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverMoveY = 8f;
    [SerializeField] private float hoverDuration = 0.18f;
    [SerializeField] private Ease hoverEase = Ease.OutCubic;

    [Header("Exit")]
    [SerializeField] private float exitDuration = 0.15f;
    [SerializeField] private Ease exitEase = Ease.OutCubic;

    [Header("Click")]
    [SerializeField] private float clickScale = 0.92f;
    [SerializeField] private float clickDuration = 0.08f;
    [SerializeField] private Ease clickEase = Ease.OutQuad;

    private Vector3 baseScale;
    private Vector2 baseAnchoredPos;

    private Tween scaleTween;
    private Tween posTween;
    private Sequence clickSequence;

    private bool isPointerInside;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        baseScale = target.localScale;
        baseAnchoredPos = target.anchoredPosition;
    }

    private void OnDisable()
    {
        KillAllTweens();
        ResetToBaseImmediate();
        isPointerInside = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        VibrationUtils.Vibrate(E_VibrateType.Light);
        PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        PlayExit();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClick();
    }

    private void PlayHover()
    {
        clickSequence?.Kill();

        scaleTween?.Kill();
        posTween?.Kill();

        scaleTween = target.DOScale(baseScale * hoverScale, hoverDuration)
            .SetEase(hoverEase);

        posTween = target.DOAnchorPos(baseAnchoredPos + new Vector2(0f, hoverMoveY), hoverDuration)
            .SetEase(hoverEase);
    }

    private void PlayExit()
    {
        clickSequence?.Kill();

        scaleTween?.Kill();
        posTween?.Kill();

        scaleTween = target.DOScale(baseScale, exitDuration)
            .SetEase(exitEase);

        posTween = target.DOAnchorPos(baseAnchoredPos, exitDuration)
            .SetEase(exitEase);
    }

    private void PlayClick()
    {
        clickSequence?.Kill();
        scaleTween?.Kill();
        posTween?.Kill();

        Vector3 hoverTargetScale = baseScale * hoverScale;
        Vector3 clickTargetScale = baseScale * clickScale;

        Vector2 hoverTargetPos = baseAnchoredPos + new Vector2(0f, hoverMoveY);

        clickSequence = DOTween.Sequence();

        clickSequence.Append(
            target.DOScale(clickTargetScale, clickDuration).SetEase(clickEase)
        );

        clickSequence.Append(
            target.DOScale(isPointerInside ? hoverTargetScale : baseScale, clickDuration).SetEase(Ease.OutBack)
        );

        clickSequence.Join(
            target.DOAnchorPos(isPointerInside ? hoverTargetPos : baseAnchoredPos, clickDuration).SetEase(Ease.OutCubic)
        );

        clickSequence.OnComplete(() =>
        {
            clickSequence = null;

            if (isPointerInside)
                PlayHover();
            else
                PlayExit(); 
        });
    }

    private void KillAllTweens()
    {
        scaleTween?.Kill();
        posTween?.Kill();
        clickSequence?.Kill();

        scaleTween = null;
        posTween = null;
        clickSequence = null;
    }

    private void ResetToBaseImmediate()
    {
        target.localScale = baseScale;
        target.anchoredPosition = baseAnchoredPos;
    }
}
#endif
