#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Deterministic UI checks. No live requests, ads, player saves or scene changes.</summary>
public static class UIContentLoadingChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string PrefabPath = "Assets/BizzaWZ/Final/Real/UI/DailyMissionPanel/DailyMissionPanel.prefab";
    private static readonly List<string> passed = new();
    public static IReadOnlyList<string> Passed => passed;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed.Add(message);
    }

    private static object Call(object obj, string method, params object[] args) =>
        obj.GetType().GetMethod(method, Private, null, Array.ConvertAll(args, arg => arg.GetType()), null).Invoke(obj, args);
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);

    [MenuItem("工具/UI/检查内容加载状态")]
    public static void Run()
    {
        passed.Clear();
        var session = new UIContentLoadSession();
        int a = session.Begin(0, 20);
        Check(session.Accept(a, 19), "Slow request remains loading before deadline");
        Check(!session.Accept(a, 20) && session.State == UIContentState.Failed, "Late callback times out before Update");
        int b = session.Begin(21, 20);
        Check(!session.Accept(a, 22) && session.Accept(b, 22), "Retry rejects old request");
        session.Complete(b, UIContentState.Ready, 23);
        Check(!session.Complete(b, UIContentState.Failed, 24) && session.State == UIContentState.Ready, "Completed request rejects duplicate callback");
        int c = session.Begin(30, 20);
        session.Close();
        int d = session.Begin(31, 20);
        Check(!session.Accept(c, 32) && session.Accept(d, 32), "Close and reopen invalidate previous request");
        session.Tick(51);
        Check(session.State == UIContentState.Failed, "Silent request times out");
        bool invalidTimeout = false;
        try { session.Begin(0, float.NaN); } catch (ArgumentOutOfRangeException) { invalidTimeout = true; }
        Check(invalidTimeout, "Invalid timeout rejected");

        TableUtils.LoadTableConfigs();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(prefab != null, "Daily mission prefab exists");
        var instance = UnityEngine.Object.Instantiate(prefab);
        instance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var page = instance.GetComponent<DailyMissionPanel>();
            var view = Field<UIContentStateView>(page, "contentView");
            if (!Field<bool>(view, "initialized")) Call(page, "OnAwake");
            var load = Field<UIContentLoadSession>(page, "content");
            var go = Field<BizzaButton>(page, "goBtn");
            var close = Field<BizzaButton>(page, "closeBtn");
            var originalPosition = go.GetComponentInChildren<TMPro.TMP_Text>(true).rectTransform.anchoredPosition;
            var list = new List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>
                { new() { Os_Cn = "test" } };
            FailHttpResponse<List<AccountModule.RoutineTaskLookAdMoneyResponse>> Mission(int state = 2) => new()
                { success = true, data = new() { new() { Os_An = 5, Os_Ln = 3, Os_My = 10, Os_Ss = state } } };
            FailHttpResponse<AccountModule.OceanShineWithdrawalPageResponse> Platforms() => new()
                { success = true, data = new() { Os_Wwf = list } };
            int Begin()
            {
                typeof(DailyMissionPanel).GetField("pendingMission", Private).SetValue(page, null);
                typeof(DailyMissionPanel).GetField("missionReceived", Private).SetValue(page, false);
                typeof(DailyMissionPanel).GetField("platformsReceived", Private).SetValue(page, false);
                Field<List<AccountModule.OceanShineWithdrawalPageResponse.WithdrawalPlatform>>(page, "plats").Clear();
                return load.Begin(Time.unscaledTime, 20);
            }
            void Deliver(int version, string method, object response)
            {
                if ((bool)Call(page, "AcceptResponse", version)) Call(page, method, version, response);
            }
            int version = Begin();
            Check(!go.interactable && close.interactable && !page.refreshTimeTxt.gameObject.activeSelf, "Loading locks action but keeps close available");
            var spinner = page.hintsTxt.GetComponentInChildren<UIContentSpinner>(true);
            Check(spinner != null && spinner.gameObject.activeSelf && spinner.GetComponent<CanvasRenderer>() != null && !spinner.raycastTarget,
                "Spinner has renderer and never blocks input");
            using (var mesh = new VertexHelper())
            {
                Call(spinner, "OnPopulateMesh", mesh);
                Check(mesh.currentVertCount > 0, "Spinner generates visible geometry");
            }
            var bg = Field<Image>(view, "buttonBackground");
            Check(bg.material != null && bg.material.shader.name == "UI/Grayscale", "Loading uses configured grayscale material");
            Check(!page.ContentReady && !page.ContentFailed, "Popup queue waits during loading");
            Deliver(version, "OnResultCallback", Mission());
            Check(load.State == UIContentState.Loading && !go.interactable, "Mission alone cannot unlock actions");
            Deliver(version, "Refresh", Platforms());
            Check(load.State == UIContentState.Ready && page.WithdrawObj.activeSelf && page.refreshTimeTxt.gameObject.activeSelf,
                "Both responses commit content and actions together");
            Check(page.hintsTxt.text.Contains("3/5"), "Progress comes from response rather than stale SDK cache");
            Check(!spinner.gameObject.activeSelf && go.GetComponentInChildren<TMPro.TMP_Text>(true).rectTransform.anchoredPosition == originalPosition,
                "Ready restores layout and hides spinner");
            version = Begin();
            Check(list.Count == 1, "Retry does not clear SDK platform cache");
            Deliver(version, "Refresh", Platforms());
            Check(load.State == UIContentState.Loading, "Platform response alone cannot unlock actions");
            Deliver(version, "OnResultCallback", Mission(1));
            Check(page.GoObj.activeSelf && go.interactable && !page.WithdrawObj.activeSelf, "Reverse response order shows Go action");
            int oldVersion = Begin();
            version = Begin();
            Deliver(oldVersion, "OnResultCallback", Mission());
            Check(!Field<bool>(page, "missionReceived"), "Old request cannot update current page");
            Deliver(version, "OnResultCallback", new FailHttpResponse<List<AccountModule.RoutineTaskLookAdMoneyResponse>> { success = false });
            Check(load.State == UIContentState.Failed && go.interactable && !page.refreshTimeTxt.gameObject.activeSelf, "Failure exposes local retry");
            Check(!page.ContentReady && page.ContentFailed, "Automatic queue sees failure without recording successful display");
            Deliver(version, "Refresh", Platforms());
            Check(load.State == UIContentState.Failed, "Late success cannot undo failure");
            version = Begin();
            Deliver(version, "OnResultCallback", new FailHttpResponse<List<AccountModule.RoutineTaskLookAdMoneyResponse>> { success = true, data = new() });
            Check(load.State == UIContentState.Empty && go.interactable, "Empty list displays empty feedback and retry");
            version = Begin();
            Deliver(version, "OnResultCallback", Mission(9));
            Check(load.State == UIContentState.Failed, "Malformed task state cannot unlock actions");
            version = Begin();
            Deliver(version, "Refresh", new FailHttpResponse<AccountModule.OceanShineWithdrawalPageResponse> { success = true, data = new() { Os_Wwf = new() } });
            Check(load.State == UIContentState.Failed, "Missing platforms display failure");
            version = Begin();
            load.Tick(Time.unscaledTime + 20);
            Check(load.State == UIContentState.Failed && !spinner.gameObject.activeSelf, "Page timeout stops spinner and enables retry");
            version = Begin();
            Call(page, "OnClose");
            Begin();
            Deliver(version, "Refresh", Platforms());
            Check(!Field<bool>(page, "platformsReceived"), "Closed page callback cannot populate reopened page");
            Check(Field<BizzaButton>(page, "withdrawBtn").interactable == false, "Loading disables withdrawal action too");
            go.interactable = false;
            Call(go, "Awake");
            Vector3 scale = go.scaleTarget.localScale;
            go.OnPointerDown(new PointerEventData(null));
            Check(go.scaleTarget.localScale == scale, "Disabled button has no press animation or sound");
            int clicked = 0;
            go.onClick.RemoveAllListeners();
            go.onClick.AddListener(() => clicked++);
            go.interactable = true;
            go.OnPointerUp(new PointerEventData(null));
            Check(clicked == 0, "Release after disabled press cannot trigger action");
            Call(page, "OnClose");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        Debug.Log($"[UIContentLoadingChecks] PASS {passed.Count} checks");
    }
}
#endif
