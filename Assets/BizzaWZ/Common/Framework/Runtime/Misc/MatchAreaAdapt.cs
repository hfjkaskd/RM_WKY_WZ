using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MatchAreaAdapt : MonoBehaviour
{
    [LabelText("长屏设备")]
    public float minRatio = 0.3f;
    [LabelText("短屏设备")]
    public float maxRatio = 0.5625f;

    [LabelText("长屏设备位置")]
    public Vector3 minPos;
    [LabelText("短屏设备位置")]
    public Vector3 maxPos;

    [LabelText("长屏设备缩放")]
    public Vector3 minScale = Vector3.one;
    [LabelText("短屏设备缩放")]
    public Vector3 maxScale = Vector3.one;

    public Transform target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var ratio = (float)Screen.width / Screen.height;
        var percent = Mathf.Clamp01((ratio - minRatio) / (maxRatio - minRatio));
        // var value = minValue + (maxValue - minValue) * percent;
        var targetTran = target != null ? target : transform;
        targetTran.localPosition = Vector3.Lerp(minPos, maxPos, percent); //new Vector3(transform.position.x, value, transform.position.z);
        targetTran.localScale = Vector3.Lerp(minScale, maxScale, percent);
    }
}
