Shader "Unlit/Volume Sample"
{
    Properties
    {
        [MainTexture]_BaseMap ("Texture", 3D) = "white" {}
        _MaxStepsCam("Max cam Steps", Range(1, 100)) = 10
        _StepSizeCam("Step Size Cam", Range(0.001, 0.1)) = 0.01
        _MaxStepsLight("Max light Steps", Range(1, 100)) = 10
        _StepSizeLight("Step Size Light", Range(0.001, 0.1)) = 0.01

        _LightAbsorb("Light Absorb", Range(0, 5)) = 2.02
        _DensityScale("Density Scale", Range(0, 1)) = 1
        _Offset("Offset", Vector) = (0,0,0,0)
        _Darkness("Darkness", Range(0, 1)) = 0.15
        _Transmittance ("Transmittance", Range(0, 1)) = 1
        _Color("Color", Color) = (1,1,1,1)
        _ShadowColor("Shadow Color", Color) = (0,0,0,1)
    }
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    struct Attributes {
        float4 posOS : POSITION;
        float3 texcoord : TEXCOORD0;
    };

    struct Varyings {
        float4 posCS : SV_POSITION;
        float3 posOS : TEXCOORD0;
    };

    TEXTURE3D(_BaseMap);
    SAMPLER(sampler_BaseMap); //多个纹理可以用同一个sampler

    CBUFFER_START(UnityPerMaterial)
        float _MaxStepsCam;
        float _StepSizeCam;
        float _MaxStepsLight;
        float _StepSizeLight;
        float _LightAbsorb;
        float _DensityScale;
        float3 _Offset;
        float _Darkness;
        float _Transmittance;
        float4 _Color;
        float4 _ShadowColor;
    CBUFFER_END

    Varyings vert(Attributes v)
    {
        Varyings o;
        VertexPositionInputs vIn = GetVertexPositionInputs(v.posOS);
        o.posCS = vIn.positionCS;
        o.posOS = v.posOS;
        return o;
    }
    half4 frag(Varyings i) : SV_Target
    {
        float transmittance = _Transmittance;
        float3 marchPosOS = i.posOS;
        float3 marDirOS = -GetObjectSpaceNormalizeViewDir(i.posOS);
        float3 lightDirOS = TransformWorldToObjectDir(GetMainLight().direction);

        float camDensity = 0;
        float transmission = 0;
        float lightAccumulation = 0; //记录整个camMarch的光向密度累计
        float finalLight = 0;

        for (int a = 0; a < _MaxStepsCam; a++)
        {
            //采样camMarch
            marchPosOS += (marDirOS * _StepSizeCam);
            float3 samplePos = marchPosOS + _Offset; //整体偏移
            float sampleDensity = SAMPLE_TEXTURE3D(_BaseMap, sampler_BaseMap, samplePos).r;
            camDensity += sampleDensity * _DensityScale;

            //采样灯光,把camMarch一路上的灯光量全加到一起lightDensity，最后处理            
            float3 lightSamplePoint = samplePos;
            //向光源采样，得到路径的密度累计
            for (int l = 0; l < _MaxStepsLight; l++)
            {
                lightSamplePoint += lightDirOS * _StepSizeLight;
                float lightDensity = SAMPLE_TEXTURE3D(_BaseMap, sampler_BaseMap, lightSamplePoint).r;
                lightAccumulation += lightDensity;
            }
            //光到light采样点的吸收项：沿着光线方向接受的灯光比例
            float lightTransmission = exp(-lightAccumulation);
            //向上lerp，提高暗部
            float shadow = lerp(lightTransmission, 1, _Darkness);
            //加上的灯光强度受到当前点的density影响，密度越大，灯光影响越小
            //density用来平衡灯光和camMarch的影响，不然灯光直射的地方会过亮
            finalLight += shadow * transmittance * camDensity;
            //灯光由camMarch位置到达相机的吸收率
            transmittance *= exp(-camDensity * _LightAbsorb);
            transmission = exp(-camDensity); //由总密度计算的透明度
        }

        float4 color = lerp(_ShadowColor, _Color, finalLight);
        color.a = 1 - transmission;
        return color;
    }
    ENDHLSL
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            ENDHLSL
        }
    }
}