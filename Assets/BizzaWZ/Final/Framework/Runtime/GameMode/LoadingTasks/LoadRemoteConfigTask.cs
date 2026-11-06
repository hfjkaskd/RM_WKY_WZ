#if BIZZA_REAL_WITHDRAW
// using System;
// using Bizza;
// using Cysharp.Threading.Tasks;
//
// namespace Bizza.Loading
// {
//     [Obfuz.ObfuzIgnore]
//     /// <summary>
//     /// 加载远程配置任务
//     /// </summary>
//     public class LoadRemoteConfigTask : LoadingTaskBase
//     {
//         public override LoadingTaskName TaskName => LoadingTaskName.LoadRemoteConfig;
//         public override float Weight => 0.2f;
//         public override LoadingTaskName[] Dependencies => new LoadingTaskName[]
//         {
//             // LoadingTaskName.CheckNetwork,
//         };
//
//         public override async UniTask Execute()
//         {
//             LogLogger.LogInfo($"{TaskName} start");
//             SetProgress(0.9f);
//
// #if UNITY_EDITOR
//             RemoteConfigModule.reviewMode = false;
//             if (UnityEditor.EditorPrefs.HasKey("Editor_ReviewMode"))
//             {
//                 RemoteConfigModule.reviewMode = UnityEditor.EditorPrefs.GetBool("Editor_ReviewMode");
//             }
//             OnRemoteConfigOver(RemoteConfigModule.reviewMode);
//             return;
// #endif
//
//             #if UNITY_ANDROID
//             RemoteConfigModule.reviewMode = false;
//             #else
//             await RemoteConfigModule.Instance.LoadRemoteConfig();
//             #endif
//
//             OnRemoteConfigOver(RemoteConfigModule.reviewMode);
//
//             SetProgress(1f);
//             LogLogger.LogInfo($"{TaskName} end");
//         }
//
//         private void OnRemoteConfigOver(bool reviewMode)
//         {
//         }
//
//         private void SaveGroup(int userGroup)
//         {
//             // var save = SaveDataUtil.channelStrategy;
//             // save.Data.UserGroup = userGroup;
//             // save.SaveData();
//         }
//     }
// }
//
#endif
