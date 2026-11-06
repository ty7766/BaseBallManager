// 빛 전용 가산 합성. 빛은 뒤의 색을 덮지 않고 더해진다 (선형 색 공간에서 물리적으로 맞는 합성).
// 텍스처 알파 × 정점색 세기만큼 정점색을 더한다. 뽑기 카드에서 새는 빛 · 윤곽 빛 · 빛 알갱이 · 공개 번짐이 함께 쓴다
Shader "BaseBallManager/UI/AdditiveLight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

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

            CardV2F vert(CardAppData v)
            {
                return CardVert(v);
            }

            fixed4 frag(CardV2F i) : SV_Target
            {
                half strength = tex2D(_MainTex, i.texcoord).a * i.color.a * CardClip(i.mask);

                return fixed4(i.color.rgb * strength, 0);
            }
            ENDCG
        }
    }
}
