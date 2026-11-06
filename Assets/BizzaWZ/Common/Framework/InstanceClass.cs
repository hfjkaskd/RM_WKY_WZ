using UnityEngine;

    public abstract class InstanceClass<T> where T : InstanceClass<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                LogLogger.LogAssert(_instance != null, $"please create a {TypeUtil.GetName<T>()} instance use!!!");
                return _instance;
            }
        }

        protected InstanceClass()
        {
            _instance = this as T;
        }

        public virtual void OnRelease()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }

    public abstract class SimpleInstanceClass<T> where T : new()
    {
        public static readonly T Instance = new();
    }

    public abstract class InstanceMono<T> : MonoBehaviour where T : InstanceMono<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                LogLogger.LogAssert(_instance != null, $"please create a {TypeUtil.GetName<T>()} instance mono use!!!");
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
