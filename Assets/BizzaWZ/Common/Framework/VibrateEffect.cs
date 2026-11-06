using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrateEffect : MonoBehaviour
{
    public E_VibrateType vibrateType = E_VibrateType.Light;

    void OnEnable()
    {
        VibrationUtils.Vibrate(vibrateType);
    }
}
