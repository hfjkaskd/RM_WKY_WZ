using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public static class GameObjUitl
    {
        public static void SetObjActive(this GameObject self, bool active)
        {
            if (self.activeSelf != active)
            {
                self.SetActive(active);
            }
        }

        public static void DestroyAllChildren(this Transform self)
        {
            foreach (Transform child in self)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        public static GameObject FindObj(string path, Transform parent = null)
        {
            if (parent != null)
            {
                var target = parent.Find(path);
                return target != null ? target.gameObject : null;
            }
            return GameObject.Find(path);
        }

        public static IEnumerator FindGameObject(string path, Action<GameObject> onFind,
            Action onFail = null, float retryInterval = 0.3f, int retryCount = 5)
        {
            for (int tryCount = 0; tryCount < retryCount; tryCount++)
            {
                var findObj = FindObj(path);
                if (findObj != null)
                {
                    onFind?.Invoke(findObj);
                    break;
                }

                if (tryCount == retryCount - 1)
                {
                    Debug.LogError("can not find gameObject :::\n" + path);
                    onFail?.Invoke();
                    yield break;
                }

                yield return new WaitForSeconds(retryInterval);
            }
        }
    }

