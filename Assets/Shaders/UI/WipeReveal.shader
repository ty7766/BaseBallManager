// 사선으로 쓸며 나타나기. _Progress 0 → 1 로 _Direction 쪽 끝에서부터 그림이 드러난다 (타이틀 붓질 연출)
// 방향은 uv 기준 (예: (-0.55, -0.83) = 오른쪽 위에서 왼쪽 아래로 쓸어내림). _Softness 는 드러나는 경계 폭
Shader "BaseBallManager/UI/WipeReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Progress ("Progress", Range(0, 1)) = 1
        _Direction ("Direction (uv)", Vector) = (-0.55, -0.83, 0, 0)
        _Softness ("Softness", Range(0.01, 1)) = 0.18

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex CardVert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "CardUiCommon.cginc"

            float _Progress;
            float4 _Direction;
            float _Softness;

            // uv 를 쓸어내리는 방향으로 0(먼저 드러남) ~ 1(마지막) 로 편다
            float SweepPosition(float2 uv)
            {
                float2 dir = normalize(_Direction.xy);
                float lo = min(0, dir.x) + min(0, dir.y);
                float hi = max(0, dir.x) + max(0, dir.y);
                return (dot(uv, dir) - lo) / max(hi - lo, 1e-4);
            }

            fixed4 frag(CardV2F i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
                float reveal = saturate((_Progress * (1.0 + _Softness) - SweepPosition(i.texcoord)) / _Softness);
                color.a *= reveal * CardClip(i.mask);
                return color;
            }
            ENDCG
        }
    }
}
