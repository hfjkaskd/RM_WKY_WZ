using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GridView : MonoBehaviour
{
    public int num = 7;
    public RectTransform GridContentTrans;
    public List<Component> ElementInstances { get => m_elementInstances; }
    private static WaitForSeconds _waitForSeconds = new WaitForSeconds(0.03f);
    private static WaitForSeconds _waitMoreForSeconds = new WaitForSeconds(0.3f);
    private List<Component> m_elementInstances = new List<Component>();
    private Coroutine m_coroutine;
    public Coroutine Coroutine => m_coroutine;
    public bool IsDelay
    {
        get; set;
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    //增加了一个delay参数 需要的时候可以将其中的Item逐个展示出来
    private IEnumerator InitGridViewCoroutineDelay<TData, TPrefab>(TPrefab prefab, List<TData> customDatas, Action<TData, TPrefab> updateViewFunc, Func<TData, bool> delayMoreFunc = null) where TPrefab : Component
    {
        //不需要全部失活，只需要将多余的失活
        int n = m_elementInstances.Count - customDatas.Count;

        if (n > 0)
        {
            for (int i = 0; i < n; i++)
            {
                m_elementInstances[m_elementInstances.Count - i - 1].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < customDatas.Count; i++)
        {
            TPrefab prefabClone;

            if (i < m_elementInstances.Count)
            {
                //如果当前PrefabClone未激活，先激活
                if (!m_elementInstances[i].gameObject.activeSelf)
                {
                    m_elementInstances[i].gameObject.SetActive(true);
                }

                prefabClone = m_elementInstances[i] as TPrefab;

                try
                {
                    updateViewFunc?.Invoke(customDatas[i], prefabClone);
                }
                catch (System.Exception e)
                {

                    Debug.LogError("InitGridViewCoroutine 异常: " + e.ToString() + "堆栈:" + e.ToString());
                }
            }
            //实例不够则创建新的实例
            else
            {
                prefabClone = InstanceItem(prefab, i);
                m_elementInstances.Add(prefabClone);
                try
                {
                    updateViewFunc?.Invoke(customDatas[i], prefabClone);

                    num--;
                }
                catch (System.Exception e)
                {

                    Debug.LogError("InitGridViewCoroutine 异常: " + e.ToString() + "堆栈:" + e.ToString());
                }
            }

            if (IsDelay)
            {
                if (delayMoreFunc != null)
                {
                    if (delayMoreFunc(customDatas[i]))
                    {
                        yield return _waitMoreForSeconds;
                    }
                }
                yield return _waitForSeconds;
            }

            if (num == 0)
            {
                num = 7;
                yield return 0;
            }
        }

        m_coroutine = null;
    }

    public void InitGridViewDelay<TData, TPrefab>(TPrefab prefab, List<TData> customDatas, Action<TData, TPrefab> updateViewFunc, Func<TData, bool> delayMoreFunc = null) where TPrefab : Component
    {
        m_coroutine = StartCoroutine(InitGridViewCoroutineDelay(prefab, customDatas, (customData, comp) =>
        {
            updateViewFunc?.Invoke(customData, comp);
        }, delayMoreFunc));
    }

    public void StopDelayCoroutine()
    {
        if (m_coroutine == null)
            return;
        StopCoroutine(m_coroutine);
        m_coroutine = null;
    }

    //-------分帧刷新列表----------
    private IEnumerator InitGridViewCoroutine<TData, TPrefab>(TPrefab prefab, List<TData> customDatas, Action<TData, TPrefab> updateViewFunc, int? count) where TPrefab : Component
    {
        int n = m_elementInstances.Count - customDatas.Count;
        if (n > 0)
        {
            for (int i = 0; i < n; i++)
            {
                m_elementInstances[m_elementInstances.Count - i - 1].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < customDatas.Count; i++)
        {
            if (i < m_elementInstances.Count)
            {
                //如果当前PrefabClone未激活，先激活
                if (!m_elementInstances[i].gameObject.activeSelf)
                {
                    m_elementInstances[i].gameObject.SetActive(true);
                }

                TPrefab prefabClone = m_elementInstances[i] as TPrefab;

                try
                {
                    updateViewFunc?.Invoke(customDatas[i], prefabClone);
                }
                catch (System.Exception e)
                {

                    Debug.LogError("InitGridViewCoroutine 异常: " + e.ToString() + "堆栈:" + e.ToString());
                }
            }
            else
            {
                TPrefab prefabClone = InstanceItem(prefab, m_elementInstances.Count);
                m_elementInstances.Add(prefabClone);
                try
                {
                    updateViewFunc?.Invoke(customDatas[i], prefabClone);
                }
                catch (System.Exception e)
                {

                    Debug.LogError("InitGridViewCoroutine 异常: " + e.ToString() + "堆栈:" + e.ToString());
                }
                yield return 0;
            }



        }
    }

    public void InitGridView<TData, TPrefab>(TPrefab prefab, List<TData> customDatas, Action<TData, TPrefab> updateViewFunc, int? count = null) where TPrefab : Component
    {
        if (gameObject.activeInHierarchy == false) return;
        StopAllCoroutines();
        StartCoroutine(InitGridViewCoroutine(prefab, customDatas, (customData, component) =>
        {
            updateViewFunc?.Invoke(customData, component);
        }, count));
    }

    private T InstanceItem<T>(T prefab, int i) where T : Component
    {
        T result = Instantiate(prefab, GridContentTrans);
        result.gameObject.name = "CommonIcon" + i.ToString();
        return result;
    }
}