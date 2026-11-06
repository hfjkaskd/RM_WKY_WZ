using UnityEngine;

namespace Whitebox.Foundation
{
    // The original Root and View stay separate in the prefab. Root hosts
    // explode/merge transforms; View hosts FallSquash and visibility.
    public sealed class OriginalRootTransform : MonoBehaviour
    {
        [SerializeField] private Transform root, view;
        [SerializeField] private OriginalFallSquash fallSquash;
        private bool hasFallSquash;
        public bool IsHidden { get; private set; }
        private void Awake() { hasFallSquash=fallSquash!=null; } // 0x2b51cc4.
        public void Init() => Reset(); // 0x2b4abc4.
        public void Reset() // 0x2b51d30.
        {
            root.localPosition=Vector3.zero; root.localScale=Vector3.one; root.localRotation=Quaternion.identity;
            if (IsHidden) ShowView();
        }
        public Vector3 GetPosition() => root.localPosition;
        public void SetPosition(Vector3 value) { root.localPosition=value; }
        public Vector3 GetScale() => root.localScale;
        public void SetScale(Vector3 value) { root.localScale=value; }
        public void SetRotation(Quaternion value) { root.localRotation=value; }
        public void AddChild(Transform child) { child.SetParent(root,false); }
        public void HideView() { IsHidden=true; view.gameObject.SetActive(false); }
        public void ShowView() { IsHidden=false; view.gameObject.SetActive(true); }
        public void ResetViewPosition() { view.transform.localPosition=Vector3.zero; }
        public Transform GetRoot() => root;
        public void BindFallContext(IOriginalFallSquashContext context)
        { if (hasFallSquash) fallSquash.BindContext(context); }
        public void UpdateFallState(int newState, float currentSpeed, float maxSpeed) // 0x2b4b000.
        { if (hasFallSquash) fallSquash.UpdateState(newState,currentSpeed,maxSpeed); }
    }
}
