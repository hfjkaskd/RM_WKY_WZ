using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class UnityExtension
{
    public static async UniTask PlayWithCallback(this Animation anim, string clipName, Action onComplete)
    {
        if (anim == null || anim[clipName] == null)
        {
            Debug.LogWarning($"Animation 或 Clip 不存在: {clipName}");
            return;
        }

        anim.Play(clipName);
        await UniTask.WaitForEndOfFrame();
        // 等待动画播放完毕
        await UniTask.WaitUntil(
            () =>
            {
                if (anim == null)
                {
                    return true;
                }
                return !anim.IsPlaying(clipName);
            });
        onComplete?.Invoke();
    }

    public static Vector3 ToVector3(this Vector2 self, float value = 0)
    {
#if BIZZA_GAME_3D
            return new Vector3(self.x, value, self.y);
#else
        return new Vector3(self.x, self.y, value);
#endif
    }


}
