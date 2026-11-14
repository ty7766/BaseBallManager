// 구단 로고에 흰 테두리를 둘러 스티커처럼 띄운다. 검정 · 남색 로고(KT · 두산 · 롯데 …)도 어두운 판 위에서 읽힌다.
// 테두리는 로고 알파를 16방향 · 두 거리(끝 · 절반)에서 뽑아 넓혀 만든다. 테두리가 그려질 자리를 위해 그림을 _Padding 만큼 안쪽으로 줄여 그린다
Shader "BaseBallManager/UI/StickerOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Logo", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline", Color) = (0.95, 0.96, 0.98, 1)
        _OutlineWidth ("Outline Width (그림 폭 대비)", Range(0, 0.15)) = 0.055
        _Padding ("Padding (한 변, 그림 폭 대비)", Range(0, 0.3)) = 0.08

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
        Blend One One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "CardUiCommon.cginc"

            half4 _OutlineColor;
            float _OutlineWidth;
            float _Padding;

            CardV2F vert(CardAppData v)
            {
                return CardVert(v);
            }

            half SampleAlpha(float2 uv)
            {
                half inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                return tex2D(_MainTex, uv).a * inside;
            }

            fixed4 frag(CardV2F i) : SV_Target
            {
                float aspect = _MainTex_TexelSize.z / max(_MainTex_TexelSize.w, 1.0);
                float2 pad = float2(_Padding, _Padding * aspect);
                float2 uv = (i.texcoord - 0.5) * (1.0 + 2.0 * pad) + 0.5;
                float2 reach = float2(_OutlineWidth, _OutlineWidth * aspect);

                half grown = 0;

                for (int k = 0; k < 16; k++)
                {
                    float angle = k * 0.392699;
                    float2 direction = float2(cos(angle), sin(angle));
                    grown = max(grown, SampleAlpha(uv + direction * reach));
                    grown = max(grown, SampleAlpha(uv + direction * reach * 0.5));
                }

                half inside = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                fixed4 logo = tex2D(_MainTex, uv) * inside;
                half outline = saturate(grown * 2.0);
                half alpha = max(logo.a, outline * _OutlineColor.a);
                half3 rgb = lerp(_OutlineColor.rgb, logo.rgb, logo.a);

                return fixed4(rgb, alpha) * i.color * CardClip(i.mask);
            }
            ENDCG
        }
    }
}
