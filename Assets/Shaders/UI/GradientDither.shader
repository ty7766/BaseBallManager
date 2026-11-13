// 가로 그라데이션 (왼쪽 정점색 → 오른쪽 투명). 텍스처 없이 픽셀마다 계산하고 화면 픽셀 단위로 디더링해 띠(밴딩)를 없앤다.
// 8비트 화면은 어두운 쪽에서 천천히 옅어지는 구간이 몇 픽셀씩 같은 색으로 뭉쳐 세로 띠가 보인다 — 결과색을 sRGB 한 단계 안에서 흔들어 흩뜨린다.
// 이미지는 스프라이트 없이 쓴다 (uv.x 가 칸 왼쪽 0 → 오른쪽 1)
Shader "BaseBallManager/UI/GradientDither"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Falloff ("Falloff (알파 = (1 - x)^n)", Float) = 3.74
        _Backdrop ("Backdrop (디더 기준 뒤 바탕색)", Color) = (0.0706, 0.0745, 0.0784, 1)
        _DitherAmount ("Dither (sRGB 단계 폭)", Float) = 1.5

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

            float _Falloff;
            float4 _Backdrop;
            float _DitherAmount;

            // 화면 픽셀마다 고르게 퍼진 0~1 잡음 (interleaved gradient noise). 이웃 픽셀끼리 겹치지 않아 결이 곱다
            float PixelNoise(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            }

            // 뒤 바탕에 얹힌 결과를 어림해 sRGB 로 ±(_DitherAmount / 2) 단계 흔든 만큼을 미리 곱한 색에 더한다
            float3 Dither(float3 premultiplied, float alpha, float2 pixel)
            {
                float shake = (PixelNoise(pixel) - 0.5) * _DitherAmount / 255.0;
                float3 estimate = premultiplied + (1.0 - alpha) * _Backdrop.rgb;
            #ifdef UNITY_COLORSPACE_GAMMA
                return premultiplied + shake;
            #else
                float3 shaken = GammaToLinearSpace(saturate(LinearToGammaSpace(estimate) + shake));
                return premultiplied + (shaken - estimate);
            #endif
            }

            fixed4 frag(CardV2F i) : SV_Target
            {
                float x = saturate(i.texcoord.x);
                float alpha = pow(1.0 - x, _Falloff) * i.color.a * CardClip(i.mask);
                float3 premultiplied = i.color.rgb * alpha;

                return float4(Dither(premultiplied, alpha, i.vertex.xy), alpha);
            }
            ENDCG
        }
    }
}
