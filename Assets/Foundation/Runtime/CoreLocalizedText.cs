using TMPro;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Authored alongside the text in its prefab; this only refreshes content.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CoreLocalizedText : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private string key;
        [SerializeField, TextArea] private string fallback;

        public string Key => key;
        public string Fallback => fallback;

        private void OnEnable()
        {
            CoreLocalization.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => CoreLocalization.Changed -= Refresh;

        public void SetKey(string value, string fallbackText = null)
        {
            key = value;
            if (fallbackText != null) fallback = fallbackText;
            Refresh();
        }

        public void Refresh()
        {
            if (target == null) target = GetComponent<TMP_Text>();
            if (target != null) target.text = CoreLocalization.Get(key, fallback);
        }
    }
}
