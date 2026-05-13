// File: Assets/Shaders/MicrobeTranslucentURP.shader
Shader "Custom/MicrobeTranslucentURP"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.55, 1.0, 0.55, 1)
        _Alpha ("Alpha", Range(0,1)) = 0.6

        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BodyColor ("Body Color", Color) = (0.15, 0.6, 0.2, 1)

        // ✅ 추가: 중심 진해짐(라디얼)
        _CenterDarkenStrength ("Center Darken Strength", Range(0,2)) = 0.9
        _CenterRadius ("Center Radius (Object space)", Range(0.01, 5)) = 1.0
        _CenterPower ("Center Power", Range(0.2, 6)) = 2.2

        _CellColor ("Cell Color", Color) = (0.35, 0.95, 0.35, 1)
        _CellScale ("Cell Scale", Range(0.1, 20)) = 6
        _CellEdge ("Cell Edge Sharpness", Range(0.1, 8)) = 2.5
        _CellHoles ("Cell Holes Amount", Range(0, 1)) = 0.55

        _NoiseTex ("Noise (R)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Range(0.1, 30)) = 8
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 0.8

        _SurfaceNoiseScale ("Surface Noise Scale", Range(0.1, 80)) = 28
        _SurfaceNoiseStrength ("Surface Noise Strength", Range(0, 2)) = 0.65

        _BumpScale ("Bump Scale", Range(0.1, 80)) = 18
        _BumpStrength ("Bump Strength", Range(0, 3)) = 1.2

        // ✅ 추가: UV 말고 월드 기준 노이즈도 섞어서 각진 노멀이 덜 딱딱하게
        _WorldNoiseScale ("World Noise Scale", Range(0.1, 20)) = 2.0

        _FresnelPower ("Fresnel Power", Range(0.5, 10)) = 3
        _EdgeGlow ("Edge Glow", Range(0, 3)) = 1.2
        _EdgeColor ("Edge Color", Color) = (0.9, 1.0, 0.95, 1)

        // ✅ 추가: 실루엣 부드럽게(각진 경계 완화)
        _EdgeSoftness ("Edge Softness (alpha)", Range(0,1)) = 0.55
        _EdgeNoiseScale ("Edge Noise Scale", Range(0.1, 80)) = 25
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0,1)) = 0.25
        _EdgeNoisePower ("Edge Noise Power", Range(0.5, 6)) = 2

        _DistortStrength ("Distort Strength", Range(0, 0.1)) = 0.02
        _DistortScale ("Distort Scale", Range(0.1, 40)) = 10
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.02)) = 0.006

        _DepthFade ("Depth Fade", Range(0.01, 5)) = 1.2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            float  _Alpha;

            float4 _BaseColor;
            float4 _BodyColor;
            float  _CenterDarkenStrength;
            float  _CenterRadius;
            float  _CenterPower;

            float4 _CellColor;
            float  _CellScale;
            float  _CellEdge;
            float  _CellHoles;

            float  _NoiseScale;
            float  _NoiseStrength;

            float  _SurfaceNoiseScale;
            float  _SurfaceNoiseStrength;

            float  _BumpScale;
            float  _BumpStrength;
            float  _WorldNoiseScale;

            float  _FresnelPower;
            float  _EdgeGlow;
            float4 _EdgeColor;

            float  _EdgeSoftness;
            float  _EdgeNoiseScale;
            float  _EdgeNoiseStrength;
            float  _EdgeNoisePower;

            float  _DistortStrength;
            float  _DistortScale;
            float  _ChromaticAberration;

            float  _DepthFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float4 screenPos   : TEXCOORD3;
                float3 positionOS  : TEXCOORD4; // ✅ 추가: 중심 그라데이션용
            };

            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float voronoi(float2 uv, float scale, out float edge)
            {
                uv *= scale;
                float2 i = floor(uv);
                float2 f = frac(uv);

                float minDist = 1e9;
                float secondMin = 1e9;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float2 o = hash22(i + g);
                        float2 r = g + o - f;
                        float d = dot(r, r);

                        if (d < minDist)
                        {
                            secondMin = minDist;
                            minDist = d;
                        }
                        else if (d < secondMin)
                        {
                            secondMin = d;
                        }
                    }
                }

                edge = saturate((secondMin - minDist) * 4.0);
                return sqrt(minDist);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                OUT.normalWS    = normalize(nrm.normalWS);
                OUT.uv          = IN.uv;
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                OUT.positionOS  = IN.positionOS.xyz;
                return OUT;
            }

            float4 SampleSceneColorChromatic(float2 uv, float ca)
            {
                float2 off = float2(ca, 0);
                float r = SampleSceneColor(uv + off).r;
                float g = SampleSceneColor(uv).g;
                float b = SampleSceneColor(uv - off).b;
                return float4(r, g, b, 1);
            }

            float DepthFadeFactor(float4 screenPos, float depthFade)
            {
                float2 uv = screenPos.xy / screenPos.w;

                float sceneRaw = SampleSceneDepth(uv);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);

                float myRaw = screenPos.z / screenPos.w;
                float myEye = LinearEyeDepth(myRaw, _ZBufferParams);

                float diff = sceneEye - myEye;
                return saturate(diff / max(1e-3, depthFade));
            }

            void BuildBasis(float3 N, out float3 T, out float3 B)
            {
                float3 up = (abs(N.y) < 0.999) ? float3(0,1,0) : float3(1,0,0);
                T = normalize(cross(up, N));
                B = normalize(cross(N, T));
            }

            float Noise01(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv).r;
            }

            float3 PerturbNormalFromNoise(float3 N, float2 uv, float3 posWS, float scale, float strength, float worldScale)
            {
                float3 T, B;
                BuildBasis(N, T, B);

                // ✅ UV 노이즈 + 월드 노이즈 섞음 (각진 노멀 “반반” 덜하게)
                float n1 = Noise01(uv * scale);
                float n2 = Noise01(posWS.xz * worldScale);
                float n = lerp(n1, n2, 0.6);

                float nx = ddx(n);
                float ny = ddy(n);

                float2 grad = float2(nx, ny);
                return normalize(N + (T * grad.x + B * grad.y) * strength);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 N0 = normalize(IN.normalWS);
                float3 V  = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float3 N = PerturbNormalFromNoise(N0, IN.uv, IN.positionWS, _BumpScale, _BumpStrength, _WorldNoiseScale);

                float fres = pow(1.0 - saturate(dot(N, V)), _FresnelPower);

                float nBase = Noise01(IN.uv * _NoiseScale);
                nBase = (nBase * 2.0 - 1.0) * _NoiseStrength;

                float nSurf = Noise01(IN.uv * _SurfaceNoiseScale);
                nSurf = (nSurf * 2.0 - 1.0) * _SurfaceNoiseStrength;

                float edge;
                float cell = voronoi(IN.uv + (nBase * 0.05) + (nSurf * 0.02), _CellScale, edge);

                float holes    = smoothstep(_CellHoles, 1.0, 1.0 - cell);
                float cellEdge = pow(saturate(edge), _CellEdge);

                float3 internalCol = _CellColor.rgb;
                internalCol += cellEdge * _EdgeColor.rgb * 0.25;
                internalCol += (nSurf * 0.12) * float3(1.0, 1.0, 0.8);

                // ✅ 몸통 색 + 내부 색 섞기
                float3 microbeCol = lerp(_Tint.rgb, internalCol, holes);
                microbeCol = lerp(_BodyColor.rgb, microbeCol, 0.6);

                // ✅ 중심으로 갈수록 진해지는 그라데이션(오브젝트 로컬 기준)
                float centerDist = length(IN.positionOS) / max(1e-3, _CenterRadius);
                float centerMask = pow(saturate(1.0 - centerDist), _CenterPower); // 0(바깥)~1(중심)
                microbeCol *= (1.0 + centerMask * _CenterDarkenStrength); // 중심 진하게

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                float2 nd = normalize(N.xz + float2(nSurf, -nSurf) * 0.35);

                float2 sNoiseUV = screenUV * _DistortScale + nSurf * 0.05;
                float sN = Noise01(sNoiseUV);
                float2 sWarp = (float2(sN, 1.0 - sN) * 2.0 - 1.0) * 0.35;

                float distortAmp = _DistortStrength * (0.25 + fres * 1.75);
                float2 distortUV = screenUV + (nd + sWarp) * distortAmp;

                float4 refr = SampleSceneColorChromatic(distortUV, _ChromaticAberration);

                float fade = DepthFadeFactor(IN.screenPos, _DepthFade);

                float refrWeight = saturate(0.40 + fres * 0.55);
                float3 col = lerp(microbeCol, refr.rgb, refrWeight);

                col += _EdgeColor.rgb * fres * _EdgeGlow;

                // ✅ 핵심: 실루엣을 “부드럽게” 만들기 (각진 경계 완화)
                // fres가 큰(엣지) 구간에서 알파를 살짝 줄이고, 노이즈로 흔들어 각이 덜 보이게
                float edgeNoise = Noise01(IN.positionWS.xz * _EdgeNoiseScale);
                edgeNoise = pow(edgeNoise, _EdgeNoisePower);
                float edgeJitter = lerp(1.0, edgeNoise, _EdgeNoiseStrength);

                float edgeAlphaFactor = 1.0 - (fres * _EdgeSoftness * edgeJitter);

                float alpha = saturate(_Alpha * fade * edgeAlphaFactor);

                col *= _BaseColor.rgb;
                alpha *= _BaseColor.a;

                return float4(col, alpha);

            }
            ENDHLSL
        }
    }
}