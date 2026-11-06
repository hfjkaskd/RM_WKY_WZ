#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// ScriptableAssetSingleton
    /// </summary>
    public class ScriptableAssetSingleton<T> : ScriptableAsset where T : ScriptableAssetSingleton<T>
    {
        static T _instance;

        /// <summary>
        /// The asset instance.
        /// </summary>
        public static T instance
        {
            get
            {
                if (!_instance)
                {
#if UNITY_EDITOR
                    _instance = Editor.AssetUtils.FindAsset<T>();
                    if (!_instance)
#endif
                        _instance = CreateInstance<T>();
                }
                return _instance;
            }
        }

        protected ScriptableAssetSingleton()
        {
            _instance = this as T;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Create asset if it does not exist, or just select it if it exist.
        /// </summary>
        protected static void CreateOrSelectAsset()
        {
            if (!_instance)
            {
                _instance = Editor.AssetUtils.FindAsset<T>();
                if (!_instance)
                    _instance = CreateInstance<T>();
            }

            if(!AssetDatabase.IsNativeAsset(_instance))
                Editor.AssetUtils.CreateAsset(_instance);

            Selection.activeObject = _instance;
        }

#endif

    } // class ScriptableAssetSingleton

} // namespace UnityExtensions