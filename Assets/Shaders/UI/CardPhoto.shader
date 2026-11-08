// 선수 누끼 사진. 배경 쪽 빛이 왼쪽 위 가장자리를 감싸고(림 라이트), 색을 배경 쪽으로 살짝 물들인다
Shader "BaseBallManager/UI/CardPhoto"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimOffset ("Rim Offset (사진 uv, 빛 쪽)", Vector) = (0.0064, 0.0063, 0, 0)
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.7
        _RimBias ("Rim Softness (mip bias)", Range(0, 4)) = 1.5
        _GradeColor ("Grade Color", Color) = (1,1,1,1)
        _GradeAmount ("Grade Amount", Range(0, 1)) = 0.14

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

            fixed4 _RimColor;
            float4 _RimOffset;
            half _RimStrength;
            half _RimBias;
            fixed4 _GradeColor;
            half _GradeAmount;

            fixed4 frag(CardV2F i) : SV_Target
            {
                half4 color = i.color * (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd);

                // 빛이 오는 쪽(왼쪽 위) 이웃이 비어 있으면 그 가장자리가 빛을 받는다
                float2 lightSide = i.texcoord + float2(-_RimOffset.x, _RimOffset.y);
                half neighbor = tex2Dbias(_MainTex, float4(lightSide, 0, _RimBias)).a;
                half rim = color.a * saturate(1 - neighbor);

                half luma = dot(color.rgb, half3(0.299, 0.587, 0.114));
                half3 graded = lerp(color.rgb, luma * _GradeColor.rgb * 1.25, _GradeAmount);

                color.rgb = graded + _RimColor.rgb * rim * _RimStrength;
                color.a *= CardClip(i.mask);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
