Shader "Whitebox/OriginalGameBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _BlendColor ("Desaturation", Color) = (0, 0, 0, 0.2)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Fragment
            #include "UnitySprites.cginc"

            half4 _BlendColor;

            half4 Fragment(v2f input) : SV_Target
            {
                half4 color = SampleSpriteTexture(input.texcoord) * input.color;
                // Royal/Blend/Saturation with the original area's achromatic blend color.
                half minimum = min(color.r, min(color.g, color.b));
                half maximum = max(color.r, max(color.g, color.b));
                half lightness = (minimum + maximum) * 0.5h;
                color.rgb = lerp(color.rgb, lightness.xxx, _BlendColor.a * _BlendColor.a) * color.a;
                return color;
            }
            ENDCG
        }
    }
}
