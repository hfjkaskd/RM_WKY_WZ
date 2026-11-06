using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using Screen = UnityEngine.Device.Screen;


public static class GameUtils
{
    public static Transform fxContainer
    {
        // get => UiManager.Instance.GetPanel<PlayerFramePanel>(UIPanelType.PlayerFramePanel).transform;
        get => null;
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        //
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    public static async UniTask DelayDo(Action action, float delayTime)
    {
        await UniTask.Delay((int)(delayTime * 1000));
        action?.Invoke();
    }

    public static bool ClickUI(int fingerId = 0)
    {
        if (EventSystem.current == null) return false;

#if UNITY_EDITOR
        if (EventSystem.current.IsPointerOverGameObject())
        {
#else
			if(EventSystem.current.IsPointerOverGameObject(fingerId)) {
#endif
            return true;
        }

        return false;
    }
}
