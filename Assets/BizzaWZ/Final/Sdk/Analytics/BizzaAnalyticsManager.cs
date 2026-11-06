#if BIZZA_REAL_WITHDRAW
// using System;
// using System.Collections.Generic;
// using System.Text;
// using UnityEngine;
//
//
// namespace Bizza.Sdk
// {
//     public class Manager : MonoBehaviour
//     {
//         private static Dictionary<string, object> eventCache = new Dictionary<string, object>();
//
//         #region 业务侧封装
//
//
//         #endregion
//
//         #region 基础接口
//
//         public static void AddParam(string key, object val)
//         {
//             if (eventCache == null)
//                 eventCache = new Dictionary<string, object>();
//             eventCache[key] = val;
//         }
//
//         /// <summary>
//         /// 添加通用参数
//         /// </summary>
//         public static void AddCommonParams()
//         {
//             string openUdid = ChannelConfig.Instance.tkConfig.appCode; //+ HttpController.Instance.infoData.ouid;
//             AddParam("appcode", openUdid);
//         }
//
//         public static void SendCustomEvent(string eventName)
//         {
//             AnalyticsHelper.SendCustomEvent(eventName, new Dictionary<string, object>(eventCache));
//             eventCache.Clear();
//         }
//
//         #endregion
//     }
//
//     public static class StringTool
//     {
//         public static string ToValueString<T, K>(this ICollection<KeyValuePair<T, K>> dictionary)
//         {
//             StringBuilder sb = new();
//             sb.Append('{');
//             foreach (var kvp in dictionary)
//             {
//                 sb.Append("\n{");
//                 sb.Append(kvp.Key.ToString());
//                 sb.Append(": ");
//                 sb.Append(kvp.Value.ToString());
//                 sb.Append("},");
//             }
//             if (sb.Length > 1)
//             {
//                 sb.Append('\n');
//             }
//             sb.Append('}');
//             return sb.ToString();
//         }
//     }
//
//     public static class EventName
//     {
//         //接入文档： https://bizzagame.feishu.cn/sheets/YMXhs0OlNhwR1Itz1pUch4BsnHf
//         public const string Ad_ShowPV = "show_ads_pv";
//         public const string Teach_Start = "newbie_guide_start";
//         public const string Teach_End = "newbie_guide_end";
//
//         public const string GameStart = "level_battle_start";
//         public const string GameWin = "level_battle_win";
//         public const string GameLose = "level_battle_fail";
//
//         public const string SelectPlane = "Select_Plane";
//         public const string SelectMainPage = "Select_Main_Page_";
//
//         public const string UpgradeTalent = "talent_ui_levelup";
//
//         public const string ClickCard = "collection_ui_click";
//         public const string CollectionReward = "collection_ui_reward";
//
//         public const string MusicSwitch = "Music_Switch";
//         public const string SoundSwitch = "Sound_Switch";
//         public const string VibrateSwitch = "Vibrate_Switch";
//         public const string SelectLanguage = "Select_Language";
//
//         public const string GameFPS = "Game_FPS";
//         public const string GameLeaderHurt = "level_battle_hurt";
//
//         public const string CopyUI_BattleStart = "copy_ui_battle";
//
//         #region Battle Shop
//
//         public const string GameShopShow = "level_battle_shop_show";
//         public const string GameShopItemBuy = "level_battle_shop_buy";
//         public const string GameShopItemDisplay = "level_battle_shop_item_display";
//         public const string GameShopItemSell = "level_battle_shop_sell";
//         public const string GameShopClose = "level_battle_shop_close";
//         #endregion
//
//         public const string AccLvUp = "account_level_up";
//
//         public const string ShopBoxOpen_Equip = "shop_equip_open";
//         public const string ShopBoxOpen_Role = "shop_role_open";
//
//         public const string TaskReward_Daily = "daily_ui_reward";
//         public const string TaskReward_Activity = "daily_ui_activity_reward";
//
//         public const string Equip_Wear = "equip_ui_wear";
//         public const string Equip_LevelUp = "equip_ui_levelup";
//         public const string Equip_Merge = "equip_ui_merge";
//
//         public const string Role_Open = "role_ui_rootclick";
//         public const string Role_SkillInfo = "role_ui_skillinfo";
//         public const string Role_Select = "role_select_click";//出战
//
//         public const string Role_UpgradeShow = "role_ui_upgrade_show";
//         public const string Role_StarUpShow = "role_ui_starup_show";
//
//         public const string Role_UpgradeClick = "role_ui_upgrade_click";
//         public const string Role_StarUpClick = "role_ui_starup_click";
//
//         public const string Role_Unlock = "role_unlock_click";
//         public const string Role_Buy = "role_buy_click";
//
//         public const string ModifyRes = "modify_account_res";
//
//         public const string BattleSkillCilick = "battle_skill_click";
//
//         public const string BattleGacha = "battle_gacha";
//     }
// }
//
#endif
