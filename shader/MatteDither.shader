Shader "Custom/MatteDither"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        [Header(Shadow Dither)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 100.0
        _ShadowMidColor ("Mid Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ShadowDarkColor ("Max Shadow Color", Color) = (0.1, 0.1, 0.1, 1)
        _ShadowSharpness ("Shadow Edge Sharpness", Range(0,1)) = 0.5

        [Header(Halftone Dot)]
        _DotScale ("Dot Grid Size", Float) = 6.0
        _DotRadius ("Dot Radius", Range(0.01, 0.49)) = 0.2
        _DotColor ("Dot Color", Color) = (0,0,0,1)
        _DotStrength ("Dot Strength", Range(0,1)) = 0.5

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // ─────────────────────────────────────────────
        // Pass 1: Outline (back-face + stencil)
        // ─────────────────────────────────────────────
        Pass
        {
            Name "Outline"
            Cull Front

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            float  _NoiseScale;
            float4 _ShadowMidColor;
            float4 _ShadowDarkColor;
            float  _ShadowSharpness;
            float  _DotScale;
            float  _DotRadius;
            float4 _DotColor;
            float  _DotStrength;
            float4 _OutlineColor;
            float  _OutlineWidth;
            CBUFFER_END

            Varyings OutlineVert(Attributes input)
            {
                Varyings output;
                float3 posWS  = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                posWS += normWS * _OutlineWidth;
                output.positionCS = TransformWorldToHClip(posWS);
                return output;
            }

            float4 OutlineFrag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Pass 2: Main (Noise Shadow + Halftone Dot)
        // ─────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);   SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            float  _NoiseScale;
            float4 _ShadowMidColor;
            float4 _ShadowDarkColor;
            float  _ShadowSharpness;
            float  _DotScale;
            float  _DotRadius;
            float4 _DotColor;
            float  _DotStrength;
            float4 _OutlineColor;
            float  _OutlineWidth;
            CBUFFER_END

            // ── Halftone: 격자마다 원형 도트 ─────────────
            float HalftoneDot(float2 screenPos)
            {
                float2 cell = frac(screenPos / _DotScale) - 0.5;
                return step(length(cell), _DotRadius);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS  = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS  = TransformWorldToHClip(output.positionWS);
                output.normalWS    = TransformObjectToWorldNormal(input.normalOS);
                output.uv          = TRANSFORM_TEX(input.uv, _MainTex);
                output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // ── Base color ───────────────────────────
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                // ── 노이즈 텍스처 샘플 (스크린 공간 기준) ──
                // 스크린 좌표를 NoiseScale로 나눠서 UV로 사용
                // → 오브젝트가 움직여도 노이즈가 화면에 고정됨 (Bayer처럼)
                float2 noiseUV = input.positionCS.xy / _NoiseScale;
                float  noise   = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // ── 그림자 판정 ──────────────────────────
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 N    = normalize(input.normalWS);
                float  NdotL = saturate(dot(N, mainLight.direction));

                // NdotL을 sharpness로 대비 조정
                float sharpened = saturate(
                    (NdotL - 0.5 + _ShadowSharpness * 0.5)
                    / max(1.0 - _ShadowSharpness, 0.001)
                );

                // ① 자체 음영: 노이즈를 threshold로
                float selfShadow = step(sharpened, noise);

                // ② Cast Shadow: shadowAttenuation도 노이즈로 디더링
                float castShadow = step(mainLight.shadowAttenuation, noise);

                float shadow = max(selfShadow, castShadow);

                // ── 그림자 색 (노이즈로 mid~dark 블렌딩 → 그레인) ──
                float3 midShadowBase  = baseColor.rgb * _ShadowMidColor.rgb;
                float3 darkShadowBase = baseColor.rgb * _ShadowDarkColor.rgb;
                float3 shadowBase     = lerp(midShadowBase, darkShadowBase, noise);

                // ── Halftone dot ─────────────────────────
                float  dotMask      = HalftoneDot(input.positionCS.xy);
                float3 shadowWithDot = lerp(shadowBase,     _DotColor.rgb, dotMask * _DotStrength);
                float3 litWithDot    = lerp(baseColor.rgb,  _DotColor.rgb, dotMask * _DotStrength * 0.15);

                float3 finalColor = lerp(litWithDot, shadowWithDot, shadow);

                return float4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Pass 3: Shadow Caster
        // ─────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // Pass 4: Depth Only
        // ─────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
