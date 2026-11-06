#ifdef VERTEX
#version 100

uniform 	vec4 _ScreenParams;
uniform 	vec4 hlslcc_mtx4x4unity_ObjectToWorld[4];
uniform 	vec4 hlslcc_mtx4x4unity_MatrixVP[4];
uniform 	float _Enabled;
uniform 	float _ScreenSpaceUI;
uniform 	vec4 _AlphaTex_ST;
uniform 	vec4 hlslcc_mtx4x4_WorldToMask[4];
uniform 	vec4 _MainTex_ST;
uniform 	vec4 _Color;
uniform 	float _PixelSnap;
attribute highp vec4 in_POSITION0;
attribute highp vec4 in_COLOR0;
attribute highp vec2 in_TEXCOORD0;
varying mediump vec4 vs_COLOR0;
varying highp vec2 vs_TEXCOORD1;
varying highp vec2 vs_TEXCOORD2;
vec4 u_xlat0;
vec4 u_xlat1;
bool u_xlatb1;
vec4 u_xlat2;
bool u_xlatb6;
vec2 u_xlat8;
bool u_xlatb8;
float unity_roundEven(float x) { float y = floor(x + 0.5); return (y - x == 0.5) ? floor(0.5*y) * 2.0 : y; }
vec2 unity_roundEven(vec2 a) { a.x = unity_roundEven(a.x); a.y = unity_roundEven(a.y); return a; }
vec3 unity_roundEven(vec3 a) { a.x = unity_roundEven(a.x); a.y = unity_roundEven(a.y); a.z = unity_roundEven(a.z); return a; }
vec4 unity_roundEven(vec4 a) { a.x = unity_roundEven(a.x); a.y = unity_roundEven(a.y); a.z = unity_roundEven(a.z); a.w = unity_roundEven(a.w); return a; }

void main()
{
    u_xlat0 = in_POSITION0.yyyy * hlslcc_mtx4x4unity_ObjectToWorld[1];
    u_xlat0 = hlslcc_mtx4x4unity_ObjectToWorld[0] * in_POSITION0.xxxx + u_xlat0;
    u_xlat0 = hlslcc_mtx4x4unity_ObjectToWorld[2] * in_POSITION0.zzzz + u_xlat0;
    u_xlat1 = u_xlat0 + hlslcc_mtx4x4unity_ObjectToWorld[3];
    u_xlat0 = hlslcc_mtx4x4unity_ObjectToWorld[3] * in_POSITION0.wwww + u_xlat0;
    u_xlat2 = u_xlat1.yyyy * hlslcc_mtx4x4unity_MatrixVP[1];
    u_xlat2 = hlslcc_mtx4x4unity_MatrixVP[0] * u_xlat1.xxxx + u_xlat2;
    u_xlat2 = hlslcc_mtx4x4unity_MatrixVP[2] * u_xlat1.zzzz + u_xlat2;
    u_xlat1 = hlslcc_mtx4x4unity_MatrixVP[3] * u_xlat1.wwww + u_xlat2;
    u_xlat2.xy = u_xlat1.xy / u_xlat1.ww;
    u_xlat8.xy = _ScreenParams.xy * vec2(0.5, 0.5);
    u_xlat2.xy = u_xlat8.xy * u_xlat2.xy;
    u_xlat2.xy = unity_roundEven(u_xlat2.xy);
    u_xlat2.xy = u_xlat2.xy / u_xlat8.xy;
    u_xlat2.xy = u_xlat1.ww * u_xlat2.xy;
    u_xlatb8 = vec4(0.0, 0.0, 0.0, 0.0)!=vec4(_PixelSnap);
    gl_Position.xy = (bool(u_xlatb8)) ? u_xlat2.xy : u_xlat1.xy;
    gl_Position.zw = u_xlat1.zw;
    u_xlat1 = in_COLOR0 * _Color;
    vs_COLOR0 = u_xlat1;
    u_xlatb1 = 0.0<_ScreenSpaceUI;
    u_xlat0 = (bool(u_xlatb1)) ? in_POSITION0 : u_xlat0;
    u_xlat1.xy = u_xlat0.yy * hlslcc_mtx4x4_WorldToMask[1].xy;
    u_xlat0.xy = hlslcc_mtx4x4_WorldToMask[0].xy * u_xlat0.xx + u_xlat1.xy;
    u_xlat0.xy = hlslcc_mtx4x4_WorldToMask[2].xy * u_xlat0.zz + u_xlat0.xy;
    u_xlat0.xy = hlslcc_mtx4x4_WorldToMask[3].xy * u_xlat0.ww + u_xlat0.xy;
    u_xlat0.xy = u_xlat0.xy + vec2(0.5, 0.5);
    u_xlat0.xy = u_xlat0.xy * _AlphaTex_ST.xy + _AlphaTex_ST.zw;
    u_xlatb6 = 0.0<_Enabled;
    vs_TEXCOORD2.xy = bool(u_xlatb6) ? u_xlat0.xy : vec2(0.0, 0.0);
    vs_TEXCOORD1.xy = in_TEXCOORD0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
    return;
}

#endif
#ifdef FRAGMENT
#version 100

#ifdef GL_FRAGMENT_PRECISION_HIGH
    precision highp float;
#else
    precision mediump float;
#endif
precision highp int;
uniform 	float _Enabled;
uniform 	float _ClampHoriz;
uniform 	float _ClampVert;
uniform 	float _UseAlphaChannel;
uniform 	float _ClampBorder;
uniform 	float _IsThisText;
uniform lowp sampler2D _MainTex;
uniform lowp sampler2D _AlphaTex;
varying mediump vec4 vs_COLOR0;
varying highp vec2 vs_TEXCOORD1;
varying highp vec2 vs_TEXCOORD2;
#define SV_Target0 gl_FragData[0]
vec3 u_xlat0;
lowp vec4 u_xlat10_0;
mediump float u_xlat16_1;
vec2 u_xlat2;
bvec3 u_xlatb2;
vec2 u_xlat3;
float u_xlat12;
lowp float u_xlat10_12;
bool u_xlatb12;
void main()
{
    u_xlat10_0 = texture2D(_MainTex, vs_TEXCOORD1.xy);
    u_xlat16_1 = u_xlat10_0.w * vs_COLOR0.w;
    u_xlat0.xyz = u_xlat10_0.xyz + vec3(vec3(_IsThisText, _IsThisText, _IsThisText));
    u_xlat0.xyz = clamp(u_xlat0.xyz, 0.0, 1.0);
    u_xlat0.xyz = u_xlat0.xyz * vs_COLOR0.xyz;
    u_xlatb12 = 0.0<_Enabled;
    if(u_xlatb12){
        u_xlatb2.xyz = notEqual(vec4(0.0, 0.0, 0.0, 0.0), vec4(_ClampHoriz, _ClampVert, _UseAlphaChannel, _ClampHoriz)).xyz;
        u_xlat12 = (-_ClampBorder) + 1.0;
        u_xlat3.xy = max(vs_TEXCOORD2.xy, vec2(_ClampBorder));
        u_xlat3.xy = min(vec2(u_xlat12), u_xlat3.xy);
        u_xlat2.x = (u_xlatb2.x) ? u_xlat3.x : vs_TEXCOORD2.x;
        u_xlat2.y = (u_xlatb2.y) ? u_xlat3.y : vs_TEXCOORD2.y;
        if(u_xlatb2.z){
            u_xlat10_12 = texture2D(_AlphaTex, u_xlat2.xy).w;
        } else {
            u_xlat10_12 = texture2D(_AlphaTex, u_xlat2.xy).x;
        }
    } else {
        u_xlat10_12 = 1.0;
    }
    u_xlat12 = u_xlat10_12 * u_xlat16_1;
    SV_Target0.xyz = vec3(u_xlat12) * u_xlat0.xyz;
    SV_Target0.w = u_xlat12;
    return;
}

#endif
