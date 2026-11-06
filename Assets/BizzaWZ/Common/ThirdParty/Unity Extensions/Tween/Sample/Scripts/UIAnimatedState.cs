using UnityEngine;

namespace UnityExtensions.Tween.Sample
{
    public class UIAnimatedState : ScriptableComponent, IStackState
    {
        public CanvasGroup canvasGroup;
        public TweenPlayer showingAnimation;
        public TweenPlayer suspendingAnimation;

        void Awake()
        {
            showingAnimation.onForwardArrived += () => canvasGroup.interactable = true;
            showingAnimation.onBackArrived += () => gameObject.SetActive(false);

            suspendingAnimation.onBackArrived += () =>
            {
                canvasGroup.interactable = true;
                suspendingAnimation.gameObject.SetActive(false);
            };
        }

        public void OnReset()
        {
            showingAnimation.normalizedTime = 0f;
            showingAnimation.enabled = false;

            suspendingAnimation.normalizedTime = 0f;
            suspendingAnimation.enabled = false;

            gameObject.SetActive(false);
            suspendingAnimation.gameObject.SetActive(false);
        }

        public void OnPush()
        {
            canvasGroup.interactable = false;
            gameObject.SetActive(true);
            showingAnimation.SetForwardDirectionAndEnable();                                                                                                                                                                                   
        }

        public void OnPop()
        {
            canvasGroup.interactable = false;
            showingAnimation.SetBackDirectionAndEnable();                                                                                                                                                                                   
        }

        public void OnSuspend()
        {
            canvasGroup.interactable = false;
            suspendingAnimation.gameObject.SetActive(true);
            suspendingAnimation.SetForwardDirectionAndEnable();
        }

        public void OnResume()
        {
            suspendingAnimation.SetBackDirectionAndEnable();
        }

        public void OnUpdate(float deltaTime)
        {
        }
    }
}