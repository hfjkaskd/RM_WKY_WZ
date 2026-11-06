#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 在每次胜利/失败结算时采集假提现快照。金额、阶段变化本身不触发上报。
/// FakeWithdraw 各金额档位独立保存阶段，因此每档一条；DanWithdraw 一条。
/// </summary>
public static class FakeWithdrawalAnalytics
{
    private static FakeWithdrawPanel _fakeConfig;
    private static WithdrawDanPanel _danConfig;
    private static string _settlementId;
    private static bool _settled;

    public static async UniTask PreloadAsync()
    {
        // 仅读取预制体上的配置，不实例化或打开提现页面。
        try
        {
            var prefab = await AssetUtils.LoadAssetAsync<GameObject>("UIPanel/FakeWithdrawPanel");
            _fakeConfig = prefab != null ? prefab.GetComponent<FakeWithdrawPanel>() : null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[FakeWithdrawalAnalytics] FakeWithdraw 配置加载失败：" + exception.Message);
        }

        try
        {
            var prefab = await AssetUtils.LoadAssetAsync<GameObject>("UIPanel/WithdrawDanPanel");
            _danConfig = prefab != null ? prefab.GetComponent<WithdrawDanPanel>() : null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[FakeWithdrawalAnalytics] DanWithdraw 配置加载失败：" + exception.Message);
        }
    }

    public static void BeginLevelAttempt()
    {
        _settlementId = Guid.NewGuid().ToString("N");
        _settled = false;
    }

    public static void TrackLevelEnd(int levelNumber, bool success)
    {
        if (_settled || string.IsNullOrEmpty(_settlementId) || SaveDataUtils.GameData == null
            || SaveDataUtils.ItemData == null)
        {
            return;
        }

        string currency = AccountModule.CountryType switch
        {
            AccountModule.E_CountryType.US => "USD",
            AccountModule.E_CountryType.BR => "BRL",
            AccountModule.E_CountryType.ID => "IDR",
            _ => null,
        };
        if (currency == null) return;

        // 同一结算只采集一次；复活成功或开始下一次关卡尝试后允许再次采集。
        _settled = true;
        try
        {
            TrackFakeWithdraw(levelNumber, success, currency);
            TrackDanWithdraw(levelNumber, success, currency);
        }
        catch (Exception exception)
        {
            // 统计失败不能中断胜负结算。
            Debug.LogWarning("[FakeWithdrawalAnalytics] 结算快照上报失败：" + exception.Message);
        }
    }

    private static void TrackFakeWithdraw(int levelNumber, bool success, string currency)
    {
        var stages = SaveDataUtils.FakeWithDrawPanelData?.curStageList;
        if (_fakeConfig == null || stages == null) return;
        var configs = _fakeConfig.withdrawMissionSOList;
        if (configs == null || configs.Count == 0 || configs[0] == null) return;

        double balance = ItemUtils.GetItemCount(E_ItemType.Dollar);
        int saveIndexOffset = AccountModule.CountryType == AccountModule.E_CountryType.US ? 1 : 0;
        for (int index = 0; index < configs.Count; index++)
        {
            var config = configs[index];
            int saveIndex = index + saveIndexOffset;
            if (config == null || saveIndex >= stages.Count) continue;

            bool starter = saveIndexOffset == 0 && index == 0;
            int stage = starter
                ? (SaveDataUtils.GameData.fakeWithdrawPanelFirstWithdraw ? 1 : 0)
                : Math.Max(0, stages[saveIndex]);
            var data = CreateSnapshot("FakeWithdraw", saveIndex, stage, config,
                configs[0].withdrawMoney, balance, currency, levelNumber, success);
            data["is_starter_tier"] = starter;
            if (starter)
            {
                data["stage_completed"] = SaveDataUtils.GameData.fakeWithdrawPanelFirstWithdraw;
            }
            BizzaAnalyticsAgent.Track("withdrawal_stage_enter", data);
        }
    }

    private static void TrackDanWithdraw(int levelNumber, bool success, string currency)
    {
        var save = SaveDataUtils.WithDrawDanPanelData;
        if (_danConfig == null || save == null) return;
        var config = _danConfig.withdrawMissionSO;
        if (config == null) return;

        var data = CreateSnapshot("DanWithdraw", 0, Math.Max(0, save.curStageIndex), config,
            config.withdrawMoney, ItemUtils.GetItemCount(E_ItemType.WithDrawDanDollar),
            currency, levelNumber, success);
        BizzaAnalyticsAgent.Track("withdrawal_stage_enter", data);
    }

    private static Dictionary<string, object> CreateSnapshot(
        string type, int tierIndex, int stage, WithDrawMissionSO config,
        double firstThreshold, double balance, string currency, int levelNumber, bool success)
    {
        var mission = config.GetMissionByStateSafe(stage);
        var data = new Dictionary<string, object>
        {
            { "withdrawal_type", type },
            { "withdrawal_tier_index", tierIndex },
            { "withdrawal_stage", stage },
            { "stage_threshold_amount", (double)config.withdrawMoney },
            { "first_withdrawal_threshold", firstThreshold },
            { "current_balance", balance },
            { "balance_unit", currency },
            { "level_id", levelNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "result", success ? "win" : "fail" },
            { "settlement_id", _settlementId },
            { "source", "level_end" },
            { "stage_completed", stage >= config.missions.Count },
        };
        // 阶段还可能要求通关、看广告或登录天数，不能都当成金额门槛。
        if (mission?.conditionData != null)
        {
            data["stage_condition"] = mission.conditionData.condition.ToString();
            data["stage_target"] = (double)mission.conditionData.targetValue;
        }
        return data;
    }
}
#endif
