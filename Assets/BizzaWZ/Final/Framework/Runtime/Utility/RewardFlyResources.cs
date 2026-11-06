#if BIZZA_REAL_WITHDRAW
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "BizzaGame/Reward Fly Resources")]
public sealed class RewardFlyResources : ScriptableObject
{
    [LabelText("巴西散落钞票")] public Sprite paperBR;
    [LabelText("巴西飞行钱束")] public Sprite bundleBR;
    [LabelText("印尼散落钞票")] public Sprite paperID;
    [LabelText("印尼飞行钱束")] public Sprite bundleID;
    [LabelText("美国散落钞票")] public Sprite paperUS;
    [LabelText("美国飞行钱束")] public Sprite bundleUS;
    [LabelText("开场爆开音效")] public AudioClip burstSound;

    public void Resolve(AccountModule.E_CountryType country, out Sprite paper, out Sprite bundle)
    {
        paper = paperBR;
        bundle = bundleBR;
        if (country == AccountModule.E_CountryType.ID)
        {
            paper = paperID; bundle = bundleID;
        }
        else if (country == AccountModule.E_CountryType.US)
        {
            paper = paperUS; bundle = bundleUS;
        }
    }
}
#endif
