#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using AdvancedInputFieldPlugin;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnClickKeyHide : MonoBehaviour, IPointerClickHandler
{
    public KeyboardClient client;
    public void OnPointerClick(PointerEventData eventData)
    {   
        if (client == null)
            return;
        client.HideKeyboard();
    }
}
#endif