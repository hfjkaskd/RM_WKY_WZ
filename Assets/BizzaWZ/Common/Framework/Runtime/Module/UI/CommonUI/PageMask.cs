using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PageMask : MonoBehaviour
{
    public Button btn;
    public bool tapToClose;

    void Awake()
    {
        btn.onClick.AddListener(() =>
        {
            if (tapToClose)
            {
                var panel = GetComponentInParent<UIPageBase>();
                if (panel != null)
                {
                    UIModule.Instance.ClosePage(panel);
                }
            }

        });
    }
}
