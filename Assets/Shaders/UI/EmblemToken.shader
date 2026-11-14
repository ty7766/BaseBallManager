// 입체 엠블럼 토큰. EmblemTokenMesh 가 알파 윤곽을 따라 세운 옆벽 + 앞면 그림 한 장을 렌더 텍스처에 그린다.
// 옆벽은 윤곽 바로 안쪽 그림 색을 어둡게 눌러 빛 방향(왼쪽 위 앞)에 따라 밝기를 주고, 앞면은 원래 색 + 비스듬한 빛 띠가 주기마다 지나간다.
// 깊이를 써서 벽끼리 가림을 맞추고, 결과는 미리 곱한 알파(premultiplied)로 남긴다
Shader "BaseBallManager/UI/EmblemToken"
{
    Properties
    {
        _MainTex ("Emblem", 2D) = "white" {}
        _WallAmbient ("Wall Ambient", Range(0, 1)) = 0.2
        _WallDiffuse ("Wall Diffuse", Range(0, 1)) = 0.6
        _LightDirection ("Light Direction (빛을 향하는 쪽)", Vector) = (-0.45, 0.4, -0.8, 0)
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
        ZWrite On
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half _WallAmbient;
            half _WallDiffuse;
            float4 _LightDirection;
            half _Turn;
            float _ShineCenter;
            float _ShineWidth;
            half _ShineStrength;
            float4 _ShineDirection;

            struct TokenAppData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 kind : TEXCOORD1;
            };

            struct TokenV2F
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 face : TEXCOORD1;
                float3 normal : TEXCOORD2;
                half front : TEXCOORD3;
            };

            // kind.x = 1 이면 앞면. face = 앞면 가운데 기준 좌표 (빛 띠용)
            TokenV2F vert(TokenAppData v)
            {
                TokenV2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.face = v.vertex.xy;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.front = v.kind.x;
                return o;
            }

            fixed4 frag(TokenV2F i) : SV_Target
            {
                fixed4 emblem = tex2D(_MainTex, i.uv);

                half lambert = saturate(dot(normalize(i.normal), normalize(_LightDirection.xyz)));
                half3 wall = emblem.rgb / max(emblem.a, 0.05) * (_WallAmbient + _WallDiffuse * lambert);

                float2 direction = normalize(_ShineDirection.xy);
                float across = dot(i.face, direction) / dot(float2(1.0, 1.0), abs(direction)) + 0.5;
                float gap = (across - _ShineCenter) / _ShineWidth;
                half band = exp(-gap * gap) * 0.84 + exp(-gap * gap / 9.0) * 0.16;
                half3 face = (emblem.rgb * (1.0 + 0.12 * _Turn) + band * _ShineStrength) * emblem.a;

                return i.front > 0.5 ? fixed4(face, emblem.a) : fixed4(wall, 1.0);
            }
            ENDCG
        }
    }
}
