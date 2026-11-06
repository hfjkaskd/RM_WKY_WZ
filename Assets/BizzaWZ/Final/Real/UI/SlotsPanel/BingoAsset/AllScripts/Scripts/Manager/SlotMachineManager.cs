#if BIZZA_REAL_WITHDRAW
using System;
using Spine;
using Spine.Unity;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SlotMachineManager : MonoBehaviour
{
    public GameObject machineRunning;
    public GameObject dropFx_Coin;
    public GameObject dropFx_Money;
    public GameObject dropFx_Common;

    public SlotEntry[] slotEntries;

    public GameObject rewardGourp;
    public GameObject[] itemList;
    public GameObject[] fxList;
    public GameObject dropFx;  
    public GameObject dropFx2;
    public SkeletonGraphic[] stars;

    public SkeletonGraphic anim;

    private TrackEntry animEntry;

    private TrackEntry animEntry2;

    public Image rewardIm1;

    public Image rewardIm2;

    public Image rewardIm3;

    private string resultType;

    [SerializeField]
    private List<SlotEntrys> slotEntryss = new List<SlotEntrys>()
    {
        new SlotEntrys(E_SlotType.Num, null),
        new SlotEntrys(E_SlotType.Coin, null),
        new SlotEntrys(E_SlotType.Dice, null),
        new SlotEntrys(E_SlotType.Grass, null),
        new SlotEntrys(E_SlotType.Brick, null),
    };
    public List<SlotEntrys> SlotEntryss => slotEntryss;

    public Action<string> onComplete;

    void OnEnable()
    {
        machineRunning.SetActive(false);
        dropFx.SetActive(false);
        dropFx2.SetActive(false);
        foreach (var v in fxList)
        {
            v.SetActive(false);
        }

        dropFx_Coin.SetActive(false);
        dropFx_Money.SetActive(false);
        dropFx_Common.SetActive(false);

        foreach (var v in slotEntries)
        {
            v.transform.localScale = Vector3.one;
            v.img2.gameObject.SetActive(false);
        }
    }

    public void Init()
    {
        foreach (var v in slotEntries)
        {
            v.img1.sprite = null; // ImageHelper.Instance.itemRewardIm[1];
        }
    }

    public void PlayScaleAnim()
    {
        foreach (var v in slotEntries)
        {
            v.transform.DOScale(Vector3.one * 1.4f, 0.2f);
            v.transform.DOScale(Vector3.one * 0.4f, 0.15f).SetDelay(0.25f);
            v.img2.gameObject.SetActive(false);
        }
    }

    public void PlayScaleAnim2()
    {
        foreach (var v in slotEntries)
        {
            v.transform.localScale = Vector3.zero;
        }

        foreach (var v in fxList)
        {
            v.gameObject.SetActive(false);
            v.gameObject.SetActive(true);
        }
    }

    public void PlayAnim(bool isAd, Action<string> onComplete = null)
    {
        E_SlotCombinationType combinationType = GetSlotCombinationTypeByAd(isAd);
        resultType = GetE_SlotResultType(combinationType);
        this.onComplete = onComplete;
        //int index = (int)type;
        var rewardIm = GetSlotSprites(combinationType);
        rewardIm1.sprite = rewardIm[0];
        rewardIm2.sprite = rewardIm[1];
        rewardIm3.sprite = rewardIm[2];
        animEntry = anim.AnimationState.SetAnimation(0, "A", false);
        animEntry.Complete += SetAnimC;

        for (var i = 0; i < slotEntries.Length; i++)
        {
            var v = slotEntries[i];
            var tmp = i;
            var reward = rewardIm[tmp];
            v.Init(this);
            GameUtils.DelayDo(() =>
            {
                v.PlayAnim(tmp, reward);
            }, tmp * 0.3f);
        }

        SoundManager.Instance.PlaySFX("SevenMachineRotate");
        Invoke("OnFinishDropDown", 4.4f);
        machineRunning.SetActive(true);
    }

    public void OnStop(int idx)
    {
        ActiveRewardIm(stars[idx], fxList[idx]);
    }

    [Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.MethodName)]
    void OnFinishDropDown()
    {
        machineRunning.SetActive(false);
        for (var i = 0; i < itemList.Length; i++)
        {
            var v = itemList[i];
            var target = v.transform;//.GetChild(1);
            if (i == 0)
            {
                target.DOScale(2f, 0.6f).OnComplete(() =>
                {
                    // UiManager.Instance.mainCanvas.GetOrAddComponent<CameraShake>().Shake();
                    SoundManager.Instance.PlaySFX("SevensGetReward");
                    target.DOScale(1f, 0.2f);
                    dropFx.SetActive(false);
                    dropFx.SetActive(true);
                    dropFx2.SetActive(false);
                    dropFx2.SetActive(true);
                });
            }
            else
            {
                target.DOScale(2f, 0.6f).OnComplete(() =>
                {
                    target.DOScale(1f, 0.2f);
                });
            }
        }

        // UiManager.Instance.ShowPanel();
    }

    public void ActiveRewardIm(SkeletonGraphic graphic, GameObject go)
    {
        VibrationUtils.Vibrate(E_VibrateType.Light);
        SoundManager.Instance.PlaySFX("SevenMachineStop");
        if (graphic != null && graphic.AnimationState != null)
        {
            graphic.AnimationState.SetAnimation(0, "animation", false);
        }
        go.gameObject.SetActive(true);
    }

    public void SetAnimC(TrackEntry track)
    {
        anim.AnimationState.SetAnimation(0, "C", true);
        // track.Complete -= OnAnimComplete;
        // EventModule.BroadCast(E_GameEvent.OnNumberBoxAnimOver);
        animEntry = null;
        onComplete?.Invoke(resultType);
        onComplete = null;
    }

    public void CloseAllText()
    {
        dropFx.SetActive(false);
        dropFx2.SetActive(false);
    }

    List<E_SlotCombinationType> NoAdtypes = new()
        {
            E_SlotCombinationType.Num_Num_Num,
            E_SlotCombinationType.Coin_Coin_Coin,
            E_SlotCombinationType.Dice_Dice_Dice,
        };
    List<E_SlotCombinationType> Adtypes = new()
        {
            E_SlotCombinationType.Grass_Grass_Grass,
            E_SlotCombinationType.Brick_Brick_Brick,
            E_SlotCombinationType.Num_Coin_Dice
        };

    private E_SlotCombinationType GetSlotCombinationTypeByAd(bool isAd)
    {
        if (isAd)
        {
            int index = UnityEngine.Random.Range(0, Adtypes.Count);
            return Adtypes[index];
        }
        else
        {
            int index = UnityEngine.Random.Range(0, NoAdtypes.Count);
            return NoAdtypes[index];
        }
    }

    private string GetE_SlotResultType(E_SlotCombinationType type)
    {
        return type switch
        {
            E_SlotCombinationType.Num_Num_Num => E_SlotResultType.Num_Num_Num,
            E_SlotCombinationType.Coin_Coin_Coin => E_SlotResultType.Coin_Coin_Coin,
            E_SlotCombinationType.Dice_Dice_Dice => E_SlotResultType.Dice_Dice_Dice,
            E_SlotCombinationType.Grass_Grass_Grass => E_SlotResultType.Grass_Grass_Grass,
            E_SlotCombinationType.Brick_Brick_Brick => E_SlotResultType.Brick_Brick_Brick,
            E_SlotCombinationType.Num_Coin_Dice => E_SlotResultType.Num_Coin_Dice,
            _ => E_SlotResultType.Num_Num_Num,
        };
    }

    private List<Sprite> GetSlotSprites(E_SlotCombinationType type)
    {
        var sprites = new List<Sprite>();
        if (type == E_SlotCombinationType.Num_Num_Num)
        {
            sprites.Add(GetSlotSprite(E_SlotType.Num));
            sprites.Add(GetSlotSprite(E_SlotType.Num));
            sprites.Add(GetSlotSprite(E_SlotType.Num));
        }
        else if (type == E_SlotCombinationType.Coin_Coin_Coin)
        {
            sprites.Add(GetSlotSprite(E_SlotType.Coin));
            sprites.Add(GetSlotSprite(E_SlotType.Coin));
            sprites.Add(GetSlotSprite(E_SlotType.Coin));
        }
        else if (type == E_SlotCombinationType.Dice_Dice_Dice)
        {
            sprites.Add(GetSlotSprite(E_SlotType.Dice));
            sprites.Add(GetSlotSprite(E_SlotType.Dice));
            sprites.Add(GetSlotSprite(E_SlotType.Dice));
        }
        else if (type == E_SlotCombinationType.Brick_Brick_Brick)
        {
            sprites.Add(GetSlotSprite(E_SlotType.Brick));
            sprites.Add(GetSlotSprite(E_SlotType.Brick));
            sprites.Add(GetSlotSprite(E_SlotType.Brick));
        }
        else if (type == E_SlotCombinationType.Grass_Grass_Grass)
        {
            sprites.Add(GetSlotSprite(E_SlotType.Grass));
            sprites.Add(GetSlotSprite(E_SlotType.Grass));
            sprites.Add(GetSlotSprite(E_SlotType.Grass));
        }
        else if (type == E_SlotCombinationType.Num_Coin_Dice)
        {
            sprites.Add(GetSlotSprite(E_SlotType.Num));
            sprites.Add(GetSlotSprite(E_SlotType.Coin));
            sprites.Add(GetSlotSprite(E_SlotType.Dice));
        }
        return sprites;
    }

    private Sprite GetSlotSprite(E_SlotType type)
    {
        var sprite = type switch
        {
            E_SlotType.Num => slotEntryss.Where(x => x.e_SlotType == E_SlotType.Num).FirstOrDefault().sprite,
            E_SlotType.Coin => slotEntryss.Where(x => x.e_SlotType == E_SlotType.Coin).FirstOrDefault().sprite,
            E_SlotType.Dice => slotEntryss.Where(x => x.e_SlotType == E_SlotType.Dice).FirstOrDefault().sprite,
            E_SlotType.Grass => slotEntryss.Where(x => x.e_SlotType == E_SlotType.Grass).FirstOrDefault().sprite,
            E_SlotType.Brick => slotEntryss.Where(x => x.e_SlotType == E_SlotType.Brick).FirstOrDefault().sprite,
            _ => null,
        };
        return sprite;
    }
}
[Obfuz.ObfuzIgnore]
public static class E_SlotResultType
{
    public const string Num_Num_Num = "HundredMoney"; // 三个数字
    public const string Coin_Coin_Coin = "HundredMoney"; // 三个金币
    public const string Dice_Dice_Dice = "HundredMoney"; // 三个骰子
    public const string Grass_Grass_Grass = "PileWealth"; // 三个草
    public const string Brick_Brick_Brick = "PileWealth"; // 三个砖块
    public const string Num_Coin_Dice = "PileWealth"; // 数字_金币_骰子
}
[Obfuz.ObfuzIgnore]
public enum E_SlotType
{
    Num, Coin, Dice, Grass, Brick
}

[Obfuz.ObfuzIgnore]
public enum E_SlotCombinationType
{
    Num_Num_Num,
    Coin_Coin_Coin,
    Dice_Dice_Dice,
    Grass_Grass_Grass,
    Brick_Brick_Brick,
    Num_Coin_Dice
}

[Serializable]
public struct SlotEntrys
{
    public E_SlotType e_SlotType;
    public Sprite sprite;

    public SlotEntrys(E_SlotType e_SlotType, Sprite sprite)
    {
        this.e_SlotType = e_SlotType;
        this.sprite = sprite;
    }
}
#endif
