#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Whitebox.Foundation;

// Run in the isolated validation project. This creates only temporary preview
// scenes and renders the actual prefab fonts; it never starts the game or SDK.
public static class LocalizationValidation
{
    [Serializable] private sealed class TestCase
    {
        public string prefab, pathSuffix, scenario;
        public Translations texts;
    }
    [Serializable] private sealed class Translations
    {
        public string english, portuguese, indonesian;
        public string Get(string language) => language == "pt-BR" ? portuguese : language == "id-ID" ? indonesian : english;
    }
    [Serializable] private sealed class CaseList { public TestCase[] cases; }
    [Serializable] private sealed class Check
    {
        public string prefab, language, viewport, node, scenario, text;
        public float width, height, fontSize;
        public int lines, characters;
        public bool overflow;
    }
    [Serializable] private sealed class Report
    {
        public bool passed;
        public string scope = "Actual Unity TMP mesh/layout in temporary preview scenes, with authored prefabs at two portrait aspect ratios. No live account or network calls.";
        public List<Check> checks = new();
        public List<string> errors = new();
        public List<string> excludedHiddenTemplates = new();
    }

    private static readonly string[] Languages = { "en-US", "pt-BR", "id-ID" };
    private static readonly Vector2Int[] Viewports = { new(1080, 1920), new(1080, 2400) };
    private static cfg.TblLanguage table;
    private static string language;
    private static string output;
    private static Report report;
    private static CaseList dynamicCases;

    public static void Run()
    {
        output = Path.GetFullPath("Logs/LocalizationValidation");
        bool focused = Environment.GetCommandLineArgs().Contains("-localizationFocused");
        if (focused) output += "Focused";
        Directory.CreateDirectory(output);
        report = new Report();
        try
        {
            table = new cfg.TblLanguage();
            table.Init(File.ReadAllBytes("Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes"));
            dynamicCases = JsonUtility.FromJson<CaseList>(File.ReadAllText("Tools/Localization/validation-cases.json"));
            var paths = File.ReadAllLines("Tools/Localization/validation-prefabs.txt")
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
            if (Environment.GetCommandLineArgs().Contains("-localizationSmoke"))
                paths = paths.Where(value => value.EndsWith("/BoosterPurchasePanel.prefab", StringComparison.Ordinal)).ToArray();
            if (focused) paths = paths.Where(value => value.EndsWith("/WithdrawFillPanel.prefab", StringComparison.Ordinal) || value.EndsWith("/WithdrawHistoryItem.prefab", StringComparison.Ordinal)).ToArray();
            foreach (var path in paths)
            {
                foreach (string locale in Languages)
                {
                    language = locale;
                    CoreLocalization.Configure(Resolve);
                    foreach (var viewport in Viewports)
                    {
                        try { ValidatePrefab(path, viewport); }
                        catch (Exception error) { report.errors.Add(path + " " + locale + " " + viewport + " " + error); }
                    }
                }
                WriteReport();
                Debug.Log("[LocalizationValidation] checked " + path);
            }
            report.passed = report.errors.Count == 0 && report.checks.Count > 0 && report.checks.All(value => !value.overflow);
        }
        catch (Exception error) { report.errors.Add(error.ToString()); }
        finally { WriteReport(); }
        Debug.Log("[LocalizationValidation] " + (report.passed ? "PASS" : "FAIL") + " checks=" + report.checks.Count + " overflow=" + report.checks.Count(value => value.overflow) + " errors=" + report.errors.Count);
        if (Application.isBatchMode) EditorApplication.Exit(report.passed ? 0 : 2);
    }

    private static void WriteReport() => File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));

    private static string Resolve(string key)
    {
        var row = table.GetOrDefault(key);
        if (row == null || !row.Dict.TryGetValue(language, out string text) || string.IsNullOrEmpty(text))
            throw new InvalidDataException("Missing translation: " + key + " / " + language);
        return text;
    }

    private static string Sample(string text)
    {
        text = Regex.Replace(text, @"\{[A-Za-z][A-Za-z0-9_]*\}", "#15B900");
        return Regex.Replace(text, @"\{\d+(?::[^{}]*)?\}", "30");
    }

    private static void ValidatePrefab(string path, Vector2Int viewport)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new InvalidDataException("Missing prefab " + path);
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture render = null;
        try
        {
            var canvasObject = new GameObject("ValidationCanvas", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.layer = 30;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvas.transform;
            canvasRect.sizeDelta = new Vector2(2360f * viewport.x / viewport.y, 2360f);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            bool worldText = path.StartsWith("Assets/Resources/Foundation/", StringComparison.Ordinal);
            root.transform.SetParent(canvas.transform, false);
            if (root.transform is RectTransform rootRect)
            {
                rootRect.anchoredPosition = Vector2.zero;
                if (rootRect.anchorMin != rootRect.anchorMax) rootRect.sizeDelta = Vector2.zero;
            }
            root.SetActive(true);
            foreach (var item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
            if (root.TryGetComponent<CanvasGroup>(out var rootGroup)) rootGroup.alpha = 1f;
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            var texts = new HashSet<TMP_Text>();
            foreach (var label in root.GetComponentsInChildren<UILanguageLabel>(true))
            {
                if (string.IsNullOrWhiteSpace(label.key) || label.key == "Withdraw_Des") continue;
                // This authored PageMask template is permanently inactive. PageMask
                // only binds its Button and has no path which displays this label.
                if (label.key == "Common_TapToClose" && !label.gameObject.activeSelf &&
                    label.transform.parent != null && label.transform.parent.name.StartsWith("PageMask", StringComparison.Ordinal))
                {
                    report.excludedHiddenTemplates.Add(path + ": " + GetPath(label.transform, root.transform));
                    continue;
                }
                var text = label.GetComponent<TMP_Text>();
                text.text = Sample(Resolve(label.key));
                texts.Add(text);
            }
            foreach (var label in root.GetComponentsInChildren<CoreLocalizedText>(true))
            {
                label.Refresh();
                var text = label.GetComponent<TMP_Text>();
                text.text = Sample(text.text);
                texts.Add(text);
            }
            foreach (var faq in root.GetComponentsInChildren<FAQDesc>(true))
            {
                string content = Resolve(faq.key).GetReplaceDesc(faq.titleColor).GetReplaceDesc(faq.contentColor).GetReplaceDesc(faq.highlightColor);
                float scale = new SerializedObject(faq).FindProperty("questionTitleScale").floatValue;
                int end = content.IndexOf("</color>", StringComparison.Ordinal);
                if (scale > 1f && end >= 0)
                {
                    end += "</color>".Length;
                    content = "<size=" + (scale * 100f).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "%>" + content.Substring(0, end) + "</size>" + content.Substring(end);
                }
                faq.tMP_Text.text = content;
                texts.Add(faq.tMP_Text);
            }
            foreach (var layout in root.GetComponentsInChildren<ReskinReferenceLayout>(true)) layout.RefreshLayout();
            Layout(root);
            foreach (var text in texts) CheckText(path, viewport, text, "localized-label");

            foreach (var test in dynamicCases.cases.Where(value => value.prefab == path))
            {
                var matches = root.GetComponentsInChildren<TMP_Text>(true)
                    .Where(text => GetPath(text.transform, root.transform).EndsWith(test.pathSuffix, StringComparison.Ordinal)).ToArray();
                if (matches.Length != 1) throw new InvalidDataException("Ambiguous/missing case " + test.scenario + " " + test.pathSuffix + ": " + matches.Length);
                var text = matches[0];
                text.text = test.texts.Get(language);
                Layout(root);
                CheckText(path, viewport, text, test.scenario);
            }
            if (root.TryGetComponent<UIWithdrawalConfirmPanel>(out var confirm))
            {
                confirm.NameObj.SetActive(true); confirm.EmailObj.SetActive(true); confirm.CPFObj.SetActive(true);
                confirm.NameText.text = language == "pt-BR" ? "João José Márcia de Albuquerque Silva Oliveira Santos" : "Muhammad Rizky Pratama Putra Ramadhan Wijaya";
                confirm.EmailText.text = new string('a', 64) + "@" + new string('b', 63) + ".example.com";
                confirm.CPF_CNPJText.text = "12345678901234";
                confirm.RefreshDetailsLayout(); Layout(root);
                CheckText(path, viewport, confirm.NameText, "full-long-name");
                CheckText(path, viewport, confirm.EmailText, "full-long-account");
            }
            if (root.TryGetComponent<UIWithdrawalPendingPanel>(out var pending))
            {
                var property = new SerializedObject(pending).FindProperty("hintText");
                var text = property?.objectReferenceValue as TMP_Text;
                if (text != null)
                {
                    string sentence = Resolve("InfoConfirm_WithdrawFailed");
                    text.text = string.Join(" ", Enumerable.Repeat(sentence, 20));
                    pending.RefreshHintLayout(); Layout(root);
                    CheckText(path, viewport, text, "long-server-result");
                }
            }
            if (root.TryGetComponent<BoosterPurchasePanel>(out var booster))
            {
                var so = new SerializedObject(booster);
                var values = new Dictionary<string, string> {
                    ["titleText"] = Resolve("Booster_Title"), ["propName"] = Resolve("Booster_Jester"),
                    ["priceText"] = string.Format(Resolve("Booster_Price"), "100"),
                    ["balanceText"] = string.Format(Resolve("Booster_Balance"), int.MaxValue.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo(language))),
                    ["buyText"] = Resolve("Booster_NotEnoughCoins"), ["adText"] = Resolve("Booster_WatchAd") };
                foreach (var pair in values)
                {
                    var text = (TMP_Text)so.FindProperty(pair.Key).objectReferenceValue;
                    text.text = pair.Value; CheckText(path, viewport, text, "booster-" + pair.Key);
                }
            }
            // Select mutually exclusive runtime display states for visual review.
            // The full dynamic text states above were measured independently.
            if (root.TryGetComponent<ServicePanel>(out var service)) service.inputText.gameObject.SetActive(false);
            foreach (var mode in root.GetComponentsInChildren<SingleCurrencyMode>(true))
                mode.gameObject.SetActive(new SerializedObject(mode).FindProperty("isReverse").boolValue);
            WithdrawalValidationSetup.Configure(root, language);
            Capture(root, canvas, scene, path, viewport, worldText, ref render);
        }
        finally
        {
            if (render != null) { render.Release(); UnityEngine.Object.DestroyImmediate(render); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Layout(GameObject root)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var rect in root.GetComponentsInChildren<RectTransform>(true).Reverse())
            if (rect.gameObject.activeInHierarchy) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        Canvas.ForceUpdateCanvases();
    }

    private static void CheckText(string path, Vector2Int viewport, TMP_Text text, string scenario)
    {
        // TMP's edit-mode Awake does not run under inactive template ancestors.
        // Select this UI state temporarily, then restore it after measuring.
        var activated = new List<GameObject>();
        bool wasEnabled = text.enabled;
        for (var parent = text.transform; parent != null; parent = parent.parent)
            if (!parent.gameObject.activeSelf) { activated.Add(parent.gameObject); parent.gameObject.SetActive(true); }
        text.enabled = true;
        try
        {
            Canvas.ForceUpdateCanvases();
            text.ForceMeshUpdate(true, true);
            var rect = ((RectTransform)text.transform).rect;
            if (text.textInfo == null) throw new InvalidOperationException("TMP did not initialize: " + GetPath(text.transform, null));
            report.checks.Add(new Check { prefab = path, language = language, viewport = viewport.ToString(),
                node = GetPath(text.transform, null), scenario = scenario, text = text.text,
                width = rect.width, height = rect.height, fontSize = text.fontSize,
                lines = text.textInfo.lineCount, characters = text.textInfo.characterCount,
                overflow = text.isTextOverflowing || rect.width <= 0f || rect.height <= 0f });
        }
        finally
        {
            text.enabled = wasEnabled;
            foreach (var item in activated) item.SetActive(false);
        }
    }

    private static string GetPath(Transform value, Transform stop)
    {
        string result = value.name;
        for (var parent = value.parent; parent != null && parent != stop; parent = parent.parent) result = parent.name + "/" + result;
        return result;
    }

    private static void Capture(GameObject root, Canvas canvas, Scene scene, string path, Vector2Int viewport, bool worldText, ref RenderTexture target)
    {
        var cameraObject = new GameObject("ValidationCamera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.GetComponent<Camera>();
        camera.scene = scene;
        camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
        camera.orthographic = true; camera.cullingMask = 1 << 30;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.03f, .08f, .12f, 1f);
        camera.nearClipPlane = .01f; camera.farClipPlane = 5000f;
        camera.transform.position = new Vector3(0, 0, -1000f);
        camera.orthographicSize = worldText ? 7f : 1180f;
        // These standalone child templates normally inherit their position from
        // the page/list controller. Center the diagnostic camera on that content.
        RectTransform focus = root.TryGetComponent<UIWithdrawalPanel>(out var fill) ? fill.BG :
            path.EndsWith("/WithdrawHistoryItem.prefab", StringComparison.Ordinal) ? root.transform as RectTransform : null;
        if (focus != null)
        {
            var center = focus.TransformPoint(focus.rect.center);
            camera.transform.position = new Vector3(center.x, center.y, -1000f);
        }
        if (worldText)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds? visibleBounds = null;
            foreach (var renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer) continue;
                if (!visibleBounds.HasValue) visibleBounds = renderer.bounds;
                else { var combined = visibleBounds.Value; combined.Encapsulate(renderer.bounds); visibleBounds = combined; }
            }
            var corners = new Vector3[4];
            foreach (var graphic in root.GetComponentsInChildren<Graphic>())
            {
                graphic.rectTransform.GetWorldCorners(corners);
                var combined = visibleBounds ?? new Bounds(corners[0], Vector3.zero);
                foreach (var corner in corners) combined.Encapsulate(corner);
                visibleBounds = combined;
            }
            if (visibleBounds.HasValue)
            {
                var bounds = visibleBounds.Value;
                camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -1000f);
                camera.orthographicSize = Mathf.Max(.5f, bounds.extents.y * 1.15f, bounds.extents.x * viewport.y / viewport.x * 1.15f);
            }
        }
        canvas.worldCamera = camera;
        Layout(root);
        target = new RenderTexture(viewport.x / 2, viewport.y / 2, 24);
        target.Create(); camera.targetTexture = target; camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var screenshot = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); screenshot.Apply();
        RenderTexture.active = previous;
        string file = Path.GetFileNameWithoutExtension(path) + "-" + language + "-" + viewport.x + "x" + viewport.y + ".png";
        File.WriteAllBytes(Path.Combine(output, file), screenshot.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(screenshot);
    }
}
#endif
