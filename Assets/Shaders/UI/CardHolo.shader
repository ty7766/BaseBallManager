// 카드 배경 홀로 반사. 대각선 무지개 결이 천천히 흐르며 밝은 무늬를 닷지로 들뜨게 한다 (4·5성 · 시그 · 골글)
Shader "BaseBallManager/UI/CardHolo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _HoloAmount ("Holo Amount", Range(0, 1)) = 0.3
        _HoloSpeed ("Holo Speed (주기/초)", Float) = 0.11
        _HoloScale ("Holo Scale (uv x, y)", Vector) = (0.6, 0.35, 0, 0)

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

            half _HoloAmount;
            float _HoloSpeed;
            float4 _HoloScale;

            // 색상환 한 바퀴 (분홍 → 노랑 → 초록 → 하늘 → 보라)
            half3 Rainbow(float hue)
            {
                half3 rgb = saturate(abs(frac(hue + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1);
                return lerp(0.3, 1.0, rgb);
            }

            fixed4 frag(CardV2F i) : SV_Target
            {
                half4 color = i.color * (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd);

                float hue = frac(dot(i.texcoord, _HoloScale.xy) - _Time.y * _HoloSpeed);
                half3 blend = Rainbow(hue) * 0.85;
                half3 dodge = saturate(color.rgb / max(1 - blend, 0.08));

                color.rgb = lerp(color.rgb, dodge, _HoloAmount);
                color.a *= CardClip(i.mask);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
