using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId WhiteWinPanel = "WhiteWinPanel";
}

namespace SnakeEscape.Recovered
{
    [DisallowMultipleComponent]
    public sealed class RecoveredVictoryPanel : UIPageBase<Action>
    {
        private const string DefaultSkinResource = "Recovered/UI/Skins/PsdVictoryPanelSkin";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform hero;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button nextButton;
        [SerializeField] private RecoveredVictoryPanelSkin skin;

        private Action nextAction;
        private Graphic maskGraphic;
        private RawImage heroImage;
        private RawImage panelImage;
        private RawImage nextButtonImage;
        private TMP_Text nextButtonText;
        private Texture2D heroTexture;
        private Texture2D panelTexture;
        private Texture2D nextButtonTexture;

        public override bool PreserveRectTransformOnOpen => true;

        public void Show(string title, string body, Action onNext)
        {
            BindPrefabReferences();
            nextAction = onNext;
            if (titleText != null)
            {
                titleText.text = string.IsNullOrEmpty(title) ? "Level Complete" : title;
            }

            if (bodyText != null)
            {
                bodyText.text = string.IsNullOrEmpty(body) ? "All dragons escaped" : body;
            }

            if (nextButtonText != null)
            {
                nextButtonText.text = "Next Level";
            }

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (card != null)
            {
                card.localScale = Vector3.one;
            }
        }

        protected override void OnOpen(Action onNext)
        {
            Show("Level Complete", "All dragons escaped", onNext);
        }

        protected override void OnClose()
        {
            nextAction = null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void Hide()
        {
            nextAction = null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        protected override void OnAwake()
        {
            BindPrefabReferences();
        }

        private void BindPrefabReferences()
        {
            canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            hero = hero != null ? hero : FindChildComponent<RectTransform>("VictoryHero");
            card = card != null ? card : FindChildComponent<RectTransform>("Card");
            titleText = titleText != null ? titleText : FindChildComponent<TMP_Text>("Title");
            bodyText = bodyText != null ? bodyText : FindChildComponent<TMP_Text>("Body");
            nextButton = nextButton != null ? nextButton : FindChildComponent<Button>("NextButton");
            nextButtonText = nextButtonText != null ? nextButtonText : FindChildComponent<TMP_Text>("Label");
            maskGraphic = maskGraphic != null ? maskGraphic : FindChildComponent<Graphic>("Mask");
            if (skin == null)
            {
                skin = Resources.Load<RecoveredVictoryPanelSkin>(DefaultSkinResource);
            }

            EnsureSkinTextures();
            EnsurePsdVisualComponents();

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextClicked);
            }
        }

        private void EnsureSkinTextures()
        {
            if (skin != null)
            {
                heroTexture = heroTexture != null ? heroTexture : skin.LoadHero();
                panelTexture = panelTexture != null ? panelTexture : skin.LoadPanel();
                nextButtonTexture = nextButtonTexture != null ? nextButtonTexture : skin.LoadNextButton();
            }

            heroTexture = heroTexture != null ? heroTexture : Resources.Load<Texture2D>("Recovered/UI/PsdVictoryPanel/hero");
            panelTexture = panelTexture != null ? panelTexture : Resources.Load<Texture2D>("Recovered/UI/PsdVictoryPanel/panel");
            nextButtonTexture = nextButtonTexture != null ? nextButtonTexture : Resources.Load<Texture2D>("Recovered/UI/PsdVictoryPanel/next_button");
        }

        private void EnsurePsdVisualComponents()
        {
            if (hero == null)
            {
                ReportMissingPrefabObject("VictoryHero");
            }

            if (hero != null)
            {
                heroImage = heroImage != null ? heroImage : GetPrefabRawImage(hero, "VictoryHero");
                if (heroImage != null)
                {
                    heroImage.raycastTarget = false;
                    heroImage.texture = heroTexture != null ? heroTexture : heroImage.texture;
                }
            }

            if (card != null)
            {
                panelImage = panelImage != null ? panelImage : GetPrefabRawImage(card, "Card");
                if (panelImage != null)
                {
                    panelImage.raycastTarget = false;
                    panelImage.texture = panelTexture != null ? panelTexture : panelImage.texture;
                }

                Image legacyCardImage = card.GetComponent<Image>();
                if (legacyCardImage != null)
                {
                    legacyCardImage.enabled = false;
                }

                Transform accent = FindChild(card, "Accent");
                if (accent != null)
                {
                    accent.gameObject.SetActive(false);
                }
            }

            if (nextButton != null)
            {
                RectTransform nextRect = nextButton.transform as RectTransform;
                if (nextRect != null)
                {
                    nextButtonImage = nextButtonImage != null ? nextButtonImage : GetPrefabRawImage(nextRect, "NextButton");
                    if (nextButtonImage != null)
                    {
                        nextButtonImage.raycastTarget = true;
                        nextButtonImage.texture = nextButtonTexture != null ? nextButtonTexture : nextButtonImage.texture;
                        nextButton.targetGraphic = nextButtonImage;
                    }
                }
            }

            if (maskGraphic != null)
            {
                maskGraphic.transform.SetAsFirstSibling();
            }

            if (hero != null)
            {
                hero.SetSiblingIndex(maskGraphic != null ? maskGraphic.transform.GetSiblingIndex() + 1 : 0);
            }

            if (card != null)
            {
                card.SetAsLastSibling();
            }
        }

        private static RawImage GetPrefabRawImage(RectTransform rectTransform, string objectName)
        {
            RawImage rawImage = rectTransform.GetComponent<RawImage>();
            if (rawImage == null)
            {
                Debug.LogError($"Recovered victory panel prefab object '{objectName}' is missing a RawImage component. Add it to the prefab instead of creating it at runtime.");
            }

            return rawImage;
        }

        private static void ReportMissingPrefabObject(string objectName)
        {
            Debug.LogError($"Recovered victory panel prefab is missing required UI object '{objectName}'. Add it to the prefab instead of creating it at runtime.");
        }

        private static void SetRawTexture(RawImage image, Texture texture)
        {
            if (image != null && texture != null)
            {
                image.texture = texture;
                image.color = Color.white;
            }
        }

        private T FindChildComponent<T>(string objectName) where T : Component
        {
            Transform found = FindChild(transform, objectName);
            return found != null ? found.GetComponent<T>() : null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void OnNextClicked()
        {
            Action callback = nextAction;
            var uiManager = UIManager.Instance;
            if (uiManager != null && uiManager.GetPage(UIPageIds.WhiteWinPanel) == this)
            {
                CloseSelf();
            }
            else
            {
                Hide();
            }

            callback?.Invoke();
            FlowModule.LoadGameLevel();
        }
    }
}
