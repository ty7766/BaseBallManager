// 카드 광택. 비스듬한 빛 띠가 주기마다 카드를 한 번 가로지른다 (더하기 합성).
// 띠 폭은 카드 기준이고, 시작 시각만 카드의 화면 위치로 늦춰 목록이 왼쪽 위부터 차례로 반짝인다.
// 카드 위치는 uv 미분으로 구하므로 카드마다 값을 넘기지 않고, CPU 갱신도 없다
Shader "BaseBallManager/UI/CardShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Period ("Period (초)", Float) = 5
        _SweepPortion ("Sweep Portion (주기 중 지나가는 비율)", Range(0.05, 1)) = 0.32
        _Width ("Band Width (카드 폭 비율)", Float) = 0.06
        _Direction ("Band Normal (카드)", Vector) = (0.92, -0.38, 0, 0)
        _Stagger ("Stagger (화면 끝에서 끝까지 늦춰지는 주기 비율)", Range(0, 1)) = 0.45

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

            float _Period;
            float _SweepPortion;
            float _Width;
            float4 _Direction;
            float _Stagger;

            struct ShineV2F
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 mask : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShineV2F vert(CardAppData v)
            {
                CardV2F card = CardVert(v);
                ShineV2F o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = card.vertex;
                o.color = card.color;
                o.texcoord = card.texcoord;
                o.mask = card.mask;
                o.screenPos = ComputeScreenPos(card.vertex);
                return o;
            }

            fixed4 frag(ShineV2F i) : SV_Target
            {
                float2 pixel = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;

                // uv 가 픽셀당 얼마나 변하는지로 카드 크기(픽셀)와 카드 한 귀퉁이 위치를 구한다
                float2 uvPerPixel = float2(ddx(i.texcoord.x), ddy(i.texcoord.y));
                float2 cardPixels = 1.0 / max(abs(uvPerPixel), 1e-6);
                float2 corner = pixel - i.texcoord * cardPixels * sign(uvPerPixel);
                float delay = saturate(dot(corner / _ScreenParams.xy, float2(0.7, 0.3))) * _Stagger;

                // 카드 안 대각선 좌표 (가로세로 비를 살려 띠가 같은 각도로 기운다)
                float2 direction = normalize(_Direction.xy);
                float2 local = i.texcoord * cardPixels;
                float across = (dot(local, direction) - min(0, direction.y) * cardPixels.y)
                    / dot(cardPixels, abs(direction));

                float sweep = saturate(frac(_Time.y / _Period - delay) / _SweepPortion);
                float center = lerp(-0.3, 1.3, sweep);
                float gap = (across - center) / _Width;

                // 가는 심 + 넓고 옅은 어깨 (시안의 0.42 · 0.08 띠)
                half band = exp(-gap * gap) * 0.84 + exp(-gap * gap / 9.0) * 0.16;
                half strength = band * i.color.a * CardClip(i.mask);

                return fixed4(i.color.rgb * strength, 0);
            }
            ENDCG
        }
    }
}
