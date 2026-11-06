// Recovered from data.unity3d Shader pathID26, GLES programs1 and9/1.
// Preserves the original world-to-mask sampling and premultiplied output.
Shader "Alpha Masked/Sprites Alpha Masked - World Coords"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        [Toggle] _Enabled ("Mask Enabled", Float) = 1
        [Toggle] _ClampHoriz ("Clamp Alpha Horizontally", Float) = 0
        [Toggle] _ClampVert ("Clamp Alpha Vertically", Float) = 0
        [Toggle] _UseAlphaChannel ("Use Mask Alpha Channel (not RGB)", Float) = 0
        [Toggle] _ScreenSpaceUI ("Is this screen space ui element?", Float) = 0
        _AlphaTex ("Alpha Mask", 2D) = "white" {}
        _ClampBorder ("Clamping Border", Float) = 0.01
        _Color ("Tint", Color) = (1,1,1,1)
        [Toggle] _PixelSnap ("Pixel snap", Float) = 0
        _IsThisText ("Is This Text?", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "CanUseSpriteAtlas"="true" "IgnoreProjector"="true" "PreviewType"="Plane" "Queue"="Transparent" "RenderType"="Transparent" "ToJMasked"="true" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_AlphaTex;
            float4 _MainTex_ST,_AlphaTex_ST,_Color;
            float4x4 _WorldToMask;
            float _Enabled,_ClampHoriz,_ClampVert,_UseAlphaChannel,_ScreenSpaceUI,_ClampBorder,_PixelSnap,_IsThisText;
            struct appdata { float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD1;float2 mask:TEXCOORD2; };
            float RoundEven(float value)
            {float y=floor(value+0.5);return y-value==0.5?floor(0.5*y)*2.0:y;}
            v2f vert(appdata v)
            {
                v2f o;
                float4 world=mul(unity_ObjectToWorld,v.vertex);
                o.vertex=mul(UNITY_MATRIX_VP,mul(unity_ObjectToWorld,float4(v.vertex.xyz,1)));
                if(_PixelSnap!=0)
                {
                    float2 halfScreen=_ScreenParams.xy*0.5;
                    float2 pixel=o.vertex.xy/o.vertex.w*halfScreen;
                    o.vertex.xy=float2(RoundEven(pixel.x),RoundEven(pixel.y))/halfScreen*o.vertex.w;
                }
                o.color=v.color*_Color;
                float4 position=_ScreenSpaceUI>0?v.vertex:world;
                float2 mask=mul(_WorldToMask,position).xy+0.5;
                o.mask=_Enabled>0?mask*_AlphaTex_ST.xy+_AlphaTex_ST.zw:float2(0,0);
                o.uv=v.uv*_MainTex_ST.xy+_MainTex_ST.zw;
                return o;
            }
            half4 frag(v2f i):SV_Target
            {
                half4 sample=tex2D(_MainTex,i.uv);
                half alpha=sample.a*i.color.a;
                float3 rgb=saturate(sample.rgb+_IsThisText)*i.color.rgb;
                half mask=1;
                if(_Enabled>0)
                {
                    float2 uv=i.mask;
                    float2 clamped=min(1-_ClampBorder,max(uv,_ClampBorder));
                    if(_ClampHoriz!=0)uv.x=clamped.x;
                    if(_ClampVert!=0)uv.y=clamped.y;
                    half4 value=tex2D(_AlphaTex,uv);
                    mask=_UseAlphaChannel!=0?value.a:value.r;
                }
                alpha*=mask;
                return half4(rgb*alpha,alpha);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
