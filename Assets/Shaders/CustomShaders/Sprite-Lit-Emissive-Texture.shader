Shader "Universal Render Pipeline/2D/Sprite-Lit-Emissive-Texture"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        _EmissionTex("Emission Mask", 2D) = "black" {}
        _EmissionColor("Emission Color", Color) = (1,1,1)
        _EmissionIntensity("Emission Intensity", Range(0, 500)) = 1
        // UI stencil compatibility (UGUI sets these on materials used by Mask/ScrollRect)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            // Stencil configured by UI via material properties
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _EmissionTex;
            float4 _EmissionTex_ST;

            float4 _Color;
            float3 _EmissionColor;
            float _EmissionIntensity;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 baseColor = tex2D(_MainTex, i.uv) * _Color;
                float emissionMask = tex2D(_EmissionTex, i.uv).r;

                float3 emission = _EmissionColor * _EmissionIntensity * emissionMask;

                return float4(baseColor.rgb + emission, baseColor.a);
            }

            ENDHLSL
        }
    }
}
