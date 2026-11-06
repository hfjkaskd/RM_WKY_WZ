using UnityEngine.UI;

namespace UnityExtensions.Tween.Sample
{
    public class PlayingState : UIAnimatedState
    {
        public Text text;

        public void SetLevel(int id)
        {
            text.text = $"Level-{id}";
        }
    }
}