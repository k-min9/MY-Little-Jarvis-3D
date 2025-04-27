// 사각형 portrait에 있는 통신느낌이 나게 하는 파란색 선 필터 Shader
Shader "Custom/BlueLineFilterOverlay"
{
    Properties
    {
        // 선 색상 (약간 밝은 파란색 추천)
        _LineColor ("Line Color", Color) = (0.2, 0.6, 1, 0.2)

        // 배경 색상 (어두운 파란색 추천)
        _BackgroundColor ("Background Color", Color) = (0, 0.2, 0.4, 0.1)

        // 스캔라인 간격 최소 (추천: 250 ~ 300)
        _LineFrequencyMin ("Line Frequency Min", Float) = 280.0

        // 스캔라인 간격 최대 (추천: 300 ~ 350)
        _LineFrequencyMax ("Line Frequency Max", Float) = 320.0

        // 스캔라인 두께 최소 (추천: 0.35 ~ 0.45)
        _LineThicknessMin ("Line Thickness Min", Float) = 0.4

        // 스캔라인 두께 최대 (추천: 0.55 ~ 0.65)
        _LineThicknessMax ("Line Thickness Max", Float) = 0.6

        // 스캔라인 스크롤 속도 (추천: 0.1 ~ 0.3)
        _LineSpeed ("Line Scroll Speed", Float) = 0.2

        // 스캔라인 밝기 강도 (추천: 0.2 ~ 0.4)
        _LineStrength ("Line Strength", Float) = 0.3

        // 좌우 흔들림 강도 (추천: 0.002 ~ 0.005)
        _WobbleStrength ("Wobble Strength", Float) = 0.003

        // 흔들림 주기 (추천: 30 ~ 60)
        _WobbleFrequency ("Wobble Frequency", Float) = 50.0

        // 노이즈 강도 (추천: 0.03 ~ 0.05)
        _NoiseStrength ("Noise Strength", Float) = 0.05

        // 노이즈 깜빡임 속도 (추천: 1.0 ~ 3.0)
        _NoiseSpeed ("Noise Speed", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Pass
        {
            // 기본 설정
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // 프로퍼티 선언
            float4 _LineColor;
            float4 _BackgroundColor;
            float _LineFrequencyMin;
            float _LineFrequencyMax;
            float _LineThicknessMin;
            float _LineThicknessMax;
            float _LineSpeed;
            float _LineStrength;
            float _WobbleStrength;
            float _WobbleFrequency;
            float _NoiseStrength;
            float _NoiseSpeed;

            // 랜덤 함수 (노이즈, 랜덤에 사용)
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;

                // --- 스캔라인 랜덤 값 계산 ---
                float randVal = frac(sin(time * 0.1) * 43758.5453); // 부드럽게 변하는 랜덤값
                float lineFrequency = lerp(_LineFrequencyMin, _LineFrequencyMax, randVal);
                float lineThicknessMin = lerp(_LineThicknessMin, _LineThicknessMax, randVal);
                float lineThicknessMax = _LineThicknessMax; // 살짝 가변적인 두께
                // -------------------------

                // 스크롤 적용
                float scroll = time * _LineSpeed;

                // 흔들림 적용
                float wobble = sin((i.uv.y * _WobbleFrequency) + (time * 2.0))
                             * cos((time * 1.5) + (i.uv.y * _WobbleFrequency * 0.5));
                wobble *= _WobbleStrength;

                float2 uv = i.uv;
                uv.x += wobble;

                // 스캔라인 패턴
                float lineEffectValue = sin((uv.y + scroll) * lineFrequency) * 0.5 + 0.5;

                // glitch 효과
                float glitch = step(0.95, rand(float2(time * 5.0, uv.y * 50.0)));
                lineEffectValue *= (1.0 - glitch * 0.8);

                // 두께 조정
                // float lineEffect = smoothstep(lineThicknessMin, lineThicknessMax, lineEffectValue);
                float threshold = lerp(lineThicknessMin, lineThicknessMax, randVal);
                float lineEffect = step(1.0 - threshold, lineEffectValue);

                // 배경과 선 보간
                fixed4 finalColor = lerp(_BackgroundColor, _LineColor, lineEffect * _LineStrength);

                // 미세 노이즈 추가
                float noise = (rand(uv * 500.0 + time * _NoiseSpeed) - 0.5) * 2.0;
                noise *= _NoiseStrength;
                noise *= (sin(time * 5.0) * 0.5 + 1.0);

                finalColor.rgb += noise;

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
