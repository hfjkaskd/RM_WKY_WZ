#if BIZZA_REAL_WITHDRAW
// using System;
// using System.Collections.Generic;
// using cfg;
//

using System;
using System.Collections.Generic;

public partial class MenuSys_Task
{
//     #region Util
//
     private static BaseRuntimeTaskUpdater GetRuntimeUpdaterByType(E_AllTaskType taskType, bool recordTogether)
     {

         // if (!recordTogether)
             // return CommonAdditiveTaskUpdater.Instance;
        switch (taskType)
        {
            case E_AllTaskType.DailyLogin:
                return LoginDaysTaskUpdater.Instance;
            case E_AllTaskType.FinishLevel:
                return FinishLevelTaskUpdater.Instance;
            case E_AllTaskType.WatchAd:
                return AdUpdater.Instance;
            case E_AllTaskType.OnlineTime:
                return DailyPlaytimeUpdater.Instance;
        
         default:
             LogLogger.LogError($"unhandle type {taskType}");
                return null;
         //        return CommonAdditiveTaskUpdater.Instance;
         // return CommonAdditiveTaskUpdater.Instance;
        }
     }
//
//     #endregion

     #region Base Updater

     public abstract class BaseRuntimeTaskUpdater
     {
         protected BaseRuntimeTaskUpdater()
         {
         }

         public abstract void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num);
     }

     private abstract class BaseRuntimeTaskUpdaterSingleton<T> : BaseRuntimeTaskUpdater
         where T : BaseRuntimeTaskUpdater, new()
     {
         protected static T m_Instance;

         public static T Instance
         {
             get
             {
                 if (m_Instance == null)
                 {
                     m_Instance = new T();
                 }

                 return m_Instance;
             }
         }
     }

     private abstract class BaseRuntimeTaskUpdaterMultiEnum<T, TEnum> : BaseRuntimeTaskUpdater
         where T : BaseRuntimeTaskUpdaterMultiEnum<T, TEnum>, new()
         where TEnum : Enum
     {
         static Dictionary<TEnum, BaseRuntimeTaskUpdater>
             m_instanceDic = new Dictionary<TEnum, BaseRuntimeTaskUpdater>();

         protected TEnum Key;

         public static BaseRuntimeTaskUpdater GetInstance(TEnum key)
         {
             BaseRuntimeTaskUpdater instance = default;
             if (!m_instanceDic.TryGetValue(key, out instance))
             {
                 T tInstance = new T();
                 tInstance.Key = key;
                 m_instanceDic.Add(key, tInstance);
                 instance = tInstance;
             }

             return instance;
         }
     }

     #endregion

     #region Updater
//
//     //自动完成更新器
//     private class AutoFinishTaskUpdater : BaseRuntimeTaskUpdaterSingleton<AutoFinishTaskUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             runtimeTaskInfo.CurrentProgress = runtimeTaskInfo.TargetProgress;
//         }
//     }
//
     //自动完成更新器
     private class LoginDaysTaskUpdater : BaseRuntimeTaskUpdaterSingleton<LoginDaysTaskUpdater>
     {
         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
         {
             var data = SaveDataUtils.GameData;
             runtimeTaskInfo.CurrentProgress = data.totalLoginDay;
         }
     }
//
//     /// <summary>
//     /// 通用累加器，会独立记录任务进度: TaskRecord
//     /// </summary>
//     private class CommonAdditiveTaskUpdater : BaseRuntimeTaskUpdaterSingleton<CommonAdditiveTaskUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             runtimeTaskInfo.CurrentProgress += num;
//             runtimeTaskInfo.Record();
//         }
//     }
//
     private class FinishLevelTaskUpdater : BaseRuntimeTaskUpdaterSingleton<FinishLevelTaskUpdater>
     {
         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
         {
             if (SaveDataUtils.TaskData == null)
                 return;

             if (SaveDataUtils.TaskData.taskRecordDataDic.TryGetValue("EnterBattleCount",
                     out TaskRecordData taskRecordData))
             {
                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
             }
         }
     }
//
//     private class ItemAmountUpdater : BaseRuntimeTaskUpdaterMultiEnum<ItemAmountUpdater, E_ItemType>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             runtimeTaskInfo.CurrentProgress = ItemUtils.GetAccountItemCount(Key);
//         }
//     }
//
//
    private class FinishBingoUpdater : BaseRuntimeTaskUpdaterSingleton<FinishBingoUpdater>
    {
        public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
        {
            if (SaveDataUtils.TaskData == null)
                return;
            if (SaveDataUtils.TaskData.taskRecordDataDic.TryGetValue("FinishBingo",
                    out TaskRecordData taskRecordData))
            {
                runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
            }
        }
    }

     private class AdUpdater : BaseRuntimeTaskUpdaterSingleton<AdUpdater>
     {
         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
         {
             if (SaveDataUtils.TaskData == null)
                 return;
             if (SaveDataUtils.TaskData.taskRecordDataDic.TryGetValue("DailyAdCount",
                     out TaskRecordData taskRecordData))
             {
                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
             }
         }
     }
//
     private class DailyPlaytimeUpdater : BaseRuntimeTaskUpdaterSingleton<DailyPlaytimeUpdater>
     {
         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
         {
             if (SaveDataUtils.TaskData == null)
                 return;
             if (SaveDataUtils.TaskData.taskRecordDataDic.TryGetValue("DailyPlaytime",
                     out TaskRecordData taskRecordData))
             {
                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
             }
         }
     }
//
//     private class KillMonsterUpdater : BaseRuntimeTaskUpdaterSingleton<KillMonsterUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             if (SaveDataModule.Data.taskSaveData == null)
//                 return;
//             if (SaveDataModule.Data.taskSaveData.taskRecordDataDic.TryGetValue("KillMonster",
//                     out TaskRecordData taskRecordData))
//             {
//                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
//             }
//         }
//     }
//
//     private class TalentLevelToUpdater : BaseRuntimeTaskUpdaterSingleton<TalentLevelToUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             // runtimeTaskInfo.CurrentProgress = GameDataModule.Instance.PlayerData.talentData.TalentLV;
//         }
//     }
//
//     private class EquipAmountUpdater : BaseRuntimeTaskUpdaterSingleton<EquipAmountUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             // int amount = 0;
//             // var wearingEquips = GameDataModule.Instance.PlayerData.inventoryData.wearingEquips;
//             // for (int i = 0; i < wearingEquips.Length; i++)
//             // {
//             //     if (string.IsNullOrEmpty(wearingEquips[i])) continue;
//             //     amount++;
//             // }
//
//             // runtimeTaskInfo.CurrentProgress = amount;
//         }
//     }
//
//     private class UnlockPlaneUpdater : BaseRuntimeTaskUpdaterSingleton<UnlockPlaneUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             // if (runtimeTaskInfo is RuntimeMainTaskInfo mainTaskInfo) //TODO 考虑一下如何优化？？？
//             // {
//             //     var roleList = GameDataModule.Instance.PlayerData.roleData.RoleList;
//             //     for (int i = 0; i < roleList.Count; i++)
//             //     {
//             //         if (roleList[i].IsUnLocked && mainTaskInfo.config.ConditionParamsStr == roleList[i].ResID)
//             //         {
//             //             runtimeTaskInfo.CurrentProgress = runtimeTaskInfo.TargetProgress;
//             //         }
//             //     }
//             // }
//         }
//     }
//
//     private class FinishLevelUpdater : BaseRuntimeTaskUpdaterSingleton<FinishLevelUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             // if (runtimeTaskInfo is RuntimeMainTaskInfo mainTaskInfo) //TODO 考虑一下如何优化？？？
//             // {
//             //     int lastChapter = GameDataModule.Instance.PlayerData.lastChapter;
//             //     int lastWave = GameDataModule.Instance.PlayerData.recordLastWaveIdx;
//             //     if (mainTaskInfo.config.ConditionParamsInt.IntValue1 < lastChapter ||
//             //         (mainTaskInfo.config.ConditionParamsInt.IntValue1 == lastChapter &&
//             //          mainTaskInfo.config.ConditionParamsInt.IntValue2 <= lastWave))
//             //     {
//             //         runtimeTaskInfo.CurrentProgress = runtimeTaskInfo.TargetProgress;
//             //     }
//             // }
//         }
//     }
//
//     /// <summary>
//     /// 完成关卡
//     /// </summary>
//     private class LevelFinishUpdater : BaseRuntimeTaskUpdaterSingleton<LevelFinishUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             if (SaveDataModule.Data.taskSaveData == null)
//                 return;
//             if (SaveDataModule.Data.taskSaveData.taskRecordDataDic.TryGetValue("LevelFinish",
//                     out TaskRecordData taskRecordData))
//             {
//                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
//             }
//         }
//     }
//
//     /// <summary>
//     /// 刷新每日商店
//     /// </summary>
//     private class RefreshDailyShopUpdater : BaseRuntimeTaskUpdaterSingleton<RefreshDailyShopUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             if (SaveDataModule.Data.taskSaveData == null)
//                 return;
//             if (SaveDataModule.Data.taskSaveData.taskRecordDataDic.TryGetValue("RefreshDailyShop",
//                     out TaskRecordData taskRecordData))
//             {
//                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
//             }
//         }
//     }
//
//     /// <summary>
//     /// 打开防御塔宝箱
//     /// </summary>
//     private class OpenTowerBoxUpdater : BaseRuntimeTaskUpdaterSingleton<OpenTowerBoxUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             if (SaveDataModule.Data.taskSaveData == null)
//                 return;
//             if (SaveDataModule.Data.taskSaveData.taskRecordDataDic.TryGetValue("OpenTowerBox",
//                     out TaskRecordData taskRecordData))
//             {
//                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
//             }
//         }
//     }
//
//     /// <summary>
//     /// 合成四阶
//     /// </summary>
//     private class MergeFourthTowerUpdater : BaseRuntimeTaskUpdaterSingleton<MergeFourthTowerUpdater>
//     {
//         public override void UpdateProgress(BaseRuntimeTaskInfo runtimeTaskInfo, int num)
//         {
//             if (SaveDataModule.Data.taskSaveData == null)
//                 return;
//             if (SaveDataModule.Data.taskSaveData.taskRecordDataDic.TryGetValue("MergeFourth",
//                     out TaskRecordData taskRecordData))
//             {
//                 runtimeTaskInfo.CurrentProgress = taskRecordData.CurrentProgress;
//             }
//         }
//     }
//
     #endregion
}
#endif
