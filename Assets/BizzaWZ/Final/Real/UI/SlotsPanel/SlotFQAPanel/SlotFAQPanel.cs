#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UIPageIds
{
    public static readonly PageId SlotFAQPanel = "SlotFAQPanel";
}

public class SlotFAQPanel : UIPageBase
{
    public BizzaButton bizzaButton;

    protected override void OnAwake()
    {
        bizzaButton.onClick.AddListener(CloseSelf);
    }
    protected override void OnClose()
    {
   
    }

    protected override void OnOpen()
    {
        
    }
}
#endif
