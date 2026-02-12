Shader "JellyGunner/ToonLit_Basic"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white"{}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Cel Shading)]
        _ShadowColor("Shadow Color", Color) = (0.3, 0.3, 0.4, 1)
        _Threshold("Shadow Threshold", Range(0, 1)) = 0.5
        _Smoothness("Shadow Smoothness", Range(0.001, 0.5)) = 0.05

        [Header(Rim Light)]
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
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
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            float _Threshold;
            float _Smoothness;
            half4 _RimColor;
            float _RimPower;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

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
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 normal = normalize(input.normalWS);

                float NdotL = dot(normal, mainLight.direction);
                float shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                
                float lightIntensity = smoothstep(
                    _Threshold - _Smoothness,
                    _Threshold + _Smoothness,
                    NdotL * shadowAtten
                );
                
                half3 lightColor = lerp(_ShadowColor.rgb, mainLight.color, lightIntensity);

                float NdotV = 1.0 - saturate(dot(normal, normalize(GetCameraPositionWS() - input.positionWS)));
                float rim = smoothstep(1.0 - (1.0 / _RimPower), 1.0, NdotV * lightIntensity);
                half3 rimEmission = _RimColor.rgb * rim;

                half3 ambient = SampleSH(normal);
                half3 finalColor = albedo.rgb * (lightColor + ambient) + rimEmission;

                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, albedo.a);
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

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

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

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float3 lightDirection = _LightDirection;
                #ifdef _CASTING_PUNCTUAL_LIGHT_SHADOW
                    lightDirection = normalize(_LightPosition - positionWS);
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
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