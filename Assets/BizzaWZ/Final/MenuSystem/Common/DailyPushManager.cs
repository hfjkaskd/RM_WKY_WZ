// #if BIZZA_REAL_WITHDRAW
// using UnityEngine;
// using System;
// using System.Globalization;
// using System.IO;
// using System.Collections.Generic;
// using UnityEngine.UI;
// using System.Collections;

// #if UNITY_IOS && !UNITY_EDITOR
// using Unity.Notifications.iOS;
// #endif

// #if UNITY_ANDROID && !UNITY_EDITOR
// using Unity.Notifications.Android;
// using UnityEngine.Networking;
// #endif

// public class DailyPushManager : MonoBehaviour
// {
//     [System.Serializable]
//     public class PushContent
//     {
//         public string titleKey;
//         public string bodyKey;

//         [Tooltip("���ó� \"14:00\" ֮�࣬����ʱ��")]
//         public string timeString;

//         [Tooltip("ͼƬ�ļ���������·������ͼƬ������� Assets/StreamingAssets/NotificationImages �ļ���")]
//         public string imageFileName;
//     }

//     [Header("��������")]
//     public PushContent[] pushContents;

//     [Header("���԰�ť")]
//     public Button testButton;

// #if UNITY_ANDROID && !UNITY_EDITOR
//     private const string ANDROID_CHANNEL_ID = "daily_reminder_channel_v2";
//     private const string ANDROID_CHANNEL_NAME = "Daily Reminder V2";
//     private const string ANDROID_DAILY_NOTIFICATION_COUNT_KEY = "daily_push_android_count";
//     private const string ANDROID_DAILY_NOTIFICATION_ID_KEY_PREFIX = "daily_push_android_id_";
//     private const string ANDROID_DAILY_NOTIFICATION_FIRE_TIME_KEY_PREFIX = "daily_push_android_fire_";
//     private const string ANDROID_TEST_NOTIFICATION_ID_KEY = "daily_push_android_test_id";
//     private const string ANDROID_TEST_NOTIFICATION_FIRE_TIME_KEY = "daily_push_android_test_fire";
// #endif

//     public static DailyPushManager Instance { get; private set; }
//     private void Awake()
//     {
//         Instance = this;
//     }
//     private void Start()
//     {
// #if UNITY_ANDROID && !UNITY_EDITOR
//         AndroidNotificationCenter.OnNotificationReceived += OnAndroidNotificationReceived;
//         LogSavedAndroidNotificationStatuses();
// #endif

//         // �󶨲��԰�ť
//         if (testButton != null)
//             testButton.onClick.AddListener(() => TestPush(1));

//         // ����Ȩ�޲���������
//         StartCoroutine(RequestNotificationPermissionCoroutine());
//     }

//     private void OnDestroy()
//     {
// #if UNITY_ANDROID && !UNITY_EDITOR
//         AndroidNotificationCenter.OnNotificationReceived -= OnAndroidNotificationReceived;
// #endif
//     }

//     /// <summary>
//     /// ����֪ͨȨ�ޣ�iOS / Android(13+)����Ȩ������ÿ������
//     /// </summary>
//     IEnumerator RequestNotificationPermissionCoroutine()
//     {
//         while (!LanguageUtils.HasReadLanguage)
//         {
//             yield return new WaitForSeconds(0.2f);
//         }

// #if UNITY_IOS && !UNITY_EDITOR
//         Debug.Log("��ʼ���� iOS ֪ͨȨ��...");

//         // registerForRemoteNotifications: ֻҪ���������� false
//         using (var req = new AuthorizationRequest(
//                    AuthorizationOption.Alert |
//                    AuthorizationOption.Badge |
//                    AuthorizationOption.Sound,
//                    false))
//         {
//             while (!req.IsFinished)
//                 yield return null;

//             Debug.Log($"֪ͨȨ���������, Granted = {req.Granted}, Error = {req.Error}");

//             if (req.Granted)
//                 SetupDailyNotifications();
//             else
//                 Debug.LogWarning("�û�δ��Ȩ֪ͨȨ�ޣ��������Ͳ�����ʾ��");
//         }

// #elif UNITY_ANDROID && !UNITY_EDITOR
//         Debug.Log("��ʼ���� Android ֪ͨȨ��...");

//         // Android 8+ ��Ҫ Channel���Ͱ汾Ҳ����ע�ᣨ����ģ�⣩
//         RegisterAndroidChannelIfNeeded();

//         // Android 13+ ��Ҫ POST_NOTIFICATIONS ����ʱȨ�ޣ�Mobile Notifications �ṩ PermissionRequest
//         var request = new PermissionRequest();
//         while (request.Status == PermissionStatus.RequestPending)
//             yield return null;

//         Debug.Log($"Android ֪ͨȨ��״̬: {request.Status}");

//         if (request.Status == PermissionStatus.Allowed)
//         {
//             // ��Ҫ����ͼƬ��StreamingAssets -> �ɶ�дĿ¼������Э�̸���
//             yield return SetupDailyNotificationsAndroidCoroutine();
//         }
//         else
//         {
//             Debug.LogWarning("�û�δ��Ȩ֪ͨȨ�ޣ�֪ͨ���ܲ�����ʾ��֪ͨ����");
//         }
// #else
//         yield return null;
// #endif
//     }

//     /// <summary>
//     /// iOS������������ÿ�չ̶�ʱ��ı������ͣ�����ʱ����
//     /// </summary>
//     public void SetupDailyNotifications()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         iOSNotificationCenter.RemoveAllScheduledNotifications();

//         for (int i = 0; i < pushContents.Length; i++)
//         {
//             var content = pushContents[i];
//             if (TryParseLocalTime(content.timeString, out TimeSpan triggerTime))
//             {
//                 CreateDailyNotification(content, triggerTime, i);
//             }
//             else
//             {
//                 Debug.LogWarning($"ʱ�����ʧ��: {content.timeString}");
//             }
//         }

//         Debug.Log($"������ {pushContents.Length} ��ÿ�ձ������� (iOS)");
// #endif
//     }

// #if UNITY_IOS && !UNITY_EDITOR
//     /// <summary>
//     /// iOS������һ��ÿ���ڹ̶�����ʱ�䴥����֪ͨ
//     /// </summary>
//     void CreateDailyNotification(PushContent content, TimeSpan triggerTime, int id)
//     {
//         var trigger = new iOSNotificationCalendarTrigger()
//         {
//             Hour = triggerTime.Hours,
//             Minute = triggerTime.Minutes,
//             Second = 0,
//             Repeats = true
//         };

//         var notification = new iOSNotification()
//         {
//             Identifier = $"daily_push_{id}",
//             Title = GetLocalizedText(content.titleKey),
//             Body = GetLocalizedText(content.bodyKey),
//             ShowInForeground = false,
//             CategoryIdentifier = "DAILY_REMINDER",
//             Trigger = trigger
//         };

//         if (!string.IsNullOrEmpty(content.imageFileName))
//             AddImageAttachment(notification, content.imageFileName);

//         iOSNotificationCenter.ScheduleNotification(notification);

//         Debug.Log($"[iOS] ����ÿ������: {triggerTime.Hours:D2}:{triggerTime.Minutes:D2} TitleKey:{content.titleKey} Image:{content.imageFileName}");
//     }

//     /// <summary>
//     /// iOS ����ͼƬ�������� StreamingAssets ��������дĿ¼�ٸ���
//     /// </summary>
//     void AddImageAttachment(iOSNotification notification, string imageFileName)
//     {
//         try
//         {
//             string sourcePath = Path.Combine(Application.streamingAssetsPath, "NotificationImages", imageFileName);

//             string destDir = Path.Combine(Application.persistentDataPath, "NotificationImages");
//             string destPath = Path.Combine(destDir, imageFileName);

//             Directory.CreateDirectory(destDir);

//             if (!File.Exists(destPath) && File.Exists(sourcePath))
//                 File.Copy(sourcePath, destPath, true);

//             if (File.Exists(destPath))
//             {
//                 var attachment = new iOSNotificationAttachment()
//                 {
//                     Id = $"image_{imageFileName}",
//                     Url = new System.Uri(destPath).AbsoluteUri
//                 };

//                 notification.Attachments = new System.Collections.Generic.List<iOSNotificationAttachment>()
//                 {
//                     attachment
//                 };

//                 Debug.Log($"[iOS] �ɹ�����ͼƬ����: {destPath} -> {attachment.Url}");
//             }
//             else
//             {
//                 Debug.LogWarning($"[iOS] ͼƬ�ļ�������: {destPath}");
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"[iOS] ����ͼƬ����ʧ��: {e.Message}");
//         }
//     }
// #endif

// #if UNITY_ANDROID && !UNITY_EDITOR
//     /// <summary>
//     /// Android��ע��֪ͨ Channel�����룩
//     /// </summary>
//     private void RegisterAndroidChannelIfNeeded()
//     {
//         var channel = new AndroidNotificationChannel()
//         {
//             Id = ANDROID_CHANNEL_ID,
//             Name = ANDROID_CHANNEL_NAME,        
//             Importance = Importance.High,
//             Description = "Daily reminders",
//             CanShowBadge = true,               // ������ʾ�Ǳ�
//             EnableVibration = true,            // ������
//             LockScreenVisibility = LockScreenVisibility.Public, // �������ݿɼ�
//             CanBypassDnd = true                // �����ƹ�������ģʽ��
//         };

//         AndroidNotificationCenter.RegisterNotificationChannel(channel);
//         Debug.Log($"[Android] 已注册通知渠道 Id:{ANDROID_CHANNEL_ID} Name:{ANDROID_CHANNEL_NAME}");
//     }

//     /// <summary>
//     /// Android������ÿ�չ̶�ʱ�����ͣ�����ʱ����
//     /// ˵����Android ���� FireTime + RepeatInterval(1��) ʵ���ظ�
//     /// </summary>
//     private IEnumerator SetupDailyNotificationsAndroidCoroutine()
//     {
//         // ����֮ǰ�����мƻ�/����ʾ֪ͨ
//         AndroidNotificationCenter.CancelAllScheduledNotifications();
//         Debug.Log($"[Android] 已清理旧的计划通知，Channel:{ANDROID_CHANNEL_ID}");

//         int scheduledCount = 0;
//         var scheduledIds = new List<int>();
//         var scheduledFireTimes = new List<DateTime>();

//         for (int i = 0; i < pushContents.Length; i++)
//         {
//             var content = pushContents[i];

//             if (!TryParseLocalTime(content.timeString, out TimeSpan triggerTime))
//             {
//                 Debug.LogWarning($"[Android] ʱ�����ʧ��: {content.timeString}");
//                 continue;
//             }

//             DateTime fireTime = GetNextLocalDateTime(triggerTime);

//             var notification = new AndroidNotification()
//             {
//                 Title = GetLocalizedText(content.titleKey),
//                 Text = GetLocalizedText(content.bodyKey),
//                 SmallIcon = "icon_0",
//                 FireTime = fireTime,
//                 RepeatInterval = TimeSpan.FromDays(1),   // ÿ���ظ�
//                 ShowInForeground = false,
//                 IntentData = $"daily_push_{i}",
//                 Number=1,
//             };

//             // ͼƬ����ѡ����BigPictureStyle.Picture ֧����Դ�����ļ�·���� URI
//             if (!string.IsNullOrEmpty(content.imageFileName))
//             {
//                 string picturePath = null;
//                 yield return CopyImageFromStreamingAssetsToPersistentCoroutine(
//                     content.imageFileName,
//                     (p) => picturePath = p
//                 );

//                 if (!string.IsNullOrEmpty(picturePath) && File.Exists(picturePath))
//                 {
//                     notification.BigPicture = new BigPictureStyle()
//                     {
//                         Picture = picturePath,
//                         LargeIcon = picturePath,
//                         ContentTitle = notification.Title,
//                         SummaryText = notification.Text,
//                         ShowWhenCollapsed = true
//                     };
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"[Android] ͼƬ׼��ʧ�ܻ򲻴��ڣ�����ͼƬ: {content.imageFileName}");
//                 }
//             }

//             int id = AndroidNotificationCenter.SendNotification(notification, ANDROID_CHANNEL_ID);
//             var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(id);
//             scheduledCount++;
//             scheduledIds.Add(id);
//             scheduledFireTimes.Add(fireTime);
//             SaveDailyNotificationDebugInfo(scheduledIds, scheduledFireTimes);
//             Debug.Log($"[Android][ScheduleStatus] Id:{id} Status:{status} Channel:{ANDROID_CHANNEL_ID}");

//             Debug.Log($"[Android] ����ÿ������: {triggerTime.Hours:D2}:{triggerTime.Minutes:D2} FireTime:{fireTime:yyyy-MM-dd HH:mm:ss} Id:{id} TitleKey:{content.titleKey} Image:{content.imageFileName}");
//         }

//         Debug.Log($"[Android] ������ {scheduledCount} ��ÿ�ձ�������");
//     }

//     /// <summary>
//     /// Android���� StreamingAssets/NotificationImages �µ�ͼƬ������ persistentDataPath�����ؿ��õľ���·��
//     /// </summary>
//     private IEnumerator CopyImageFromStreamingAssetsToPersistentCoroutine(
//     string imageFileName,
//     Action<string> onDone)
//     {
//         onDone?.Invoke(null);

//         string destDir = Path.Combine(Application.persistentDataPath, "NotificationImages");
//         string destPath = Path.Combine(destDir, imageFileName);
//         Directory.CreateDirectory(destDir);

//         // �Ѵ���ֱ�ӷ���
//         if (File.Exists(destPath))
//         {
//             onDone?.Invoke(destPath);
//             yield break;
//         }

//         string sourcePath = Path.Combine(
//             Application.streamingAssetsPath,
//             "NotificationImages",
//             imageFileName
//         );

//         UnityWebRequest req = UnityWebRequest.Get(sourcePath);

// #if UNITY_2020_2_OR_NEWER
//         yield return req.SendWebRequest();
//         if (req.result != UnityWebRequest.Result.Success)
// #else
//         yield return req.SendWebRequest();
//         if (req.isNetworkError || req.isHttpError)
// #endif
//         {
//             Debug.LogWarning($"[Android] ��ȡ StreamingAssets ͼƬʧ��: {sourcePath}, error={req.error}");
//             yield break;
//         }

//         byte[] data = req.downloadHandler.data;
//         if (data == null || data.Length == 0)
//         {
//             Debug.LogWarning($"[Android] ͼƬ����Ϊ��: {sourcePath}");
//             yield break;
//         }

//         try
//         {
//             File.WriteAllBytes(destPath, data);
//             onDone?.Invoke(destPath);
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"[Android] д��ͼƬʧ��: {e.Message}");
//         }
//     }


//     /// <summary>
//     /// ���㡰��һ��������ʱ��㣨����ʱ�䣩
//     /// </summary>
//     private DateTime GetNextLocalDateTime(TimeSpan triggerTime)
//     {
//         DateTime now = DateTime.Now;
//         DateTime todayTarget = new DateTime(now.Year, now.Month, now.Day, triggerTime.Hours, triggerTime.Minutes, 0);

//         // �������������ѹ����Ƶ�����
//         if (todayTarget <= now)
//             todayTarget = todayTarget.AddDays(1);

//         return todayTarget;
//     }

//     private void SaveDailyNotificationDebugInfo(List<int> ids, List<DateTime> fireTimes)
//     {
//         PlayerPrefs.SetInt(ANDROID_DAILY_NOTIFICATION_COUNT_KEY, ids.Count);

//         for (int i = 0; i < ids.Count; i++)
//         {
//             PlayerPrefs.SetInt($"{ANDROID_DAILY_NOTIFICATION_ID_KEY_PREFIX}{i}", ids[i]);
//             PlayerPrefs.SetString($"{ANDROID_DAILY_NOTIFICATION_FIRE_TIME_KEY_PREFIX}{i}", fireTimes[i].ToString("o", CultureInfo.InvariantCulture));
//         }

//         PlayerPrefs.Save();
//     }

//     private void SaveTestNotificationDebugInfo(int id, DateTime fireTime)
//     {
//         PlayerPrefs.SetInt(ANDROID_TEST_NOTIFICATION_ID_KEY, id);
//         PlayerPrefs.SetString(ANDROID_TEST_NOTIFICATION_FIRE_TIME_KEY, fireTime.ToString("o", CultureInfo.InvariantCulture));
//         PlayerPrefs.Save();
//     }

//     private void LogSavedAndroidNotificationStatuses()
//     {
//         int dailyCount = PlayerPrefs.GetInt(ANDROID_DAILY_NOTIFICATION_COUNT_KEY, 0);
//         if (dailyCount > 0)
//         {
//             Debug.Log($"[Android][SavedScheduleStatus] Checking {dailyCount} saved daily notifications");

//             for (int i = 0; i < dailyCount; i++)
//             {
//                 string idKey = $"{ANDROID_DAILY_NOTIFICATION_ID_KEY_PREFIX}{i}";
//                 string fireKey = $"{ANDROID_DAILY_NOTIFICATION_FIRE_TIME_KEY_PREFIX}{i}";
//                 if (!PlayerPrefs.HasKey(idKey))
//                     continue;

//                 int id = PlayerPrefs.GetInt(idKey, -1);
//                 string fireTimeText = PlayerPrefs.GetString(fireKey, string.Empty);
//                 var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(id);
//                 Debug.Log($"[Android][SavedScheduleStatus] DailyIndex:{i} Id:{id} Status:{status} SavedFireTime:{fireTimeText}");
//             }
//         }

//         if (PlayerPrefs.HasKey(ANDROID_TEST_NOTIFICATION_ID_KEY))
//         {
//             int testId = PlayerPrefs.GetInt(ANDROID_TEST_NOTIFICATION_ID_KEY, -1);
//             string fireTimeText = PlayerPrefs.GetString(ANDROID_TEST_NOTIFICATION_FIRE_TIME_KEY, string.Empty);
//             var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(testId);
//             Debug.Log($"[Android][SavedTestStatus] Id:{testId} Status:{status} SavedFireTime:{fireTimeText}");
//         }
//     }

//     private void OnAndroidNotificationReceived(AndroidNotificationIntentData data)
//     {
//         if (data == null)
//         {
//             Debug.Log("[Android][Received] Notification intent data is null");
//             return;
//         }

//         Debug.Log($"[Android][Received] Id:{data.Id} Channel:{data.Channel} Title:{data.Notification.Title} FireTime:{data.Notification.FireTime:yyyy-MM-dd HH:mm:ss}");
//     }
// #endif


//     public void TestPush(float min)
//     {
//         StartCoroutine(SendTestNotification(min));
//     }


//     /// <summary>
//     /// �������ͣ�1 ���Ӻ�һ������֪ͨ
//     /// </summary>
//     [ContextMenu("测试配置通知(3分钟/4分钟)")]
//     // Quick validation entry: schedules pushContents[0] and [1] at now+3m and now+4m.
//     public void TestConfiguredPushContents()
//     {
//         StartCoroutine(SendConfiguredPushContentsNotificationCoroutine());
//     }

//     IEnumerator SendTestNotification(float min)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         var settings = iOSNotificationCenter.GetNotificationSettings();
//         if (settings.AuthorizationStatus != AuthorizationStatus.Authorized)
//         {
//             Debug.LogWarning("֪ͨȨ��δ��Ȩ���޷����Ͳ�������");
//             yield return null;
//         }

//         var notification = new iOSNotification()
//         {
//             Identifier = "test_push_1_min",
//             Title = "�������ͱ���",
//             Body = $"��������ʱ�䣺{DateTime.Now:HH:mm:ss}",
//             ShowInForeground = true,
//             CategoryIdentifier = "TEST_CATEGORY",
//             Trigger = new iOSNotificationTimeIntervalTrigger()
//             {
//                 TimeInterval = new TimeSpan(0, min, 0),
//                 Repeats = false
//             }
//         };

//         iOSNotificationCenter.ScheduleNotification(notification);
//         Debug.Log("�����ò������ͣ����� 1 ���Ӻ󴥷� (iOS)");

// #elif UNITY_ANDROID && !UNITY_EDITOR
//         RegisterAndroidChannelIfNeeded();
    
//         var notification = new AndroidNotification()
//         {
//             Title = "�������ͱ���",
//             Text = $"��������ʱ�䣺{DateTime.Now:HH:mm:ss}",
//             FireTime = DateTime.Now.AddMinutes(min),
//             ShowInForeground = true,
//             IntentData = "test_push_1_min",
//             Number=1,
//         };

//         // ͼƬ����ѡ����BigPictureStyle.Picture ֧����Դ�����ļ�·���� URI
//         if (pushContents.Length > 0)
//         {
//             var content = pushContents[0];
//             string picturePath = null;
//             yield return CopyImageFromStreamingAssetsToPersistentCoroutine(
//                 content.imageFileName,
//                 (p) => picturePath = p
//             );
//             if (!string.IsNullOrEmpty(picturePath) && File.Exists(picturePath))
//             {
//                 notification.BigPicture = new BigPictureStyle()
//                 {
//                     Picture = picturePath,
//                     LargeIcon = picturePath,
//                     ContentTitle = notification.Title,
//                     SummaryText = notification.Text,
//                     ShowWhenCollapsed = true
//                 };
//             }
//         }
//         int id = AndroidNotificationCenter.SendNotification(notification, ANDROID_CHANNEL_ID);
//         var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(id);
//         SaveTestNotificationDebugInfo(id, notification.FireTime);
//         Debug.Log($"[Android][TestScheduleStatus] Id:{id} Status:{status} Channel:{ANDROID_CHANNEL_ID}");
//         Debug.Log($"�����ò������ͣ����� {min} ���Ӻ󴥷� (Android) Id:{id}");
// #else
//         Debug.Log("�������ͽ��� iOS/Android �������Ч");
//         yield return null;
// #endif
//     }

//     /// <summary>
//     /// ֧�ֶ���ʱ���ʽ�Ľ�����"14:00"��"9:05" ��
//     /// </summary>
//     private IEnumerator SendConfiguredPushContentsNotificationCoroutine()
//     {
//         if (pushContents == null || pushContents.Length < 2)
//         {
//             Debug.LogWarning("pushContents 至少需要配置 2 条数据，才能执行 3 分钟/4 分钟的快速通知测试");
//             yield break;
//         }

// #if UNITY_IOS && !UNITY_EDITOR
//         var settings = iOSNotificationCenter.GetNotificationSettings();
//         if (settings.AuthorizationStatus != AuthorizationStatus.Authorized)
//         {
//             Debug.LogWarning("通知权限未授权，无法发送配置通知测试");
//             yield break;
//         }

//         ScheduleConfiguredTestNotification(pushContents[0], 3, 0);
//         ScheduleConfiguredTestNotification(pushContents[1], 4, 1);
//         Debug.Log("[iOS] 已按配置创建 2 条快速测试通知，分别在 3 分钟和 4 分钟后触发");

// #elif UNITY_ANDROID && !UNITY_EDITOR
//         RegisterAndroidChannelIfNeeded();

//         DateTime now = DateTime.Now;
//         int[] minuteOffsets = { 3, 4 };

//         for (int i = 0; i < 2; i++)
//         {
//             PushContent content = pushContents[i];
//             DateTime fireTime = now.AddMinutes(minuteOffsets[i]);
//             var notification = new AndroidNotification()
//             {
//                 Title = GetLocalizedText(content.titleKey),
//                 Text = GetLocalizedText(content.bodyKey),
//                 SmallIcon = "icon_0",
//                 FireTime = fireTime,
//                 ShowInForeground = false,
//                 IntentData = $"configured_test_push_{i}",
//                 Number = 1,
//             };

//             if (!string.IsNullOrEmpty(content.imageFileName))
//             {
//                 string picturePath = null;
//                 yield return CopyImageFromStreamingAssetsToPersistentCoroutine(
//                     content.imageFileName,
//                     (p) => picturePath = p
//                 );

//                 if (!string.IsNullOrEmpty(picturePath) && File.Exists(picturePath))
//                 {
//                     notification.BigPicture = new BigPictureStyle()
//                     {
//                         Picture = picturePath,
//                         LargeIcon = picturePath,
//                         ContentTitle = notification.Title,
//                         SummaryText = notification.Text,
//                         ShowWhenCollapsed = true
//                     };
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"[Android] 配置测试通知图片准备失败，将继续发送无图通知: {content.imageFileName}");
//                 }
//             }

//             int id = AndroidNotificationCenter.SendNotification(notification, ANDROID_CHANNEL_ID);
//             var status = AndroidNotificationCenter.CheckScheduledNotificationStatus(id);
//             Debug.Log($"[Android][ConfiguredTestStatus] Index:{i} Id:{id} Status:{status} FireTime:{fireTime:yyyy-MM-dd HH:mm:ss} Channel:{ANDROID_CHANNEL_ID}");
//         }

//         Debug.Log("[Android] 已按配置创建 2 条快速测试通知，分别在 3 分钟和 4 分钟后触发");
// #else
//         Debug.Log("配置测试通知仅支持 iOS/Android 真机环境");
//         yield return null;
// #endif
//     }

// #if UNITY_IOS && !UNITY_EDITOR
//     private void ScheduleConfiguredTestNotification(PushContent content, int minutesLater, int index)
//     {
//         var notification = new iOSNotification()
//         {
//             Identifier = $"configured_test_push_{index}",
//             Title = GetLocalizedText(content.titleKey),
//             Body = GetLocalizedText(content.bodyKey),
//             ShowInForeground = false,
//             CategoryIdentifier = "TEST_CATEGORY",
//             Trigger = new iOSNotificationTimeIntervalTrigger()
//             {
//                 TimeInterval = TimeSpan.FromMinutes(minutesLater),
//                 Repeats = false
//             }
//         };

//         if (!string.IsNullOrEmpty(content.imageFileName))
//             AddImageAttachment(notification, content.imageFileName);

//         iOSNotificationCenter.ScheduleNotification(notification);
//         Debug.Log($"[iOS] 已创建配置测试通知 Index:{index} DelayMinutes:{minutesLater} TitleKey:{content.titleKey} Image:{content.imageFileName}");
//     }
// #endif

//     bool TryParseLocalTime(string timeString, out TimeSpan time)
//     {
//         return TimeSpan.TryParseExact(
//             timeString,
//             new[] { "HH\\:mm", "H\\:mm", "hh\\:mm", "h\\:mm" },
//             CultureInfo.InvariantCulture,
//             out time
//         );
//     }

//     /// <summary>
//     /// ���ػ��ı���װ�������Ժ��滻ʵ��
//     /// </summary>
//     string GetLocalizedText(string key)
//     {
//         return LanguageUtils.GetText(key);
//     }

//     /// <summary>
//     /// ���������Ѽƻ������ʹ������
//     /// </summary>
//     public void ClearAllNotifications()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         iOSNotificationCenter.RemoveAllScheduledNotifications();
//         iOSNotificationCenter.RemoveAllDeliveredNotifications();
//         Debug.Log("������������� (iOS)");
// #elif UNITY_ANDROID && !UNITY_EDITOR
//         AndroidNotificationCenter.CancelAllNotifications();
//         Debug.Log("������������� (Android)");
// #endif
//     }

//     /// <summary>
//     /// ��ѡ���� Inspector �Ҽ��˵����ٲ鿴��ǰȨ��״̬
//     /// </summary>
//     [ContextMenu("���֪ͨȨ��״̬")]
//     public void CheckNotificationPermission()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         var settings = iOSNotificationCenter.GetNotificationSettings();
//         Debug.Log($"AuthorizationStatus: {settings.AuthorizationStatus}");
//         Debug.Log($"AlertSetting: {settings.AlertSetting}");
//         Debug.Log($"BadgeSetting: {settings.BadgeSetting}");
//         Debug.Log($"SoundSetting: {settings.SoundSetting}");
// #elif UNITY_ANDROID && !UNITY_EDITOR
//         // PermissionRequest �����ء���Ҫ���ʡ���Ҳ�Ỻ���û�ѡ��������ϵͳȨ�޿����ж�
//         bool granted = UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS");
//         Debug.Log($"Android POST_NOTIFICATIONS Granted: {granted}");
// #endif
//     }



// }
// #endif