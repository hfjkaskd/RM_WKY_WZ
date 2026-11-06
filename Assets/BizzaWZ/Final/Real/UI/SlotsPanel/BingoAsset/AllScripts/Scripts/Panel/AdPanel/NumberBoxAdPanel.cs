#if BIZZA_REAL_WITHDRAW
// using Spine;
// using Spine.Unity;
// using System.Collections;
// using System.Collections.Generic;
// using DG.Tweening;
// using Unity.VisualScripting;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class NumberBoxAdPanel : BasePanel
// {
//     public Button adButton;

//     public Button leaveButton;

//     public Button claimButton;

//     public SlotMachineManager slotMachine;

//     ItemEntry curReward;

//     public AdBottom adBottom;

//     public Transform[] posArray;

//     public TextMeshProUGUI adText;

//     public TextMeshProUGUI freeText;

//     public TextMeshProUGUI noThanksText;
//     private E_AdPos _adPos = E_AdPos.NumberBoxRewardAd;

//     public void SetNumberBoxReward(ItemEntry res, bool isAd)
//     {
//         if (res.Type <= E_ItemType.Dollar)
//         {
//             for (int i = 0; i < posArray.Length; i++)
//             {
//                 if (res.Type == E_ItemType.Dollar)
//                 {
//                     RewardManager.Instance.AddReward(NumberRewardType.Dollar, res.Count / 3, isAd, PoolItemEnum.BigAddMoneyFx, pos: posArray[i].position);
//                 }
//                 else if (res.Type == E_ItemType.Gold)
//                 {
//                     RewardManager.Instance.AddReward(NumberRewardType.Gold, res.Count / 3, isAd, PoolItemEnum.BigAddCoinFx, pos: posArray[i].position);
//                 }
//                 else if (res.Type == E_ItemType.AmazonCoin)
//                 {
//                     GameUtils.PlayAddMoneyFx(posArray[i].position, true, PoolItemEnum.AddItemFx, () =>
//                     {
//                         PlayerDataManager.Instance.AmazonCoins += res.Count;
//                     }, ImageHelper.Instance.itemRewardIm[(int)res.Type]);
//                 }
//             }
//             DelayClosePanel(GameUtils.GetFxTime(res.Type == E_ItemType.Dollar ? PoolItemEnum.BigAddMoneyFx : PoolItemEnum.AddItemFx));
//             return;
//         }

//         slotMachine.PlayScaleAnim();
//         GameUtils.DelayDo(() =>
//         {
//             slotMachine.PlayScaleAnim2();
//             var num = res.Count;// * (isAd ? BingoConfig.Instance.NumberBoxRewardsAdRatio : 1);
//             for (int i = 0; i < num; i++)
//             {
//                 var startPos = posArray[i % 3].position;

//                 if (RemoteConfigManager.reviewMode)
//                 {
//                     var tmp = i;
//                     var trail = PoolManager.Instance.GetFromPool(PoolItemEnum.ItemTrail, GameUtils.fxContainer);
//                     trail.GetComponentInChildren<Image>().sprite = ImageHelper.Instance.itemRewardIm[(int)res.Type];
//                     trail.transform.position = startPos;
//                     var randomPos = CoinMachineManager.Instance.GetRandomPosInArea();
//                     var pos = GameUtils.PusherPos2UIWorldPos(randomPos, GameUtils.fxContainer.transform as RectTransform);
//                     // trail.transform.DOShakeScale(0.7f, 0.2f, 5);
//                     // trail.transform.DOScale(Vector3.one * 1.5f, 0.3f);
//                     // trail.transform.DOScale(Vector3.one, 0.2f).SetDelay(0.3f);
//                     trail.transform.DOBezier(startPos, pos, 0.6f + i * 0.05f, Random.Range(-0.2f, 0.2f)).OnComplete(() =>
//                     {
//                         PoolManager.Instance.BackToPool(PoolItemEnum.ItemTrail, trail);
//                         CoinMachineManager.Instance.CreatItem(res.Type, isAd, 1, randomPos);
//                     });
//                 }
//                 else
//                 {
//                     ItemUtils.AddItem(new ItemEntry() { Type = res.Type, Count = 1 }, true, isAd, startPos);
//                 }
//             }
//         }, 0.5f);
//         DelayClosePanel(0.6f);
//     }

//     private void Awake()
//     {

//         EventModule.AddListener(E_GameEvent.OnNumberBoxAnimOver, OnAnimComplete);


//     }
//     private bool _isFree = false;
//     public void SetFree(bool isFree)
//     {
//         _isFree = isFree;
//     }
//     private void AdAction()
//     {
//         AdModule.OpenRewardAds(_adPos, closeCallback: OnAdClaimSuccess);
//     }

//     private void FreeAction()
//     {
//         SetNumberBoxReward(curReward, true);
//     }

//     private void LeaveAction()
//     {
//         // SetNumberBoxReward(curReward, false);
//         string adPosStr = _adPos.ToString();
//         if (curReward.Type == E_ItemType.Dollar && TableUtils.Global.NoAdClaimMoneyAdPosMap.ContainsKey(adPosStr) && TableUtils.Global.NoAdClaimMoneyAdPosMap[adPosStr] > 0)
//         {
//             curReward.Count = curReward.Count * TableUtils.Global.NoAdClaimMoneyAdPosMap[adPosStr];
//             SetNumberBoxReward(curReward, false);
//         }
//         else
//         {
//             ClosePanel();
//         }
//         NoThanks();
//     }

//     public void PlayAnim()
//     {
//         slotMachine.PlayAnim(curReward.Type, curReward.Count);
//         //adBottom.Init(_adPos, curReward, AdAction, FreeAction, LeaveAction,key: _isFree?"Free_"+_adPos:null,thisFree: _isFree);
//     }

//     public void OnAnimComplete()
//     {
//         int temp = Random.Range(0, 101);
//         if (GameManager.Instance.CheckMode())
//         {
//             temp = 100;
//         }
//         if (temp <= BingoConfig.Instance.NumberBoxAdRatio * 100)
//         {
//             //adButton.gameObject.SetActive(true);
//             leaveButton.GetComponent<CanvasGroup>().alpha = 0;
//             leaveButton.GetComponent<CanvasGroup>().DOFade(1, 0.5f).SetDelay(1.5f);
//             leaveButton.gameObject.SetActive(true);
//         }
//         else
//         {
//             claimButton.gameObject.SetActive(true);
//         }

//     }

//     public override void ShowPanel()
//     {
//         adButton.gameObject.SetActive(false);
//         leaveButton.gameObject.SetActive(false);
//         claimButton.gameObject.SetActive(false);
//         gameObject.GetOrAddComponent<CanvasGroup>().alpha = 1;
//         UiManager.Instance.OpenMask();
//         curReward = ItemUtils.GetReward(TableUtils.Global.SlotMachineReward);
//         adBottom.Init(_adPos, curReward, AdAction, FreeAction, LeaveAction, key: _isFree ? "Free_" + _adPos : null, thisFree: _isFree);
//         base.ShowPanel();
//         if (StatisticManager.Instance != null)
//         {
//             StatisticManager.Instance.OnDisplayVideo(new Dictionary<string, object>(), _adPos);
//         }
//         Debug.Log("宝箱奖励倍率：" + AdManager.Instance.boxAdRatio);


//         slotMachine.Init();
//         GameUtils.DelayDo(() => { SoundManager.Instance.PlaySFX("箱子掉下来"); }, 0.2f);
//         GameUtils.DelayDo(() => { SoundManager.Instance.PlaySFX("箱子打开(1)"); }, 0.8f);
//         GameUtils.DelayDo(() => { SoundManager.Instance.PlaySFX("奖励翻倍"); }, 1.8f);
//     }

//     public override void ClosePanel()
//     {
//         UiManager.Instance.CloseMask();
//         GamePanel.Instance.TryResumeBingo();
//         slotMachine.CloseAllText();
//         base.ClosePanel();
//     }

//     public override void ShowPanelOver()
//     {
//         base.ShowPanelOver();
//         // UiManager.Instance.mainCanvas.GetComponent<CanvasGroup>().blocksRaycasts = true;

//         GameUtils.DelayDo(() =>
//         {
//             PlayAnim();
//         }, 1f);
//     }

//     private void OnAdClaimSuccess(bool success)//
//     {
//         if (success)
//         {
//             OnServerRespones();
//         }
//         else
//         {
//             Debug.Log("");
//             EventModule.BroadCast(E_GameEvent.ShowMask, false);
//         }
//     }

//     private void OnServerRespones()//
//     {
//         // OnAdSuccess();
//         //PlayerDataManager.Instance.OnAdWatchSuccess();
//         EventModule.BroadCast(E_GameEvent.ShowMask, false);
//         adBottom.CheckDoubleReward(GiveReward, GiveRewardDouble);
//     }

//     private void GiveReward()
//     {
//         OnAdSuccess();
//     }

//     private void GiveRewardDouble()
//     {
//         curReward.Count *= 2;
//         slotMachine.SetText(curReward.Type, curReward.Count);
//         OnAdSuccess();
//     }

//     public void OnAdSuccess()
//     {
//         E_ItemType type = curReward.Type;
//         if (type == E_ItemType.Dollar)
//         {
//             // int temp = (int)curReward.Count;
//             // curReward.Count = InfoHelper.cDollar(temp, BingoConfig.Instance.NumberBoxRewardsAdRatio);
//         }
//         else if (type == E_ItemType.Gold || type == E_ItemType.AmazonCoin)
//         {
//             // int temp = (int)curReward.Count;
//             // curReward.Count = temp * BingoConfig.Instance.NumberBoxRewardsAdRatio;
//         }
//         SetNumberBoxReward(curReward, true);

//         Dictionary<string, object> dic = new Dictionary<string, object>();
//         dic.Add("ad_id", "number_box");
//         dic.Add("is_bonus", true);
//         dic.Add("bonus", 1);
//         dic.Add("is_first", PlayerDataManager.Instance.AdWatchTimes == 0);
//         dic.Add("cost_item_detail", "item_name:" + curReward.Type.ToString() + " item_num:" + (curReward.Count * BingoConfig.Instance.NumberBoxRewardsAdRatio));
//         StatisticManager.Track("c_give_ad_reward", dic);
//     }

//     public void NoThanks()
//     {

//     }

//     public void Claim()
//     {

//     }
// }
#endif
