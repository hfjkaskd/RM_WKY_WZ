#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Exercises the real history prefab without sending requests or changing player data.</summary>
public static class WithdrawHistoryLoadingChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string PrefabPath = "Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistory.prefab";
    private static readonly List<string> passed = new();
    public static IReadOnlyList<string> Passed => passed;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        passed.Add(message);
    }

    private static object Call(object obj, string method, params object[] args) =>
        obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);

    [MenuItem("工具/UI/检查提现记录加载状态")]
    public static void Run()
    {
        passed.Clear();
        TableUtils.LoadTableConfigs();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(prefab != null, "History prefab exists");
        Check(!prefab.GetComponent<WithdrawHistory>().emptyHint.activeSelf, "Prefab does not flash empty history before opening");
        var instance = UnityEngine.Object.Instantiate(prefab);
        instance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var page = instance.GetComponent<WithdrawHistory>();
            // Mirror ScrollViewClearChilds.Awake: edit-mode prefabs retain their designer preview rows.
            if (!Application.isPlaying)
                for (int i = page.root.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(page.root.GetChild(i).gameObject);
            if (Field<RectTransform>(page, "statusRoot") == null) Call(page, "OnAwake");
            var load = Field<UIContentLoadSession>(page, "content");
            var group = Field<CanvasGroup>(page, "listGroup");
            var retry = Field<BizzaButton>(page, "retryButton");
            var close = Field<BizzaButton>(page, "closeButton");
            var spinner = instance.GetComponentInChildren<UIContentSpinner>(true);
            var rows = Field<List<WithdrawHistoryItem>>(page, "items");
            var payment = Field<PaymentConfig>(page.item, "paymentConfig").PaymentDatas[0].paymentKey;
            int Begin() => load.Begin(Time.unscaledTime, 20);
            void Deliver(int version, FailHttpResponse<List<AccountModule.OceanShineWithdrawalRecord>> response) =>
                Call(page, "Refresh", version, response);
            FailHttpResponse<List<AccountModule.OceanShineWithdrawalRecord>> Records() => new()
            {
                success = true,
                data = new()
                {
                    new() { Os_Sts = 1, Os_Pym = payment, Os_Dat = "2026-09-28", Os_Prc = 10 },
                    new() { Os_Sts = 2, Os_Pym = payment, Os_Dat = "2026-09-28", Os_Prc = 20, Os_Tsm = "Test reason" },
                    new() { Os_Sts = 3, Os_Pym = payment, Os_Dat = "2026-09-28", Os_Prc = 30 }
                }
            };

            int version = Begin();
            Check(!page.emptyHint.activeSelf && group.alpha == 0 && !group.blocksRaycasts && !group.interactable,
                "Loading hides empty hint and makes old rows invisible and inert");
            Check(spinner != null && spinner.gameObject.activeInHierarchy && !spinner.raycastTarget,
                "Loading uses shared nonblocking spinner");
            Check(!retry.gameObject.activeSelf && close.interactable, "Loading hides retry and keeps close available");
            page.Retry();
            Check(load.Version == version, "Retry ignored while loading");
            Deliver(version, Records());
            Check(load.State == UIContentState.Ready && group.alpha == 1 && group.blocksRaycasts,
                "Successful records reveal initialized list");
            Check(rows.Count == 3 && rows[0].processingObj.activeSelf && rows[1].failObj.activeSelf && rows[2].successObj.activeSelf,
                "Processing rejected and successful order states preserved");
            Check(page.root.GetComponentsInChildren<WithdrawHistoryItem>().Length == 3 && rows[0].timeTxt.text == "2026-09-28",
                "Visible list contains response data without designer preview rows");
            Check(!spinner.gameObject.activeInHierarchy && !retry.gameObject.activeSelf && !page.emptyHint.activeSelf,
                "Ready hides all request feedback");
            Deliver(version, new() { success = false });
            Check(load.State == UIContentState.Ready, "Duplicate callback cannot replace ready records");

            version = Begin();
            Check(group.alpha == 0, "Reload hides previous records immediately");
            Deliver(version, new() { success = true, data = new() });
            Check(load.State == UIContentState.Empty && page.emptyHint.activeSelf && !retry.gameObject.activeSelf,
                "Successful empty array shows only the existing empty hint");
            Check(rows.TrueForAll(row => !row.gameObject.activeSelf), "Empty response deactivates previous rows");
            page.Retry();
            Check(load.Version == version, "Empty state cannot start a retry");

            version = Begin();
            Deliver(version, new() { success = false, data = new() });
            Check(load.State == UIContentState.Failed && !page.emptyHint.activeSelf && retry.interactable && retry.gameObject.activeInHierarchy,
                "Failed response shows retry instead of empty history");
            Check(!spinner.gameObject.activeInHierarchy && close.interactable, "Failure stops spinner and keeps close available");
            Check(retry.GetComponent<Image>().sprite != null && retry.GetComponentInChildren<TMPro.TMP_Text>().text == LanguageUtils.GetText("UIContent_Retry"),
                "Retry uses existing framework artwork and localization");
            version = Begin();
            Deliver(version, new() { success = true, data = null });
            Check(load.State == UIContentState.Failed, "Successful null data is malformed rather than empty");
            version = Begin();
            Deliver(version, new() { success = true, data = new() { null } });
            Check(load.State == UIContentState.Failed, "Null record fails without rendering partial rows");

            int old = Begin();
            version = Begin();
            Deliver(old, Records());
            Check(load.State == UIContentState.Loading && load.Version == version, "Superseded response cannot reveal stale records");
            Call(page, "OnClose");
            Check(load.State == UIContentState.Closed && !page.emptyHint.activeSelf && group.alpha == 0, "Close clears all visible content");
            Begin();
            Deliver(version, Records());
            Check(load.State == UIContentState.Loading, "Reopened page rejects response from previous open");

            version = load.Begin(Time.unscaledTime - 21f, 20);
            Deliver(version, Records());
            Check(load.State == UIContentState.Failed && retry.gameObject.activeSelf, "Callback respects timeout even before Update");
            Begin();
            load.Tick(Time.unscaledTime + 20);
            Check(load.State == UIContentState.Failed && !spinner.gameObject.activeInHierarchy, "Silent timeout exposes retry and stops spinner");
            version = Begin();
            instance.SetActive(false);
            // Edit-mode instances do not receive MonoBehaviour lifecycle callbacks automatically.
            if (!Application.isPlaying) Call(page, "OnDisable");
            Check(load.State == UIContentState.Closed, "Direct disable invalidates request");
            instance.SetActive(true);
            Begin();
            Deliver(version, Records());
            Check(load.State == UIContentState.Loading, "Reactivated page rejects pre-disable callback");
            Call(page, "OnClose");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        Debug.Log($"[WithdrawHistoryLoadingChecks] PASS {passed.Count} checks");
    }
}
#endif
