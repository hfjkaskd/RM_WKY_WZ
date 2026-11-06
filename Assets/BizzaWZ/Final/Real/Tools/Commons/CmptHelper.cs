#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza
{
    public interface IInit<in TData>
    {
        void Init(TData data);
    }

    public static class CmptHelper
    {
        public static void CompleteCmptList<TCmpt>(this List<TCmpt> cmptList, TCmpt prefab, Transform parent, int count)
            where TCmpt : MonoBehaviour
        {
            for (int i = cmptList.Count; i < count; i++)
            {
                var cmpt = UnityEngine.Object.Instantiate(prefab, parent);
                cmptList.Add(cmpt);
            }
        }

        public static void SetCmptListCount<TCmpt>(this List<TCmpt> cmptList, TCmpt prefab, Transform parent, int count)
            where TCmpt : MonoBehaviour
        {
            CompleteCmptList(cmptList, prefab, parent, count);

            for (int i = count, l = cmptList.Count; i < l; i++)
            {
                cmptList[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < count; i++)
            {
                cmptList[i].gameObject.SetActive(true);
            }
        }

        public static void InitCmptList<TCmpt, TData>(this List<TCmpt> cmptList, TCmpt prefab, Transform parent,
            IReadOnlyList<TData> dataList)
            where TCmpt : MonoBehaviour, IInit<TData>
        {
            int count = dataList.Count;
            SetCmptListCount(cmptList, prefab, parent, count);
            for (int i = 0; i < count; i++)
            {
                cmptList[i].Init(dataList[i]);
            }
        }

        public static void InitCmptList<TCmpt, TData>(this List<TCmpt> cmptList, TCmpt prefab, Transform parent,
            IReadOnlyList<TData> dataList, Action<TCmpt, TData> initAction)
            where TCmpt : MonoBehaviour
        {
            int count = dataList.Count;
            SetCmptListCount(cmptList, prefab, parent, count);
            for (int i = 0; i < count; i++)
            {
                initAction.Invoke(cmptList[i], dataList[i]);
            }
        }
    }
}
#endif