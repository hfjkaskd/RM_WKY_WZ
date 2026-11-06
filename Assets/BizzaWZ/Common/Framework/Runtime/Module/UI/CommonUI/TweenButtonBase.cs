using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityExtensions.Tween;

public abstract class TweenButtonBase : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] protected Graphic target;
    [SerializeField] protected TweenPlayer tweenPlayer;
    public string clickSound = string.Empty;

    protected bool interactable = true;
    public bool Interactable
    {
        get { return interactable; }
        set
        {
            if (interactable == value) return;
            interactable = value;
        }
    }

    private bool press = false;

    protected abstract void Click();

    public void Awake()
    {
        if (target == null)
        {
            target = GetComponent<Graphic>();
        }
        if (tweenPlayer == null)
        {
            tweenPlayer = GetComponent<TweenPlayer>();
        }
    }

    private void Cancel()
    {
        if (press)
        {
            press = false;
            if (tweenPlayer != null)
            {
                tweenPlayer.SetBackDirectionAndEnable();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable)
        {
            press = true;
            if (tweenPlayer != null)
            {
                tweenPlayer.ForwardRestart();
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (press && interactable)
        {
            Click();
            // if (!string.IsNullOrEmpty(clickSound))
            // {
            //     SoundModule.Instance.PlayOneShot(clickSound);
            // }
            // AudioUtil.PlayOneShot("SFX_Btn");
        }
        Cancel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cancel();
    }
}