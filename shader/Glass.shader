// URP 유리 셰이더: 반투명 틴트 + 프레넬(가장자리 반짝) + 스페큘러 + (옵션) 굴절
// - 굴절을 쓰려면 URP Asset에서 Opaque Texture 켜야 함 (_CameraOpaqueTexture 사용).
//   안 켜도 RefractionStrength=0이면 그냥 투명 유리로 동작.
Shader "Custom/Glass"
{
    Properties
    {
        _BaseColor      ("Tint (RGB) + 투명도(A)", Color) = (0.6, 0.8, 0.9, 0.25)
        _FresnelColor   ("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower   ("Fresnel Power(클수록 가장자리에만)", Range(0.5, 8)) = 3
        _FresnelStrength("Fresnel Strength", Range(0, 3)) = 1
        _Smoothness     ("Smoothness(반짝임)", Range(0, 1)) = 0.9
        _SpecStrength   ("Specular Strength", Range(0, 4)) = 1.5
        _RefractionStrength ("Refraction(굴절 왜곡, Opaque Texture 필요)", Range(0, 0.2)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "GlassForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _FresnelColor;
                float  _FresnelPower;
                float  _FresnelStrength;
                float  _Smoothness;
                float  _SpecStrength;
                float  _RefractionStrength;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.screenPos  = ComputeScreenPos(p.positionCS);
                OUT.fogFactor  = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // 프레넬: 시선과 수직인 가장자리일수록 강함
                float fresnel = pow(saturate(1.0 - saturate(dot(N, V))), _FresnelPower) * _FresnelStrength;

                // 배경 굴절 (Opaque Texture) — 노멀로 스크린 UV를 살짝 밀어 왜곡
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 refractUV = screenUV + N.xy * _RefractionStrength;
                half3 sceneCol = SampleSceneColor(refractUV);

                // 틴트 = 배경(굴절)과 유리색 혼합. RefractionStrength=0이면 순수 틴트(투명).
                half3 tint = lerp(_BaseColor.rgb, sceneCol * _BaseColor.rgb, saturate(_RefractionStrength * 10));

                // 메인 라이트 스페큘러(Blinn-Phong)
                Light mainLight = GetMainLight();
                float3 H = normalize(mainLight.direction + V);
                float specPow = exp2(_Smoothness * 10.0 + 1.0);
                float spec = pow(saturate(dot(N, H)), specPow) * _SpecStrength;
                half3 specCol = mainLight.color * spec;

                half3 color = tint + _FresnelColor.rgb * fresnel + specCol;

                // 가장자리·반짝일수록 더 불투명하게
                half alpha = saturate(_BaseColor.a + fresnel + spec);

                color = MixFog(color, IN.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
