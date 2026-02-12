Shader "JellyGunner/JellyDeform_Instanced"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Cel Shading)]
        _ShadowColor("Shadow Color", Color) = (0.3, 0.3, 0.4, 1)
        _Threshold("Shadow Threshold", Range(0, 1)) = 0.5
        _Smoothness("Shadow Smoothness", Range(0.001, 0.5)) = 0.05
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3

        [Header(Jelly Deform)]
        _BreathAmplitude("Breath Amplitude", Range(0, 0.3)) = 0.08
        _BreathSpeed("Breath Speed", Range(0.5, 8)) = 2
        _ImpactStrength("Impact Deform Strength", Range(0, 1)) = 0.4
        _ImpactFrequency("Impact Frequency", Range(5, 30)) = 12

        [Header(HP Feedback)]
        _DamageColor("Damage Tint", Color) = (1, 0.9, 0.9, 1)
        _DamageEmission("Damage Emission", Range(0, 2)) = 0.5

        [Header(Highlight)]
        _HighlightColor("Highlight Color", Color) = (1, 1, 1, 0.6)
        _HighlightSpeed("Highlight Pulse Speed", Range(1, 10)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #pragma instancing_options procedural : Setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct JellyInstanceData
            {
                float4x4 objectToWorld;
                float4x4 worldToObject;
                float4 color;
                float deformImpact;
                float hpNormalized;
                float deathProgress;
                float highlightPulse;
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 instanceColor : TEXCOORD3;
                float3 perInstanceParams : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            StructuredBuffer<JellyInstanceData> _VisibleBuffer;

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            float _Threshold;
            float _Smoothness;
            half4 _RimColor;
            float _RimPower;
            float _BreathAmplitude;
            float _BreathSpeed;
            float _ImpactStrength;
            float _ImpactFrequency;
            half4 _DamageColor;
            float _DamageEmission;
            half4 _HighlightColor;
            float _HighlightSpeed;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            void Setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    JellyInstanceData data = _VisibleBuffer[unity_InstanceID];
                    unity_ObjectToWorld = data.objectToWorld;
                    unity_WorldToObject = data.worldToObject;
                #endif
            }

            float3 ApplyJellyDeform(float3 posOS, float3 normalOS, float impact, float deathProg)
            {
                float3 result = posOS;

                float breathPhase = _Time.y * _BreathSpeed + posOS.y * 2.0;
                result += normalOS * sin(breathPhase) * _BreathAmplitude * (1.0 - deathProg);

                float impactWave = sin(_Time.y * _ImpactFrequency + posOS.y * 6.0);
                float squash = impact * _ImpactStrength * impactWave;
                result.x *= 1.0 + squash * 0.5;
                result.z *= 1.0 + squash * 0.5;
                result.y *= 1.0 - squash;

                result *= 1.0 - deathProg;

                return result;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float impact = 0;
                float hpN = 1;
                float deathProg = 0;
                float highlight = 0;
                float4 iColor = float4(1, 1, 1, 1);

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    JellyInstanceData inst = _VisibleBuffer[unity_InstanceID];
                    impact = inst.deformImpact;
                    hpN = inst.hpNormalized;
                    deathProg = inst.deathProgress;
                    highlight = inst.highlightPulse;
                    iColor = inst.color;
                #endif

                float3 deformed = ApplyJellyDeform(input.positionOS.xyz, input.normalOS, impact, deathProg);

                output.positionWS = TransformObjectToWorld(deformed);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.instanceColor = iColor;
                output.perInstanceParams = float3(hpN, deathProg, highlight);

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float hpN = input.perInstanceParams.x;
                float deathProg = input.perInstanceParams.y;
                float highlight = input.perInstanceParams.z;

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv)
                             * _BaseColor
                             * input.instanceColor;

                float damageLerp = 1.0 - hpN;
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * _DamageColor.rgb, damageLerp * 0.6);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 normal = normalize(input.normalWS);

                float NdotL = dot(normal, mainLight.direction);
                float lightIntensity = smoothstep(
                    _Threshold - _Smoothness,
                    _Threshold + _Smoothness,
                    NdotL * mainLight.shadowAttenuation
                );
                half3 lightColor = lerp(_ShadowColor.rgb, mainLight.color, lightIntensity);

                float NdotV = 1.0 - saturate(
                    dot(normal, normalize(GetCameraPositionWS() - input.positionWS))
                );
                float rim = smoothstep(1.0 - (1.0 / _RimPower), 1.0, NdotV * lightIntensity);
                half3 rimEmission = _RimColor.rgb * rim;

                half3 damageGlow = albedo.rgb * _DamageEmission * damageLerp;

                float pulse = sin(_Time.y * _HighlightSpeed) * 0.5 + 0.5;
                half3 highlightGlow = _HighlightColor.rgb * highlight * pulse * 0.8;

                half3 ambient = SampleSH(normal) * 0.3;
                half3 finalColor = albedo.rgb * (lightColor + ambient)
                                 + rimEmission
                                 + damageGlow
                                 + highlightGlow;

                float deathFade = 1.0 - deathProg;
                return half4(finalColor * deathFade, albedo.a * deathFade);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma instancing_options procedural : Setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct JellyInstanceData
            {
                float4x4 objectToWorld;
                float4x4 worldToObject;
                float4 color;
                float deformImpact;
                float hpNormalized;
                float deathProgress;
                float highlightPulse;
            };

            StructuredBuffer<JellyInstanceData> _VisibleBuffer;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void Setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    JellyInstanceData data = _VisibleBuffer[unity_InstanceID];
                    unity_ObjectToWorld = data.objectToWorld;
                    unity_WorldToObject = data.worldToObject;
                #endif
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS.xyz));
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
