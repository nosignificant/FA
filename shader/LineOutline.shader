Shader "Custom/LineOutline"
{
    // LineRenderer 전용: 선 폭 방향(UV.y)을 이용해 가장자리는 외곽선 색, 안쪽은 본색.
    Properties
    {
        _Color        ("Fill Color", Color)        = (1,1,1,1)
        _OutlineColor ("Outline Color", Color)     = (0,0,0,1)
        _OutlineWidth ("Outline Width (0-0.5)", Range(0,0.5)) = 0.2
        [Toggle] _UseVertexColor ("LineRenderer 색 곱하기", Float) = 0
    }

    SubShader
    {
        // 월드 지오메트리(잡초 등)처럼: 불투명 + 깊이 기록 → 각도 바뀌어도 가림이 일관됨
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        ZWrite On
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _UseVertexColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv    = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // uv.y: 선 폭 방향 0~1. 가장자리(0/1 근처)일수록 edge가 작음.
                float edge = min(IN.uv.y, 1.0 - IN.uv.y);   // 가장자리=0, 중앙=0.5
                float4 col = (edge < _OutlineWidth) ? _OutlineColor : _Color;

                // 옵션: LineRenderer 자체 색/알파(그라디언트) 곱하기
                if (_UseVertexColor > 0.5) col *= IN.color;

                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
