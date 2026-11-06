using UnityEngine;

namespace Whitebox.Foundation
{
    // Native SpriteRendererMask's existing non-UI quad branch restores
    // uniform ownership. The paired shader is restored from the original
    // GLES GPU program; no different masking technique is substituted.
    public sealed class OriginalWorldSpaceSpriteMask : MonoBehaviour
    {
        public bool _isMaskingEnabled,_clampAlphaHorizontally,_clampAlphaVertically,_useMaskAlphaChannel;
        public float _clampingBorder;
        public Vector2 mainTexTiling,mainTexOffset;
        public SpriteRenderer maskedRenderer;
        public MeshRenderer debugMaskMeshRenderer;
        public string mainTexResourcePath;
        private Texture mainTex;
        private MaterialPropertyBlock properties;
        private Matrix4x4 oldWorldToMask;
        private static readonly int alphaTex=Shader.PropertyToID("_AlphaTex"),alphaTexSt=Shader.PropertyToID("_AlphaTex_ST"),
            worldToMask=Shader.PropertyToID("_WorldToMask"),enabledId=Shader.PropertyToID("_Enabled"),
            clampH=Shader.PropertyToID("_ClampHoriz"),clampV=Shader.PropertyToID("_ClampVert"),
            clampBorder=Shader.PropertyToID("_ClampBorder"),useAlpha=Shader.PropertyToID("_UseAlphaChannel"),isText=Shader.PropertyToID("_IsThisText");
        private void Start()
        { mainTex=Resources.Load<Texture>(mainTexResourcePath);properties=new MaterialPropertyBlock();debugMaskMeshRenderer.enabled=false; }
        private void LateUpdate()
        {
            Matrix4x4 matrix=Matrix4x4.TRS(transform.position,transform.rotation,transform.lossyScale).inverse;
            if(matrix==oldWorldToMask)return;oldWorldToMask=matrix;
            maskedRenderer.GetPropertyBlock(properties);
            properties.SetTexture(alphaTex,mainTex);properties.SetVector(alphaTexSt,new Vector4(mainTexTiling.x,mainTexTiling.y,mainTexOffset.x,mainTexOffset.y));
            properties.SetMatrix(worldToMask,matrix);properties.SetFloat(enabledId,_isMaskingEnabled?1f:0f);
            properties.SetFloat(clampH,_clampAlphaHorizontally?1f:0f);properties.SetFloat(clampV,_clampAlphaVertically?1f:0f);
            properties.SetFloat(clampBorder,_clampingBorder);properties.SetFloat(useAlpha,_useMaskAlphaChannel?1f:0f);properties.SetFloat(isText,0f);
            maskedRenderer.SetPropertyBlock(properties);
        }
    }
}
