Shader "Shader Graphs/Cupboard_Door_Particle"
{
    Properties
    {
        _LightDirection ("LightDirection", Vector) = (0, 1, -1, 0)
        [NoScaleOffset] _SampleReflectedCubemap_996c0d7a9fa646acaab13bc9c71de387_Cube_1 ("Cubemap", Cube) = "" {}
        [NoScaleOffset] _SampleTexture2D_fc9181a81b614b6ba6d6cbe6f6b778cb_Texture_1 ("Texture2D", 2D) = "white" {}
        [HideInInspector] _BUILTIN_QueueOffset ("Float", Float) = 0
        [HideInInspector] _BUILTIN_QueueControl ("Float", Float) = -1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "BuiltInMaterialType" = "Unlit" }
        Cull Back
        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        // datapack.unity3d Shader 1082, GLES forward pass.
        Pass
        {
            Name "Pass"
            Tags { "LightMode" = "ForwardBase" }
            ColorMask RGB

            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex Vert
            #pragma fragment Fragment

            float3 _LightDirection;
            samplerCUBE _SampleReflectedCubemap_996c0d7a9fa646acaab13bc9c71de387_Cube_1;
            sampler2D _SampleTexture2D_fc9181a81b614b6ba6d6cbe6f6b778cb_Texture_1;

            struct DoorVertex
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldView : TEXCOORD2;
            };

            DoorVertex Vert(appdata_base input)
            {
                DoorVertex output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord.xy;
                float3 worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                float3 worldNormal = mul(input.normal, (float3x3)unity_WorldToObject);
                output.worldNormal = worldNormal * rsqrt(max(dot(worldNormal, worldNormal), 1.17549435e-38));
                output.worldView = unity_OrthoParams.w == 0
                    ? _WorldSpaceCameraPos - worldPosition : UNITY_MATRIX_V[2].xyz;
                return output;
            }

            float4 Fragment(DoorVertex input) : SV_Target
            {
                float3 worldNormal = normalize(input.worldNormal);
                float3 localNormal = normalize(mul(worldNormal, (float3x3)unity_ObjectToWorld));
                float3 localView = normalize(mul((float3x3)unity_WorldToObject, normalize(input.worldView)));
                float3 reflectedView = reflect(-localView, localNormal);
                float3 environment = texCUBE(_SampleReflectedCubemap_996c0d7a9fa646acaab13bc9c71de387_Cube_1, reflectedView).rgb;
                float reflectionWeight = saturate(dot(localNormal.yz, float2(1, -.44))) * 1.01;
                float3 lowReflection = environment * reflectionWeight * .3;
                float3 highReflection = 1 - (1 - environment * .15) * 2 * (1 - reflectionWeight);
                float3 reflections = float3(
                    environment.r <= 3.33333325 ? lowReflection.r : highReflection.r,
                    environment.g <= 3.33333325 ? lowReflection.g : highReflection.g,
                    environment.b <= 3.33333325 ? lowReflection.b : highReflection.b);
                reflections = pow(reflections, 1.52);

                float lighting = saturate((dot(_LightDirection, worldNormal) + .23) * .378787875);
                lighting = lighting * lighting * (3 - 2 * lighting);
                float3 albedo = tex2D(_SampleTexture2D_fc9181a81b614b6ba6d6cbe6f6b778cb_Texture_1, input.uv).rgb;
                float3 shaded = lighting >= .5
                    ? (2 - 2 * lighting) * albedo + (2 * lighting - 1) * sqrt(albedo)
                    : 2 * lighting * albedo + (1 - 2 * lighting) * albedo * albedo;
                return float4(2 * reflections + lerp(albedo, shaded, .5), 1);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex ShadowVert
            #pragma fragment ShadowFragment
            #pragma multi_compile_shadowcaster
            struct ShadowVertex { V2F_SHADOW_CASTER; };

            ShadowVertex ShadowVert(appdata_base v)
            {
                ShadowVertex output;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(output)
                return output;
            }
            float4 ShadowFragment(ShadowVertex input) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(input)
            }
            ENDCG
        }
    }
}
