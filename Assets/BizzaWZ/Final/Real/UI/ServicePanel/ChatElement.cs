#if BIZZA_REAL_WITHDRAW
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatElement : MonoBehaviour
{
    public RectTransform root;
    public RectTransform chatInfoRoot;
    public RectTransform chatTxtRect;

    public TMP_Text chatTxt;
    public TMP_Text timeTxt;

    public ChatInfo chatInfo;

    [SerializeField] private Image bgImage;
    [SerializeField] private VerticalLayoutGroup rootLayoutGroup;
    [SerializeField] private Image IssueImage;
    [SerializeField] private Image PlayerImage;

    [Header("Layout")]
    [SerializeField] private float maxBubbleWidth = 760f;
    [SerializeField] private float outerHorizontalPadding = 24f;
    [SerializeField] private float bubbleHorizontalPadding = 32f;
    [SerializeField] private float bubbleVerticalPadding = 20f;
    [SerializeField] private float bubbleTimeSpacing = 12f;

    [Header("Style")]
    [SerializeField] private Color issueBubbleColor = new Color32(232, 70, 255, 255);
    [SerializeField] private Color playerBubbleColor = new Color32(248, 243, 224, 255);
    [SerializeField] private Color issueTextColor = Color.white;
    [SerializeField] private Color playerTextColor = new Color32(55, 42, 18, 255);
    [SerializeField] private Color timeTextColor = Color.white;

    public void Init(ChatInfo chatInfo)
    {
        this.chatInfo = chatInfo;

        CacheRefs();

        bool hasContent = !string.IsNullOrWhiteSpace(chatInfo.chatcontent);
        gameObject.SetActive(hasContent);
        if (!hasContent)
        {
            return;
        }

        chatTxt.text = chatInfo.chatcontent;
        timeTxt.text = chatInfo.time ?? string.Empty;

        ApplyStyle();
        RefreshLayout();
    }

    private void Awake()
    {
        CacheRefs();
    }

    private void OnValidate()
    {
        CacheRefs();
    }

    private void CacheRefs()
    {
        if (root == null)
        {
            root = transform as RectTransform;
        }

        if (chatTxtRect == null && chatTxt != null)
        {
            chatTxtRect = chatTxt.rectTransform;
        }

        if (rootLayoutGroup == null && root != null)
        {
            rootLayoutGroup = root.GetComponent<VerticalLayoutGroup>();
        }

        if (bgImage == null && chatInfoRoot != null)
        {
            bgImage = chatInfoRoot.GetComponentInChildren<Image>(true);
        }
    }

    private void ApplyStyle()
    {
        bool isIssue = chatInfo.spokesperson == Spokesperson.Issue;

        if (rootLayoutGroup != null)
        {
            rootLayoutGroup.childAlignment = isIssue ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            rootLayoutGroup.childControlWidth = false;
            rootLayoutGroup.childControlHeight = false;
            rootLayoutGroup.childForceExpandWidth = false;
            rootLayoutGroup.childForceExpandHeight = false;
            rootLayoutGroup.spacing = bubbleTimeSpacing;

            RectOffset padding = rootLayoutGroup.padding ?? new RectOffset();
            int sidePadding = Mathf.RoundToInt(outerHorizontalPadding);
            padding.left = sidePadding;
            padding.right = sidePadding;
            padding.top = 0;
            padding.bottom = 0;
            rootLayoutGroup.padding = padding;
        }

        if (IssueImage != null)
        {
            IssueImage.gameObject.SetActive(isIssue);
            IssueImage.type = Image.Type.Sliced;
            IssueImage.color = Color.white;
        }

        if (PlayerImage != null)
        {
            PlayerImage.gameObject.SetActive(!isIssue);
            PlayerImage.type = Image.Type.Sliced;
            PlayerImage.color = Color.white;
        }

        if (IssueImage == null && PlayerImage == null && bgImage != null)
        {
            bgImage.type = Image.Type.Sliced;
            bgImage.color = Color.white;
        }

        if (chatTxt != null)
        {
            chatTxt.enableWordWrapping = true;
            chatTxt.overflowMode = TextOverflowModes.Overflow;
            chatTxt.alignment = TextAlignmentOptions.TopLeft;
            chatTxt.color = isIssue ? issueTextColor : playerTextColor;
        }

        if (timeTxt != null)
        {
            timeTxt.enableWordWrapping = false;
            timeTxt.overflowMode = TextOverflowModes.Overflow;
            timeTxt.alignment = isIssue ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
            timeTxt.color = timeTextColor;
        }
    }

    private void RefreshLayout()
    {
        if (root == null || chatInfoRoot == null || chatTxtRect == null || chatTxt == null || timeTxt == null)
        {
            return;
        }

        float rootWidth = Mathf.Max(1f, GetRootWidth());
        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rootWidth);

        float horizontalPadding = bubbleHorizontalPadding * 2f;
        float verticalPadding = bubbleVerticalPadding * 2f;
        float availableBubbleWidth = Mathf.Max(1f, rootWidth - outerHorizontalPadding * 2f);
        float effectiveMaxBubbleWidth = maxBubbleWidth > 0f
            ? Mathf.Min(maxBubbleWidth, availableBubbleWidth)
            : availableBubbleWidth;
        float maxTextWidth = Mathf.Max(1f, effectiveMaxBubbleWidth - horizontalPadding);

        Vector2 preferredTextSize = chatTxt.GetPreferredValues(chatTxt.text, maxTextWidth, 0f);
        float bubbleWidth = Mathf.Min(effectiveMaxBubbleWidth, preferredTextSize.x + horizontalPadding);
        float finalTextWidth = Mathf.Max(1f, bubbleWidth - horizontalPadding);
        float finalTextHeight = chatTxt.GetPreferredValues(chatTxt.text, finalTextWidth, 0f).y;
        float bubbleHeight = finalTextHeight + verticalPadding;

        chatInfoRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bubbleWidth);
        chatInfoRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bubbleHeight);

        chatTxtRect.anchorMin = Vector2.zero;
        chatTxtRect.anchorMax = Vector2.one;
        chatTxtRect.offsetMin = new Vector2(bubbleHorizontalPadding, bubbleVerticalPadding);
        chatTxtRect.offsetMax = new Vector2(-bubbleHorizontalPadding, -bubbleVerticalPadding);

        bool hasTime = !string.IsNullOrWhiteSpace(timeTxt.text);
        timeTxt.gameObject.SetActive(hasTime);

        float timeHeight = 0f;
        float spacing = 0f;
        if (hasTime)
        {
            Vector2 preferredTimeSize = timeTxt.GetPreferredValues(timeTxt.text);
            float timeWidth = Mathf.Min(availableBubbleWidth, Mathf.Max(bubbleWidth, preferredTimeSize.x));
            timeHeight = preferredTimeSize.y;
            spacing = bubbleTimeSpacing;

            RectTransform timeRect = timeTxt.rectTransform;
            timeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, timeWidth);
            timeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, timeHeight);
        }

        root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bubbleHeight + spacing + timeHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatInfoRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        if (root.parent is RectTransform parentRect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }
    }

    private float GetRootWidth()
    {
        if (root != null && root.parent is RectTransform parentRect && parentRect.rect.width > 0f)
        {
            return parentRect.rect.width;
        }

        if (root != null && root.rect.width > 0f)
        {
            return root.rect.width;
        }

        float fallbackWidth = maxBubbleWidth > 0f
            ? maxBubbleWidth + outerHorizontalPadding * 2f
            : outerHorizontalPadding * 2f;

        return Mathf.Max(1f, fallbackWidth);
    }
}
#endif