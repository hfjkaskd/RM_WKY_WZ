#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestServicePanel : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
           UIModule.Instance.OpenPage(UIPageIds.ServicePanel);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
           SaveDataUtils.GameData.chatInfos.Clear();
        }
    }
}
#endif