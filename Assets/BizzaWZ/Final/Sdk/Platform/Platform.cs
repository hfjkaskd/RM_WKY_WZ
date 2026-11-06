#if BIZZA_REAL_WITHDRAW
using UnityEngine;

namespace Bizza.Sdk
{
    public abstract class InstanceClass<T> where T : InstanceClass<T>
    {
        public static T Instance { get; private set; }

        public InstanceClass()
        {
            Instance = this as T;
        }
    }


}
#endif
