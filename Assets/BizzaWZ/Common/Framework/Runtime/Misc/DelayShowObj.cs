using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayShowObj : MonoBehaviour
{
    public GameObject targetObj;
    public float delayTime = 1.5f;

    void OnEnable()
    {
        StartCoroutine(DelayShowCloseBtn());
    }

    void OnDisable()
    {
        StopCoroutine(DelayShowCloseBtn());
    }

    private IEnumerator DelayShowCloseBtn()
    {
        if (targetObj != null)
        {
            targetObj.gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(delayTime);
        if (targetObj != null)
        {
            targetObj.gameObject.SetActive(true);
        }
    }
}
