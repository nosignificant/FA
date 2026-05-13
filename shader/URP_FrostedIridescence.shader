Shader "Custom/URP_FrostedIridescence"
{
    Properties
    {
        [Header(Base Glass Settings)]
        _BaseColor ("Base Color (Alpha for Transparency)", Color) = (0.9, 0.95, 1.0, 0.5)
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.9
        [NoScaleOffset]_BumpMap("Normal Map", 2D) = "bump" {}

        [Header(Frosted Effect)]
        _Blurriness ("Blur Amount (Mipmap Bias)", Range(0.0, 6.0)) = 2.5
        _RefractionStrength ("Refraction Strength", Range(0.0, 0.1)) = 0.02

        [Header(Iridescence Effect)]
        _IridStrength ("Iridescence Strength", Range(0.0, 5.0)) = 2.0
        _IridFrequency ("Iridescence Frequency", Range(1.0, 10.0)) = 3.0
        _IridPower ("Fresnel Power (Sharpness)", Range(0.5, 8.0)) = 3.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #pragma shader_feature_local _NORMALMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 tangentWS    : TEXCOORD2;
                float2 uv           : TEXCOORD3;
                float4 screenPos    : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Smoothness;
                float _Blurriness;
                float _RefractionStrength;
                float _IridStrength;
                float _IridFrequency;
                float _IridPower;
                float4 _BumpMap_ST;
            CBUFFER_END

            TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);

            float3 RainbowPalette(float t)
            {
                float3 a = float3(0.5, 0.5, 0.5);
                float3 b = float3(0.5, 0.5, 0.5);
                float3 c = float3(1.0, 1.0, 1.0);
                float3 d = float3(0.00, 0.33, 0.67);
                return a + b * cos(6.28318 * (c * t * _IridFrequency + d));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BumpMap);
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 positionWS = input.positionWS;
                half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);

                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
                half3 normalTS = UnpackNormal(normalSample);

                float3 sgn = input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
                normalWS = normalize(normalWS);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float2 refractionOffset = normalWS.xy * _RefractionStrength * (1.0 - _Smoothness);
                half3 blurredBackground = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + refractionOffset, _Blurriness).rgb;

                float NdotV = saturate(dot(normalWS, viewDirectionWS));
                float fresnel = pow(1.0 - NdotV, _IridPower);

                half3 iridColor = RainbowPalette(fresnel);

                // InputData 초기화
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
                inputData.fogCoord = ComputeFogFactor(input.positionCS.z);
                inputData.vertexLighting = half3(0.0h, 0.0h, 0.0h);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = half4(1, 1, 1, 1);

                // ▼▼▼ [수정된 핵심] SurfaceData를 0으로 먼저 초기화 ▼▼▼
                SurfaceData surfaceData = (SurfaceData)0; 
                
                surfaceData.albedo = lerp(blurredBackground, _BaseColor.rgb, _BaseColor.a);
                surfaceData.alpha = _BaseColor.a;
                surfaceData.metallic = 0.0;
                // surfaceData.specular = 0.0; // 위에서 (SurfaceData)0으로 초기화했으므로 생략 가능
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0;
                surfaceData.emission = iridColor * fresnel * _IridStrength;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.a = max(_BaseColor.a, 0.2); 

                return color;
            }
            ENDHLSL
        }
    }
}