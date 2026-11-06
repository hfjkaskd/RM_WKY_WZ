#if BIZZA_REAL_WITHDRAW
using Bizza.Channel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UIBizzaAAA : UIPageBase
{
    [SerializeField] private Button btnFlowTreature;
    [SerializeField] private TMP_Text txtRewardNum;
    [SerializeField] private float flowSpeed;
    [SerializeField] private Bounds flowArea;
    [SerializeField] private float flowDelay;
    [SerializeField] private float firstFlowDelay = 30f;
    [SerializeField] private float flowShowTime = 60.0f;
    [SerializeField] private Vector2 flowCD;

    //private UIScaleAnimation levelTextScaleAnimation;
    private Coroutine flowCoroutine;
    private Vector3 flowDir = new Vector3(1, 1, 0);

    private float flowCDTimer = -1;

    private bool adsEnable = false;
    
    public void Awake()
    {
        btnFlowTreature.onClick.AddListener(OnBtnFlowTreatureClick);
    }

    

    public void OnEnable()
    {
        adsEnable = true;
        flowDir.Normalize();
        flowCDTimer = Time.time + firstFlowDelay;
        btnFlowTreature.gameObject.SetActive(false);
    }

    public ItemEntry thisTimeMoney;
    public float thisTimeRatio;
    

    public void OnDisable()
    {
        thisTimeMoney = ItemEntry.None;
        thisTimeRatio = 0;
        if (flowCoroutine != null)
            StopCoroutine(flowCoroutine);

    }

    private void Update()
    {
        WaitFlowTreature();
        MoveFlowTreature();
    }

    private void WaitFlowTreature()
    {
        if (btnFlowTreature.gameObject.activeInHierarchy)
        {
            return;
        }

        if (flowCDTimer < Time.time && !TeachUtil.IsTeach && SaveDataUtils.TeachData.IsCompleted("Teach_01"))
        {
            flowCDTimer = Time.time + flowShowTime;
            btnFlowTreature.gameObject.SetActive(true);

            // {
            //     // float curRatio = RewardManager.Instance.GetRatio();
            //     // thisTimeMoney = InfoHelper.cDollar(BingoConfig.Instance.flowDollarReward, curRatio);
            //     // thisTimeRatio = curRatio;
            //     var item = new ItemEntry() { Type = E_ItemType.Dollar, Count = TableUtils.Global.FlowDollarNum };
            //     thisTimeMoney = item;
            //     txtRewardNum.text = ItemUtils.GetItemText(thisTimeMoney);
            // }
        }
    }

    private void MoveFlowTreature()
    {
        if (!btnFlowTreature.gameObject.activeInHierarchy)
        {
            return;
        }

        if (!CheckIsInBounds(btnFlowTreature.transform.localPosition, out Vector2 normal, out var offset))
        {
            flowDir = Vector2.Reflect(flowDir, normal);
            flowDir.Normalize();
            btnFlowTreature.transform.localPosition += (Vector3)offset;
        }

        btnFlowTreature.transform.localPosition += flowDir * (flowSpeed * Time.deltaTime);

        if (flowCDTimer < Time.time)
        {
            flowCDTimer = Time.time + Random.Range(flowCD.x, flowCD.y);
            btnFlowTreature.gameObject.SetActive(false);
        }
    }

    private bool CheckIsInBounds(Vector2 pos, out Vector2 normal, out Vector2 offset)
    {
        normal = Vector2.zero;
        offset = Vector2.zero;
        Vector3 max = flowArea.max; // + transform.position;
        Vector3 min = flowArea.min; // + transform.position;
        if (pos.x > max.x)
        {
            normal.x = -1;
            offset.x -= 10;
        }

        if (pos.x < min.x)
        {
            normal.x = 1;
            offset.x += 10;
        }

        if (pos.y > max.y)
        {
            normal.y = -1;
            offset.y -= 10;
        }

        if (pos.y < min.y)
        {
            normal.y = 1;
            offset.y += 10;
        }

        normal.Normalize();

        return normal.sqrMagnitude < 0.001f;
    }

    private void OnBtnFlowTreatureClick()
    {
        //UIController.ShowPage<UITreatureAds>();
        
        // AdModule.OpenRewardAds(E_AdPos.Flow, closeCallback: OnAdClaimSuccess);
        btnFlowTreature.gameObject.SetActive(false);
        // ItemUtils.ShowGetItemPanel(new ItemEntry()
        // {
        //     Count = thisTimeMoney,
        //     Type = E_ItemType.Dollar,
        // }, E_AdPos.Flow);

        #if BIZZA_REAL_WITHDRAW // 真网赚走这个流程
        Real_GetRewardPanelUtil.OpenGetRewardPanel(DoubleGetRewardPanel.E_UseScene.Bubble, null);
        return;
        #else
        var money = thisTimeMoney;
        if (money.Type == E_ItemType.None || money.Count <= 0)
        {
            money = ItemUtils.ToCorrect(new ItemEntry()
            {
                Type = E_ItemType.Dollar,
                Count = TableUtils.Global.FlowDollarNum,
            });
        }
            
        var coin = new ItemEntry();
        coin.Type = E_ItemType.Gold;
        coin.Count = TableUtils.Global.DefaultDollarNum;
        coin = ItemUtils.ToCorrect(coin);
        
        UIModule.Instance.OpenPage<ItemEntry, ItemEntry, DoubleGetRewardPanel.E_UseScene, Action<bool>>(UIPageIds.GetRewardPanel, coin, money, DoubleGetRewardPanel.E_UseScene.Bubble, null);
        #endif
        
        
    }

    protected override void OnOpen()
    {
        
    }

    protected override void OnClose()
    {
        
    }
}
#endif
