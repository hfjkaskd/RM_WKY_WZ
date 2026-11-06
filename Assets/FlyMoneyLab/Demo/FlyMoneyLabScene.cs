using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bizza.FlyMoney.Demo
{
    public sealed class FlyMoneyLabScene : MonoBehaviour
    {
        [Header("Existing project sprites")]
        public Sprite paperBR;
        public Sprite bundleBR;
        public Sprite paperID;
        public Sprite bundleID;
        public Sprite paperUS;
        public Sprite bundleUS;
        [Header("Optional custom sprites")]
        public Sprite customPaper;
        public Sprite customBundle;
        public Color customTrailColor = new Color(1f, 0.7f, 0.15f, 0.42f);
        [Header("Opening sound")]
        public AudioClip burstSound;
        public bool soundEnabled = true;
        [Range(0f, 1f)] public float soundVolume = 0.65f;
        [Header("Test settings (no real currency or ads)")]
        public FlyMoneySettings settings = FlyMoneySettings.Standard;
        public bool autoLoop;
        public bool fixedSeed = true;
        public FlyMoneyPlayer Player { get; private set; }
        public RectTransform Stage { get; private set; }
        public RectTransform Target { get; private set; }
        public RectTransform Origin { get; private set; }

        private Canvas canvas;
        private CanvasScaler scaler;
        private RectTransform panel;
        private RectTransform safeRoot;
        private RectTransform playButton;
        private RectTransform gmButton;
        private RectTransform title;
        private Text statistics;
        private Text resultText;
        private Text balance;
        private Text pauseText;
        private Text timelineReadout;
        private Image originImage;
        private Image targetImage;
        private readonly Image[] themeButtons = new Image[4];
        private readonly Image[] qualityButtons = new Image[2];
        private readonly Image[] targetButtons = new Image[3];
        private Slider rainSlider;
        private Slider flySlider;
        private Slider durationSlider;
        private Slider sizeSlider;
        private Slider scatterSlider;
        private Slider fallSpeedSlider;
        private Slider timeline;
        private Toggle trailToggle;
        private Toggle loopToggle;
        private Font font;
        private bool ownsFont;
        private int theme;
        private int completed;
        private int previousWidth;
        private int previousHeight;
        private Rect previousSafeArea;
        private Vector2 previousLayoutSize;
        private float nextPlay;
        private float nextStats;
        private float smoothedFrameTime;
        private bool panelOpen = true;
        private uint seed = 1;
        private System.Action<FlyMoneyEndReason> completionHandler;

        private static readonly Color Ink = new Color(0.13f, 0.16f, 0.15f);
        private static readonly Color Muted = new Color(0.42f, 0.46f, 0.44f);
        private static readonly Color Green = new Color(0.13f, 0.53f, 0.36f);
        private static readonly Color Surface = new Color(0.96f, 0.97f, 0.96f);

        private void Awake()
        {
            if (Player != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Noto Sans CJK SC", "Droid Sans Fallback", "Arial" }, 24);
            ownsFont = font != null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            completionHandler = OnFinished;
            GameObject ui = new GameObject("Fly Money Lab UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui.transform.SetParent(transform, false);
            canvas = ui.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            Image background = MakeImage((RectTransform)ui.transform, "Background", new Color(0.12f, 0.15f, 0.14f));
            Stretch(background.rectTransform);
            safeRoot = MakeRect((RectTransform)ui.transform, "Safe Area");
            Stretch(safeRoot);
            title = MakeText(safeRoot, "\u98de\u94b1\u52a8\u753b\u6d4b\u8bd5", 23, Color.white).rectTransform;
            Stage = MakeRect(safeRoot, "Animation Stage");
            Stage.sizeDelta = new Vector2(606f, 1080f);
            Image stageBackground = MakeImage(Stage, "Stage Background", Surface);
            Stretch(stageBackground.rectTransform);
            for (int x = -250; x <= 250; x += 50)
                Box(Stage, "Grid", new Vector2(x, -20), new Vector2(1f, 820f), new Color(0.87f, 0.90f, 0.88f));
            for (int y = -420; y <= 380; y += 50)
                Box(Stage, "Grid", new Vector2(0, y), new Vector2(500f, 1f), new Color(0.87f, 0.90f, 0.88f));
            Image counter = Box(Stage, "Counter", new Vector2(-140f, 455f), new Vector2(210f, 76f), new Color(0.83f, 0.93f, 0.85f));
            targetImage = MakeImage(counter.rectTransform, "Target", Color.white, bundleBR);
            Place(targetImage.rectTransform, new Vector2(-65, 0), new Vector2(58, 58));
            Target = targetImage.rectTransform;
            balance = MakeText(counter.rectTransform, "100", 27, Ink);
            Place(balance.rectTransform, new Vector2(34, 0), new Vector2(108, 60));
            balance.alignment = TextAnchor.MiddleCenter;
            Origin = MakeRect(Stage, "Origin");
            Place(Origin, new Vector2(0, -75), new Vector2(80, 80));
            originImage = MakeImage(Origin, "Origin Icon", new Color(1f, 1f, 1f, 0.22f), bundleBR);
            Stretch(originImage.rectTransform);
            Text sourceLabel = MakeText(Origin, "\u8d77\u70b9", 22, Muted);
            Place(sourceLabel.rectTransform, new Vector2(0, -65), new Vector2(160, 35));
            sourceLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform fx = MakeRect(Stage, "Fly Money Canvas");
            Stretch(fx);
            Player = fx.gameObject.AddComponent<FlyMoneyPlayer>();
            Player.SoundEnabled = soundEnabled;
            Player.SoundVolume = soundVolume;
            Player.PrepareSound(burstSound);
            Player.PrepareSprite(paperBR); Player.PrepareSprite(bundleBR);
            Player.PrepareSprite(paperID); Player.PrepareSprite(bundleID);
            Player.PrepareSprite(paperUS); Player.PrepareSprite(bundleUS);
            if (customPaper != null) Player.PrepareSprite(customPaper);
            if (customBundle != null) Player.PrepareSprite(customBundle);
            BuildPanel();
            playButton = MakeButton(safeRoot, "\u64ad\u653e", Play, Green).GetComponent<RectTransform>();
            gmButton = MakeButton(safeRoot, "GM", TogglePanel, new Color(0.33f, 0.37f, 0.35f)).GetComponent<RectTransform>();
            if (EventSystem.current == null)
            {
                GameObject events = new GameObject("Lab EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }
            if (Camera.main == null)
            {
                GameObject cameraObject = new GameObject("Lab Camera", typeof(Camera));
                cameraObject.transform.SetParent(transform, false);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.15f, 0.14f);
                camera.cullingMask = 0;
                cameraObject.tag = "MainCamera";
            }
            if (FindObjectOfType<AudioListener>() == null)
            {
                GameObject listener = new GameObject("Lab Audio Listener", typeof(AudioListener));
                listener.transform.SetParent(transform, false);
            }
            ApplyTheme(0);
            SetTarget(0);
            settings = settings.Sanitized();
            SyncControls();
            Highlight(qualityButtons, 0);
            panelOpen = Screen.width > Screen.height;
            Relayout();
        }

        private void BuildPanel()
        {
            panel = MakeImage(safeRoot, "GM Panel", Color.white).rectTransform;
            RectTransform viewport = MakeImage(panel, "Viewport", Color.white).rectTransform;
            viewport.GetComponent<Image>().raycastTarget = true;
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = MakeRect(viewport, "Controls");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(-32, 1108);
            ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            Text heading = MakeText(content, "GM", 24, Ink);
            RowRect(heading.rectTransform, 16, 34);
            statistics = MakeText(content, "", 17, Muted);
            RowRect(statistics.rectTransform, 52, 52);
            for (int i = 0; i < 4; i++)
            {
                int selection = i;
                Button button = MakeButton(content, new[] { "BR", "ID", "US", "\u81ea\u5b9a\u4e49" }[i], () => ApplyTheme(selection), Surface);
                themeButtons[i] = button.image;
                Segment(button.GetComponent<RectTransform>(), i, 4, 111, 40);
            }
            for (int i = 0; i < 2; i++)
            {
                bool low = i == 1;
                Button button = MakeButton(content, low ? "\u4f4e\u914d" : "\u6807\u51c6", () => ApplyQuality(low), Surface);
                qualityButtons[i] = button.image;
                Segment(button.GetComponent<RectTransform>(), i, 2, 161, 40);
            }
            rainSlider = MakeSlider(content, "\u98d8\u843d\u6570\u91cf", 214, 0, 64, 32, true, value => settings.rainCount = (int)value, out _);
            flySlider = MakeSlider(content, "\u98de\u884c\u6570\u91cf", 278, 0, 64, 28, true, value => settings.flyCount = (int)value, out _);
            durationSlider = MakeSlider(content, "\u603b\u65f6\u957f (s)", 342, 0.5f, 5f, 1.7f, false, value => settings.duration = value, out _);
            scatterSlider = MakeSlider(content, "\u6563\u5f00\u534a\u5f84", 406, 20f, 220f, 108f, true, value => settings.scatterRadius = value, out _);
            sizeSlider = MakeSlider(content, "\u98de\u884c\u56fe\u7247\u5927\u5c0f", 470, 10f, FlyMoneySettings.MaxImageSize, 34f, true, value => settings.flySize = value, out _);
            fallSpeedSlider = MakeSlider(content, "\u4e0b\u843d\u901f\u5ea6", 534, 0f, 120f, 48f, true, value => settings.fallSpeed = value, out _);
            trailToggle = MakeToggle(content, "\u957f\u62d6\u5c3e", 600, true, value => settings.trails = value);
            loopToggle = MakeToggle(content, "\u5faa\u73af\u64ad\u653e", 644, autoLoop, value => { autoLoop = value; nextPlay = 0f; });
            MakeToggle(content, "\u5f00\u573a\u97f3\u6548", 688, soundEnabled, value =>
            {
                soundEnabled = value;
                Player.SoundEnabled = value;
            });
            for (int i = 0; i < 3; i++)
            {
                int destination = i;
                Button button = MakeButton(content, new[] { "\u5de6\u4e0a", "\u4e0a\u65b9", "\u53f3\u4e0a" }[i], () => SetTarget(destination), Surface);
                targetButtons[i] = button.image;
                Segment(button.GetComponent<RectTransform>(), i, 3, 740, 40);
            }
            timeline = MakeSlider(content, "\u9010\u5e27\u8fdb\u5ea6", 792, 0f, 1f, 0f, false, value =>
            {
                if (Player.ActiveCount == 0) Play();
                Player.SeekNormalized(value);
                pauseText.text = "\u7ee7\u7eed";
            }, out timelineReadout);
            Button pause = MakeButton(content, "\u6682\u505c", TogglePause, Surface);
            pauseText = pause.GetComponentInChildren<Text>();
            Segment(pause.GetComponent<RectTransform>(), 0, 2, 862, 42);
            Button stop = MakeButton(content, "\u6e05\u7a7a", Clear, Surface);
            Segment(stop.GetComponent<RectTransform>(), 1, 2, 862, 42);
            Button burst = MakeButton(content, "\u7a81\u53d1 10 \u6b21", Burst, Surface);
            Segment(burst.GetComponent<RectTransform>(), 0, 2, 915, 42);
            Button lost = MakeButton(content, "\u76ee\u6807\u9500\u6bc1\u6d4b\u8bd5", LoseTarget, Surface);
            Segment(lost.GetComponent<RectTransform>(), 1, 2, 915, 42);
            Button reset = MakeButton(content, "\u91cd\u7f6e\u53c2\u6570", () => { Clear(); ApplyQuality(false); SetTarget(0); }, Surface);
            RowRect(reset.GetComponent<RectTransform>(), 969, 42);
            resultText = MakeText(content, "\u5c31\u7eea", 17, Muted);
            RowRect(resultText.rectTransform, 1023, 64);
        }

        public void Play()
        {
            Sprite paper = theme == 0 ? paperBR : theme == 1 ? paperID : theme == 2 ? paperUS : customPaper;
            Sprite bundle = theme == 0 ? bundleBR : theme == 1 ? bundleID : theme == 2 ? bundleUS : customBundle;
            Player.Paused = false;
            pauseText.text = "\u6682\u505c";
            if (Player.TryPlayFromTransforms(paper, bundle, Origin, Target, settings, fixedSeed ? 37u : ++seed, completionHandler))
                resultText.text = "\u64ad\u653e\u4e2d";
            else resultText.text = "\u672a\u64ad\u653e\uff1a\u5df2\u8fbe\u4e0a\u9650\u6216\u56fe\u7247\u672a\u8bbe\u7f6e";
            nextPlay = Time.unscaledTime + settings.Sanitized().duration + 0.35f;
        }

        private void OnFinished(FlyMoneyEndReason reason)
        {
            if (this == null || balance == null || resultText == null) return;
            if (reason == FlyMoneyEndReason.Completed)
            {
                completed++;
                balance.text = (100 + completed).ToString();
                resultText.text = "\u64ad\u653e\u5b8c\u6210";
            }
            else resultText.text = reason == FlyMoneyEndReason.TargetLost ? "\u76ee\u6807\u5df2\u9500\u6bc1\uff0c\u52a8\u753b\u5df2\u6e05\u7406" :
                reason == FlyMoneyEndReason.Disabled ? "\u7ec4\u4ef6\u5df2\u7981\u7528\uff0c\u52a8\u753b\u5df2\u6e05\u7406" : "\u5df2\u6e05\u7a7a";
        }

        public void ApplyTheme(int value)
        {
            theme = Mathf.Clamp(value, 0, 3);
            Sprite bundle = theme == 0 ? bundleBR : theme == 1 ? bundleID : theme == 2 ? bundleUS : customBundle;
            targetImage.sprite = bundle;
            originImage.sprite = bundle;
            settings.trailColor = theme == 0 ? new Color(0.94f, 0.22f, 0.69f, 0.42f) :
                theme == 1 ? new Color(0.16f, 0.68f, 0.91f, 0.42f) :
                theme == 2 ? new Color(0.31f, 0.78f, 0.10f, 0.42f) : customTrailColor;
            Highlight(themeButtons, theme);
        }

        public void ApplyQuality(bool low)
        {
            Color color = settings.trailColor;
            settings = low ? FlyMoneySettings.Low : FlyMoneySettings.Standard;
            settings.trailColor = color;
            if (low) settings.trailColor.a = Mathf.Min(settings.trailColor.a, 0.36f);
            Player.concurrentLimit = FlyMoneyPlayer.HardConcurrencyLimit;
            SyncControls();
            Highlight(qualityButtons, low ? 1 : 0);
        }

        private void SyncControls()
        {
            rainSlider.value = settings.rainCount; flySlider.value = settings.flyCount;
            durationSlider.value = settings.duration; scatterSlider.value = settings.scatterRadius;
            sizeSlider.value = settings.flySize; trailToggle.isOn = settings.trails;
            fallSpeedSlider.value = settings.fallSpeed;
        }

        public void SetTarget(int value)
        {
            if (Target == null) return;
            Player.StopAll();
            Target.parent.localPosition = new Vector3(Mathf.Clamp(value, 0, 2) * 140f - 140f, 455f, 0f);
            Highlight(targetButtons, value);
        }

        public void SetPanelVisible(bool visible)
        {
            panelOpen = visible;
            panel.gameObject.SetActive(visible);
        }

        private void TogglePanel() => SetPanelVisible(!panelOpen);
        private void TogglePause()
        {
            Player.Paused = !Player.Paused;
            pauseText.text = Player.Paused ? "\u7ee7\u7eed" : "\u6682\u505c";
        }
        private void Clear()
        {
            autoLoop = false; loopToggle.SetIsOnWithoutNotify(false);
            Player.StopAll(); Player.Paused = false;
            pauseText.text = "\u6682\u505c";
            timeline.SetValueWithoutNotify(0f);
            timelineReadout.text = "0.00";
        }
        private void Burst() { for (int i = 0; i < 10; i++) Play(); }
        private void LoseTarget()
        {
            Clear();
            RectTransform temporary = MakeRect(Stage, "Disposable Target");
            temporary.position = Target.position;
            Sprite paper = theme == 0 ? paperBR : theme == 1 ? paperID : theme == 2 ? paperUS : customPaper;
            Sprite bundle = theme == 0 ? bundleBR : theme == 1 ? bundleID : theme == 2 ? bundleUS : customBundle;
            Player.TryPlayFromTransforms(paper, bundle, Origin, temporary, settings, 37, completionHandler);
            Destroy(temporary.gameObject, 0.55f);
        }

        private void Update()
        {
            if (Player == null) return;
            if (Screen.width != previousWidth || Screen.height != previousHeight || Screen.safeArea != previousSafeArea ||
                safeRoot.rect.size != previousLayoutSize) Relayout();
            if (autoLoop && !Player.Paused && !Player.ApplicationPaused && Time.unscaledTime >= nextPlay) Play();
            if (!panelOpen) return;
            smoothedFrameTime = Mathf.Lerp(smoothedFrameTime, Time.unscaledDeltaTime, 0.08f);
            if (Time.unscaledTime < nextStats) return;
            nextStats = Time.unscaledTime + 0.5f;
            statistics.text = "FPS " + (1f / Mathf.Max(0.001f, smoothedFrameTime)).ToString("0") +
                "   \u6d3b\u8dc3 " + Player.ActiveCount + "   \u7c92\u5b50 " + Player.VisibleParticles +
                "\n\u9876\u70b9 " + Player.VertexCount + "   \u63a5\u53d7 " + Player.AcceptedCount + "   \u62d2\u7edd " + Player.RejectedCount;
            timeline.SetValueWithoutNotify(Player.LatestProgress);
            timelineReadout.text = Player.LatestProgress.ToString("0.00");
        }

        private void Relayout()
        {
            previousWidth = Screen.width; previousHeight = Screen.height; previousSafeArea = Screen.safeArea;
            bool landscape = Screen.width > Screen.height;
            scaler.referenceResolution = landscape ? new Vector2(1280, 800) : new Vector2(606, 1080);
            scaler.matchWidthOrHeight = landscape ? 1f : 0f;
            Rect safe = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(safe.xMin / Mathf.Max(1, Screen.width), safe.yMin / Mathf.Max(1, Screen.height));
            safeRoot.anchorMax = new Vector2(safe.xMax / Mathf.Max(1, Screen.width), safe.yMax / Mathf.Max(1, Screen.height));
            Canvas.ForceUpdateCanvases();
            Vector2 area = safeRoot.rect.size;
            previousLayoutSize = area;
            if (area.x < 1f || area.y < 1f) return;
            float stageArea = landscape ? Mathf.Max(200, area.x - 378f) : area.x;
            float scale = Mathf.Min((stageArea - 28f) / 606f, (area.y - 100f) / 1080f);
            Stage.anchorMin = Stage.anchorMax = new Vector2(0f, 0.5f);
            Stage.anchoredPosition = new Vector2(stageArea * 0.5f, -3f);
            Stage.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            title.anchorMin = title.anchorMax = new Vector2(0, 1);
            title.pivot = new Vector2(0, 1); title.anchoredPosition = new Vector2(20, -10); title.sizeDelta = new Vector2(320, 36);
            panel.anchorMin = panel.anchorMax = landscape ? new Vector2(1, 0.5f) : new Vector2(0.5f, 0.5f);
            panel.pivot = landscape ? new Vector2(1, 0.5f) : new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = landscape ? new Vector2(-16, -8) : new Vector2(0, -4);
            panel.sizeDelta = new Vector2(landscape ? 346f : Mathf.Min(area.x - 28, 540), area.y - 112f);
            playButton.anchorMin = playButton.anchorMax = new Vector2(0.5f, 0);
            playButton.anchoredPosition = new Vector2(-58, 27); playButton.sizeDelta = new Vector2(104, 42);
            gmButton.anchorMin = gmButton.anchorMax = new Vector2(0.5f, 0);
            gmButton.anchoredPosition = new Vector2(58, 27); gmButton.sizeDelta = new Vector2(104, 42);
            panel.gameObject.SetActive(panelOpen);
        }

        private static void Highlight(Image[] buttons, int selected)
        {
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null) buttons[i].color = i == selected ? new Color(0.75f, 0.91f, 0.82f) : Surface;
        }
        private RectTransform MakeRect(RectTransform parent, string name)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); return rect;
        }
        private Image MakeImage(RectTransform parent, string name, Color color, Sprite sprite = null)
        {
            Image image = MakeRect(parent, name).gameObject.AddComponent<Image>();
            image.color = color; image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
        private Image Box(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            Image image = MakeImage(parent, name, color); Place(image.rectTransform, position, size); return image;
        }
        private Text MakeText(RectTransform parent, string text, int size, Color color)
        {
            Text label = MakeRect(parent, "Label").gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.raycastTarget = false; label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        private Button MakeButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action, Color color)
        {
            Image image = MakeImage(parent, label, color); image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text text = MakeText(image.rectTransform, label, 19, color.r < 0.4f ? Color.white : Ink);
            Stretch(text.rectTransform); text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        private Slider MakeSlider(RectTransform parent, string label, float top, float min, float max, float initial,
            bool whole, UnityEngine.Events.UnityAction<float> action, out Text readout)
        {
            RectTransform row = MakeRect(parent, label); RowRect(row, top, 58);
            Text text = MakeText(row, label, 18, Ink); RowRect(text.rectTransform, 0, 26);
            Text number = MakeText(row, "", 18, Muted); RowRect(number.rectTransform, 0, 26); number.alignment = TextAnchor.MiddleRight;
            readout = number;
            Image hit = MakeImage(row, "Slider", new Color(1, 1, 1, 0)); hit.raycastTarget = true; RowRect(hit.rectTransform, 28, 28);
            Image track = MakeImage(hit.rectTransform, "Track", new Color(0.87f, 0.90f, 0.88f)); Stretch(track.rectTransform);
            track.rectTransform.offsetMin = new Vector2(8, 11); track.rectTransform.offsetMax = new Vector2(-8, -11);
            RectTransform handleArea = MakeRect(hit.rectTransform, "Handle Area"); Stretch(handleArea);
            handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
            Image handle = MakeImage(handleArea, "Handle", Green); handle.rectTransform.sizeDelta = new Vector2(18, 0);
            Slider slider = hit.gameObject.AddComponent<Slider>(); slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle; slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = whole;
            slider.SetValueWithoutNotify(initial);
            number.text = initial.ToString(whole ? "0" : "0.00");
            slider.onValueChanged.AddListener(value => { number.text = value.ToString(whole ? "0" : "0.00"); action(value); });
            return slider;
        }
        private Toggle MakeToggle(RectTransform parent, string label, float top, bool value, UnityEngine.Events.UnityAction<bool> action)
        {
            RectTransform row = MakeRect(parent, label); RowRect(row, top, 34);
            Image rowHit = row.gameObject.AddComponent<Image>(); rowHit.color = Color.clear; rowHit.raycastTarget = true;
            Image box = MakeImage(row, "Checkbox", new Color(0.86f, 0.90f, 0.87f));
            box.raycastTarget = true; box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0, 0.5f);
            box.rectTransform.anchoredPosition = new Vector2(15, 0); box.rectTransform.sizeDelta = new Vector2(28, 28);
            Image check = MakeImage(box.rectTransform, "Check", Green); check.rectTransform.sizeDelta = new Vector2(16, 16);
            Text text = MakeText(row, label, 18, Ink); Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(42, 0);
            Toggle toggle = row.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = box; toggle.graphic = check;
            toggle.SetIsOnWithoutNotify(value); toggle.onValueChanged.AddListener(action); return toggle;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private static void RowRect(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(0, height);
        }
        private static void Segment(RectTransform rect, int index, int count, float top, float height)
        {
            rect.anchorMin = new Vector2(index / (float)count, 1); rect.anchorMax = new Vector2((index + 1f) / count, 1);
            rect.pivot = new Vector2(0.5f, 1); rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(-6, height);
        }

        private void OnDestroy()
        {
            if (Player != null) Player.StopAll();
            if (ownsFont && font != null) Destroy(font);
        }
    }
}
