Shader "Custom/AnyTrail"
{
    Properties {}

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Assets/AnyTrail/AnyTrailShaderInclude.hlsl"


    struct Attributes {
        float3 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
    };

    Varyings vert(Attributes IN)
    {
        Varyings OUT;
        OUT.positionCS = TransformObjectToHClip(IN.positionOS);
        OUT.positionWS = TransformObjectToWorld(IN.positionOS);
        return OUT;
    }


    half4 frag(Varyings IN) : SV_Target
    {
        float3 posWS = IN.positionWS;

        float4 color = 1;
        float3 fluidTex = SampleAnyTrailFluid(posWS);
        float3 waveTex = sampleAnyTrailWave(posWS);
        float3 trailTex = sampleAnyTrailTrail(posWS);
        float3 patternTex = sampleAnyTrailPattern(posWS);

        //自定义调色
        // float2 velDir = fluidTex.zy;
        // // //角度_VelocityMap.xy
        // float hue = remap(atan2(velDir.y, velDir.x), -3.1415926, 3.1415926, 0, 1);
        // float v = length(velDir);
        // float lightness = atan(15 * (v - 0.05));
        // float saturation = saturate(v * v + 0.3);
        // float3 colorFluid = HsvToRgb(float3(hue, saturation * 2, lightness));
        // // color.xyz = colorFluid+waveTex+trailTex+patternTex;
        float col = length(fluidTex.xy) + waveTex + trailTex + patternTex;
        color.xyz = 1-col;


        return float4(color);
    }
    ENDHLSL


    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Example"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}