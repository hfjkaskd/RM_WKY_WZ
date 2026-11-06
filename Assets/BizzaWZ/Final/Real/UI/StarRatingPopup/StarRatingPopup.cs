#if BIZZA_REAL_WITHDRAW
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using DG.Tweening; // ✅ DOTween

#if UNITY_ANDROID && !UNITY_EDITOR && BIZZA_ENABLE_MAX
using Google.Play.Review;
#endif

public partial class UIPageIds
{
    public static readonly PageId StarRatingPopup = "StarRatingPopup";
}

public class StarRatingPopup : UIPageBase
{
    [Header("UI Refs")]
    public GameObject popupRoot;
    public BizzaButton[] starButtons;
    public Image[] starImages;
    public Sprite starOn;
    public Sprite starOff;
    public BizzaButton goToRateButton;

    [Header("Tween Tuning")]
    public float stagger = 0.03f;          // 星星依次动画间隔
    public float onDuration = 0.22f;        // 点亮时长
    public float offDuration = 0.16f;       // 熄灭时长
    public float punchScale = 0.10f;        // Q弹力度
    public float onOvershoot = 1.10f;       // 点亮时放大到多少
    public float offShrink = 0.90f;         // 熄灭时缩到多少
    public float dimAlpha = 0.75f;          // 暗星透明度（可改为1表示不变）

    private int currentRating = 0;

    // 每颗星各自的动画，避免叠加抖动
    private Sequence[] starSeqs;

#if UNITY_ANDROID && !UNITY_EDITOR && BIZZA_ENABLE_MAX
    private ReviewManager reviewManager;
    private PlayReviewInfo reviewInfo;
#endif

    protected override void OnAwake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR && BIZZA_ENABLE_MAX
        reviewManager = new ReviewManager();
#endif
        starSeqs = new Sequence[starImages.Length];

        for (int i = 0; i < starButtons.Length; i++)
        {
            int rating = i + 1;
            starButtons[i].onClick.AddListener(() => OnSelectRating(rating));
        }

        goToRateButton.onClick.AddListener(OnClickGoToRate);
    }

    protected override void OnOpen()
    {
        KillAllStarTweens();
        SetRatingInstant(0);
        goToRateButton.interactable = true;
    }

    protected override void OnClose()
    {
        if (currentRating == 5)
        {
            LogLogger.LogVerbose("五星好评 --- 可以跳转商店");
            GameInstance.Instance.StartCoroutine(OpenGoogleInAppReview());
        }
        else
        {
            LogLogger.LogVerbose("不是好评 --- 不可以跳转商店");
        }
        KillAllStarTweens();
    }

    private void OnSelectRating(int rating)
    {
        SetRating(rating, true);
    }

    /// <summary>
    /// 无动画直接设置
    /// </summary>
    private void SetRatingInstant(int rating)
    {
        currentRating = rating;
        for (int i = 0; i < starImages.Length; i++)
        {
            bool on = (i < currentRating);
            starImages[i].sprite = on ? starOn : starOff;

            var rt = starImages[i].rectTransform;
            rt.localScale = Vector3.one;

            var c = starImages[i].color;
            c.a = on ? 1f : dimAlpha;
            starImages[i].color = c;
        }
    }

    /// <summary>
    /// 动画设置（丝滑Q弹）
    /// </summary>
    private void SetRating(int rating, bool animate)
    {
        if (!animate)
        {
            SetRatingInstant(rating);
            return;
        }

        int prev = currentRating;
        currentRating = rating;

        // 逐颗星处理：只对状态发生变化的那部分做动画
        for (int i = 0; i < starImages.Length; i++)
        {
            bool wasOn = (i < prev);
            bool nowOn = (i < currentRating);

            if (wasOn == nowOn)
                continue;

            if (nowOn)
                PlayStarOn(i, delay: Mathf.Abs(i - (prev - 1)) * stagger);
            else
                PlayStarOff(i, delay: Mathf.Abs(i - (currentRating)) * stagger);
        }
    }

    private void PlayStarOn(int index, float delay)
    {
        Image img = starImages[index];
        RectTransform rt = img.rectTransform;

        KillStarTween(index);

        // 确保从“暗”切到“亮”时过程更自然：先把sprite换亮，但用轻微缩小+弹回来
        img.sprite = starOn;

        // alpha 先拉满
        var c = img.color;
        c.a = 1f;
        img.color = c;

        rt.localScale = Vector3.one * 0.92f;

        var seq = DOTween.Sequence();
        starSeqs[index] = seq;

        seq.SetDelay(delay);

        // 丝滑：小缩 -> 弹大 -> 回正 + punch
        seq.Append(rt.DOScale(onOvershoot, onDuration).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));

        // Q弹额外一口气（可选但很好看）
        seq.Join(rt.DOPunchScale(Vector3.one * punchScale, 0.18f, 6, 0.85f));

        seq.SetUpdate(true); // 如果你UI有 TimeScale=0 的情况，这行能让动画照常跑
    }

    private void PlayStarOff(int index, float delay)
    {
        Image img = starImages[index];
        RectTransform rt = img.rectTransform;

        KillStarTween(index);

        var seq = DOTween.Sequence();
        starSeqs[index] = seq;

        seq.SetDelay(delay);

        // 先轻微缩小+淡一点，再切暗星，再回到1（看起来更顺）
        seq.Append(rt.DOScale(offShrink, offDuration).SetEase(Ease.InOutQuad));
        seq.Join(img.DOFade(dimAlpha, offDuration).SetEase(Ease.InOutQuad));

        seq.AppendCallback(() =>
        {
            img.sprite = starOff;
        });

        seq.Append(rt.DOScale(1f, 0.10f).SetEase(Ease.OutQuad));

        seq.SetUpdate(true);
    }

    private void KillStarTween(int index)
    {
        if (starSeqs == null) return;
        if (index < 0 || index >= starSeqs.Length) return;

        if (starSeqs[index] != null && starSeqs[index].IsActive())
        {
            starSeqs[index].Kill(false);
            starSeqs[index] = null;
        }

        // 保险：如果别人也对这个UI做Tween，KillTarget避免叠加
        starImages[index].rectTransform.DOKill(false);
        starImages[index].DOKill(false);
    }

    private void KillAllStarTweens()
    {
        if (starSeqs == null) return;
        for (int i = 0; i < starSeqs.Length; i++)
        {
            KillStarTween(i);
        }
    }

    private void OnClickGoToRate()
    {
        CloseSelf();
        
        
    }

    private IEnumerator OpenGoogleInAppReview()
    {
#if UNITY_ANDROID && !UNITY_EDITOR && BIZZA_ENABLE_MAX
        var request = reviewManager.RequestReviewFlow();
        yield return request;

        if (request.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning("RequestReviewFlow error: " + request.Error);
            yield break;
        }

        reviewInfo = request.GetResult();

        var launch = reviewManager.LaunchReviewFlow(reviewInfo);
        yield return launch;

        reviewInfo = null;

        if (launch.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning("LaunchReviewFlow error: " + launch.Error);
        }
#endif
        yield break;
    }
}
#endif
