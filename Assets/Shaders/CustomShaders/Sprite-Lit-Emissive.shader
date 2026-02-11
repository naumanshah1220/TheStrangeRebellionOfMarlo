Shader "Universal Render Pipeline/2D/Sprite-Lit-Emissive"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionIntensity("Emission Intensity", Float) = 1.0
        _ZWrite("ZWrite", Float) = 0
        _StencilComp("Stencil Comparison", Float) = 8 // Default to Always
        _StencilRef("Stencil Reference", Float) = 0
        _StencilReadMask("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]
        ZTest Off

        Pass
        {
            Name "LitWithEmission"
            Tags { "LightMode" = "Universal2D" }

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                Comp [_StencilComp]
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                half2 lightingUV : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float4 _EmissionColor;
            float _EmissionIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.lightingUV = ComputeScreenPos(o.vertex).xy;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                half brightness = dot(texColor.rgb, half3(0.299, 0.587, 0.114));
                half3 emission = _EmissionColor.rgb * _EmissionIntensity * brightness;
                return half4(texColor.rgb + emission, texColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}