Shader "Custom/SoftSphere"
{
    // 구 메시 전용: 카메라를 정면으로 바라보는 중심은 진하고, 실루엣 가장자리로 갈수록 투명해짐.
    // normal과 view 방향의 각도(inverse fresnel)로 알파를 깎아 부드러운 구/글로우 연출.
    Properties
    {
        _Color      ("Color", Color)                 = (1,1,1,1)
        _Power      ("Edge Falloff (높을수록 중심만 진함)", Range(0.1, 8)) = 2
        _Intensity  ("Intensity (전체 진하기)", Range(0, 3))  = 1
        [Toggle] _Additive ("가산 혼합(빛나는 글로우)", Float)   = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        // 가산이면 (One One), 아니면 일반 알파 블렌드 — 토글에 맞춰 양쪽 패스 중 하나만 컴파일
        ZWrite Off
        Cull Back

        Pass
        {
            // _Additive 토글에 따라 블렌드를 바꿀 수 없어, SrcAlpha 기반으로 두고
            // 가산일 때는 프래그먼트에서 색을 알파로 미리 곱해(premultiply) One/OneMinus로 흉내냄.
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _Power;
                float  _Intensity;
                float  _Additive;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // 정면(중심)=1, 실루엣(가장자리)=0. Power로 falloff 조절.
                float facing = saturate(dot(N, V));
                float a = pow(facing, _Power) * _Color.a * _Intensity;
                a = saturate(a);

                half3 rgb = _Color.rgb;

                // 가산 혼합 흉내: 색을 알파로 미리 곱하고 알파를 0으로 → SrcAlpha*src + ... 대신 src 더해짐
                if (_Additive > 0.5)
                    return half4(rgb * a, 0.0);

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
