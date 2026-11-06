using System;
using Spine.Unity;
using UnityEngine;

public class SpineAutoPlay : MonoBehaviour
{
    [SpineAnimation] public string startAnimationName = "A";
    public bool loop = false;
    public float delay;

    private SkeletonGraphic skeletonGraphic;
    private SkeletonAnimation skeletonAnimation;

    private void Awake()
    {

    }

    private void OnEnable()
    {
        if (delay <= 0)
        {
            Play();
        }
        else
        {
            GameUtils.DelayDo(()=> Play(), delay);
        }
    }

    private void Play()
    {
        if (this == null)
        {
            return;
        }
        skeletonGraphic = GetComponent<SkeletonGraphic>();
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonGraphic != null && skeletonGraphic.AnimationState != null)
        {
            try
            {
                skeletonGraphic.AnimationState.SetAnimation(0, startAnimationName, loop);
            }
            catch (Exception e)
            {
                Debug.LogError(gameObject.name);
            }
        }
        if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, startAnimationName, loop);
        }
    }
}