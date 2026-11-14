// 미리 곱한 알파(premultiplied) 텍스처를 UI 에 얹는다. 렌더 텍스처에 반투명하게 겹쳐 그린 결과(입체 엠블럼 토큰)를
// 일반 UI 셰이더로 그리면 가장자리 색이 알파로 한 번 더 곱해져 검게 테가 진다. 정점색 알파로 전체를 반투명하게 만든다
Shader "BaseBallManager/UI/PremultipliedImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
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
        Blend One OneMinusSrcAlpha
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
                fixed4 texel = tex2D(_MainTex, i.texcoord);
                half strength = i.color.a * CardClip(i.mask);

                return fixed4(texel.rgb * i.color.rgb, texel.a) * strength;
            }
            ENDCG
        }
    }
}
