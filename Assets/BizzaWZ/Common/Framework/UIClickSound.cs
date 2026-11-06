using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIClickSound : MonoBehaviour
{
    // void Awake()
    // {
    //     var btn = GetComponent<Button>();
    //     btn.onClick.AddListener(() =>
    //     {
    //         SoundManager.Instance.PlaySFX("SFX_Click");
    //     });
    // }

    public EventSystem eventSystem;
 
    void OnEnable()
    {
        BizzaEventSystem.Set(EventDefine.Frame.OpenPage, OnOpenPage, true);
    }

    void OnDisable()
    {
        BizzaEventSystem.Set(EventDefine.Frame.OpenPage, OnOpenPage, false);
    }
    private void OnOpenPage(PageId obj)
    {
        SoundManager.Instance.PlaySFX("SFX_OpenPage");
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 左键或触屏按下
        {
            var obj = EventSystem.current.currentSelectedGameObject;

            if (obj != null)
            {
                LogLogger.LogInfo($"Click:{obj.name}");
                var btn = obj.GetComponent<Button>();
                if (btn != null)
                {
                    // Debug.Log($"全局检测到按钮点击：{btn.name}");
                    {
                        SoundManager.Instance.PlaySFX("SFX_Click");
                    }
                }
                else
                {
                    var bizzaButton = obj.GetComponent<BizzaButton>();
                    if (bizzaButton != null)
                    {
                        // Debug.Log($"全局检测到按钮点击：{btn.name}");
                        {
                            SoundManager.Instance.PlaySFX("SFX_Click");
                        }
                    }
                }
            }
        }
    }
}
