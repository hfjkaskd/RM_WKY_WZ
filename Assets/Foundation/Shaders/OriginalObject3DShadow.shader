Shader "Royal/Object3DShadowShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"
               "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off
        ZTest LEqual

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        half4 _Color;

        struct ShadowVertex
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        ShadowVertex Vert(appdata_base vertex)
        {
            ShadowVertex output;
            output.position = UnityObjectToClipPos(vertex.vertex);
            output.uv = vertex.texcoord.xy;
            return output;
        }
        ENDCG

        // data.unity3d Shader 39, GLES program 1.
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            fixed4 Fragment(ShadowVertex input) : SV_Target
            {
                return tex2D(_MainTex, input.uv) * _Color;
            }
            ENDCG
        }

        // The second native pass outputs transparent black and writes no depth.
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            fixed4 Fragment(ShadowVertex input) : SV_Target { return 0; }
            ENDCG
        }
    }
}
