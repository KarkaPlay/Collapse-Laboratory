// ============================================================================
// TemporalInstability.shader
// URP PBR + Temporal Instability + Dissolve
// ============================================================================

Shader "Custom/TemporalInstability"
{
    Properties
    {
        // === PBR Textures ===
        _BaseMap            ("Base Map (Albedo)", 2D)               = "white" {}
        _BaseColor          ("Base Color", Color)                   = (1, 1, 1, 1)

        _MetallicGlossMap   ("Metallic (R) Smoothness (A)", 2D)    = "white" {}
        _Metallic           ("Metallic", Range(0, 1))               = 0.0
        _Smoothness         ("Smoothness", Range(0, 1))             = 0.5

        _BumpMap            ("Normal Map", 2D)                      = "bump" {}
        _BumpScale          ("Normal Scale", Float)                 = 1.0

        _OcclusionMap       ("Occlusion (R)", 2D)                   = "white" {}
        _OcclusionStrength  ("Occlusion Strength", Range(0, 1))     = 1.0

        _EmissionMap        ("Emission Map", 2D)                    = "black" {}
        [HDR] _EmissionColor("Emission Color", Color)              = (0, 0, 0, 1)

        // === Surface Options ===
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff             ("Alpha Cutoff", Range(0, 1))           = 0.5

        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull               ("Cull Mode", Float)                    = 2

        // === Temporal Effect Parameters ===
        _TemporalIntensity  ("Effect Intensity", Range(0, 1))      = 0.0
        _TemporalSpeed      ("Animation Speed", Range(0, 10))      = 1.5

        _WarpAmount         ("Warp Amount (smooth)", Range(0, 0.5)) = 0.05
        _GlitchAmount       ("Glitch Amount (sharp)", Range(0, 1.0))= 0.1

        _NoiseScale         ("Noise Scale", Range(0.1, 20))        = 3.0
        _ColorShiftAmount   ("Color Shift", Range(0, 1))           = 0.15
        _GlowColor          ("Glow Color", Color)                  = (0.4, 0.5, 1.0, 1.0)
        _InstanceSeed       ("Instance Seed", Float)               = 0.0
        _EffectEnabled      ("Effect Enabled", Float)              = 1.0

        [Toggle] _DistortShadows ("Distort Shadows", Float)        = 0.0

        // === Dissolve ===
        _Dissolve           ("Dissolve", Range(0, 1))              = 0.0
        _DissolveEdgeWidth  ("Dissolve Edge Width", Range(0.01, 0.25)) = 0.06
        [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color)    = (3.0, 1.2, 0.2, 1.0)
        _DissolveNoiseScale ("Dissolve Noise Scale", Range(0.1, 50)) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }

        LOD 300

        // =================================================================
        // FORWARD LIT PASS
        // =================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _METALLICGLOSSMAP
            #pragma shader_feature_local _OCCLUSIONMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile _ _FORWARD_PLUS

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // =============================================================
            // CBUFFER
            // =============================================================
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float4 _MetallicGlossMap_ST;
                half   _Metallic;
                half   _Smoothness;
                float4 _BumpMap_ST;
                half   _BumpScale;
                float4 _OcclusionMap_ST;
                half   _OcclusionStrength;
                float4 _EmissionMap_ST;
                half4  _EmissionColor;
                half   _Cutoff;
                float  _Cull;
                half   _TemporalIntensity;
                half   _TemporalSpeed;
                half   _WarpAmount;
                half   _GlitchAmount;
                half   _NoiseScale;
                half   _ColorShiftAmount;
                half4  _GlowColor;
                float  _InstanceSeed;
                half   _EffectEnabled;
                half   _DistortShadows;
                half   _Dissolve;
                half   _DissolveEdgeWidth;
                half4  _DissolveEdgeColor;
                half   _DissolveNoiseScale;
            CBUFFER_END

            // === Textures ===
            TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);

            // =============================================================
            // STRUCTURES
            // =============================================================
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS       : SV_POSITION;
                float2 uv               : TEXCOORD0;
                float3 positionWS       : TEXCOORD1;
                half3  normalWS         : TEXCOORD2;

                #ifdef _NORMALMAP
                half3  tangentWS        : TEXCOORD3;
                half3  bitangentWS      : TEXCOORD4;
                #endif

                half   distortionMask   : TEXCOORD5;

                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 6);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord      : TEXCOORD7;
                #endif

                half   fogFactor        : TEXCOORD8;

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3  vertexLighting   : TEXCOORD9;
                #endif

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // =============================================================
            // NOISE FUNCTIONS
            // =============================================================
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n = i.x + i.y * 57.0 + i.z * 113.0;

                float a = hash(n);
                float b = hash(n + 1.0);
                float c = hash(n + 57.0);
                float d = hash(n + 58.0);
                float e = hash(n + 113.0);
                float ff = hash(n + 114.0);
                float g = hash(n + 170.0);
                float h = hash(n + 171.0);

                return lerp(
                    lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
                    lerp(lerp(e, ff, f.x), lerp(g, h, f.x), f.y),
                    f.z
                );
            }

            float glitchNoise(float time, float seed)
            {
                float t = time * 3.0 + seed;
                float glitch = step(0.85, hash(floor(t * 5.0)));
                float amount = hash(floor(t * 7.0)) * glitch;
                return amount;
            }

            // =============================================================
            // Temporal Distortion
            // =============================================================
            half ApplyTemporalDistortion(
                inout float3 posOS,
                float3 normalOS,
                half effectMask,
                half speed,
                half warpAmount,
                half glitchAmount,
                half noiseScale,
                float seed)
            {
                half distMask = 0.0;

                if (effectMask > 0.001)
                {
                    float time = _Time.y * speed;

                    float warpDisplacement = 0.0;
                    if (warpAmount > 0.001)
                    {
                        float3 noiseCoord = posOS * noiseScale
                            + float3(seed, 0, 0);
                        float noiseVal = noise3D(noiseCoord +
                            float3(time * 0.7, time * 0.3, time * 0.5));

                        warpDisplacement = (noiseVal - 0.5) * 2.0
                            * warpAmount * effectMask;
                        posOS += normalOS * warpDisplacement;

                        float pulse = sin(time * 2.0 + seed) * 0.5 + 0.5;
                        float pulseFactor = 1.0 + (pulse - 0.5)
                            * warpAmount * effectMask * 0.3;
                        posOS *= pulseFactor;
                    }

                    float glitchVal = 0.0;
                    if (glitchAmount > 0.001)
                    {
                        float glitch = glitchNoise(time, seed);
                        float glitchOffset = glitch * glitchAmount
                            * effectMask * 2.0;

                        float sliceSelect = step(0.5,
                            frac(posOS.y * 7.0 + time * 0.5));
                        posOS.x += glitchOffset * sliceSelect;
                        posOS.z += glitchOffset * (1.0 - sliceSelect) * 0.5;

                        glitchVal = glitch;
                    }

                    float warpMask = abs(warpDisplacement)
                        / max(warpAmount * effectMask, 0.001);
                    distMask = saturate(warpMask + glitchVal);
                }

                return distMask;
            }

            // =============================================================
            // VERTEX SHADER
            // =============================================================
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 posOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;

                half effectMask = _EffectEnabled * _TemporalIntensity;
                output.distortionMask = ApplyTemporalDistortion(
                    posOS, normalOS, effectMask,
                    _TemporalSpeed,
                    _WarpAmount,
                    _GlitchAmount,
                    _NoiseScale, _InstanceSeed
                );

                VertexPositionInputs vertexInput =
                    GetVertexPositionInputs(posOS);
                VertexNormalInputs normalInput =
                    GetVertexNormalInputs(normalOS, input.tangentOS);

                output.positionCS   = vertexInput.positionCS;
                output.positionWS   = vertexInput.positionWS;
                output.normalWS     = normalInput.normalWS;
                output.uv           = TRANSFORM_TEX(input.uv, _BaseMap);

                #ifdef _NORMALMAP
                output.tangentWS    = normalInput.tangentWS;
                output.bitangentWS  = normalInput.bitangentWS;
                #endif

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST,
                    output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.vertexLighting = half3(0, 0, 0);
                uint lightsCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightsCount; ++i)
                {
                    Light light = GetAdditionalLight(
                        i, vertexInput.positionWS);
                    half3 attenuatedColor = light.color
                        * (light.distanceAttenuation
                            * light.shadowAttenuation);
                    output.vertexLighting += LightingLambert(
                        attenuatedColor, light.direction,
                        normalInput.normalWS);
                }
                #endif

                output.fogFactor =
                    ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            // =============================================================
            // FRAGMENT SHADER
            // =============================================================
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;

                // ---- Base Color + Alpha Test ----
                half4 baseMap = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap, uv);
                half4 albedo = baseMap * _BaseColor;

                #ifdef _ALPHATEST_ON
                clip(albedo.a - _Cutoff);
                #endif

                // ---- DISSOLVE ----
                half dissolveEdgeGlow = 0.0;
                if (_Dissolve > 0.001)
                {
                    float3 dCoord = input.positionWS * _DissolveNoiseScale;
                    half dNoise = noise3D(dCoord) * 0.65
                                + noise3D(dCoord * 2.3 + 17.3) * 0.35;
                    half dThreshold = lerp(
                        -_DissolveEdgeWidth,
                        1.0 + _DissolveEdgeWidth,
                        _Dissolve);
                    clip(dNoise - dThreshold);

                    half dEdgeDist = dNoise - dThreshold;
                    dissolveEdgeGlow = 1.0 - saturate(
                        dEdgeDist / _DissolveEdgeWidth);
                    dissolveEdgeGlow *= dissolveEdgeGlow;
                }

                // ---- Metallic / Smoothness ----
                half metallic = _Metallic;
                half smoothness = _Smoothness;
                #ifdef _METALLICGLOSSMAP
                half4 metallicGloss = SAMPLE_TEXTURE2D(
                    _MetallicGlossMap, sampler_MetallicGlossMap, uv);
                metallic = metallicGloss.r * _Metallic;
                smoothness = metallicGloss.a * _Smoothness;
                #endif

                // ---- Normal ----
                half3 normalWS = normalize(input.normalWS);
                #ifdef _NORMALMAP
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv),
                    _BumpScale);
                half3x3 TBN = half3x3(
                    input.tangentWS,
                    input.bitangentWS,
                    input.normalWS);
                normalWS = normalize(mul(normalTS, TBN));
                #endif

                // ---- Occlusion ----
                half occlusion = 1.0;
                #ifdef _OCCLUSIONMAP
                half occSample = SAMPLE_TEXTURE2D(
                    _OcclusionMap, sampler_OcclusionMap, uv).r;
                occlusion = lerp(1.0, occSample, _OcclusionStrength);
                #endif

                // ---- Emission ----
                half3 emission = half3(0, 0, 0);
                #ifdef _EMISSION
                emission = SAMPLE_TEXTURE2D(
                    _EmissionMap, sampler_EmissionMap, uv).rgb
                    * _EmissionColor.rgb;
                #endif

                // Dissolve edge glow → emission
                emission += _DissolveEdgeColor.rgb * dissolveEdgeGlow;

                // ---- Shadow Coord ----
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_NEEDS_SHADOW_COORD)
                float4 shadowCoord = TransformWorldToShadowCoord(
                    input.positionWS);
                #else
                float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                // ---- InputData ----
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS =
                    GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = input.fogFactor;

                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                inputData.vertexLighting = input.vertexLighting;
                #else
                inputData.vertexLighting = half3(0, 0, 0);
                #endif

                inputData.bakedGI = SAMPLE_GI(
                    input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(input.positionCS);

                #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aoFactor =
                    GetScreenSpaceAmbientOcclusion(
                        inputData.normalizedScreenSpaceUV);
                inputData.bakedGI *= aoFactor.indirectAmbientOcclusion;
                occlusion = min(occlusion,
                    aoFactor.directAmbientOcclusion);
                #endif

                // ---- SurfaceData ----
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = emission;
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = albedo.a;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                // ========= PBR Lighting =========
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // ---- Temporal Color Shift ----
                half effectMask = _EffectEnabled * _TemporalIntensity;

                if (effectMask > 0.001 && _ColorShiftAmount > 0.001)
                {
                    float time = _Time.y * _TemporalSpeed;
                    half shift = _ColorShiftAmount * effectMask
                        * input.distortionMask;

                    half r_offset = sin(time * 3.0 + _InstanceSeed)
                        * shift;
                    half b_offset = cos(time * 2.7 + _InstanceSeed + 1.5)
                        * shift;

                    color.r += r_offset * 0.3;
                    color.b += b_offset * 0.3;

                    half glowMask = input.distortionMask * effectMask;
                    color.rgb = lerp(color.rgb, _GlowColor.rgb,
                        glowMask * _GlowColor.a * 0.3);

                    half edgeGlow = pow(glowMask, 2.0) * 0.5;
                    color.rgb += _GlowColor.rgb * edgeGlow;
                }

                if (effectMask > 0.001)
                {
                    half temporalEmission = input.distortionMask
                        * effectMask * 0.2;
                    color.rgb += _GlowColor.rgb * temporalEmission;
                }

                color.rgb = MixFog(color.rgb, input.fogFactor);

                return half4(color.rgb, color.a);
            }

            ENDHLSL
        }

        // =================================================================
        // SHADOW CASTER PASS
        // =================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float4 _MetallicGlossMap_ST;
                half   _Metallic;
                half   _Smoothness;
                float4 _BumpMap_ST;
                half   _BumpScale;
                float4 _OcclusionMap_ST;
                half   _OcclusionStrength;
                float4 _EmissionMap_ST;
                half4  _EmissionColor;
                half   _Cutoff;
                float  _Cull;
                half   _TemporalIntensity;
                half   _TemporalSpeed;
                half   _WarpAmount;
                half   _GlitchAmount;
                half   _NoiseScale;
                half   _ColorShiftAmount;
                half4  _GlowColor;
                float  _InstanceSeed;
                half   _EffectEnabled;
                half   _DistortShadows;
                half   _Dissolve;
                half   _DissolveEdgeWidth;
                half4  _DissolveEdgeColor;
                half   _DissolveNoiseScale;
            CBUFFER_END

            #ifdef _ALPHATEST_ON
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                #ifdef _ALPHATEST_ON
                float2 uv         : TEXCOORD0;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD1;
                #ifdef _ALPHATEST_ON
                float2 uv          : TEXCOORD0;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float hash_s(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float noise3D_s(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n = i.x + i.y * 57.0 + i.z * 113.0;
                float a = hash_s(n);
                float b = hash_s(n + 1.0);
                float c = hash_s(n + 57.0);
                float d = hash_s(n + 58.0);
                float e = hash_s(n + 113.0);
                float ff = hash_s(n + 114.0);
                float g = hash_s(n + 170.0);
                float h = hash_s(n + 171.0);
                return lerp(
                    lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
                    lerp(lerp(e, ff, f.x), lerp(g, h, f.x), f.y),
                    f.z);
            }

            float glitchNoise_s(float time, float seed)
            {
                float t = time * 3.0 + seed;
                float glitch = step(0.85, hash_s(floor(t * 5.0)));
                float amount = hash_s(floor(t * 7.0)) * glitch;
                return amount;
            }

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;

                if (_DistortShadows > 0.5)
                {
                    half effectMask = _EffectEnabled
                        * _TemporalIntensity;

                    if (effectMask > 0.001)
                    {
                        float time = _Time.y * _TemporalSpeed;
                        float seed = _InstanceSeed;

                        if (_WarpAmount > 0.001)
                        {
                            float3 noiseCoord = posOS * _NoiseScale
                                + float3(seed, 0, 0);
                            float noiseVal = noise3D_s(noiseCoord +
                                float3(time * 0.7, time * 0.3,
                                    time * 0.5));
                            float displacement = (noiseVal - 0.5) * 2.0
                                * _WarpAmount * effectMask;
                            posOS += normalOS * displacement;

                            float pulse = sin(time * 2.0 + seed)
                                * 0.5 + 0.5;
                            float pulseFactor = 1.0 + (pulse - 0.5)
                                * _WarpAmount * effectMask * 0.3;
                            posOS *= pulseFactor;
                        }

                        if (_GlitchAmount > 0.001)
                        {
                            float glitch = glitchNoise_s(time, seed);
                            float glitchOffset = glitch * _GlitchAmount
                                * effectMask * 2.0;
                            float sliceSelect = step(0.5,
                                frac(posOS.y * 7.0 + time * 0.5));
                            posOS.x += glitchOffset * sliceSelect;
                            posOS.z += glitchOffset
                                * (1.0 - sliceSelect) * 0.5;
                        }
                    }
                }

                float3 posWS =
                    TransformObjectToWorld(posOS);
                float3 normalWS =
                    TransformObjectToWorldNormal(normalOS);

                output.positionWS = posWS;

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(
                    _LightPosition - posWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(
                    ApplyShadowBias(posWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                output.positionCS.z = min(
                    output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(
                    output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                #ifdef _ALPHATEST_ON
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                #endif

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                half4 col = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap, input.uv);
                clip(col.a * _BaseColor.a - _Cutoff);
                #endif

                // Dissolve clip
                if (_Dissolve > 0.001)
                {
                    float3 dCoord = input.positionWS
                        * _DissolveNoiseScale;
                    half dNoise = noise3D_s(dCoord) * 0.65
                                + noise3D_s(dCoord * 2.3 + 17.3) * 0.35;
                    half dThreshold = lerp(
                        -_DissolveEdgeWidth,
                        1.0 + _DissolveEdgeWidth,
                        _Dissolve);
                    clip(dNoise - dThreshold);
                }

                return 0;
            }

            ENDHLSL
        }

        // =================================================================
        // DEPTH ONLY PASS
        // =================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float4 _MetallicGlossMap_ST;
                half   _Metallic;
                half   _Smoothness;
                float4 _BumpMap_ST;
                half   _BumpScale;
                float4 _OcclusionMap_ST;
                half   _OcclusionStrength;
                float4 _EmissionMap_ST;
                half4  _EmissionColor;
                half   _Cutoff;
                float  _Cull;
                half   _TemporalIntensity;
                half   _TemporalSpeed;
                half   _WarpAmount;
                half   _GlitchAmount;
                half   _NoiseScale;
                half   _ColorShiftAmount;
                half4  _GlowColor;
                float  _InstanceSeed;
                half   _EffectEnabled;
                half   _DistortShadows;
                half   _Dissolve;
                half   _DissolveEdgeWidth;
                half4  _DissolveEdgeColor;
                half   _DissolveNoiseScale;
            CBUFFER_END

            #ifdef _ALPHATEST_ON
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            #endif

            // Noise for dissolve
            float hash_d(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float noise3D_d(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n = i.x + i.y * 57.0 + i.z * 113.0;
                float a = hash_d(n);
                float b = hash_d(n + 1.0);
                float c = hash_d(n + 57.0);
                float d = hash_d(n + 58.0);
                float e = hash_d(n + 113.0);
                float ff = hash_d(n + 114.0);
                float g = hash_d(n + 170.0);
                float h = hash_d(n + 171.0);
                return lerp(
                    lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
                    lerp(lerp(e, ff, f.x), lerp(g, h, f.x), f.y),
                    f.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                #ifdef _ALPHATEST_ON
                float2 uv         : TEXCOORD0;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD1;
                #ifdef _ALPHATEST_ON
                float2 uv          : TEXCOORD0;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(
                    input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(
                    input.positionOS.xyz);

                #ifdef _ALPHATEST_ON
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                #endif

                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                half4 col = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap, input.uv);
                clip(col.a * _BaseColor.a - _Cutoff);
                #endif

                // Dissolve clip
                if (_Dissolve > 0.001)
                {
                    float3 dCoord = input.positionWS
                        * _DissolveNoiseScale;
                    half dNoise = noise3D_d(dCoord) * 0.65
                                + noise3D_d(dCoord * 2.3 + 17.3) * 0.35;
                    half dThreshold = lerp(
                        -_DissolveEdgeWidth,
                        1.0 + _DissolveEdgeWidth,
                        _Dissolve);
                    clip(dNoise - dThreshold);
                }

                return 0;
            }

            ENDHLSL
        }

        // =================================================================
        // DEPTH NORMALS PASS
        // =================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float4 _MetallicGlossMap_ST;
                half   _Metallic;
                half   _Smoothness;
                float4 _BumpMap_ST;
                half   _BumpScale;
                float4 _OcclusionMap_ST;
                half   _OcclusionStrength;
                float4 _EmissionMap_ST;
                half4  _EmissionColor;
                half   _Cutoff;
                float  _Cull;
                half   _TemporalIntensity;
                half   _TemporalSpeed;
                half   _WarpAmount;
                half   _GlitchAmount;
                half   _NoiseScale;
                half   _ColorShiftAmount;
                half4  _GlowColor;
                float  _InstanceSeed;
                half   _EffectEnabled;
                half   _DistortShadows;
                half   _Dissolve;
                half   _DissolveEdgeWidth;
                half4  _DissolveEdgeColor;
                half   _DissolveNoiseScale;
            CBUFFER_END

            #ifdef _ALPHATEST_ON
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            #endif

            #ifdef _NORMALMAP
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            #endif

            // Noise for dissolve
            float hash_dn(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float noise3D_dn(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n = i.x + i.y * 57.0 + i.z * 113.0;
                float a = hash_dn(n);
                float b = hash_dn(n + 1.0);
                float c = hash_dn(n + 57.0);
                float d = hash_dn(n + 58.0);
                float e = hash_dn(n + 113.0);
                float ff = hash_dn(n + 114.0);
                float g = hash_dn(n + 170.0);
                float h = hash_dn(n + 171.0);
                return lerp(
                    lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
                    lerp(lerp(e, ff, f.x), lerp(g, h, f.x), f.y),
                    f.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half3  normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD4;
                #ifdef _NORMALMAP
                half3  tangentWS   : TEXCOORD2;
                half3  bitangentWS : TEXCOORD3;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput =
                    GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS  = vertexInput.positionCS;
                output.positionWS  = vertexInput.positionWS;
                output.normalWS    = normalInput.normalWS;
                output.uv          = TRANSFORM_TEX(input.uv, _BaseMap);

                #ifdef _NORMALMAP
                output.tangentWS   = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                #endif

                return output;
            }

            half4 DepthNormalsFrag(Varyings input) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                half4 col = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap, input.uv);
                clip(col.a * _BaseColor.a - _Cutoff);
                #endif

                // Dissolve clip
                if (_Dissolve > 0.001)
                {
                    float3 dCoord = input.positionWS
                        * _DissolveNoiseScale;
                    half dNoise = noise3D_dn(dCoord) * 0.65
                                + noise3D_dn(dCoord * 2.3 + 17.3) * 0.35;
                    half dThreshold = lerp(
                        -_DissolveEdgeWidth,
                        1.0 + _DissolveEdgeWidth,
                        _Dissolve);
                    clip(dNoise - dThreshold);
                }

                half3 normalWS = normalize(input.normalWS);

                #ifdef _NORMALMAP
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(
                        _BumpMap, sampler_BumpMap, input.uv),
                    _BumpScale);
                half3x3 TBN = half3x3(
                    input.tangentWS,
                    input.bitangentWS,
                    input.normalWS);
                normalWS = normalize(mul(normalTS, TBN));
                #endif

                return half4(normalWS, 0.0);
            }

            ENDHLSL
        }

        // =================================================================
        // META PASS
        // =================================================================
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex MetaVert
            #pragma fragment MetaFrag

            #pragma shader_feature_local _EMISSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float4 _MetallicGlossMap_ST;
                half   _Metallic;
                half   _Smoothness;
                float4 _BumpMap_ST;
                half   _BumpScale;
                float4 _OcclusionMap_ST;
                half   _OcclusionStrength;
                float4 _EmissionMap_ST;
                half4  _EmissionColor;
                half   _Cutoff;
                float  _Cull;
                half   _TemporalIntensity;
                half   _TemporalSpeed;
                half   _WarpAmount;
                half   _GlitchAmount;
                half   _NoiseScale;
                half   _ColorShiftAmount;
                half4  _GlowColor;
                float  _InstanceSeed;
                half   _EffectEnabled;
                half   _DistortShadows;
                half   _Dissolve;
                half   _DissolveEdgeWidth;
                half4  _DissolveEdgeColor;
                half   _DissolveNoiseScale;
            CBUFFER_END

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            // Noise for dissolve
            float hash_m(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float noise3D_m(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n = i.x + i.y * 57.0 + i.z * 113.0;
                float a = hash_m(n);
                float b = hash_m(n + 1.0);
                float c = hash_m(n + 57.0);
                float d = hash_m(n + 58.0);
                float e = hash_m(n + 113.0);
                float ff = hash_m(n + 114.0);
                float g = hash_m(n + 170.0);
                float h = hash_m(n + 171.0);
                return lerp(
                    lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
                    lerp(lerp(e, ff, f.x), lerp(g, h, f.x), f.y),
                    f.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float2 uvLM       : TEXCOORD1;
                float2 uvDLM      : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
            };

            Varyings MetaVert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityMetaVertexPosition(
                    input.positionOS.xyz, input.uvLM, input.uvDLM);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = TransformObjectToWorld(
                    input.positionOS.xyz);
                return output;
            }

            half4 MetaFrag(Varyings input) : SV_Target
            {
                // Dissolve clip
                if (_Dissolve > 0.001)
                {
                    float3 dCoord = input.positionWS
                        * _DissolveNoiseScale;
                    half dNoise = noise3D_m(dCoord) * 0.65
                                + noise3D_m(dCoord * 2.3 + 17.3) * 0.35;
                    half dThreshold = lerp(
                        -_DissolveEdgeWidth,
                        1.0 + _DissolveEdgeWidth,
                        _Dissolve);
                    clip(dNoise - dThreshold);
                }

                MetaInput metaInput;
                metaInput.Albedo = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap, input.uv).rgb
                    * _BaseColor.rgb;

                metaInput.Emission = half3(0, 0, 0);
                #ifdef _EMISSION
                metaInput.Emission = SAMPLE_TEXTURE2D(
                    _EmissionMap, sampler_EmissionMap, input.uv).rgb
                    * _EmissionColor.rgb;
                #endif

                return UnityMetaFragment(metaInput);
            }

            ENDHLSL
        }
    }

    CustomEditor "TemporalInstabilityShaderGUI"
    FallBack "Universal Render Pipeline/Lit"
}