// File: Shaders/ProjectorBeam.shader
Shader "Custom/URP/ProjectorBeam"
{
    Properties
    {
        _BeamColor ("Beam Color", Color) = (1, 0.95, 0.85, 0.03)
        _BeamIntensity ("Beam Intensity", Range(0, 1)) = 0.15
        _BeamNoiseTex ("Beam Noise (optional)", 2D) = "white" {}
        _BeamNoiseScale ("Noise Scale", Float) = 2.0
        _BeamNoiseSpeed ("Noise Scroll Speed", Float) = 0.3
        _BeamEdgeFade ("Edge Fade", Range(0.01, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "BeamPass"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex BeamVert
            #pragma fragment BeamFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BeamNoiseTex);   SAMPLER(sampler_BeamNoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _BeamColor;
                float  _BeamIntensity;
                float  _BeamNoiseScale;
                float  _BeamNoiseSpeed;
                float  _BeamEdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 worldNormal: TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 viewDir    : TEXCOORD3;
            };

            Varyings BeamVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                o.worldPos    = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS  = TransformWorldToHClip(o.worldPos);
                o.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                o.uv          = input.uv;
                o.viewDir     = normalize(_WorldSpaceCameraPos - o.worldPos);
                return o;
            }

            half4 BeamFrag(Varyings input) : SV_Target
            {
                // Fresnel-like edge fade — луч прозрачнее при взгляде "в лоб"
                float fresnel = 1.0 - abs(dot(normalize(input.viewDir),
                    normalize(input.worldNormal)));
                fresnel = pow(fresnel, 1.5);

                // Edge fade по UV (конус луча)
                float2 centeredUV = input.uv * 2.0 - 1.0;
                float radialDist = length(centeredUV);
                float edgeFade = 1.0 - smoothstep(
                    1.0 - _BeamEdgeFade, 1.0, radialDist);

                // Анимированный noise
                float2 noiseUV = input.uv * _BeamNoiseScale
                    + float2(0, _Time.y * _BeamNoiseSpeed);
                float noise = SAMPLE_TEXTURE2D(_BeamNoiseTex,
                    sampler_BeamNoiseTex, noiseUV).r;
                noise = lerp(0.7, 1.0, noise); // Не слишком контрастный

                // Финальный beam
                half3 beamColor = _BeamColor.rgb * _BeamIntensity
                    * fresnel * edgeFade * noise;

                return half4(beamColor, 0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}