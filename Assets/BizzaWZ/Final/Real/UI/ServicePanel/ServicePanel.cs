#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;
using AdvancedInputFieldPlugin;


public partial class UIPageIds
{
    public static readonly PageId ServicePanel = "ServicePanel";
}

public class ServicePanel : UIPageBase
{
    [Header("Chat UI")]
    public ChatElement chatElementPrefab; // 聊天预制件
    public Transform contentRoot; // 聊天内容根节点
    public ScrollRect scrollRect; // 滚动容器

    [Header("Input")]
    public AdvancedInputField inputText; // 输入框
    public AllowKeyboardDismiss allowKeyboardDismiss; // 允许输入框丢失焦点

    public BizzaButton selectQuestionButton; // 选择问题按钮
    public TMP_Text selectQuestionText; // 选择问题按钮文本

    public ViewportResizer viewportResizer; // 视口调整器

    [Header("Send State")]
    public GameObject canSendObj;
    public GameObject notCanSendObj;
    public bool CanSend
    {
        get
        {
            bool _hasContent = !string.IsNullOrWhiteSpace(inputText.Text);
            return _hasContent;
        }
    }

    [Header("按钮")]
    [SerializeField] private BizzaButton faqBtn;
    [SerializeField] private BizzaButton closeBtn;
    [SerializeField] private BizzaButton historyBtn;
    [SerializeField] private BizzaButton sendBtn;
    [SerializeField] private BizzaButton canNotSendBtn;
    [SerializeField] private BizzaButton clearBtn;
    
    [SerializeField] private BizzaButton defaultQABtn;

    protected override void OnAwake()
    {
        base.OnAwake();
        faqBtn.onClick.AddListener(() => { OnClickQFA(); });
        closeBtn.onClick.AddListener(() => { CloseSelf(); });
        historyBtn.onClick.AddListener(() => { OnClickHistory(); });
        sendBtn.onClick.AddListener(() => { OnClickSend(); });
        canNotSendBtn.onClick.AddListener(() => { OnClickSend(); });
        clearBtn.onClick.AddListener(() => { OnClickClearInput(); });
        defaultQABtn.onClick.AddListener(() => { OnClickOpenSelectPanel(); });

    }

    public List<ChatElement> chatElements = new();

    private KeyboardClient keyboardClient;
    public KeyboardClient client
    {
        get
        {
            if (keyboardClient == null)
            {
                keyboardClient = GetComponentInChildren<KeyboardClient>();
            }
            return keyboardClient;
        }
    }

    private void OnEnable()
    {
        NativeKeyboardManager.AddKeyboardHeightChangedListener(OnKeyboardHeightChanged);
    }

    private void OnDisable()
    {
        NativeKeyboardManager.RemoveKeyboardHeightChangedListener(OnKeyboardHeightChanged);
    }

    protected override void OnClose()
    {
        client.HideKeyboard();
    }

    protected override void OnOpen()
    {
        if (viewportResizer == null)
        {
            viewportResizer = GetComponentInChildren<ViewportResizer>();
        }
        inputText.Text = "";
        selectQuestionText.text = LanguageUtils.GetText("ServicePanel_Please");
        for (int i = contentRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.transform.GetChild(i).gameObject);
        }
        chatElements.Clear();
        RefreshSendState(false);
        SwitchDefaultInputState(true);
        var request = AccountModule.Instance.CreateOceanShineFeedbackListV2Request();
        AccountModule.Instance.Request_FeedbackListV2Request(request, OnRefreshPanle);// 这里进行HTTP请求
    }

    List<ChatInfo> records = new();
    private void OnRefreshPanle(FailHttpResponse<AccountModule.OceanShineFeedbackListV2Response> response) // 
    {
        if (this == null)
        {
            return;
        }
        records.Clear();
        foreach (var loaclData in SaveDataUtils.GameData.chatInfos)
        {
            records.Add(loaclData);
        }
        if (response.success && response.data != null)
        {
            var data = response.data.GetSortedFeedbackByCreatedTime();
            SaveDataUtils.GameData.serviceAlter = false;
            BizzaEventSystem.Emit(EventDefine.Item.ServiceDataAlter);
            SaveDataUtils.Save();
            foreach (var feedbackData in data)
            {
                bool isRepetition = false;
                string _content = feedbackData.Os_Ctt;
                foreach (var loaclData in SaveDataUtils.GameData.chatInfos)
                {
                    if (string.Equals(loaclData.chatcontent, _content))
                    {
                        isRepetition = true;
                        break;
                    }
                }
                if (isRepetition)
                {
                    continue;
                }
                records.Add(new ChatInfo()
                {
                    time = TimeUtil.ConvertServerTimestampToTimeString(feedbackData.Os_Cte),
                    spokesperson = (Spokesperson)feedbackData.Os_Tpe,
                    chatcontent = feedbackData.Os_Ctt,
                });
            }
            records.Sort((a, b) =>
            {
                // 1. 首先比较时间 (精确到毫秒)
                // 将字符串转换为 long，确保保留了完整的时间戳精度
                long timeA = TimeUtil.ConvertStringToLong(a.time);
                long timeB = TimeUtil.ConvertStringToLong(b.time);
                // Debug.Log("string timeA: " + a.time + " timeB: " + b.time);
                // Debug.LogError("long timeA: " + timeA + " timeB: " + timeB);

                // 【关键点】使用 CompareTo 进行比较
                // 这比直接相减 (timeA - timeB) 更安全，能防止数值溢出导致的排序错误
                // 同时能保证精确到 long 的最小单位（1毫秒）
                int timeCompare = timeA.CompareTo(timeB);

                // 如果时间不相同，直接返回时间的比较结果
                if (timeCompare != 0)
                {
                    return timeCompare;
                }

                // 2. 如果时间完全相同 (精确到毫秒)，则比较 Spokesperson
                // 需求：Issue (2) 排在 Player (1) 前面 (降序)
                return b.spokesperson.CompareTo(a.spokesperson);
            });
        }

        foreach (var record in records)
        {
            CreateElements(record, 0, false);
        }


    }

    private float gapTime = 0.5f; private float currentTime = 0f;
    private void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime <= gapTime) { return; }
        currentTime = 0f;
        RefreshSendState(CanSend);
    }

    private void SwitchDefaultInputState(bool defaultState)
    {
        selectQuestionButton.gameObject.SetActive(defaultState);
        inputText.gameObject.SetActive(!defaultState);
    }

    private void RefreshSendState(bool canSend)
    {
        canSendObj.SetActive(canSend);
        notCanSendObj.SetActive(!canSend);
    }

    private void OnKeyboardHeightChanged(int height)
    {
        if (height <= 0)
        {
            //Debug.LogError("键盘高度小于0");
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(allowKeyboardDismiss.gameObject);
        }
        viewportResizer.UpdateViewportBottom(height);
        //Debug.LogError("键盘高度发生变化");
    }

    public void OnClickClearInput() // 点击 × 按钮
    {
        //Debug.LogError("点击 × 按钮");
        ClearInputContent();
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(allowKeyboardDismiss.gameObject);
        SwitchDefaultInputState(true);
    }

    private void ClearInputContent() // 清空输入框内容
    {
        inputText.Text = "";
        selectQuestionText.text = LanguageUtils.GetText("ServicePanel_Please");
    }

    private ChatInfo chatInfo = new ChatInfo();
    public void OnClickSend() // 发送按钮点击事件
    {
        if (string.IsNullOrWhiteSpace(inputText.Text))
        {
            return;
        }
        chatInfo.Clear();
        string currentTime = GetCurrentTimeString();  // 当前时间
        chatInfo.time = currentTime; // 当前时间
        chatInfo.chatcontent = inputText.Text;
        chatInfo.spokesperson = Spokesperson.Player;
        if (selectIndex == customIndex)
        {
            AccountModule.Instance.Request_FeedbackRequest(AccountModule.Instance.CreateFeedbackRequest(inputText.Text));
        }
        CreateElements(chatInfo, 0);
        ReplayDefaultQ(selectIndex);
        ClearInputContent();
    }

    private int selectIndex;
    private int customIndex = -2;
    public void FillDefaultQuent(string info, int index) // 二级界面选择的信息填充到输入框
    {
        selectQuestionText.text = info;
        inputText.Text = info;
        selectIndex = index;
    }

    public void FillCustomQuent() // 自定义信息填充到输入框
    {
        SwitchDefaultInputState(false);
        inputText.Text = "";
        selectIndex = customIndex;
    }

    public void OnClickInputClose() // 点击输入框丢失
    {
        client.HideKeyboard();
        SwitchDefaultInputState(false);
    }

    public void OnClickOpenSelectPanel() // 点击按钮弹出二级界面
    {
        if (UIModule.Instance.GetPage(UIPageIds.ServiceSelectPanel) == null)
        {
            UIModule.Instance.OpenPage(UIPageIds.ServiceSelectPanel, this).Forget();
        }
    }


    public void OnClickQFA()
    {
        UIModule.Instance.OpenPage(UIPageIds.QFA).Forget();
    }

    public void OnClickHistory()
    {
        UIModule.Instance.OpenPage(UIPageIds.WithdrawHistory).Forget();
    }

    public void OnClickClose()
    {
        CloseSelf();
    }

    [SerializeField] private int delayShowTime = 1;
    public void ReplayDefaultQ(int index)
    {
        if (selectIndex == customIndex) return;
        ChatInfo chatInfo = new ChatInfo()
        {
            time = GetCurrentTimeString(delayShowTime),
            chatcontent = LanguageUtils.GetText($"ServicePanel_A{index + 1}"),
            spokesperson = Spokesperson.Issue
        };
        CreateElements(chatInfo, delayShowTime);

    }

    public string GetCurrentTimeString(int addSeconds = 0)
    {
        return System.DateTime.Now.AddSeconds(addSeconds).ToString("yyyy-MM-dd HH:mm:ss");
    }

    [Button("CreateElements")]
    public async UniTaskVoid CreateElements(ChatInfo chatInfo, int delay = 0, bool isRecord = true)
    {
        if (isRecord) RecordServiceMessage(chatInfo);
        if (delay > 0)
        {
            await UniTask.Delay(delay * 1000);
        }
        if (this == null) return;
        var ele = GameObject.Instantiate(chatElementPrefab, contentRoot, false);
        ele.Init(
            chatInfo
        );
        chatElements.Add(ele);
        ScrollToLatestMessage().Forget();
    }

    private async UniTaskVoid ScrollToLatestMessage()
    {
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        RefreshScrollToBottom();
    }

    private void RefreshScrollToBottom()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        }

        if (contentRoot is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (scrollRect.viewport != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.viewport);
        }

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void RecordServiceMessage(ChatInfo chatInfo)
    {
        SaveDataUtils.GameData.chatInfos.Add(chatInfo);
        SaveDataUtils.gameStrategy.SaveData();
    }


}
#endif