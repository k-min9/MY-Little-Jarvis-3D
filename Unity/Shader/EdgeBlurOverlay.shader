// 대화 중 화면에 울렁임을 주는 Shader
Shader "Custom/EdgeWaveOverlay"
{
    Properties
    {
        _EdgeOpacity ("Edge Opacity", Range(0, 1)) = 0.8
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.0
        _WaveStrength ("Wave Strength", Range(0, 0.2)) = 0.05
        _OverlayColor ("Overlay Color", Color) = (0, 0, 1, 1) // 기본 파란색
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _EdgeOpacity;
            float _WaveSpeed;
            float _WaveStrength;
            float4 _OverlayColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float waveEffect(float dist, float time)
            {
                return sin((dist * 10.0 + time) * 6.28318) * 0.5 + 0.5; // 파동 계산
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5; // 중심(0, 0) 기준
                float dist = length(uv); // 중심에서의 거리 (0 ~ √2 범위)

                // Edge 범위 동적 계산
                float innerEdge = 0.6 - (_EdgeOpacity * 0.2); // 중심으로 파고드는 범위
                float outerEdge = 0.8; // 가장자리 기본 범위

                // 가장자리 불투명도 계산
                float edgeFactor = smoothstep(innerEdge, outerEdge, dist);

                // 파동 효과 (가장자리에서만 적용)
                float wave = waveEffect(dist, _Time.y * _WaveSpeed) * _WaveStrength * edgeFactor;

                // 최종 알파 값
                float alpha = edgeFactor * _EdgeOpacity + wave;

                // _OverlayColor의 RGB 값과 alpha를 최종적으로 반환
                return fixed4(_OverlayColor.rgb, alpha); // 파란색 필터
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
