using TMPro;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCurvedText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro text;
        [SerializeField] private AnimationCurve vertexCurve;
        [SerializeField] private float curveScale;

        private void OnEnable()
        {
            text.OnPreRenderText += ApplyCurve;
            text.havePropertiesChanged = true;
        }

        private void OnDisable() => text.OnPreRenderText -= ApplyCurve;

        private void ApplyCurve(TMP_TextInfo info)
        {
            if (text.text.Length > 3) OriginalTextCurve.Apply(info, vertexCurve, curveScale);
        }
    }
}
