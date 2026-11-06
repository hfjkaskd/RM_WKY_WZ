using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Spine.Unity;
using UnityExtensions;

public class UITeachMaskPage : UIPageBase<UITeachMaskPage.InitParam>
{
     [Obfuz.ObfuzIgnore]
    public enum Type
    {
        None,
        Path,
        Pos,
    }
  [Obfuz.ObfuzIgnore]

    public enum Shape
    {
        [LabelText("矩形")] Rect,
        [LabelText("圆形")] Circle,
    }

    public struct InitParam
    {
        public Action<UITeachMaskPage> onOpen;
        public Action onClose;
        public GameObject target;
        public Type type;
        public Vector3 worldPos;
        public string path;
        public float width;
        public float height;
        public bool block;
        public Shape shape;
        public float alpha;
        public int retryCount;
        public float retryInterval;
        public float clickCD;
        public bool bgBlock;
        public bool showHand;
    }

    public PageId LegacyPageType => UIPageIds.UI_TeachMask;

    private InitParam param;

    public RectTransform maskParent;
    public UITouchListener fakeButton;
    // public Button fakeButtonTwon;
    // public UIMask mask;
    public Image imgMask;
    public Image block;
    public SkeletonGraphic finger;
    public Sprite circleSprite;
    private GameObject target;

    private float clickTime = 0;

    private bool press = false;

    private void Awake()
    {
        void OnPointerDown(PointerEventData eventData)
        {
            if (target == null || Time.unscaledTime < clickTime)
            {
                return;
            }

            press = true;
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
        }

        void OnPointerUp(PointerEventData eventData)
        {

            if (target == null || Time.unscaledTime < clickTime || !press)
            {
                return;
            }

            press = false;
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
            CloseSelf();
        }

        void OnPointerExit(PointerEventData eventData)
        {
            if (target == null || Time.unscaledTime < clickTime || !press)
            {
                return;
            }

            press = false;
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);
        }

        fakeButton.OnTouchStart += OnPointerDown;
        fakeButton.OnTouchEnd += OnPointerUp;
        fakeButton.OnTouchExit += OnPointerExit;
    }

    protected override void OnOpen(InitParam param)
    {
        this.param = param;
        target = null;
        clickTime = float.MaxValue;
        maskParent.sizeDelta = Vector2.zero;
        maskParent.position = new Vector3(-10000, 0, 0);
        finger.gameObject.SetObjActive(false);
        block.raycastTarget = true;
        if (param.type == Type.Pos)
        {
            if (param.target != null)
            { 
                target = param.target;
            }
            Init();
        }
        else if (param.type == Type.Path)
        {
            StartCoroutine(GameObjUitl.FindGameObject(param.path, findObj =>
            {
                target = findObj;
                maskParent.gameObject.SetActive(false);
                // Invoke(nameof(Init), 0.03f);
                GameUtils.DelayDo(() =>
                {
                    Init();
                }, 0.05f);
            }, CloseSelf, param.retryInterval, param.retryCount));
        }

        this.param.onOpen?.Invoke(this);

        // fakeButtonTwon.onClick.AddListener(CloseSelf);
    }

    public static (Vector2 pos, Vector2 size) GetCanvasPosByTransform(Transform target, bool isUI)
    {
        Vector2 canvasPos;
        Vector2 size;
        if (isUI)
        {
            var rtf = target as RectTransform;
            Rect canvasRect = rtf.GetWorldRect().ScreenToCanvasRect(UIModule.Instance.UICanvas);
            canvasPos = canvasRect.center;
            size = new Vector2(canvasRect.width, canvasRect.height);
        }
        else
        {
            float rate = 1 / UIModule.Instance.UICanvas.scaleFactor;
            var worldPos = target.position;
            canvasPos = Camera.main.WorldToScreenPoint(worldPos);
            canvasPos.x *= rate;
            canvasPos.y *= rate;
            size = Vector2.zero;
        }

        return (canvasPos, size);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (param.bgBlock)
            {
                CloseSelf();
            }
        }
    }

    
    public void Init()
    {
        Vector2 canvasPos;
        Vector2 size = new Vector2(param.width, param.height);
        maskParent.gameObject.SetActive(true);
        switch (param.type)
        {
            case Type.Pos:
                var worldPos = param.worldPos;
                canvasPos = UIUtils.WorldPos2UIPos(worldPos, maskParent.parent.GetComponent<RectTransform>());
                break;
            case Type.Path:
            default:
                var (outPos, outSize) = GetCanvasPosByTransform(target.transform, target.transform is RectTransform);
                canvasPos = outPos;
                if (size is { x: <= 0, y: <= 0 })
                {
                    size = outSize;
                }
                break;
        }
        
        // maskParent
        maskParent.anchoredPosition = canvasPos;
        maskParent.sizeDelta = size;

        // maskImg
        imgMask.sprite = param.shape == Shape.Rect ? null : circleSprite;

        // fakeButton
        var btnImg = fakeButton.Target;
        btnImg.raycastTarget = param.block;

        // block
        Color color = block.color;
        color.a = param.alpha / 255;
        block.color = color;
        block.raycastTarget = param.block;
        finger.gameObject.SetObjActive(param.showHand);

        clickTime = Time.unscaledTime + param.clickCD;

        press = false;
    }

    protected override void OnShow()
    {
    }

    protected override void OnHide()
    {
    }

    protected override void OnClose()
    {
        this.param.onClose?.Invoke();
        // fakeButtonTwon.onClick.RemoveListener(CloseSelf);
    }
}


public static class UIPositionHelper
{
    /// <summary>
    /// 把世界坐标点转换到 UI 坐标（RectTransform 坐标系下）
    /// </summary>
    /// <param name="worldPos">世界坐标</param>
    /// <param name="worldCamera">渲染世界物体的相机</param>
    /// <param name="uiParent">目标 UI 的父节点（通常是 Canvas 下的某个 RectTransform）</param>
    /// <param name="uiCamera">UI 相机（如果 Canvas 是 Overlay 模式可以传 null）</param>
    /// <returns>UI 坐标 (anchoredPosition 用)</returns>
    public static Vector2 WorldToUIPosition(Vector3 worldPos, Camera worldCamera, RectTransform uiParent, Camera uiCamera = null)
    {
        // 1. 世界坐标 -> 屏幕像素坐标
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // 2. 屏幕像素坐标 -> UI局部坐标
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiParent,
            screenPos,
            uiCamera,
            out localPos);

        return localPos;
    }
}

public static partial class UIPageIds
{
    public static readonly PageId UI_TeachMask = "UI_TeachMask";
}
