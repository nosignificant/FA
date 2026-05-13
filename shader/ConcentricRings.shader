Shader "Custom/ConcentricRings"
{
    Properties
    {
        _ColorA ("Ring Color A", Color) = (0, 0.5, 1, 1)
        _ColorB ("Ring Color B", Color) = (0, 1, 0.3, 1)
        _BgColor ("Background Color", Color) = (1, 1, 1, 1)
        _RingSpacing ("Ring Spacing", Range(0.01, 1.0)) = 0.2
        _RingWidth ("Ring Width", Range(0.001, 0.5)) = 0.03
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ConcentricRings"
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _BgColor;
                float _RingSpacing;
                float _RingWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 구체 위 위치를 위도(latitude) 기준으로 동심원 계산
                float3 n = normalize(IN.normalWS);

                // y축 기준 위도 (-1 ~ 1)
                float lat = n.y * 0.5 + 0.5; // 0 ~ 1

                // spacing마다 반복되는 값
                float ring = frac(lat / _RingSpacing);

                // ring 값이 RingWidth 이내면 링으로 판정
                float isRing = step(ring, _RingWidth / _RingSpacing);

                // 짝수/홀수 링 구분해서 두 색상 교차
                float ringIndex = floor(lat / _RingSpacing);
                float isEven = fmod(ringIndex, 2.0);

                float4 ringColor = lerp(_ColorA, _ColorB, isEven);
                return lerp(_BgColor, ringColor, isRing);
            }
            ENDHLSL
        }
    }
}
