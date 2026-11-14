// 입체 엠블럼 토큰. 엠블럼 그림을 같은 크기의 판 수십 장으로 깊이 방향에 겹쳐 두께를 낸다 (TeamEmblemTokenView 가 메시를 만든다).
// 옆면 판은 엠블럼 색을 어둡게 눌러 뒤로 갈수록 더 어둡게, 앞면 판은 원래 색 + 비스듬한 빛 띠가 주기마다 가로지른다.
// 렌더 텍스처에 뒤 → 앞 순서로 겹쳐 그리고, 결과는 미리 곱한 알파(premultiplied)로 남긴다
Shader "BaseBallManager/UI/EmblemToken"
{
    Properties
    {
        _MainTex ("Emblem", 2D) = "white" {}
        _SideBack ("Side Back Shade (맨 뒤 판 밝기)", Range(0, 1)) = 0.14
        _SideFront ("Side Front Shade (앞면 바로 뒤 판 밝기)", Range(0, 1)) = 0.42
        _Turn ("Turn (-1 ~ 1, 돌아간 정도)", Range(-1, 1)) = 0
        _ShineCenter ("Shine Center (띠 위치, 앞면 대각선 0 ~ 1)", Float) = -1
        _ShineWidth ("Shine Width", Float) = 0.07
        _ShineStrength ("Shine Strength", Float) = 0.55
        _ShineDirection ("Shine Normal", Vector) = (0.92, -0.38, 0, 0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _SideBack;
            half _SideFront;
            half _Turn;
            float _ShineCenter;
            float _ShineWidth;
            half _ShineStrength;
            float4 _ShineDirection;

            struct TokenAppData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 layer : TEXCOORD1;
            };

            struct TokenV2F
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 layer : TEXCOORD1;
                float2 face : TEXCOORD2;
            };

            // layer.x = 판의 깊이 (0 맨 뒤 ~ 1 앞면), layer.y = 1 이면 앞면. face = 앞면 기준 0 ~ 1 좌표 (빛 띠용)
            TokenV2F vert(TokenAppData v)
            {
                TokenV2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.layer = v.layer;
                o.face = v.vertex.xy;
                return o;
            }

            fixed4 frag(TokenV2F i) : SV_Target
            {
                fixed4 emblem = tex2D(_MainTex, i.uv);

                half sideShade = lerp(_SideBack, _SideFront, i.layer.x) * (1.0 + 0.35 * abs(_Turn));
                half3 side = emblem.rgb * sideShade;

                float2 direction = normalize(_ShineDirection.xy);
                float across = dot(i.face, direction) / dot(float2(1.0, 1.0), abs(direction)) + 0.5;
                float gap = (across - _ShineCenter) / _ShineWidth;
                half band = exp(-gap * gap) * 0.84 + exp(-gap * gap / 9.0) * 0.16;
                half3 front = emblem.rgb * (1.0 + 0.12 * _Turn) + band * _ShineStrength;

                half3 color = lerp(side, front, step(0.5, i.layer.y));

                return fixed4(color * emblem.a, emblem.a);
            }
            ENDCG
        }
    }
}
