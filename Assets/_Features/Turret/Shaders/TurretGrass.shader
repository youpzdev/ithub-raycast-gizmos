Shader "Turret/Grass"
{
    Properties
    {
        [MainColor] _BaseColor("Ground Color", Color) = (.32, .66, .24, 1)
        _TipColor("Tip Color", Color) = (.36, .68, .28, 1)
        _ShadowColor("Shadow Tint", Color) = (.46, .5, .62, 1)
        [IntRange] _Steps("Light Steps", Range(2, 3)) = 3
        _AmbientStrength("Ambient Strength", Range(0, 1)) = .15
        _WindStrength("Wind Strength", Range(0, .3)) = .075
        _WindSpeed("Wind Speed", Range(0, 5)) = 1.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _TipColor;
            float4 _ShadowColor;
            float _Steps;
            float _AmbientStrength;
            float _WindStrength;
            float _WindSpeed;
        CBUFFER_END

        float3 GrassPosition(float3 positionOS, float height)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            float phase = positionWS.x * .48 + positionWS.z * .31 + _Time.y * _WindSpeed;
            float sway = sin(phase) + sin(phase * 1.73 + 1.8) * .28;
            positionWS.xz += float2(1, .42) * sway * _WindStrength * height * height;
            return positionWS;
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        ENDHLSL

        Pass
        {
            Name "Grass"
            Tags { "LightMode" = "UniversalForwardOnly" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassVertex
            #pragma fragment GrassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half height : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings GrassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = GrassPosition(input.positionOS.xyz, input.uv.y);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.height = input.uv.y;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 GrassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 normal = normalize(input.normalWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light light = GetMainLight(shadowCoord);
                half illumination = saturate(dot(normal, light.direction)) * light.shadowAttenuation * light.distanceAttenuation;
                half steps = clamp(round(_Steps), 2.0h, 3.0h);
                half edge = max(fwidth(illumination), .001h);
                half band = smoothstep(1.0h / steps - edge, 1.0h / steps + edge, illumination);
                if (steps > 2.5h) band = (band + smoothstep(2.0h / steps - edge, 2.0h / steps + edge, illumination)) * .5h;
                half3 tint = lerp(_ShadowColor.rgb, light.color, band);
                half3 baseColor = lerp(_BaseColor.rgb, _TipColor.rgb, smoothstep(0.0h, 1.0h, input.height));
                half3 color = baseColor * (tint + SampleSH(normal) * _AmbientStrength);
                return half4(MixFog(color, input.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassShadowVertex
            #pragma fragment GrassShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            float4 GrassShadowVertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = GrassPosition(input.positionOS.xyz, input.uv.y);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction = normalize(_LightPosition - positionWS);
                #else
                    float3 direction = _LightDirection;
                #endif
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, direction)));
            }

            half4 GrassShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
