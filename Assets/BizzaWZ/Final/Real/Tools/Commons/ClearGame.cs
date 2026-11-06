#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ClearChildGame 
{
    public static void ClearChildsForRoot(Transform root)
    {
        int childCount = root.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }
}
#endif