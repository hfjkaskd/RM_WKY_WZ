#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bizza.Sdk.Editor
{
    /// <summary>
    /// ChannelConfig 编辑器窗口
    ///
    /// 使用 Unity 原生 EditorWindow/IMGUI 绘制配置
    ///
    /// 序列化：
    ///     使用自己的 ChannelConfigBinarySerializer
    ///
    /// 保存位置：
    ///     Assets/StreamingAssets/ChannelConfig.bytes
    /// </summary>
    public class ChannelConfigEditorWindow : EditorWindow
    {
        private ChannelConfig _config;
        private Vector2 _scrollPosition;
        private string _currentUserId = string.Empty;
        private bool _showRuntimeConfig = true;
        private bool _showDebugConfig = true;
        private bool _showEditorOnlyConfig;
        private bool _showBasicConfig = true;
        private bool _showHttpConfig = true;
        private bool _showAdStatisticsConfig = true;
        private bool[] _adStatisticsRangeFoldouts = Array.Empty<bool>();
        private bool _showAttributionConfig = true;
        private bool _showAdsConfig = true;

#if DEBUG_MODE
        private const bool IsDebugModeEnabled = true;
#else
        private const bool IsDebugModeEnabled = false;
#endif

#if BIZZA_HTTP_AD
        private const bool IsHttpAdEnabled = true;
#else
        private const bool IsHttpAdEnabled = false;
#endif

        /// <summary>
        /// StreamingAssets 中的配置文件名
        /// </summary>
        private const string CONFIG_FILE_NAME =
            "ChannelConfig.bytes";

        /// <summary>
        /// Unity 编译脚本时会触发 Assembly Reload，编辑器窗口中的内存对象会随之丢失。
        /// 使用 SessionState 暂存一份当前配置，只用于跨本次 Unity 会话的脚本重载恢复，
        /// 不会替代 StreamingAssets 中的正式配置文件。
        /// </summary>
        private const string SESSION_BACKUP_KEY_PREFIX =
            "Bizza.ChannelConfigEditorWindow.SessionBackup.";

        private bool _assemblyReloadCallbackRegistered;


        #region Window

        [MenuItem("Bizza/Channel Config")]
        private static void OpenWindow()
        {
            bool wasAlreadyOpen =
                HasOpenInstances<ChannelConfigEditorWindow>();

            ChannelConfigEditorWindow window =
                GetWindow<ChannelConfigEditorWindow>();

            window.titleContent =
                new GUIContent("Channel Config");

            window.minSize =
                new Vector2(800, 600);

            window.Show();

            // 首次打开时 OnEnable 会读取配置；窗口已存在时，
            // 再次点击菜单也要主动从 StreamingAssets 刷新。
            if (wasAlreadyOpen)
            {
                window.RefreshFromStreamingAssetsOnOpen();
            }
        }


        /// <summary>
        /// Unity EditorWindow 的初始化。
        /// </summary>
        private void OnEnable()
        {
            if (!_assemblyReloadCallbackRegistered)
            {
                AssemblyReloadEvents.beforeAssemblyReload +=
                    SaveSessionBackupBeforeAssemblyReload;

                _assemblyReloadCallbackRegistered = true;
            }

            // 窗口打开或脚本重新编译后，优先恢复编译前的编辑内容，
            // 其次读取 StreamingAssets 中已保存的配置，最后才创建空配置。
            if (!TryLoadSessionBackup() &&
                !TryLoadFromStreamingAssets(false))
            {
                CreateNewConfig();
            }
        }


        /// <summary>
        /// 使用 Unity 原生 IMGUI 绘制窗口。
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space(5);
            DrawCurrentUserId();
            EditorGUILayout.Space(5);

            if (_config == null)
            {
                CreateNewConfig();
            }

            DrawEnvironmentSummary();

            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawConfigFields();
            EditorGUILayout.EndScrollView();
        }


        private void OnInspectorUpdate()
        {
            RefreshCurrentUserId();
        }


        private void RefreshCurrentUserId()
        {
            string userId = PlayerPrefs.GetString(AccountModule.m_userIdKey, string.Empty);
            if (_currentUserId == userId)
            {
                return;
            }

            _currentUserId = userId;
            Repaint();
        }


        private void OnDisable()
        {
            if (_assemblyReloadCallbackRegistered)
            {
                AssemblyReloadEvents.beforeAssemblyReload -=
                    SaveSessionBackupBeforeAssemblyReload;

                _assemblyReloadCallbackRegistered = false;
            }

        }

        #endregion


        #region Config

        /// <summary>
        /// 创建新的配置。
        /// </summary>
        private void CreateNewConfig()
        {
            _config =
                new ChannelConfig();
            ChannelConfig.Instance = _config;
            Repaint();
        }

        #endregion


        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(
                EditorStyles.toolbar);


            // =========================================
            // 新建
            // =========================================

            if (GUILayout.Button(
                    "新建",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(70)))
            {
                NewConfig();
            }


            // =========================================
            // 读取 StreamingAssets
            // =========================================

            if (GUILayout.Button(
                    "读取配置",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(90)))
            {
                LoadFromStreamingAssets();
            }


            GUILayout.FlexibleSpace();


            // =========================================
            // 保存 StreamingAssets
            // =========================================

            if (GUILayout.Button(
                    "保存配置",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(90)))
            {
                SaveToStreamingAssets();
            }


            EditorGUILayout.EndHorizontal();


            // =========================================
            // 保存路径
            // =========================================

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                "保存路径",
                GUILayout.Width(60));


            EditorGUILayout.SelectableLabel(
                GetConfigPath(),
                EditorStyles.textField,
                GUILayout.Height(
                    EditorGUIUtility.singleLineHeight));


            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurrentUserId()
        {
            // 读取账号模块实际使用的 UID；自定义账号配置需登录生效后才会写入此值。
            RefreshCurrentUserId();
            bool hasUserId = !string.IsNullOrEmpty(_currentUserId);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                new GUIContent("当前用户 UID", "当前编辑器本地保存的用户 UID（只读）"),
                GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(
                hasUserId ? _currentUserId : "暂无 UID，请先运行游戏完成登录",
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            using (new EditorGUI.DisabledScope(!hasUserId))
            {
                if (GUILayout.Button("复制", GUILayout.Width(60)))
                {
                    EditorGUIUtility.systemCopyBuffer = _currentUserId;
                    ShowNotification(new GUIContent("UID 已复制"));
                }
            }

            if (GUILayout.Button(
                    new GUIContent("刷新", "重新读取本地保存的 UID，无需运行游戏"),
                    GUILayout.Width(60)))
            {
                RefreshCurrentUserId();
                ShowNotification(new GUIContent(string.IsNullOrEmpty(_currentUserId)
                    ? "本地暂无 UID，请先运行游戏完成一次登录"
                    : "UID 已刷新"));
            }

            EditorGUILayout.EndHorizontal();
        }


        private void DrawConfigFields()
        {
            DrawRuntimeConfig();

            EditorGUILayout.Space(8f);
            DrawDebugConfig();

            EditorGUILayout.Space(8f);
            DrawEditorOnlyConfig();
        }

        private static void DrawEnvironmentSummary()
        {
            string debugMode = IsDebugModeEnabled ? "已开启" : "未开启";
            string httpAdMode = IsHttpAdEnabled ? "已开启" : "未开启";
            MessageType messageType = IsDebugModeEnabled
                ? MessageType.Warning
                : MessageType.Info;

            string message =
                $"当前构建目标：{EditorUserBuildSettings.activeBuildTarget}\n" +
                $"DEBUG_MODE：{debugMode}    BIZZA_HTTP_AD：{httpAdMode}";

            if (IsDebugModeEnabled)
            {
                message += "\n当前调试配置会生效；正式出包前请确认是否需要移除 DEBUG_MODE。";
            }

            EditorGUILayout.HelpBox(message, messageType);
        }

        private void DrawRuntimeConfig()
        {
            _showRuntimeConfig = EditorGUILayout.Foldout(
                _showRuntimeConfig,
                "正式 / 通用配置",
                true);
            if (!_showRuntimeConfig)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "以下配置参与正式运行；“默认/兜底国家”不受 BIZZA_HTTP_AD 限制。",
                MessageType.Info);

            _showBasicConfig = EditorGUILayout.Foldout(
                _showBasicConfig,
                "基础与业务配置",
                true);
            if (_showBasicConfig)
            {
                EditorGUI.indentLevel++;
                _config.AppId = EditorGUILayout.TextField(
                    "AppId/AppCode",
                    _config.AppId);

                Real_CustomConfig userConfig = _config.real_CustomConfig;
                userConfig.DefaultCountry =
                    (AccountModule.E_CountryType)EditorGUILayout.EnumPopup(
                        "默认/兜底国家",
                        userConfig.DefaultCountry);
                userConfig.singleCurrencyMode = EditorGUILayout.Toggle(
                    "是否开启单货币模式",
                    userConfig.singleCurrencyMode);
                userConfig.realWithdrawPassMode = EditorGUILayout.Toggle(
                    "真提现界面是否为 pass",
                    userConfig.realWithdrawPassMode);
                _config.GetFreeDollarRewardRate = EditorGUILayout.FloatField(
                    new GUIContent("免费奖励倍率",
                        "所有国家通用的免费现金奖励倍率，默认值为 5。"),
                    _config.GetFreeDollarRewardRate);
                _config.SynthesisRewardMinRemainingCount = Mathf.Max(0, EditorGUILayout.IntField(
                    new GUIContent("合成奖励最少剩余次数",
                        "剩余合成次数小于此值时，跳过奖励弹窗和免费钞票检查。默认 5；设为 0 不限制。"),
                    _config.SynthesisRewardMinRemainingCount));
                userConfig.versionLog = EditorGUILayout.TextField(
                    "版本日志",
                    userConfig.versionLog);
                _config.real_CustomConfig = userConfig;
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            _showHttpConfig = EditorGUILayout.Foldout(
                _showHttpConfig,
                "HTTP配置",
                true);
            if (_showHttpConfig)
            {
                EditorGUI.indentLevel++;
                if (_config.httpConfig == null)
                {
                    _config.httpConfig = new HttpDataConfig();
                }

                _config.httpConfig.domain = EditorGUILayout.TextField(
                    "domain",
                    _config.httpConfig.domain);
                _config.httpConfig.aes_key = EditorGUILayout.TextField(
                    "aes_key",
                    _config.httpConfig.aes_key);
                _config.isReportServer = EditorGUILayout.Toggle(
                    "上报服务器",
                    _config.isReportServer);
                _config.incomeRate = EditorGUILayout.DoubleField(
                    "广告收入上报比例",
                    _config.incomeRate);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            _showAdStatisticsConfig = EditorGUILayout.Foldout(
                _showAdStatisticsConfig,
                "广告数值配置",
                true);
            if (_showAdStatisticsConfig)
            {
                DrawAdStatisticsConfig();
            }

            EditorGUILayout.Space(4f);
            _showAttributionConfig = EditorGUILayout.Foldout(
                _showAttributionConfig,
                "归因 SDK",
                true);
            if (_showAttributionConfig)
            {
                EditorGUI.indentLevel++;
                _config.adjustKey = EditorGUILayout.TextField(
                    "adjust key",
                    _config.adjustKey);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            _showAdsConfig = EditorGUILayout.Foldout(
                _showAdsConfig,
                "广告配置",
                true);
            if (_showAdsConfig)
            {
                DrawAdsConfig();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDebugConfig()
        {
            _showDebugConfig = EditorGUILayout.Foldout(
                _showDebugConfig,
                "DEBUG_MODE 调试配置",
                true);
            if (!_showDebugConfig)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                IsDebugModeEnabled
                    ? "DEBUG_MODE 已开启，以下测试配置当前会生效。"
                    : "DEBUG_MODE 未开启，以下配置当前不生效，已设为只读。",
                IsDebugModeEnabled ? MessageType.Warning : MessageType.Info);

            EditorGUI.BeginDisabledGroup(!IsDebugModeEnabled);
            EditorGUI.indentLevel++;
            Real_CustomConfig userConfig = _config.real_CustomConfig;
            _config.GM = EditorGUILayout.Toggle(
                "GM开关",
                _config.GM);
            userConfig.Country = (AccountModule.E_CountryType)EditorGUILayout.EnumPopup(
                "强制测试国家",
                userConfig.Country);

            userConfig.testECPM1000 = EditorGUILayout.Toggle(
                "强制测试 ECPM",
                userConfig.testECPM1000);
            if (userConfig.testECPM1000)
            {
                userConfig.TestECPMValue = EditorGUILayout.FloatField(
                    "ecpm1000 值",
                    userConfig.TestECPMValue);
            }

            userConfig.openTestDevice = EditorGUILayout.Toggle(
                "是否开启测试设备",
                userConfig.openTestDevice);
            if (userConfig.openTestDevice)
            {
                userConfig.testDeviceIds = DrawStringArray(
                    "测试设备",
                    userConfig.testDeviceIds);
            }

            userConfig.interAdNotReady = EditorGUILayout.Toggle(
                "模拟插屏未准备好",
                userConfig.interAdNotReady);
            userConfig.rewardAdNotReady = EditorGUILayout.Toggle(
                "模拟激励未准备好",
                userConfig.rewardAdNotReady);
            _config.real_CustomConfig = userConfig;
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }

        private void DrawEditorOnlyConfig()
        {
            _showEditorOnlyConfig = EditorGUILayout.Foldout(
                _showEditorOnlyConfig,
                "仅 Unity 编辑器配置",
                true);
            if (!_showEditorOnlyConfig)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "以下配置用于 Unity 编辑器本地测试，不作为正式 Android 包的运行参数。",
                MessageType.Info);

            EditorGUI.indentLevel++;
            Real_CustomConfig userConfig = _config.real_CustomConfig;
            _config.RecordMode = EditorGUILayout.Toggle(
                new GUIContent(
                    "录制模式",
                    "仅在 Unity 编辑器下生效。"),
                _config.RecordMode);
            _config.isEditorReportServer = EditorGUILayout.Toggle(
                "编辑器上报服务器",
                _config.isEditorReportServer);
            userConfig.useEditorUserId = EditorGUILayout.Toggle(
                "是否开启自定义账号",
                userConfig.useEditorUserId);
            if (userConfig.useEditorUserId)
            {
                userConfig.editorUserId = EditorGUILayout.TextField(
                    "账号 id",
                    userConfig.editorUserId);
            }

            userConfig.GenerateFakeAndroidId = EditorGUILayout.TextField(
                "生成用户的 ID 记录路径",
                userConfig.GenerateFakeAndroidId);
            userConfig.enterNewbieGuide = EditorGUILayout.Toggle(
                "强制进入新手引导",
                userConfig.enterNewbieGuide);
            userConfig.failOpenAd = EditorGUILayout.Toggle(
                "模拟广告打开失败",
                userConfig.failOpenAd);
            _config.real_CustomConfig = userConfig;
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawAdStatisticsConfig()
        {
            EditorGUI.indentLevel++;
            _config.adStatisticsLevelRanges ??=
                new System.Collections.Generic.List<AdStatisticsLevelRange>();

            if (_adStatisticsRangeFoldouts.Length != _config.adStatisticsLevelRanges.Count)
            {
                int oldLength = _adStatisticsRangeFoldouts.Length;
                Array.Resize(
                    ref _adStatisticsRangeFoldouts,
                    _config.adStatisticsLevelRanges.Count);
                for (int i = oldLength; i < _adStatisticsRangeFoldouts.Length; i++)
                {
                    _adStatisticsRangeFoldouts[i] = true;
                }
            }

            int removeIndex = -1;
            for (int i = 0; i < _config.adStatisticsLevelRanges.Count; i++)
            {
                AdStatisticsLevelRange range = _config.adStatisticsLevelRanges[i];
                if (range == null)
                {
                    range = new AdStatisticsLevelRange();
                    _config.adStatisticsLevelRanges[i] = range;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                _adStatisticsRangeFoldouts[i] = EditorGUILayout.Foldout(
                    _adStatisticsRangeFoldouts[i],
                    $"区间 {i + 1}：第 {range.StartLevel} 关 - 第 {range.EndLevel} 关",
                    true);
                if (GUILayout.Button("删除", GUILayout.Width(55f)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();

                if (_adStatisticsRangeFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    range.StartLevel = Mathf.Max(
                        1,
                        EditorGUILayout.IntField("起始关卡", range.StartLevel));
                    range.EndLevel = Mathf.Max(
                        range.StartLevel,
                        EditorGUILayout.IntField("结束关卡", range.EndLevel));

                    GameAB_CustomData statistics = range.Statistics;
                    statistics.CloseGetRewardCount = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            "关闭恭喜界面触发插屏次数",
                            statistics.CloseGetRewardCount));
                    statistics.ShowGetRewardCount = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            "合成后显示恭喜界面次数",
                            statistics.ShowGetRewardCount));
                    statistics.ShowDollarCount = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            "合成后显示假钞次数",
                            statistics.ShowDollarCount));
                    statistics.InterAdCooldownMs = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            "插屏广告间隔（毫秒）",
                            statistics.InterAdCooldownMs));
                    statistics.InterAdStartLevel = Mathf.Max(
                        1,
                        EditorGUILayout.IntField(
                            "插屏广告起始关卡",
                            statistics.InterAdStartLevel));
                    statistics.ReviveAdStartLevel = Mathf.Max(
                        1,
                        EditorGUILayout.IntField(
                            "复活广告起始关卡",
                            statistics.ReviveAdStartLevel));
                    range.Statistics = statistics;
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                _config.adStatisticsLevelRanges.RemoveAt(removeIndex);
                Array.Resize(
                    ref _adStatisticsRangeFoldouts,
                    _config.adStatisticsLevelRanges.Count);
            }

            if (HasOverlappingAdStatisticsRanges(_config.adStatisticsLevelRanges))
            {
                EditorGUILayout.HelpBox(
                    "检测到关卡区间重叠。运行时会使用起始关卡更大的配置，请尽量避免重叠。",
                    MessageType.Warning);
            }

            if (GUILayout.Button("添加关卡区间"))
            {
                int startLevel = 1;
                if (_config.adStatisticsLevelRanges.Count > 0)
                {
                    AdStatisticsLevelRange lastRange =
                        _config.adStatisticsLevelRanges[_config.adStatisticsLevelRanges.Count - 1];
                    if (lastRange != null && lastRange.EndLevel < int.MaxValue)
                    {
                        startLevel = Mathf.Max(1, lastRange.EndLevel + 1);
                    }
                }

                _config.adStatisticsLevelRanges.Add(new AdStatisticsLevelRange
                {
                    StartLevel = startLevel,
                    EndLevel = startLevel,
                    Statistics = new GameAB_CustomData
                    {
                        InterAdStartLevel = 1,
                        ReviveAdStartLevel = 1
                    }
                });
                Array.Resize(
                    ref _adStatisticsRangeFoldouts,
                    _config.adStatisticsLevelRanges.Count);
                _adStatisticsRangeFoldouts[_adStatisticsRangeFoldouts.Length - 1] = true;
            }

            EditorGUI.indentLevel--;
        }

        private static bool HasOverlappingAdStatisticsRanges(
            System.Collections.Generic.List<AdStatisticsLevelRange> ranges)
        {
            if (ranges == null)
            {
                return false;
            }

            for (int i = 0; i < ranges.Count; i++)
            {
                AdStatisticsLevelRange left = ranges[i];
                if (left == null)
                {
                    continue;
                }

                int leftStart = Mathf.Max(1, left.StartLevel);
                int leftEnd = Mathf.Max(leftStart, left.EndLevel);
                for (int j = i + 1; j < ranges.Count; j++)
                {
                    AdStatisticsLevelRange right = ranges[j];
                    if (right == null)
                    {
                        continue;
                    }

                    int rightStart = Mathf.Max(1, right.StartLevel);
                    int rightEnd = Mathf.Max(rightStart, right.EndLevel);
                    if (leftStart <= rightEnd && rightStart <= leftEnd)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string[] DrawStringArray(string label, string[] values)
        {
            values ??= Array.Empty<string>();
            int currentSize = values.Length;
            int newSize = Mathf.Max(
                0,
                EditorGUILayout.IntField(label + "数量", currentSize));
            if (newSize != currentSize)
            {
                Array.Resize(ref values, newSize);
            }

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = EditorGUILayout.TextField(
                    label + " " + (i + 1),
                    values[i]);
            }

            return values;
        }

        private void DrawAdsConfig()
        {
            EditorGUI.indentLevel++;
            if (_config.sourceAds == null)
            {
                _config.sourceAds = new System.Collections.Generic.List<AdConfig>();
            }

            EditorGUILayout.LabelField("广告源列表", EditorStyles.boldLabel);
            for (int i = 0; i < _config.sourceAds.Count; i++)
            {
                AdConfig adConfig = _config.sourceAds[i];
                if (adConfig == null)
                {
                    adConfig = new AdConfig(E_AdsSource.Max);
                    _config.sourceAds[i] = adConfig;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    "广告项 " + (i + 1),
                    EditorStyles.boldLabel);
                if (GUILayout.Button("移除", GUILayout.Width(55f)))
                {
                    _config.sourceAds.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();
                adConfig.AdsSource = (E_AdsSource)EditorGUILayout.EnumPopup(
                    "广告源",
                    adConfig.AdsSource);
                adConfig.rewardAdId = EditorGUILayout.TextField(
                    "奖励广告 ID",
                    adConfig.rewardAdId);
                adConfig.interAdId = EditorGUILayout.TextField(
                    "插屏广告 ID",
                    adConfig.interAdId);
                adConfig.bannerAdId = EditorGUILayout.TextField(
                    "横幅广告 ID",
                    adConfig.bannerAdId);
                adConfig.openAdId = EditorGUILayout.TextField(
                    "开屏广告 ID",
                    adConfig.openAdId);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3f);
            }

            if (GUILayout.Button("添加广告源"))
            {
                _config.sourceAds.Add(new AdConfig(E_AdsSource.Max));
            }

            _config.useInterReplenishReward = EditorGUILayout.Toggle(
                "使用插屏补充激励",
                _config.useInterReplenishReward);
            _config.useRewardReplenishInter = EditorGUILayout.Toggle(
                "使用激励补充插屏",
                _config.useRewardReplenishInter);
            EditorGUI.indentLevel--;
        }

        #endregion


        #region New

        private void NewConfig()
        {
            bool confirm =
                EditorUtility.DisplayDialog(
                    "新建配置",
                    "确定创建新的 ChannelConfig？\n\n" +
                    "当前配置不会保存。",
                    "确定",
                    "取消");


            if (!confirm)
            {
                return;
            }


            ClearSessionBackup();

            CreateNewConfig();

            Repaint();
        }

        #endregion


        #region Path

        /// <summary>
        /// Assets/StreamingAssets
        /// </summary>
        private static string GetStreamingAssetsPath()
        {
            return Path.Combine(
                Application.dataPath,
                "StreamingAssets");
        }


        /// <summary>
        /// Assets/StreamingAssets/ChannelConfig.bytes
        /// </summary>
        private static string GetConfigPath()
        {
            return Path.Combine(
                GetStreamingAssetsPath(),
                CONFIG_FILE_NAME);
        }

        #endregion


        #region Save

        /// <summary>
        /// 保存到：
        ///
        /// Assets/StreamingAssets/ChannelConfig.bytes
        /// </summary>
        private void SaveToStreamingAssets()
        {
            if (_config == null)
            {
                EditorUtility.DisplayDialog(
                    "保存失败",
                    "ChannelConfig 为空。",
                    "确定");

                return;
            }


            try
            {
                // =====================================
                // 创建 StreamingAssets
                // =====================================

                string directory =
                    GetStreamingAssetsPath();


                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }


                // =====================================
                // 自定义二进制序列化
                // =====================================

                byte[] data =
                    ChannelConfigBinarySerializer.Serialize(
                        _config);


                // =====================================
                // 保存
                // =====================================

                string path =
                    GetConfigPath();


                File.WriteAllBytes(
                    path,
                    data);


                // =====================================
                // 刷新 Unity
                // =====================================

                AssetDatabase.Refresh();


                Debug.Log(
                    "====================================\n" +
                    "ChannelConfig 保存成功\n" +
                    $"Path: {path}\n" +
                    $"Size: {data.Length} Bytes\n" +
                    "====================================");


                EditorUtility.DisplayDialog(
                    "保存成功",
                    "ChannelConfig 保存成功！\n\n" +
                    $"文件：\n{path}\n\n" +
                    $"大小：{data.Length} Bytes",
                    "确定");

                // 正式配置已经写入磁盘，不再需要编译重载备份。
                ClearSessionBackup();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "ChannelConfig 保存失败：\n" +
                    e);


                EditorUtility.DisplayDialog(
                    "保存失败",
                    e.Message,
                    "确定");
            }
        }

        #endregion


        #region Load

        /// <summary>
        /// 窗口已经存在时，再次从菜单打开需要重新读取磁盘配置。
        /// </summary>
        private void RefreshFromStreamingAssetsOnOpen()
        {
            if (!TryLoadFromStreamingAssets(false) &&
                _config == null)
            {
                CreateNewConfig();
            }
        }

        /// <summary>
        /// 从：
        ///
        /// Assets/StreamingAssets/ChannelConfig.bytes
        ///
        /// 加载配置。
        /// </summary>
        private void LoadFromStreamingAssets()
        {
            string path = GetConfigPath();

            if (!TryLoadFromStreamingAssets(true))
            {
                return;
            }

            ClearSessionBackup();

            EditorUtility.DisplayDialog(
                "读取成功",
                "ChannelConfig 读取成功！\n\n" +
                path,
                "确定");
        }


        /// <summary>
        /// 尝试从 StreamingAssets 加载配置。
        /// 自动加载时不弹窗，避免每次编译或打开窗口打断工作流。
        /// </summary>
        private bool TryLoadFromStreamingAssets(bool showErrorDialog)
        {
            string path = GetConfigPath();

            if (!File.Exists(path))
            {
                if (showErrorDialog)
                {
                    EditorUtility.DisplayDialog(
                        "读取失败",
                        "StreamingAssets 中不存在配置文件。\n\n" +
                        path,
                        "确定");
                }

                return false;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                ApplySerializedConfig(data, path);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "ChannelConfig 加载失败：\n" +
                    e);

                if (showErrorDialog)
                {
                    EditorUtility.DisplayDialog(
                        "读取失败",
                        e.Message,
                        "确定");
                }

                return false;
            }
        }


        /// <summary>
        /// 应用一份序列化后的配置。
        /// </summary>
        private void ApplySerializedConfig(byte[] data, string source)
        {
            if (data == null || data.Length == 0)
            {
                throw new Exception(
                    "ChannelConfig.bytes 内容为空。");
            }

            ChannelConfig config =
                ChannelConfigBinarySerializer.Deserialize(data);

            if (config == null)
            {
                throw new Exception(
                    "读取出来的 ChannelConfig 为空。");
            }

            _config = config;
            ChannelConfig.Instance = _config;

            Repaint();

            Debug.Log(
                "====================================\n" +
                "ChannelConfig 加载成功\n" +
                $"Path: {source}\n" +
                "====================================");
        }


        /// <summary>
        /// 在 Unity 脚本重载前保存当前编辑内容，避免未点击“保存配置”时编译导致内容丢失。
        /// </summary>
        private void SaveSessionBackupBeforeAssemblyReload()
        {
            if (_config == null)
            {
                return;
            }

            try
            {
                byte[] data =
                    ChannelConfigBinarySerializer.Serialize(_config);

                SessionState.SetString(
                    GetSessionBackupKey(),
                    Convert.ToBase64String(data));
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "ChannelConfig 编译前临时保存失败：\n" +
                    e);
            }
        }


        /// <summary>
        /// 尝试恢复上一次脚本重载前的编辑内容。
        /// </summary>
        private bool TryLoadSessionBackup()
        {
            string encoded =
                SessionState.GetString(
                    GetSessionBackupKey(),
                    string.Empty);

            if (string.IsNullOrEmpty(encoded))
            {
                return false;
            }

            try
            {
                byte[] data = Convert.FromBase64String(encoded);

                ApplySerializedConfig(
                    data,
                    "Unity 编译前临时备份");

                // 成功恢复后立即清理，避免旧备份覆盖用户之后主动读取的正式配置。
                ClearSessionBackup();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "ChannelConfig 临时备份恢复失败，将尝试读取正式配置：\n" +
                    e);

                ClearSessionBackup();
                return false;
            }
        }


        private static string GetSessionBackupKey()
        {
            return SESSION_BACKUP_KEY_PREFIX +
                   Application.dataPath.Replace('\\', '/');
        }


        private static void ClearSessionBackup()
        {
            SessionState.EraseString(GetSessionBackupKey());
        }

        #endregion
    }
}

#endif
