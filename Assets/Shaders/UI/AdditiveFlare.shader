// 중계 이펙트 플립북용 가산 합성. 텍스처 한 장에 두 겹을 싣는다: R = 흰 심(섬광 · 줄기 심 · 불똥 머리), A = 색 번짐.
// 심은 _HotColor, 번짐은 정점색으로 더한다. 정점색 알파가 전체 세기다 (Design/ui_assets/render_match_fx.py)
Shader "BaseBallManager/UI/AdditiveFlare"
{
    Properties
    {
        [PerRendererData] _MainTex ("Flipbook", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HotColor ("Hot Core", Color) = (1, 0.97, 0.9, 1)

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
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "CardUiCommon.cginc"

            half4 _HotColor;

            CardV2F vert(CardAppData v)
            {
                return CardVert(v);
            }

            fixed4 frag(CardV2F i) : SV_Target
            {
                fixed4 texel = tex2D(_MainTex, i.texcoord);
                half strength = i.color.a * CardClip(i.mask);

                return fixed4((i.color.rgb * texel.a + _HotColor.rgb * texel.r) * strength, 0);
            }
            ENDCG
        }
    }
}
