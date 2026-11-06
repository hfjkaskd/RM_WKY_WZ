using UnityEngine;
using Sirenix.OdinInspector;

public class UIRedPoint : MonoBehaviour
{
    [SerializeField]
    private string _key = string.Empty;

    public GameObject red;

    public string Key
    {
        get => _key;
        set
        {
            if (_key != value)
            {
                _key = value;
                RefreshRedPoint();
            }
        }
    }

    private void Start()
    {
        red ??= gameObject;
    }

    void OnEnable()
    {
        RefreshRedPoint();
        SetEventListener(true);
    }

    private void OnDisable()
    {
        SetEventListener(false);
    }

    [Button]
    private void RefreshRedPoint()
    {
        if (!string.IsNullOrEmpty(_key))
        {
            red.SetObjActive(RedPointSystem.Instance.GetRed(_key));
            // Debug.LogError($"Refresh {_key} {RedPointSystem.Instance.GetRed(_key)}");
        }
    }

    private void OnRefreshRedPoint(string key)
    {
        if (this._key == key)
        {
            RefreshRedPoint();
        }
    }

    private void SetEventListener(bool add)
    {
        BizzaEventSystem.Set(EventDefine.RedPoint.Refresh, OnRefreshRedPoint, add);
    }
}