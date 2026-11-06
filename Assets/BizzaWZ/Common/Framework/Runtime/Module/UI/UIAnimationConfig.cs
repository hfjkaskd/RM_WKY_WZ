using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu(fileName = "UI动画配置", menuName = "UI动画配置")]
public class UIAnimationConfig:ScriptableObject
{
    [LabelText("UI动画配置"),SerializeField]
    public List<UIAnimationConfigData> Datas = new List<UIAnimationConfigData>();

    [Serializable]
    [Obfuz.ObfuzIgnore]
    public class UIAnimationConfigData
    {
        [LabelText("UI动画ID")] public int Id;
        [LabelText("本地序列帧")] public Sprite[] spriteReferences;
        [LabelText("动画帧间隔")] public int spritePerFrame = 6;
        [LabelText("是否循环")] public bool loop = true;
        [LabelText("播完后是否销毁")] public bool destroyOnEnd = false;
        [LabelText("加载完后自动播放")] public bool autoPlay = false;
        [LabelText("倒序播放")] public bool backPlay = false;
    }
}
