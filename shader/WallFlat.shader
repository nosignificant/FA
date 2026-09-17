Shader "Custom/WallFlat"
{
    // 방 벽용 lit 셰이더: 조명(메인+추가광)·그림자 받음 + 환경 반사(금속) + 발광.
    // WallActivationTint 호환(_BaseColor). 발광은 URP Bloom(post) 켜져 있어야 실제로 "빛남".
    Properties
    {
        _BaseColor       ("Base Color", Color)              = (1,1,1,1)
        [HDR] _EmissionColor ("Emission Color", Color)      = (0,0,0,1)
        _EmissionStrength("Emission Strength", Range(0,8))  = 1.0

        [Header(Metallic Reflection)]
        _Metallic        ("Metallic (반사량)", Range(0,1))   = 0.0
        _Smoothness      ("Smoothness (매끄러움)", Range(0,1)) = 0.5
        _ReflectionStrength ("Reflection Strength", Range(0,2)) = 1.0

        [Header(Lighting)]
        _AmbientStrength ("Ambient Strength", Range(0,2))   = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 조명/그림자 키워드
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float  _EmissionStrength;
                float  _Metallic;
                float  _Smoothness;
                float  _ReflectionStrength;
                float  _AmbientStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS   = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                half3 albedo = _BaseColor.rgb;

                // ── 메인 라이트 (그림자 포함) ──
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light main = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(N, main.direction));
                half3 lighting = main.color * (ndotl * main.shadowAttenuation);

                // ── 추가 라이트 ──
                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint i = 0; i < count; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    half nl = saturate(dot(N, l.direction));
                    lighting += l.color * (nl * l.distanceAttenuation * l.shadowAttenuation);
                }
                #endif

                // ── 앰비언트(간접광, SH) ──
                half3 ambient = SampleSH(N) * _AmbientStrength;

                half3 col = albedo * (lighting + ambient);

                // ── 환경 반사(금속 광택) — 기본색 위에 더함 ──
                if (_Metallic > 0.0001)
                {
                    float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                    float3 R = reflect(-V, N);
                    half roughness = 1.0h - _Smoothness;
                    half mip = roughness * 6.0h;
                    half4 enc = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, R, mip);
                    half3 refl = DecodeHDREnvironment(enc, unity_SpecCube0_HDR);
                    col += refl * (_ReflectionStrength * _Metallic);
                }

                col += _EmissionColor.rgb * _EmissionStrength;   // 발광

                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }

        // 그림자 드리우기(다른 표면에 그림자 지게)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct SAtt { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SVar { float4 positionCS : SV_POSITION; };

            SVar shadowVert(SAtt IN)
            {
                SVar OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(IN.normalOS);
                float4 pos   = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    pos.z = min(pos.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    pos.z = max(pos.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = pos;
                return OUT;
            }

            half4 shadowFrag(SVar IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
