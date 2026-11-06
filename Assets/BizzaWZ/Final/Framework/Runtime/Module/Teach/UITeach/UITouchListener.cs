using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Graphic))]
public class UITouchListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private Graphic target;
    public Graphic Target => target;

    public event Action<PointerEventData> OnTouchStart;
    public event Action<PointerEventData> OnTouchEnd;
    public event Action<PointerEventData> OnTouchExit;

    private void Awake()
    {
        target = GetComponent<Graphic>();
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        OnTouchStart?.Invoke(eventData);
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        OnTouchEnd?.Invoke(eventData);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        OnTouchExit?.Invoke(eventData);
    }
}
