#if BIZZA_REAL_WITHDRAW
namespace Bizza.Sdk
{
    public abstract class PlayerPrefsSystem
    {
        /// <summary>
        /// 同步读取字符串
        /// </summary>
        /// <param name="str"></param>
        public abstract string GetString(string key);

        public abstract bool HasKey(string key);

        /// <summary>
        /// 同步保存字符串
        /// </summary>
        /// <param name="str"></param>
        public abstract void SetString(string key, string str);
        
        public abstract void DeleteKey(string key);
    }
    
    public class UnityPlayerPrefs : PlayerPrefsSystem
    {
        public override string GetString(string key)
        {
            return UnityEngine.PlayerPrefs.HasKey(key)
                ? UnityEngine.PlayerPrefs.GetString(key)
                : string.Empty;
        }

        public override bool HasKey(string key)
        {
            return UnityEngine.PlayerPrefs.HasKey(key);
        }

        public override void SetString(string key, string str)
        {
            UnityEngine.PlayerPrefs.SetString(key, str);
        }
        
        public override void DeleteKey(string key)
        {
            UnityEngine.PlayerPrefs.DeleteKey(key);
        }
    }
    
#if Bizza_Platform_P8wx || Bizza_Platform_WeChat || Bizza_Platform_KuaiShou || Bizza_Platform_ByteNative
    public class WxKsPlayerPrefs : PlayerPrefsSystem
    {
        public override string GetString(string key)
        {
            return PlayerPrefs.HasKey(key)
                ? PlayerPrefs.GetString(key)
                : string.Empty;
        }
        
        public override void SetString(string key, string str)
        {
            PlayerPrefs.SetString(key, str);
        }
        
                
        public override void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
#endif
}
#endif
