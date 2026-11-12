// 사진 뒤에 깔리는 그림자(오른쪽 아래)와 빛 번짐(왼쪽 위). 사진 알파를 밉맵으로 흐려 쓴다.
// 이미지 칸은 카드 전체이고 사진은 _PhotoRect 안에 있다 (번짐이 사진 칸 경계에서 잘리지 않게)
Shader "BaseBallManager/UI/CardPhotoBack"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _PhotoRect ("Photo Rect (카드 uv x, y, w, h)", Vector) = (0, 0.0036, 1, 0.8932)
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.8)
        _ShadowOffset ("Shadow Offset (사진 uv)", Vector) = (0.0172, 0.0172, 0, 0)
        _ShadowBlur ("Shadow Blur (사진 폭 비율)", Float) = 0.0115
        _GlowColor ("Glow Color", Color) = (1,1,1,0.32)
        _GlowOffset ("Glow Offset (사진 uv)", Vector) = (0.0186, 0.0172, 0, 0)
        _GlowBlur ("Glow Blur (사진 폭 비율)", Float) = 0.0315

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex CardVert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "CardUiCommon.cginc"

            float4 _PhotoRect;
            fixed4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowBlur;
            fixed4 _GlowColor;
            float4 _GlowOffset;
            float _GlowBlur;

            // 흐림 반경(사진 폭 비율)에 맞는 밉 단계에서 네 점을 찍어 평균. 사진 칸 밖은 비어 있다
            half BlurredAlpha(float2 uv, float blur)
            {
                float2 radius = float2(blur, blur * _MainTex_TexelSize.z / _MainTex_TexelSize.w);
                float lod = log2(max(1.0, blur * _MainTex_TexelSize.z * 1.5));
                half sum = 0;
                sum += tex2Dlod(_MainTex, float4(uv + float2(-0.5, -0.5) * radius, 0, lod)).a;
                sum += tex2Dlod(_MainTex, float4(uv + float2( 0.5, -0.5) * radius, 0, lod)).a;
                sum += tex2Dlod(_MainTex, float4(uv + float2(-0.5,  0.5) * radius, 0, lod)).a;
                sum += tex2Dlod(_MainTex, float4(uv + float2( 0.5,  0.5) * radius, 0, lod)).a;
                float2 inside = step(0, uv) * step(uv, 1);
                return sum * 0.25 * inside.x * inside.y;
            }

            fixed4 frag(CardV2F i) : SV_Target
            {
                float2 photoUv = (i.texcoord - _PhotoRect.xy) / _PhotoRect.zw;

                // 그림자는 오른쪽 아래로 밀린다 = 왼쪽 위 픽셀을 가져온다. 빛 번짐은 그 반대
                half shadow = BlurredAlpha(photoUv + float2(-_ShadowOffset.x, _ShadowOffset.y), _ShadowBlur) * _ShadowColor.a;
                half glow = BlurredAlpha(photoUv + float2(_GlowOffset.x, -_GlowOffset.y), _GlowBlur) * _GlowColor.a;

                half alpha = shadow + glow * (1 - shadow);
                half3 rgb = (_ShadowColor.rgb * shadow + _GlowColor.rgb * glow * (1 - shadow)) / max(alpha, 0.0001);

                half4 color = half4(rgb, alpha * i.color.a * CardClip(i.mask) * CardShapeAlpha(i.texcoord));
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
