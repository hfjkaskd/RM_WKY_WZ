
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine;


[Obfuz.ObfuzIgnore]
public class RealGamePanel : UIPageBase
{
    private const float RecoveredLifeHintScale = 1.3432f;
    private const float RecoveredLifeHintOvershootScale = 0.9064f;
    private const float RecoveredLifeHintReboundScale = 1.104f;
    private const float RecoveredLifeHintScaleUpDuration = 0.1f;
    private const float RecoveredLifeHintScaleDownDuration = 0.08f;
    private const float RecoveredLifeHintReboundDuration = 0.07f;
    private const float RecoveredLifeHintSettleDuration = 0.08f;
    private const float RecoveredLifeHintInterval = 0.0175f;
    public GameObject uIPropPrefab;
    public RectTransform propsRoot;
    public List<UIPropEntry> propEntries;

    public Transform content;
    public GameObject whiteUiPrefab;

    private GameObject recoveredTopHudInstance;
    private Coroutine recoveredTopHudLifeHintRoutine;
    private readonly Dictionary<RectTransform, Vector3> recoveredTopHudLifeBaseScales = new Dictionary<RectTransform, Vector3>();

    public RectTransform RecoveredTopHudRoot => recoveredTopHudInstance != null
        ? recoveredTopHudInstance.transform as RectTransform
        : null;

    void Awake()
    {
        CreateRecoveredTopHud();
        CreateGameUi();
        InitPropEntries();
    }

    private void CreateGameUi()
    {
        var prefab = Resources.Load<GameObject>("GameUiWidget");
        if (prefab == null) return;

        var go = GameObject.Instantiate(prefab, content);
        go.transform.localScale = Vector3.one;
    }

    private void InitPropEntries()
    {
        propEntries ??= new List<UIPropEntry>();
        propEntries.Clear();

        if (uIPropPrefab == null || propsRoot == null)
        {
            Debug.Log("[WhiteBootstrap] RealGamePanel prop prefab or root is missing, skip prop entries.");
            return;
        }

        PropConfigSO propConfig = PropConfigSO.Instance;
        if (propConfig == null || propConfig.PropCfgInfos == null)
        {
            Debug.LogError("[WhiteBootstrap] RealGamePanel prop config is missing.");
            return;
        }

        foreach (var propInfo in propConfig.PropCfgInfos)
        {
            var propEntryGo = GameObject.Instantiate(uIPropPrefab, propsRoot);
            var _propEntry = propEntryGo.GetComponent<UIPropEntry>();
            if (_propEntry == null)
            {
                Debug.LogError("UIPropEntry prefab is missing UIPropEntry component.");
                continue;
            }

            _propEntry.Init(propInfo);
            propEntries.Add(_propEntry);
        }
    }

    private void CreateRecoveredTopHud()
    {
        GameObject prefab = GetRecoveredTopHudPrefab();
        if (prefab == null || content == null)
        {
            Debug.LogWarning("[WhiteBootstrap] RealGamePanel RecoveredTopHud prefab or content root is missing.");
            return;
        }

        recoveredTopHudInstance = GameObject.Instantiate(prefab, content);
        recoveredTopHudInstance.name = prefab.name;
        Debug.Log("[WhiteBootstrap] RealGamePanel RecoveredTopHud created:" + recoveredTopHudInstance.name);
    }

    private GameObject GetRecoveredTopHudPrefab()
    {
        return whiteUiPrefab;
    }

    protected override void OnOpen()
    {
        NumbericalStatistics.InitProp();
        UIPropEntry.usingPropType = E_ItemType.None;
        #if BIZZA_REAL_WITHDRAW
        AccountModule.Instance.CheckPendingAutoPopups();
        #endif
    }

    protected override void OnClose()
    {
        StopRecoveredTopHudLifeHint();
    }

    public bool PlayRecoveredTopHudLifeHint(int heartCount = 3)
    {
        if (recoveredTopHudInstance == null || heartCount <= 0)
        {
            return false;
        }

        List<RectTransform> hearts = new List<RectTransform>();
        int maxHeartCount = Mathf.Min(heartCount, 3);
        for (int i = 0; i < maxHeartCount; i++)
        {
            RectTransform heart = FindRecoveredTopHudRect($"Life_{i}");
            if (heart == null || !heart.gameObject.activeInHierarchy)
            {
                continue;
            }

            hearts.Add(heart);
        }

        if (hearts.Count == 0)
        {
            return false;
        }

        StopRecoveredTopHudLifeHint();
        for (int i = 0; i < hearts.Count; i++)
        {
            CacheRecoveredTopHudLifeBaseScale(hearts[i]);
        }

        recoveredTopHudLifeHintRoutine = StartCoroutine(PlayRecoveredTopHudLifeHintRoutine(hearts));
        return true;
    }

    private void StopRecoveredTopHudLifeHint()
    {
        if (recoveredTopHudLifeHintRoutine != null)
        {
            StopCoroutine(recoveredTopHudLifeHintRoutine);
            recoveredTopHudLifeHintRoutine = null;
        }

        foreach (KeyValuePair<RectTransform, Vector3> pair in recoveredTopHudLifeBaseScales)
        {
            if (pair.Key != null)
            {
                pair.Key.localScale = pair.Value;
            }
        }
    }

    private IEnumerator PlayRecoveredTopHudLifeHintRoutine(List<RectTransform> hearts)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            RectTransform heart = hearts[i];
            if (heart == null)
            {
                continue;
            }

            Vector3 baseScale = CacheRecoveredTopHudLifeBaseScale(heart);
            heart.localScale = baseScale;
            yield return TweenRecoveredTopHudLifeScale(heart, baseScale, baseScale * RecoveredLifeHintScale, RecoveredLifeHintScaleUpDuration);
            yield return TweenRecoveredTopHudLifeScale(heart, baseScale * RecoveredLifeHintScale, baseScale * RecoveredLifeHintOvershootScale, RecoveredLifeHintScaleDownDuration);
            yield return TweenRecoveredTopHudLifeScale(heart, baseScale * RecoveredLifeHintOvershootScale, baseScale * RecoveredLifeHintReboundScale, RecoveredLifeHintReboundDuration);
            yield return TweenRecoveredTopHudLifeScale(heart, baseScale * RecoveredLifeHintReboundScale, baseScale, RecoveredLifeHintSettleDuration);

            if (RecoveredLifeHintInterval > 0f)
            {
                yield return WaitRealtime(RecoveredLifeHintInterval);
            }
        }

        recoveredTopHudLifeHintRoutine = null;
    }

    private IEnumerator TweenRecoveredTopHudLifeScale(RectTransform heart, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.0001f, duration);
        while (elapsed < safeDuration)
        {
            if (heart == null)
            {
                yield break;
            }

            float t = Mathf.Clamp01(elapsed / safeDuration);
            t = t * t * (3f - 2f * t);
            heart.localScale = Vector3.LerpUnclamped(from, to, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (heart != null)
        {
            heart.localScale = to;
        }
    }

    private static IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private Vector3 CacheRecoveredTopHudLifeBaseScale(RectTransform heart)
    {
        if (heart == null)
        {
            return Vector3.one;
        }

        if (!recoveredTopHudLifeBaseScales.TryGetValue(heart, out Vector3 baseScale))
        {
            baseScale = heart.localScale;
            recoveredTopHudLifeBaseScales[heart] = baseScale;
        }

        return baseScale;
    }

    private RectTransform FindRecoveredTopHudRect(string objectName)
    {
        Transform found = FindRecoveredTopHudTransform(RecoveredTopHudRoot, objectName);
        return found != null ? found as RectTransform : null;
    }

    private static Transform FindRecoveredTopHudTransform(Transform root, string objectName)
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
            Transform found = FindRecoveredTopHudTransform(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
