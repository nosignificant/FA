Shader "Custom/SimpleObjectDither"
{
    Properties
    {
        _MainColor ("Bright Color", Color) = (1, 1, 1, 1) 
        _ShadowColor ("Dark Color", Color) = (0, 0, 0, 1) 
        _DitherTex ("Dither Pattern", 2D) = "gray" {} 
        _DitherScale ("Dither Scale", Float) = 5.0 
        _Threshold ("Light Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes //cpu >> vert
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings // vert >> frag
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0; // 화면 좌표
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _ShadowColor;
                float _DitherScale;
                float _Threshold;
            CBUFFER_END
            
            TEXTURE2D(_DitherTex); //gpu에 텍스쳐 보내기 
            SAMPLER(sampler_DitherTex); // 텍스쳐 wrap, filter 모드 

            Varyings vert(Attributes input)
            {
                Varyings output;
                // 정점의 좌표 -> cliping space 기준으로 바꾸기 
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                //노말 오브젝트 -> 월드 기준으로 바꾸기 
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                //cliping space -> screenPos
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                //빛방향 받아옴
                Light mainLight = GetMainLight();
                //빛방향 - 노말 내적해서 노말이 얼마나 그 방향을 향해있는지 계산
                float NdotL = dot(normalize(input.normalWS), mainLight.direction);
                //빛 방향 0~1으로 clamp 
                float lightIntensity = saturate(NdotL); // 0~1 밝기

                // 원근에 따른 화면 좌표 , 이걸 vert에서 하면 점 단위에서만 하기 때문에 픽셀단위로 할 수 있는 frag에서 한다 
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // screenParam은 화면 해상도 
                float aspect = _ScreenParams.x / _ScreenParams.y;
                //모니터가 가로로 길어서 x에다가 비율을 곱해서 x축으로 텍스처가 1.77번 반복되게 함
                screenUV.x *= aspect;
                screenUV *= _DitherScale; // 점 크기 조절

                // 텍셀에서 정보 가져옴
                float ditherValue = SAMPLE_TEXTURE2D(_DitherTex, sampler_DitherTex, screenUV).r;

                // 빛이 약한 곳은 ditherValue와 싸워서 지면 어두운 색, 이기면 밝은 색
                float result = step(ditherValue, lightIntensity + _Threshold - 0.5);
                
                return lerp(_ShadowColor, _MainColor, result);
            }
            ENDHLSL
        }
        
    }
}