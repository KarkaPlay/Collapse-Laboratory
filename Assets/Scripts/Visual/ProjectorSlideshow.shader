// File: Shaders/ProjectorSlideshow.shader
Shader "Custom/URP/ProjectorSlideshow"
{
    Properties
    {
        [Header(Slide Textures)]
        _SlideTexA ("Slide A", 2D) = "black" {}
        _SlideTexB ("Slide B", 2D) = "black" {}
        _TransitionBlend ("Transition Blend", Range(0, 1)) = 0

        [Header(Projection Settings)]
        _Brightness ("Brightness", Range(0, 5)) = 1.5
        _Contrast ("Contrast", Range(0.5, 3)) = 1.1
        _LampColor ("Lamp Color", Color) = (1, 0.95, 0.85, 1)
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.08
        _DistortionStrength ("Distortion Strength", Range(0, 0.05)) = 0.008
        _VignetteStrength ("Vignette Strength", Range(0, 2)) = 0.6

        [Header(Atmosphere)]
        _FlickerIntensity ("Flicker Intensity", Range(0, 0.3)) = 0.05
        _FlickerSpeed ("Flicker Speed", Range(0, 50)) = 12
        _DustIntensity ("Dust/Grain Intensity", Range(0, 0.15)) = 0.03

        [Header(Depth Falloff)]
        _FalloffStart ("Falloff Start Distance", Float) = 0.5
        _FalloffEnd ("Falloff End Distance", Float) = 10.0

        [Header(Projection Matrix)]
        _ProjectorAspect ("Projector Aspect Ratio", Float) = 1.333
        _ProjectorFOV ("Projector FOV (degrees)", Float) = 40
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "ProjectorSlidePass"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One          // Additive — свет проектора добавляется к сцене
            ZWrite Off
            ZTest LEqual
            Cull Front             // Рендерим внутренние грани frustum-бокса

            HLSLPROGRAM
            #pragma vertex ProjectorVert
            #pragma fragment ProjectorFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ─────────────────────────────────────────────
            // Uniforms
            // ─────────────────────────────────────────────

            TEXTURE2D(_SlideTexA);      SAMPLER(sampler_SlideTexA);
            TEXTURE2D(_SlideTexB);      SAMPLER(sampler_SlideTexB);

            CBUFFER_START(UnityPerMaterial)
                float  _TransitionBlend;
                float  _Brightness;
                float  _Contrast;
                half4  _LampColor;
                float  _EdgeSoftness;
                float  _DistortionStrength;
                float  _VignetteStrength;
                float  _FlickerIntensity;
                float  _FlickerSpeed;
                float  _DustIntensity;
                float  _FalloffStart;
                float  _FalloffEnd;
                float  _ProjectorAspect;
                float  _ProjectorFOV;
            CBUFFER_END

            // Projector world→local матрица (передаётся из C#)
            float4x4 _ProjectorVP;
            float4   _ProjectorPosition;   // world-space позиция проектора
            float4   _ProjectorForward;     // world-space направление проектора

            // ─────────────────────────────────────────────
            // Depth texture для реконструкции world position
            // ─────────────────────────────────────────────

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            // ─────────────────────────────────────────────
            // Hash-based noise (без текстуры, GPU-friendly)
            // ─────────────────────────────────────────────

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            // Аналоговое мерцание — сумма нескольких синусоид разной частоты
            float AnalogFlicker(float time, float intensity)
            {
                float flicker = 0.0;
                flicker += sin(time * _FlickerSpeed * 1.0) * 0.4;
                flicker += sin(time * _FlickerSpeed * 2.37 + 1.5) * 0.25;
                flicker += sin(time * _FlickerSpeed * 5.13 + 3.7) * 0.15;
                flicker += (Hash11(floor(time * _FlickerSpeed * 0.7)) - 0.5) * 0.2;
                return 1.0 + flicker * intensity;
            }

            // Barrel distortion (бочкообразное искажение линзы)
            float2 BarrelDistortion(float2 uv, float strength)
            {
                float2 centered = uv * 2.0 - 1.0;
                float r2 = dot(centered, centered);
                float distort = 1.0 + r2 * strength + r2 * r2 * strength * 0.5;
                centered *= distort;
                return centered * 0.5 + 0.5;
            }

            // ─────────────────────────────────────────────
            // Structures
            // ─────────────────────────────────────────────

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldRayDir  : TEXCOORD0;
                float3 worldRayOrig : TEXCOORD1;
                float4 screenPos    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ─────────────────────────────────────────────
            // Vertex
            // ─────────────────────────────────────────────

            Varyings ProjectorVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Frustum-бокс вершина → world space
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                output.positionCS = TransformWorldToHClip(worldPos);
                output.screenPos  = ComputeScreenPos(output.positionCS);

                // Луч от камеры к вершине бокса
                output.worldRayOrig = _WorldSpaceCameraPos.xyz;
                output.worldRayDir  = worldPos - _WorldSpaceCameraPos.xyz;

                return output;
            }

            // ─────────────────────────────────────────────
            // Fragment
            // ─────────────────────────────────────────────

            half4 ProjectorFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ── 1. Реконструкция world position из depth buffer ──

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SAMPLE_TEXTURE2D_X(
                    _CameraDepthTexture, sampler_CameraDepthTexture, screenUV
                ).r;

                // Если нет геометрии — отбрасываем
                #if UNITY_REVERSED_Z
                    if (rawDepth < 0.0001) return half4(0, 0, 0, 0);
                #else
                    if (rawDepth > 0.9999) return half4(0, 0, 0, 0);
                #endif

                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // Реконструкция world position
                float3 rayDir = normalize(input.worldRayDir);
                float viewDirDotForward = dot(rayDir,
                    -UNITY_MATRIX_V[2].xyz);
                float3 worldPos = input.worldRayOrig +
                    rayDir * (sceneEyeDepth / max(viewDirDotForward, 0.001));

                // ── 2. World → Projector clip space ──

                float4 projClip = mul(_ProjectorVP, float4(worldPos, 1.0));

                // Perspective divide
                float3 projNDC = projClip.xyz / projClip.w;

                // Отсечение: только перед проектором
                if (projClip.w <= 0.0) return half4(0, 0, 0, 0);

                // NDC → UV [0,1]
                float2 projUV = projNDC.xy * 0.5 + 0.5;

                // Отсечение вне frustum
                if (any(projUV < -0.05) || any(projUV > 1.05))
                    return half4(0, 0, 0, 0);

                // ── 3. Barrel distortion ──

                float2 distortedUV = BarrelDistortion(projUV,
                    _DistortionStrength * 20.0);

                // Повторная проверка после дисторсии
                if (any(distortedUV < 0.0) || any(distortedUV > 1.0))
                    return half4(0, 0, 0, 0);

                // ── 4. Сэмплирование слайдов ──

                half4 slideA = SAMPLE_TEXTURE2D(_SlideTexA,
                    sampler_SlideTexA, distortedUV);
                half4 slideB = SAMPLE_TEXTURE2D(_SlideTexB,
                    sampler_SlideTexB, distortedUV);
                half4 slideColor = lerp(slideA, slideB, _TransitionBlend);

                // ── 5. Edge softness (мягкие края) ──

                float2 edgeDist = min(distortedUV, 1.0 - distortedUV);
                float edgeFade = saturate(edgeDist.x / _EdgeSoftness)
                               * saturate(edgeDist.y / _EdgeSoftness);
                edgeFade = smoothstep(0.0, 1.0, edgeFade);

                // ── 6. Виньетка ──

                float2 vignetteCoord = distortedUV * 2.0 - 1.0;
                float vignette = 1.0 - dot(vignetteCoord, vignetteCoord)
                    * _VignetteStrength;
                vignette = saturate(vignette);

                // ── 7. Depth falloff ──

                float3 toSurface = worldPos - _ProjectorPosition.xyz;
                float projDist = length(toSurface);
                float depthFade = 1.0 - saturate(
                    (projDist - _FalloffStart) / max(_FalloffEnd - _FalloffStart, 0.001)
                );
                depthFade = smoothstep(0.0, 1.0, depthFade);

                // Также fade-in от проектора (не проецировать слишком близко)
                float nearFade = saturate((projDist - _FalloffStart * 0.5)
                    / max(_FalloffStart * 0.5, 0.001));

                // ── 8. Угловой falloff (Lambertian-like) ──

                float3 projForward = normalize(_ProjectorForward.xyz);
                float3 toSurfaceDir = normalize(toSurface);
                float angleFade = saturate(
                    dot(projForward, toSurfaceDir) * 1.5 - 0.3
                );

                // ── 9. Аналоговое мерцание ──

                float flicker = AnalogFlicker(_Time.y, _FlickerIntensity);

                // ── 10. Пыль/зерно ──

                float grain = Hash21(distortedUV * 500.0 +
                    _Time.y * 30.0) * _DustIntensity;

                // Случайные «пылинки» — крупные яркие точки
                float dustParticle = step(0.997,
                    Hash21(floor(distortedUV * 80.0) +
                    floor(_Time.y * 8.0) * 7.13));
                grain += dustParticle * _DustIntensity * 3.0;

                // ── 11. Контраст ──

                half3 color = slideColor.rgb;
                color = saturate((color - 0.5) * _Contrast + 0.5);

                // ── 12. Финальная композиция ──

                float totalMask = edgeFade * vignette * depthFade
                    * nearFade * angleFade;

                half3 finalColor = color * _LampColor.rgb * _Brightness
                    * flicker * totalMask;
                finalColor += grain * _LampColor.rgb * totalMask;

                // Alpha для потенциальной маскировки
                half finalAlpha = slideColor.a * totalMask;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}