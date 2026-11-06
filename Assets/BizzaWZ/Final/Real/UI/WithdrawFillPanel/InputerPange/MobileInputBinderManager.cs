#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MobileInputBinderManager : MonoBehaviour
{
    public static MobileInputBinderManager Instance;
    
    public List<TMP_InputField> inputList;
    
    private bool isInit;
    
    private void Start()
    {
        inputList = GetComponentsInChildren<TMP_InputField>(false).ToList();

        foreach (var input in inputList)
        {
            if (input.GetComponent<MobileInputBinder>() == null)
            {
                Debug.LogError($"{input.name} prefab is missing MobileInputBinder.");
            }
            if (input.GetComponent<CanvasGroup>() == null)
            {
                Debug.LogError($"{input.name} prefab is missing CanvasGroup.");
            }
            if (input.GetComponent<InputerDrag>() == null)
            {
                Debug.LogError($"{input.name} prefab is missing InputerDrag.");
            }

            input.onSelect.AddListener((value) => OnSelect(input, value));
            input.onDeselect.AddListener((value) => OnDeselect(input, value));
            input.onEndEdit.AddListener((value) => OnEndEdit(input, value));
            input.onSubmit.AddListener((value) => OnSubmit(input, value));
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        isInit = true;
    }

    private void OnSelect(TMP_InputField input, string value)
    {
        if (isInit)
        {
            KeyboardAvoider.Instance.StartFollow();
            isInit = false;
        }
    }

    private void OnDeselect(TMP_InputField input, string value)
    {
    }

    private void OnEndEdit(TMP_InputField input, string value)
    {
    }

    private void OnSubmit(TMP_InputField input, string value)
    {
    }
    
    public void SetectFouscus(bool isFocus)
    {
        foreach (var input in inputList)
        {
            var canvasGroup = input.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                continue;
            }

            canvasGroup.blocksRaycasts = isFocus;
            canvasGroup.interactable = isFocus;
        }
    }
}
#endif
