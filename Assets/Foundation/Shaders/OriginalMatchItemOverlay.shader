Shader "Royal/Blend/Overlay"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _OverlayColor("Blend Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            half4 _OverlayColor;

            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct FragmentInput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            FragmentInput Vert(VertexInput input)
            {
                FragmentInput output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            half4 Frag(FragmentInput input) : SV_Target
            {
                float4 baseColor = tex2D(_MainTex, input.uv) * input.color;
                half3 darkOverlay = 2 * baseColor.rgb * _OverlayColor.rgb;
                half3 lightOverlay = 1 - 2 * (1 - baseColor.rgb) * (1 - _OverlayColor.rgb);
                half3 overlay = lerp(darkOverlay, lightOverlay, step(0.5, baseColor.rgb));
                half3 color = lerp(baseColor.rgb, overlay, _OverlayColor.a * _OverlayColor.a);
                return half4(color * baseColor.a, baseColor.a);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
