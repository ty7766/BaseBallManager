// 카드 UI 셰이더 공용 — UI/Default 와 같은 정점 처리 (RectMask2D 클리핑 · 감마 정점색)
#ifndef CARD_UI_COMMON_INCLUDED
#define CARD_UI_COMMON_INCLUDED

#include "UnityCG.cginc"
#include "UnityUI.cginc"

struct CardAppData
{
    float4 vertex : POSITION;
    float4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct CardV2F
{
    float4 vertex : SV_POSITION;
    fixed4 color : COLOR;
    float2 texcoord : TEXCOORD0;
    float4 worldPosition : TEXCOORD1;
    float4 mask : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _MainTex_TexelSize;
fixed4 _Color;
fixed4 _TextureSampleAdd;
float4 _ClipRect;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;
int _UIVertexColorAlwaysGammaSpace;

CardV2F CardVert(CardAppData v)
{
    CardV2F o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 position = UnityObjectToClipPos(v.vertex);
    o.worldPosition = v.vertex;
    o.vertex = position;

    float2 pixelSize = position.w;
    pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
    o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
    o.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
        0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

    if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
        v.color.rgb = UIGammaToLinear(v.color.rgb);

    o.color = v.color * _Color;
    return o;
}

// RectMask2D 밖은 지운다 (스크롤 목록 안의 카드)
half CardClip(float4 mask)
{
#ifdef UNITY_UI_CLIP_RECT
    half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(mask.xy)) * mask.zw);
    return m.x * m.y;
#else
    return 1;
#endif
}

#endif
