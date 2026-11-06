using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffect : MonoBehaviour
{
    public string clipName;
    public float delay;

    void OnEnable()
    {
        if (!string.IsNullOrEmpty(clipName))
        {
            if (delay > 0)
            {
                GameUtils.DelayDo(() =>
                {
                    SoundManager.Instance.PlaySFX(clipName);
                }, delay);
            }
            else
            {
                SoundManager.Instance.PlaySFX(clipName);
            }
        }
    }
}
