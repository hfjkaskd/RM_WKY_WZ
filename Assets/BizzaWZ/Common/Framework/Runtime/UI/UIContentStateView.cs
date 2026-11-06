using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Serializable, optional local feedback for a page. Bind its status label and primary button
/// once in OnAwake, then call Show when the content session changes. Business actions stay in the page.
/// </summary>
[Serializable]
public sealed class UIContentStateView
{
    [SerializeField] private string loadingKey = "Tips_Loading";
    [SerializeField] private string failedKey = "UIContent_LoadFailed";
    [SerializeField] private string emptyKey = "UIContent_Empty";
    [SerializeField] private string waitKey = "UIContent_PleaseWait";
    [SerializeField] private string retryKey = "UIContent_Retry";
    [SerializeField] private Color spinnerColor = new Color(.11f, .64f, .86f, 1f);
    [SerializeField] private float spinnerSize = 32f;
    [SerializeField] private Vector2 spinnerPosition = new Vector2(0, 30);
    [SerializeField] private float loadingTopMargin = 46f;
    [SerializeField] private Image buttonBackground;
    [SerializeField] private Material disabledButtonMaterial;
    [SerializeField] private Sprite disabledButtonSprite;
    [SerializeField] private GameObject[] readyOnlyDecorations = Array.Empty<GameObject>();
    private TMP_Text statusLabel, buttonLabel;
    private BizzaButton button;
    private UILanguageLabel localizedButton;
    private string originalButtonKey, originalButtonText;
    private Vector2 originalButtonPosition, originalButtonSize;
    private bool originalButtonAutoSize;
    private bool originalButtonWrapping;
    private float originalButtonFontSize, originalButtonFontMin, originalButtonFontMax;
    private Vector4 originalMargin;
    private bool originalAutoSize;
    private float originalFontSize, originalFontMin, originalFontMax;
    private Material originalMaterial;
    private Sprite originalSprite;
    private Color originalBackgroundColor;
    private RectTransform spinner;
    private bool initialized;

    public void Bind(TMP_Text status, BizzaButton actionButton)
    {
        if (initialized) return;
        initialized = true;
        statusLabel = status;
        button = actionButton;
        buttonLabel = button.GetComponentInChildren<TMP_Text>(true);
        localizedButton = buttonLabel.GetComponent<UILanguageLabel>();
        originalButtonKey = localizedButton != null ? localizedButton.key : null;
        originalButtonText = buttonLabel.text;
        originalButtonPosition = buttonLabel.rectTransform.anchoredPosition;
        originalButtonSize = buttonLabel.rectTransform.sizeDelta;
        originalButtonAutoSize = buttonLabel.enableAutoSizing;
        originalButtonWrapping = buttonLabel.enableWordWrapping;
        originalButtonFontSize = buttonLabel.fontSize;
        originalButtonFontMin = buttonLabel.fontSizeMin;
        originalButtonFontMax = buttonLabel.fontSizeMax;
        originalMargin = status.margin;
        originalAutoSize = status.enableAutoSizing;
        originalFontSize = status.fontSize;
        originalFontMin = status.fontSizeMin;
        originalFontMax = status.fontSizeMax;
        if (buttonBackground != null)
        {
            originalMaterial = buttonBackground.material;
            originalSprite = buttonBackground.sprite;
            originalBackgroundColor = buttonBackground.color;
        }
        var obj = new GameObject("ContentLoadingSpinner", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIContentSpinner));
        obj.SetActive(false);
        obj.layer = status.gameObject.layer;
        spinner = (RectTransform)obj.transform;
        spinner.SetParent(status.transform, false);
        spinner.anchorMin = spinner.anchorMax = new Vector2(.5f, .5f);
        spinner.anchoredPosition = spinnerPosition;
        spinner.sizeDelta = Vector2.one * spinnerSize;
        var graphic = obj.GetComponent<UIContentSpinner>();
        graphic.color = spinnerColor;
        graphic.raycastTarget = false;
    }

    public void Show(UIContentState state)
    {
        if (!initialized) return;
        bool loading = state == UIContentState.Loading;
        bool ready = state == UIContentState.Ready;
        bool retry = state == UIContentState.Failed || state == UIContentState.Empty;
        spinner.gameObject.SetActive(loading);
        button.interactable = ready || retry;
        foreach (var decoration in readyOnlyDecorations)
            if (decoration != null) decoration.SetActive(ready);
        statusLabel.margin = loading ? originalMargin + new Vector4(0, loadingTopMargin, 0, 0) : originalMargin;
        statusLabel.enableAutoSizing = ready ? originalAutoSize : true;
        statusLabel.fontSizeMin = ready ? originalFontMin : Mathf.Min(24f, originalFontSize);
        statusLabel.fontSizeMax = ready ? originalFontMax : originalFontSize;
        statusLabel.fontSize = originalFontSize;
        if (buttonBackground != null)
        {
            buttonBackground.material = loading && disabledButtonMaterial != null ? disabledButtonMaterial : originalMaterial;
            buttonBackground.sprite = loading && disabledButtonSprite != null ? disabledButtonSprite : originalSprite;
            buttonBackground.color = loading && disabledButtonMaterial == null && disabledButtonSprite == null
                ? originalBackgroundColor * new Color(.55f, .55f, .55f, 1) : originalBackgroundColor;
        }
        string buttonKey = loading ? waitKey : retry ? retryKey : originalButtonKey;
        // Status labels can be longer than the original short action next to an ad icon.
        bool status = loading || retry;
        buttonLabel.rectTransform.anchoredPosition = status ? Vector2.zero : originalButtonPosition;
        buttonLabel.rectTransform.sizeDelta = status
            ? new Vector2(((RectTransform)button.transform).rect.width - 60f, originalButtonSize.y) : originalButtonSize;
        buttonLabel.enableAutoSizing = status || originalButtonAutoSize;
        buttonLabel.enableWordWrapping = !status && originalButtonWrapping;
        buttonLabel.fontSizeMin = status ? Mathf.Min(28f, originalButtonFontSize) : originalButtonFontMin;
        buttonLabel.fontSizeMax = status ? originalButtonFontSize : originalButtonFontMax;
        buttonLabel.fontSize = originalButtonFontSize;
        if (localizedButton != null) localizedButton.key = buttonKey;
        buttonLabel.text = string.IsNullOrEmpty(buttonKey) ? originalButtonText : LanguageUtils.GetText(buttonKey);
        if (loading) statusLabel.text = LanguageUtils.GetText(loadingKey);
        else if (retry) statusLabel.text = LanguageUtils.GetText(state == UIContentState.Empty ? emptyKey : failedKey);
        else if (state == UIContentState.Closed) statusLabel.text = string.Empty;
    }
}
