Shader "Custom/UnlitFlat"
{
    // Quad에 알파 텍스처 붙여 2D 이미지처럼: 빛 무시(unlit) + 평면 색 + 알파 컷아웃.
    Properties
    {
        _MainTex ("Texture (RGBA)", 2D) = "white" {}
        _Color   ("Tint", Color)        = (1,1,1,1)
        _Cutoff  ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        // AlphaTest: 알파로 잘라내되 깊이는 불투명처럼 정확 → 각도 바뀌어도 자연스럽게 배치
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Cull Off      // 양면 (Quad 뒤에서도 보임)
        ZWrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Cutoff;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;
                clip(c.a - _Cutoff);        // 알파 < cutoff면 픽셀 버림(투명)
                return half4(c.rgb, 1.0);   // 라이팅 없음 → 평면 색 그대로 (emission 느낌)
            }
            ENDHLSL
        }
    }
    Fallback Off
}
