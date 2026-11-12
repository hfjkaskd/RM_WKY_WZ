using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class ButtonState
{
    public int StateId;

    [LabelText("开关状态跟随按钮状态的对象")]
    public List<GameObject> StateObjects;


    [LabelText("进入时 开启的对象")]
    public List<GameObject> StateToEnableObjects;

    [LabelText("进入时 关闭的对象")]
    public List<GameObject> StateDisableObjects;

    public void SetState(bool isOn)
    {
        foreach (var stateObj in StateObjects)
        {
            stateObj?.SetActive(isOn);
        }

        if (isOn)
        {
            foreach (var stateObj in StateToEnableObjects)
            {
                stateObj?.SetActive(true);
            }

            foreach (var toDisable in StateDisableObjects)
            {
                toDisable?.SetActive(false);
            }
        }
    }
}

public class BizzaButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private Button standardButton;
    [SerializeField] private Button[] additionalStandardButtons;
    [SerializeField] private Graphic standardStateGraphic;
    [SerializeField] private Color standardNormalGraphicColor = Color.white;
    [SerializeField] private Color standardDisabledGraphicColor = Color.white;
    private bool lastStandardInteractable;

    public Transform scaleTarget;
    public bool canDrag = true;

    [SerializeField, Min(0f)]
    private float pressScaleRatio = 0.92f;

    public float longClickPeriod = 0.5f; // 长按时间

    public bool interactable = true; // 是否可交互

    public UnityEvent onLongClick;

    public UnityEvent onClick;

    private bool isLongClick = false; // 是否长按

    private float m_LastInvokeTime;

    public List<ButtonState> ButtonStates = new List<ButtonState>();

    public int StateId;

    private Vector3 m_NormalScale = Vector3.one;

    private bool m_IsPointerDown;

    private void Awake()
    {
        if (scaleTarget == null)
        {
            scaleTarget = transform.Find("Scale");
        }

        if (scaleTarget == null)
        {
            scaleTarget = transform;
        }
        m_NormalScale = scaleTarget.localScale;
        ChangeButtonState(0);
        if (standardButton != null)
        {
            ApplyStandardState();
            standardButton.onClick.AddListener(OnStandardButtonClick);
            if (additionalStandardButtons != null)
                for (int i = 0; i < additionalStandardButtons.Length; i++)
                    if (additionalStandardButtons[i] != null)
                        additionalStandardButtons[i].onClick.AddListener(OnStandardButtonClick);
        }
    }

    private void OnEnable()
    {
        if (standardButton != null)
        {
            ApplyStandardState();
        }
    }

    private void ApplyStandardState()
    {
        lastStandardInteractable = interactable;
        standardButton.enabled = isActiveAndEnabled;
        standardButton.interactable = interactable;
        if (additionalStandardButtons != null)
            for (int i = 0; i < additionalStandardButtons.Length; i++)
                if (additionalStandardButtons[i] != null)
                {
                    additionalStandardButtons[i].enabled = isActiveAndEnabled;
                    additionalStandardButtons[i].interactable = interactable;
                }
        if (standardStateGraphic != null)
            standardStateGraphic.color = interactable ? standardNormalGraphicColor : standardDisabledGraphicColor;
    }

    private void OnDestroy()
    {
        if (standardButton != null)
            standardButton.onClick.RemoveListener(OnStandardButtonClick);
        if (additionalStandardButtons != null)
            for (int i = 0; i < additionalStandardButtons.Length; i++)
                if (additionalStandardButtons[i] != null)
                    additionalStandardButtons[i].onClick.RemoveListener(OnStandardButtonClick);
    }

    private void OnStandardButtonClick()
    {
        if (!interactable || !isActiveAndEnabled)
            return;
        SoundManager.Instance.PlaySFX("SFX_Click");
        onClick.Invoke();
    }

    private void OnDisable()
    {
        if (standardButton != null)
            standardButton.enabled = false;
        if (additionalStandardButtons != null)
            for (int i = 0; i < additionalStandardButtons.Length; i++)
                if (additionalStandardButtons[i] != null)
                    additionalStandardButtons[i].enabled = false;
        RestoreNormalScale();
    }

    private void Update()
    {
        if (standardButton != null && lastStandardInteractable != interactable)
        {
            ApplyStandardState();
        }
        if (!interactable)
        {
            RestoreNormalScale();
            return;
        }
        if (interactable && isLongClick)
        {
            if (Time.time - m_LastInvokeTime >= longClickPeriod)
            {
                onLongClick.Invoke();

                m_LastInvokeTime = Time.time;
            }
        }
    }

    // 按下事件
    public void OnPointerDown(PointerEventData eventData)
    {
        if (standardButton != null)
            return;
        if (!interactable || !isActiveAndEnabled) return;
        if (scaleTarget != null)
        {
            if (!m_IsPointerDown)
            {
                m_NormalScale = scaleTarget.localScale;
                m_IsPointerDown = true;
            }

            scaleTarget.localScale = m_NormalScale * pressScaleRatio;
        }
        // if (interactable)
        // {
        // isLongClick = true;
        // }
        SoundManager.Instance.PlaySFX("SFX_Click");
    }

    // 抬起事件
    public void OnPointerUp(PointerEventData eventData)
    {
        if (standardButton != null)
            return;
        bool pressed = m_IsPointerDown;
        RestoreNormalScale();
        // isLongClick = false;
        if (pressed && interactable && isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint(gameObject.GetComponent<RectTransform>(), eventData.position))
        {
            onClick.Invoke();
        }
    }

    public void StopClick()
    {
        if (interactable)
        {
            isLongClick = false;
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (canDrag)
        {
            GetComponentInParent<ScrollRect>()?.OnDrag(eventData);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canDrag)
        {
            GetComponentInParent<ScrollRect>()?.OnBeginDrag(eventData);
        }
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        if (canDrag)
        {
            GetComponentInParent<ScrollRect>()?.OnEndDrag(eventData);
        }
    }

    private void RestoreNormalScale()
    {
        if (!m_IsPointerDown || scaleTarget == null)
        {
            return;
        }

        scaleTarget.localScale = m_NormalScale;
        m_IsPointerDown = false;
    }


    public void ChangeButtonState(int stateId)
    {
        StateId = stateId;
        foreach (var item in ButtonStates)
        {
            item.SetState(item.StateId == stateId);
        }
    }

}
