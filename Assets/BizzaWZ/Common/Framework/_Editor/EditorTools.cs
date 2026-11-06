#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEngine;
using System.Collections;
using Unity.EditorCoroutines.Editor;

public static class _EditorTools
{
    // 延迟调用函数
    public static void DelayCall(float delaySeconds, System.Action callback)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(DelayCoroutine(delaySeconds, callback));
    }

    // 延迟协程
    private static IEnumerator DelayCoroutine(float delay, System.Action callback)
    {
        Debug.Log("Delay called 1");
        float timer = 0;
        while (timer < delay)
        {
            Debug.Log("Delay called 2:" + DateTime.Now.ToString("HH:mm:ss"));
            timer += Time.deltaTime;
            yield return null; // 等待下一帧
        }
        Debug.Log(Time.deltaTime);
        Debug.Log("Delay called 3");
        callback?.Invoke();
    }
}
#endif