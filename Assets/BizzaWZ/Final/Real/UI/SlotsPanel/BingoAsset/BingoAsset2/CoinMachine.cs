#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CoinMachine : BaseSingleton<CoinMachine>
{
    public GameObject prefab;
    public Transform spawnPos;

    [Button]
    public void DropItem()
    {
        var rdx = Random.Range(-0.5f, 0.5f);
        var rdz = Random.Range(-0.5f, 0.5f);
        var targetPos = spawnPos.position + new Vector3(rdx, 0, rdz);
        var inst = GameObject.Instantiate(prefab);
        inst.transform.position = targetPos;
        inst.transform.localScale = Vector3.one;
        // view.transform.SetParent(inst.transform);
        // view.transform.localPosition = Vector3.zero;
        // view.transform.localScale = Vector3.one * 5;
    }
}
#endif
