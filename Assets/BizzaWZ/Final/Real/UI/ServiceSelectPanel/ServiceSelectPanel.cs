#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class UIPageIds
{
    public static readonly PageId ServiceSelectPanel = "ServiceSelectPanel";
}

public class ServiceSelectPanel : UIPageBase<ServicePanel>
{
    public BizzaButton[] defaultButtons;
    public TMP_Text[] defaultTexts;
    public BizzaButton customButton;

    private ServicePanel servicePanel1;
    [SerializeField] private BizzaButton closeBtn;

    protected override void OnAwake()
    {
        base.OnAwake();
        for (int i = 0; i < defaultButtons.Length; i++)
        {
            int index = i;
            defaultButtons[i].onClick.AddListener(() => OnClickDefaultButton(index));
        }
        customButton.onClick.AddListener(OnclickCustomButton);
        closeBtn.onClick.AddListener(() => { CloseSelf(); });
    }

    protected override void OnClose()
    {

    }

    protected override void OnOpen(ServicePanel servicePanel)
    {
        this.servicePanel1 = servicePanel;
    }

    public void OnClickDefaultButton(int index)
    {
        servicePanel1.FillDefaultQuent(defaultTexts[index].text, index);
        CloseSelf();
    }

    public void OnclickCustomButton()
    {
        servicePanel1.FillCustomQuent();
        CloseSelf();
    }
}
#endif