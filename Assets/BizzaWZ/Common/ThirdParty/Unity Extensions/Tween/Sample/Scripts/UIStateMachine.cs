
namespace UnityExtensions.Tween.Sample
{
    public class UIStateMachine : StackStateMachineComponent<UIAnimatedState>
    {
        public PlayingState playingState;
        public DialogState dialogState;
        public TweenPlayer blackScreenAnimation;

        private void Start()
        {
            blackScreenAnimation.onBackArrived += () => blackScreenAnimation.gameObject.SetActive(false);

            playingState.SetLevel(1);
            PushState(playingState);
        }

        public void PushQuitDialog()
        {
            dialogState.SetText("Do you want to quit game?");
            dialogState.onOKClick = OnOKClick;
            PushState(dialogState);

            void OnOKClick()
            {
                dialogState.onOKClick = null;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                UnityEngine.Application.Quit();
#endif
            }
        }

        public void PushStartLevelDialog(int levelID)
        {
            dialogState.SetText($"Do you want to start level-{levelID}?");
            dialogState.onOKClick = OnOKClick;
            PushState(dialogState);

            void OnOKClick()
            {
                dialogState.onOKClick = null;

                PushState(null);    // disable UI during black screen animation
                blackScreenAnimation.gameObject.SetActive(true);
                blackScreenAnimation.onForwardArrived += StartLevel;
                blackScreenAnimation.SetForwardDirectionAndEnable();

                void StartLevel()
                {
                    blackScreenAnimation.onForwardArrived -= StartLevel;
                    blackScreenAnimation.SetBackDirectionAndEnable();

                    ResetStack();
                    playingState.SetLevel(levelID);
                    PushState(playingState);
                }
            }
        }

    }
}