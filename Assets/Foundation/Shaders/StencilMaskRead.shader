Shader "Custom/StencilMaskRead"
{
    Properties { [IntRange] _StencilRef ("Stencil Reference Value", Range(0,255)) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
        Pass
        {
            Blend Zero One
            ColorMask RGBA
            ZWrite Off
            ZTest LEqual
            ZClip On
            Cull Back
            Stencil
            {
                Ref [_StencilRef]
                ReadMask 255
                WriteMask 255
                Comp Always
                Pass DecrSat
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 position : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(position.xyz);
            }
            fixed4 frag() : SV_Target { return fixed4(0,0,0,0); }
            ENDHLSL
        }
    }
}
