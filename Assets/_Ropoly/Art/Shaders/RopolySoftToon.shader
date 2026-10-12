Shader "Ropoly/Soft Toon Surface"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor("Shade Color", Color) = (0.35, 0.28, 0.55, 1)
        _RimColor("Rim Color", Color) = (0.45, 1, 0.9, 1)
        _RimStrength("Rim Strength", Range(0, 2)) = 0.35
        _RimPower("Rim Tightness", Range(0.5, 8)) = 3
        _ShadowThreshold("Light Threshold", Range(-1, 1)) = 0.15
        _ShadowSoftness("Light Softness", Range(0.01, 0.5)) = 0.12
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.75
        _Smoothness("Highlight Smoothness", Range(0, 1)) = 0.35
        _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength("Emission Strength", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "SoftToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _RimColor;
                half _RimStrength;
                half _RimPower;
                half _ShadowThreshold;
                half _ShadowSoftness;
                half _AmbientStrength;
                half _Smoothness;
                half4 _EmissionColor;
                half _EmissionStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.shadowCoord = GetShadowCoord(positions);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight(input.shadowCoord);

                half lightAmount = dot(normalWS, mainLight.direction);
                half toonBand = smoothstep(
                    _ShadowThreshold - _ShadowSoftness,
                    _ShadowThreshold + _ShadowSoftness,
                    lightAmount);
                half attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 toonColor = lerp(_ShadeColor.rgb, _BaseColor.rgb, toonBand);
                half3 direct = toonColor * mainLight.color * attenuation;
                half3 ambient = SampleSH(normalWS) * _BaseColor.rgb * _AmbientStrength;

                half rim = pow(1.0h - saturate(dot(normalWS, viewDirection)), _RimPower);
                half3 rimLight = _RimColor.rgb * rim * _RimStrength;

                half3 halfVector = SafeNormalize(mainLight.direction + viewDirection);
                half specularPower = lerp(12.0h, 96.0h, _Smoothness);
                half specular = pow(saturate(dot(normalWS, halfVector)), specularPower);
                half3 highlight = mainLight.color * specular * _Smoothness * 0.6h;

                half3 emission = _EmissionColor.rgb * _EmissionStrength;
                return half4(direct + ambient + rimLight + highlight + emission, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
