using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static partial class FrameEvent
{
    public static partial class Android
    {
        public static readonly GameEvent<JObject> UnityJavaStringMessage = new();
        
        public static readonly GameEvent<int> UnityJavaObjectRootMessage = new();
        
        public static readonly GameEvent<int> UnityJavaObjectSimCardMessage = new();
        
        public static readonly GameEvent<string> UnityJavaObjectGoogleMessage = new();
    }
}

namespace Bizza.Unity.Android
{
     
    public class AndroidJavaMessageDispatcher : MonoBehaviour
    {
        public const string MessageName = nameof(MessageName);
        private static Action<int, string> httpMessageHandler;

        /// <summary>
        /// 由宿主注入 HTTP 回调处理器，公共设备模块不直接依赖 HttpUtil。
        /// SDK 可注册 HttpUtil.LoginHTTPDict。
        /// </summary>
        public static void SetHttpMessageHandler(Action<int, string> handler)
        {
            httpMessageHandler = handler ??
                                 throw new ArgumentNullException(nameof(handler));
        }

        public static void ClearHttpMessageHandler()
        {
            httpMessageHandler = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            httpMessageHandler = null;
        }

        public static void EnsureCreated()
        {
            var obj = GameObject.Find(nameof(AndroidJavaMessageDispatcher));

            if (obj == null)
                obj = new GameObject(nameof(AndroidJavaMessageDispatcher));

            if (obj.GetComponent<AndroidJavaMessageDispatcher>() == null)
                obj.AddComponent<AndroidJavaMessageDispatcher>();
        }
        
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            gameObject.name = nameof(AndroidJavaMessageDispatcher);
        }

        public void DispatchMessage(string args)
        {
            JObject json = JObject.Parse(args);
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "OnUnityJavaStringMessage:::" + json);
            BizzaEventSystem.Emit(FrameEvent.Android.UnityJavaStringMessage, json);
        }

        public void DispatchRootMessage(string args)
        {
            JObject json = JObject.Parse(args);
            string message = (string)json[AndroidJavaMessageDispatcher.MessageName];
            int.TryParse(message, out int rootId);
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "Root权限 Java 回调:::" + json + " rootId:" + rootId);
            BizzaEventSystem.Emit(FrameEvent.Android.UnityJavaObjectRootMessage, rootId);
        }
        
        public void DispatchsimCardMessage(string args)
        {
            JObject json = JObject.Parse(args);
            string message = (string)json[AndroidJavaMessageDispatcher.MessageName];
            int.TryParse(message, out int rootId);
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "OnUnityJavaStringMessage:::" + json + " simCard:" + rootId);
            BizzaEventSystem.Emit(FrameEvent.Android.UnityJavaObjectSimCardMessage, rootId);
        }
        
        public void DispatchGoogleIdMessage(string args)
        {
            JObject json = JObject.Parse(args);
            string message = (string)json[AndroidJavaMessageDispatcher.MessageName];
            
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "谷歌ID Java 回调，GoogleId 可用:" + !string.IsNullOrEmpty(message));
            BizzaEventSystem.Emit(FrameEvent.Android.UnityJavaObjectGoogleMessage, message);
        }

        public void DispatchHTTPMessage(string args)
        {
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Http, "Java方法回调 -- " + DateTime.Now + "call" + args);
            JObject json = JObject.Parse(args);

            // id
            int id = json["id"]?.Value<int>() ?? -1;
            if (id < 0)
            {
                return;
            }

            // data（Java 侧 success / error 都放在 data）
            string data = json["data"]?.ToString();

            // 写回等待中的请求
            Action<int, string> handler = httpMessageHandler;
            if (handler == null)
            {
                Debug.LogWarning(
                    $"Android HTTP 回调未注册处理器，id={id}");
                return;
            }

            try
            {
                handler.Invoke(id, data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Android HTTP 回调处理失败，id={id}：{exception.Message}");
            }
        }
        
    }
}
