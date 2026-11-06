#if BIZZA_REAL_WITHDRAW
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileInputBinder : MonoBehaviour, IPointerDownHandler
{
    private TMP_InputField input;

    private void Awake()
    {
        input = GetComponent<TMP_InputField>();
        input.shouldHideMobileInput = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MobileKeyboardInputManager.Instance.Focus(input);
    }
}
#endif