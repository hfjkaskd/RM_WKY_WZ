using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrateEffectLoop : MonoBehaviour
{
    public E_VibrateType vibrateType = E_VibrateType.Light;
    public float interval => 0.18f;

    private float _timer;
    void Update()
    {
        if (_timer < 0)
        {
            _timer = interval;
            VibrationUtils.Vibrate(vibrateType);
        }

        _timer -= Time.deltaTime;
    }
}
