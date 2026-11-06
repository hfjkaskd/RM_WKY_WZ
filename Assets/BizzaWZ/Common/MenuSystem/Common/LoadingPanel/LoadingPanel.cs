using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : UIPageBase
{
    public Image progressBar;
    public TMP_Text progressTxt;
    public RectTransform busRect;
    public GameObject CameraObj;

    private readonly Vector3[] _progressBarWorldCorners = new Vector3[4];

    void OnLoadingProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        progressBar.fillAmount = progress;
        progressTxt.text = Mathf.RoundToInt((progress * 100)) + "%";
        UpdateBusPosition(progress);
    }

    void UpdateBusPosition(float progress)
    {
        if (busRect == null || progressBar == null)
        {
            return;
        }

        RectTransform progressBarRect = progressBar.rectTransform;
        RectTransform busParentRect = busRect.parent as RectTransform;
        if (busParentRect == null)
        {
            return;
        }

        progressBarRect.GetWorldCorners(_progressBarWorldCorners);
        float leftX = busParentRect.InverseTransformPoint(_progressBarWorldCorners[0]).x;
        float rightX = busParentRect.InverseTransformPoint(_progressBarWorldCorners[3]).x;

        Vector2 anchoredPosition = busRect.anchoredPosition;
        anchoredPosition.x = Mathf.Lerp(leftX, rightX, progress);
        busRect.anchoredPosition = anchoredPosition;
    }

    protected override void OnOpen()
    {
        BizzaEventSystem.Set(EventDefine.Frame.LoadingProgress, OnLoadingProgress, true);
        var initWzCameraObj = GameObject.Find("InitWzCamera");
        if (initWzCameraObj != null)
        {
            initWzCameraObj.SetActive(false);
        }
        CameraObj.SetActive(true);
        OnLoadingProgress(0f);
    }

    protected override void OnClose()
    {
        BizzaEventSystem.Set(EventDefine.Frame.LoadingProgress, OnLoadingProgress, false);
    }
}
