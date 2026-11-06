#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotEntry : MonoBehaviour
{
    public Image img1;
    public Image img2;

    private Sprite _finalSprite;

    private bool _flag;
    private SlotMachineManager slotMachineManager;

    public void Init(SlotMachineManager slotMachineManager)
    {
        this.slotMachineManager = slotMachineManager;
    }

    public void PlayAnim(int idx, Sprite finalSprite)
    {
        _flag = true;
        _finalSprite = finalSprite;
        img1.gameObject.SetActive(true);
        img2.gameObject.SetActive(true);
        GetComponent<Animation>().PlayWithCallback("SlotEntry", () =>
        {
            GetComponentInParent<SlotMachineManager>().OnStop(idx);
        });
    }

    [Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.MethodName)]
    public void OnAnimNext()
    {
        var img = _flag ? img1 : img2;
        int rand = Random.Range(0, slotMachineManager.SlotEntryss.Count);
        var sprite = slotMachineManager.SlotEntryss[rand].sprite;
        img.sprite = sprite;
        _flag = !_flag;
    }

    [Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.MethodName)]
    public void OnAnimEnd()
    {
        img1.sprite = _finalSprite;
    }
}
#endif
