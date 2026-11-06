using System;

    public abstract class SingletonCSharp<T>
        where T : class, new()
    {
        private static T m_Instance;
        private static readonly object m_LockHelper = new object();

        public static T InsanceNonAlloc
        {
            get
            {
                return m_Instance;
            }
        }

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (m_LockHelper)
                    {
                        if (m_Instance == null)
                        {
                            m_Instance = new T();
                        }
                    }
                }
                return m_Instance;
            }
        }

        protected SingletonCSharp()
        {
            if (m_Instance != null)
            {
                throw new InvalidOperationException("Can't create singleton instance more than once.");
            }
        }
    }
