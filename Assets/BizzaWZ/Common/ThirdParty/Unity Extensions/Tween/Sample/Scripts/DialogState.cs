using System;
using UnityEngine.UI;

namespace UnityExtensions.Tween.Sample
{
    public class DialogState : UIAnimatedState
    {
        public Text text;

        public Action onOKClick;

        public void SetText(string text)
        {
            this.text.text = text;
        }

        public void OnOKClick()
        {
            onOKClick?.Invoke();
        }
    }
}