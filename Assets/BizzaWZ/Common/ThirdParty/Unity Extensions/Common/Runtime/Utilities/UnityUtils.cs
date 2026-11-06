using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace UnityExtensions
{
    /// <summary>
    /// Extensions for Unity.
    /// </summary>
    public static class UnityUtils
    {
        public static event Action fixedUpdate;
        public static event Action waitForFixedUpdate;
        public static event Action update;
        public static event Action lateUpdate;
        public static event Action waitForEndOfFrame;

        public static GameObject persistentGameObject { get; private set; }

        const string nullToString = "Null";


        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            persistentGameObject = new GameObject("Persistent");
            persistentGameObject.AddComponent<PersistentComponent>().hideFlags = HideFlags.HideInInspector;
            persistentGameObject.transform.ResetLocal();

            UObject.DontDestroyOnLoad(persistentGameObject);

#if !DEBUG
            persistentGameObject.hideFlags = HideFlags.HideInHierarchy;
#endif
        }


        public static void AddUpdate(UpdateMode mode, Action action)
        {
            switch (mode)
            {
                case UpdateMode.FixedUpdate: fixedUpdate += action; return;
                case UpdateMode.WaitForFixedUpdate: waitForFixedUpdate += action; return;
                case UpdateMode.Update: update += action; return;
                case UpdateMode.LateUpdate: lateUpdate += action; return;
                case UpdateMode.WaitForEndOfFrame: waitForEndOfFrame += action; return;
            }
        }


        public static void RemoveUpdate(UpdateMode mode, Action action)
        {
            switch (mode)
            {
                case UpdateMode.FixedUpdate: fixedUpdate -= action; return;
                case UpdateMode.WaitForFixedUpdate: waitForFixedUpdate -= action; return;
                case UpdateMode.Update: update -= action; return;
                case UpdateMode.LateUpdate: lateUpdate -= action; return;
                case UpdateMode.WaitForEndOfFrame: waitForEndOfFrame -= action; return;
            }
        }


        public static float GetDeltaTime(TimeMode timeMode)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return Editor.EditorUtils.deltaTime;
#endif
            return timeMode == TimeMode.Normal ? Time.deltaTime : Time.unscaledDeltaTime;
        }


        /// <summary>
        /// Destroy Unity Object in a safe way
        /// </summary>
        public static void DestroySafely(this UObject obj)
        {
            if (obj)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UObject.DestroyImmediate(obj);
                else
#endif
                    UObject.Destroy(obj);
            }
        }


        /// <summary>
        /// Find a loaded scene
        /// </summary>
        /// <returns> The loaded scene, return default(Scene) if no matched </returns>
        public static Scene FindScene(Predicate<Scene> match)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (match(scene)) return scene;
            }
            return default;
        }


        /// <summary>
        /// Find a loaded scene
        /// </summary>
        /// <returns> index of loaded scene (use SceneManager.GetSceneAt to get the scene), return -1 if no matched </returns>
        public static int FindSceneIndex(Predicate<Scene> match)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (match(scene)) return i;
            }
            return -1;
        }


        /// <summary>
        /// Get component if it exists or add a new one.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject target) where T : Component
        {
            if (!target.TryGetComponent(out T component))
            {
                component = target.AddComponent<T>();
            }
            return component;
        }
        
        public static Component GetOrAddComponent(this GameObject target, Type type)
        {
            if (!target.TryGetComponent(type, out var component))
            {
                component = target.AddComponent(type);
            }
            return component;
        }


        /// <summary>
        /// Get component if it exists or add a new one.
        /// </summary>
        public static T GetOrAddComponent<T>(this Component target) where T : Component
        {
            return target.gameObject.GetOrAddComponent<T>();
        }
        
        public static Component GetOrAddComponent(this Component target, Type type)
        {
            return target.gameObject.GetOrAddComponent(type);
        }


        public static bool TryGetComponentInParent<T>(this Component target, out T result)
        {
            while (!target.TryGetComponent(out result))
            {
                target = target.transform.parent;
                if (target == null) return false;
            }

            return true;
        }


        /// <summary>
        /// Get the RectTransform component.
        /// </summary>
        public static RectTransform GetRectTransform(this Component target)
        {
            return (RectTransform)target.transform;
        }


        /// <summary>
        /// Get the RectTransform component.
        /// </summary>
        public static RectTransform GetRectTransform(this GameObject target)
        {
            return (RectTransform)target.transform;
        }


        /// <summary>
        /// Set time scale and FixedUpdate frequency at sametime.
        /// </summary>
        public static void SetTimeScaleAndFixedUpdateFrequency(float timeScale, float fixedFrequency)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = timeScale / fixedFrequency;
        }


        public static string GetFullName(this Transform transform)
        {
            using (StringBuilderPool.global.Spawn(out var builder))
            {
                builder.Append(transform.gameObject.name);

                while (transform.parent)
                {
                    transform = transform.parent;
                    builder.Insert(0, '/');
                    builder.Insert(0, transform.gameObject.name);
                }

                builder.Insert(0, '/');
                builder.Insert(0, transform.gameObject.scene.name);

                return builder.ToString();
            }
        }


        public static string GetFullName(this GameObject gameObject)
        {
            return gameObject.transform.GetFullName();
        }


        /// <summary>
        /// Reset localPosition, localRotation and localScale of transform.
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }


        /// <summary>
        /// Set localPosition.x
        /// </summary>
        public static void SetLocalPositionX(this Transform transform, float x)
        {
            var pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }


        /// <summary>
        /// Set localPosition.y
        /// </summary>
        public static void SetLocalPositionY(this Transform transform, float y)
        {
            var pos = transform.localPosition;
            pos.y = y;
            transform.localPosition = pos;
        }


        /// <summary>
        /// Set localPosition.z
        /// </summary>
        public static void SetLocalPositionZ(this Transform transform, float z)
        {
            var pos = transform.localPosition;
            pos.z = z;
            transform.localPosition = pos;
        }


        /// <summary>
        /// Set localPosition.x & y
        /// </summary>
        public static void SetLocalPositionXY(this Transform transform, float x, float y)
        {
            var pos = transform.localPosition;
            pos.x = x;
            pos.y = y;
            transform.localPosition = pos;
        }


        /// <summary>
        /// Set anchoredPosition.x
        /// </summary>
        public static void SetAnchoredPositionX(this RectTransform rectTransform, float x)
        {
            var pos = rectTransform.anchoredPosition;
            pos.x = x;
            rectTransform.anchoredPosition = pos;
        }


        /// <summary>
        /// Set anchoredPosition.y
        /// </summary>
        public static void SetAnchoredPositionY(this RectTransform rectTransform, float y)
        {
            var pos = rectTransform.anchoredPosition;
            pos.y = y;
            rectTransform.anchoredPosition = pos;
        }


        /// <summary>
        /// Set sizeDelta.x
        /// </summary>
        public static void SetSizeDeltaX(this RectTransform rectTransform, float x)
        {
            var size = rectTransform.sizeDelta;
            size.x = x;
            rectTransform.sizeDelta = size;
        }


        /// <summary>
        /// Set sizeDelta.y
        /// </summary>
        public static void SetSizeDeltaY(this RectTransform rectTransform, float y)
        {
            var size = rectTransform.sizeDelta;
            size.y = y;
            rectTransform.sizeDelta = size;
        }


        /// <summary>
        /// Set anchorMin.x
        /// </summary>
        public static void SetAnchorMinX(this RectTransform rectTransform, float x)
        {
            var anchorMin = rectTransform.anchorMin;
            anchorMin.x = x;
            rectTransform.anchorMin = anchorMin;
        }


        /// <summary>
        /// Set anchorMin.y
        /// </summary>
        public static void SetAnchorMinY(this RectTransform rectTransform, float y)
        {
            var anchorMin = rectTransform.anchorMin;
            anchorMin.y = y;
            rectTransform.anchorMin = anchorMin;
        }


        /// <summary>
        /// Set anchorMax.x
        /// </summary>
        public static void SetAnchorMaxX(this RectTransform rectTransform, float x)
        {
            var anchorMax = rectTransform.anchorMax;
            anchorMax.x = x;
            rectTransform.anchorMax = anchorMax;
        }


        /// <summary>
        /// Set anchorMax.y
        /// </summary>
        public static void SetAnchorMaxY(this RectTransform rectTransform, float y)
        {
            var anchorMax = rectTransform.anchorMax;
            anchorMax.y = y;
            rectTransform.anchorMax = anchorMax;
        }


        /// <summary>
        /// Set pivot.x
        /// </summary>
        public static void SetPivotX(this RectTransform rectTransform, float x)
        {
            var pivot = rectTransform.pivot;
            pivot.x = x;
            rectTransform.pivot = pivot;
        }


        /// <summary>
        /// Set pivot.y
        /// </summary>
        public static void SetPivotY(this RectTransform rectTransform, float y)
        {
            var pivot = rectTransform.pivot;
            pivot.y = y;
            rectTransform.pivot = pivot;
        }


        /// <summary>
        /// Traverse transform tree (root node first).
        /// </summary>
        /// <param name="root"> The root node of transform tree. </param>
        /// <param name="operate"> A custom operation on every transform node. </param>
        /// <param name="depthLimit"> Negative value means no limit, zero means root only, positive value means maximum children depth </param>
        public static void TraverseHierarchy(this Transform root, Action<Transform> operate, int depthLimit = -1)
        {
            operate(root);

            if (depthLimit != 0)
            {
                int count = root.childCount;
                for (int i = 0; i < count; i++)
                {
                    TraverseHierarchy(root.GetChild(i), operate, depthLimit - 1);
                }
            }
        }


        /// <summary>
        /// Traverse transform tree (leaf node first).
        /// </summary>
        /// <param name="root"> The root node of transform tree. </param>
        /// <param name="operate"> A custom operation on every transform node. </param>
        /// <param name="depthLimit"> Negative value means no limit, zero means root only, positive value means maximum children depth </param>
        public static void InverseTraverseHierarchy(this Transform root, Action<Transform> operate, int depthLimit = -1)
        {
            if (depthLimit != 0)
            {
                int count = root.childCount;
                for (int i = 0; i < count; i++)
                {
                    InverseTraverseHierarchy(root.GetChild(i), operate, depthLimit - 1);
                }
            }

            operate(root);
        }


        /// <summary>
        /// Find a transform in the transform tree (root node first)
        /// </summary>
        /// <param name="root"> The root node of transform tree. </param>
        /// <param name="match"> match function. </param>
        /// <param name="depthLimit"> Negative value means no limit, zero means root only, positive value means maximum children depth </param>
        /// <returns> The matched node or null if no matched. </returns>
        public static Transform SearchHierarchy(this Transform root, Predicate<Transform> match, int depthLimit = -1)
        {
            if (match(root)) return root;
            if (depthLimit == 0) return null;

            int count = root.childCount;
            Transform result = null;

            for (int i = 0; i < count; i++)
            {
                result = SearchHierarchy(root.GetChild(i), match, depthLimit - 1);
                if (result) break;
            }

            return result;
        }


        public static TransformDescendantsEnumerable Descendants(this Transform root) => new TransformDescendantsEnumerable(root);


        public static Rect GetWorldRect(this RectTransform rectTransform)
        {
            var rect = rectTransform.rect;
            rect.min = rectTransform.TransformPoint(rect.min);
            rect.max = rectTransform.TransformPoint(rect.max);
            return rect;
        }


        public static Rect Encapsulate(this Rect rect, Vector2 point)
        {
            if (rect.xMin > point.x) rect.xMin = point.x;
            if (rect.xMax < point.x) rect.xMax = point.x;
            if (rect.yMin > point.y) rect.yMin = point.y;
            if (rect.yMax < point.y) rect.yMax = point.y;
            return rect;
        }


        public static Rect Extend(this Rect rect, float delta)
        {
            return new Rect(rect.x - delta, rect.y - delta, rect.width + delta + delta, rect.height + delta + delta);
        }


        /// <summary>
        /// Get the overlapped rect.
        /// </summary>
        public static Rect GetIntersection(this Rect rect, Rect other)
        {
            if (rect.xMin > other.xMin) other.xMin = rect.xMin;
            if (rect.xMax < other.xMax) other.xMax = rect.xMax;
            if (rect.yMin > other.yMin) other.yMin = rect.yMin;
            if (rect.yMax < other.yMax) other.yMax = rect.yMax;
            return other;
        }


        public static Vector2 GetXY(this Vector3 v) => new Vector2(v.x, v.y);
        public static Vector2 GetYZ(this Vector3 v) => new Vector2(v.y, v.z);
        public static Vector2 GetXZ(this Vector3 v) => new Vector2(v.x, v.z);
        
        public static void SetXY(ref this Vector3 v, Vector2 xy)
        {
            v.x = xy.x;
            v.y = xy.y;
        }

        public static void SetYZ(ref this Vector3 v, Vector2 yz)
        {
            v.y = yz.x;
            v.z = yz.y;
        }

        public static void SetXZ(ref this Vector3 v, Vector2 xz)
        {
            v.x = xz.x;
            v.z = xz.y;
        }


        /// <summary>
        /// Clone the AnimationCurve instance.
        /// </summary>
        public static AnimationCurve Clone(this AnimationCurve target)
        {
            var newCurve = new AnimationCurve(target.keys);
            newCurve.postWrapMode = target.postWrapMode;
            newCurve.preWrapMode = target.preWrapMode;

            return newCurve;
        }


        /// <summary>
        /// Clone the Gradient instance.
        /// </summary>
        public static Gradient Clone(this Gradient target)
        {
            var newGradient = new Gradient();
            newGradient.alphaKeys = target.alphaKeys;
            newGradient.colorKeys = target.colorKeys;
            newGradient.mode = target.mode;

            return newGradient;
        }


        public static float ScreenToWorldSize(this Camera camera, float pixelSize, float clipPlane = 0f)
        {
            if (camera.orthographic)
            {
                return pixelSize * camera.orthographicSize * 2f / camera.pixelHeight;
            }
            else
            {
                return pixelSize * clipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / camera.pixelHeight;
            }
        }


        public static float WorldToScreenSize(this Camera camera, float worldSize, float clipPlane = 0f)
        {
            if (camera.orthographic)
            {
                return worldSize * camera.pixelHeight * 0.5f / camera.orthographicSize;
            }
            else
            {
                return worldSize * camera.pixelHeight * 0.5f / (clipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            }
        }


        public static Vector4 GetClipPlane(this Camera camera, Vector3 point, Vector3 normal)
        {
            Matrix4x4 wtoc = camera.worldToCameraMatrix;
            point = wtoc.MultiplyPoint(point);
            normal = wtoc.MultiplyVector(normal).normalized;

            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal));
        }


        /// <summary>
        /// Calculate ZBufferParams, can used in compute shader 
        /// </summary>
        public static Vector4 GetZBufferParams(this Camera camera)
        {
            double f = camera.farClipPlane;
            double n = camera.nearClipPlane;

            double rn = 1f / n;
            double rf = 1f / f;
            double fpn = f / n;

            return SystemInfo.usesReversedZBuffer
                ? new Vector4((float)(fpn - 1.0), 1f, (float)(rn - rf), (float)rf)
                : new Vector4((float)(1.0 - fpn), (float)fpn, (float)(rf - rn), (float)rn);
        }


        public static void AddListener(this EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            var triggers = eventTrigger.triggers;
            var index = triggers.FindIndex(entry => entry.eventID == type);
            if (index < 0)
            {
                var entry = new EventTrigger.Entry();
                entry.eventID = type;
                entry.callback.AddListener(callback);
                triggers.Add(entry);
            }
            else
            {
                triggers[index].callback.AddListener(callback);
            }
        }


        public static void RemoveListener(this EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            var triggers = eventTrigger.triggers;
            var index = triggers.FindIndex(entry => entry.eventID == type);
            if (index >= 0)
            {
                triggers[index].callback.RemoveListener(callback);
            }
        }


        public static void Log<T>(this T obj) => Debug.Log(obj is null ? nullToString : obj.ToString());
        public static void Log<T>(this T obj, UObject context) => Debug.Log(obj is null ? nullToString : obj.ToString(), context);
        public static void LogWarning<T>(this T obj) => Debug.LogWarning(obj is null ? nullToString : obj.ToString());
        public static void LogWarning<T>(this T obj, UObject context) => Debug.LogWarning(obj is null ? nullToString : obj.ToString(), context);
        public static void LogError<T>(this T obj) => Debug.LogError(obj is null ? nullToString : obj.ToString());
        public static void LogError<T>(this T obj, UObject context) => Debug.LogError(obj is null ? nullToString : obj.ToString(), context);


        public struct TransformDescendantsEnumerable
        {
            Transform _root;
            public TransformDescendantsEnumerable(Transform root) => _root = root;
            public Enumerator GetEnumerator() => new Enumerator(_root);

            public struct Enumerator
            {
                Transform _root;
                Transform _currnet;
                int _index;

                public Enumerator(Transform root)
                {
                    _root = root;
                    _currnet = null;
                    _index = -1;
                }

                public Transform Current => _currnet;

                public bool MoveNext()
                {
                    if (_currnet)
                    {
                        if (_currnet.childCount > 0)
                        {
                            _currnet = _currnet.GetChild(_index = 0);
                            return true;
                        }
                        else
                        {
                            while (_currnet != _root)
                            {
                                _index++;
                                if (_index < _currnet.parent.childCount)
                                {
                                    _currnet = _currnet.parent.GetChild(_index);
                                    return true;
                                }
                                else
                                {
                                    _currnet = _currnet.parent;
                                    _index = _currnet.GetSiblingIndex();
                                }
                            }

                            _currnet = null;
                            _index = -1;
                            return false;
                        }
                    }
                    else
                    {
                        _currnet = _root;
                        _index = 0;
                        return true;
                    }
                }

                public void Reset()
                {
                    _currnet = null;
                    _index = -1;
                }
            }
        }


        public class PersistentComponent : ScriptableComponent
        {
            void Start()
            {
                StartCoroutine(WaitForFixedUpdate());
                StartCoroutine(WaitForEndOfFrame());
            }

            static IEnumerator WaitForFixedUpdate()
            {
                var wait = new WaitForFixedUpdate();
                while (true)
                {
                    yield return wait;
                    waitForFixedUpdate?.Invoke();
                }
            }

            static IEnumerator WaitForEndOfFrame()
            {
                var wait = new WaitForEndOfFrame();
                while (true)
                {
                    yield return wait;
                    waitForEndOfFrame?.Invoke();
                }
            }

            void FixedUpdate()
            {
                fixedUpdate?.Invoke();
            }

            void Update()
            {
                update?.Invoke();
            }

            void LateUpdate()
            {
                lateUpdate?.Invoke();
            }

        } // class GlobalComponent

    } // class Extensions

} // namespace UnityExtensions