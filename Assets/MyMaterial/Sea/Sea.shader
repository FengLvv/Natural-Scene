Shader "Custom/CartoonSea"
{
    Properties
    {
        [Header(BigWaveMove)]
        _WaveMovePara("BigWaveMove dir:xy,Frequency:z,Speed:w",Vector)=(0.1,0.1,0.1,0.1)
        _WaveHeight("WaveHeight",Range(0,1))=0.5
        _FarEnhance("FarEnhance",Range(1,5))=1

        [Header(Shore)]
        _ShoreDistance("ShoreDistance",Range(0,0.5))=0.06
        _ShoreColor("ShoreColor",Color)=(1,1,1,1)
        _WaterColor("WaterColor",Color)=(1,1,1,1)

        [Header(Wave)]
        _WaveNormalMap("WaveNormalMap",2D)= "white" {}
        _WaveSpeed("WaveSpeed dir1:xy/size1:z/size2:w",Vector)=(0.1,0.1,0.1,0.1)
        _WrapDepthNoiseIntense("Wrap Depth Noise Intense",Range(0,5))=1

        [Header(Foam)]
        _FoamMoveSpeed("FoamMoveSpeed",Float)=1
        _FoamGradient("FoamGradient",Range(0,1))=1
        _FoamClamp("FoamClamp",Range(0,1))=1
        _FoamColor("FoamColor",Color)=(1,1,1,1)
        _FoamNoise("FoamNoisePerlin",2D)= "white" {}

        [Header(PBR)]
        _ShapeOfGGX("ShapeOfGGX",Range(1,3))=1.2
        _DiffusionLightness("DiffusionLightness",Range(0,1))=0.7
        _LightEnhance("LightEnhance",Range(0,5))=1

        [Header(Tesellation)]
        _MaxTessellationDistance( "MaxTessellationDistance",Float)=5
        _MaxTessellation( "MaxTessellation",Float)=5

    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Assets/MyMaterial/Repo/PBR.hlsl"

    TEXTURE2D_X_FLOAT(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);
    TEXTURE2D_X_FLOAT(_CameraOpaqueTexture);
    SAMPLER(sampler_CameraOpaqueTexture);
    TEXTURE2D(_WaveNormalMap);
    SAMPLER(sampler_WaveNormalMap);
    TEXTURE2D(_FoamNoise);
    SAMPLER(sampler_FoamNoise);

    CBUFFER_START(UnityPerMaterial)
        float4 _WaveMovePara;
        float _WaveHeight;
        float _FarEnhance;



        float _ShoreDistance;
        float4 _ShoreColor;
        float4 _WaterColor;


        float4 _WaveNormalMap_ST;
        float4 _WaveSpeed;

        float _WrapDepthNoiseIntense;

        float4 _FoamColor;
        float _FoamGradient;
        float _FoamMoveSpeed;
        float _FoamClamp;
        float4 _FoamNoise_ST;

        float _ShapeOfGGX;
        float _DiffusionLightness;

        float _LightEnhance;

        float _MaxTessellationDistance;
        float _MaxTessellation;

    CBUFFER_END

    struct Attributes {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
        float4 tangent :TANGENT;
    };

    struct Varyings {
        float4 positionCS : SV_POSITION;
        float4 positionNDC : TEXCOORD1;
        float3 positionVS : TEXCOORD2;
        float3 positionWS : TEXCOORD6;
        float2 uv : TEXCOORD0;
        float3 normalWS : TEXCOORD3;
        float3 tangentWS : TEXCOORD4;
        float3 bitangentWS : TEXCOORD5;
    };

    half4 sampleWithST(Texture2D texture2d, SamplerState texture2d_sampler, float2 oriUV, float4 st)
    {
        float2 uvst = oriUV * st.xy + st.zw;
        return SAMPLE_TEXTURE2D(texture2d, texture2d_sampler, uvst);
    }

    struct TessellationInput {
        float4 vertex : INTERNALTESSPOS;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
        float4 tangent :TANGENT;
    };

    struct HullOutput {
        float3 positionOS : TEXCOORD0;
        float2 uv : TEXCOORD1;
        float3 normal : NORMAL;
        float4 tangent :TANGENT;
    };

    struct TessellationFactor {
        float edge[3] : SV_TessFactor;
        float inside : SV_InsideTessFactor;
    };

    //顶点着色器
    TessellationInput vert(Attributes v)
    {
        TessellationInput p;
        p.vertex = v.positionOS;
        p.uv = v.uv;
        p.normal = v.normal;
        p.tangent = v.tangent;
        return p;
    }

    // 自定义Remap函数
    float Remap(float value, float inputMin, float inputMax, float outputMin, float outputMax)
    {
        return outputMin + (value - inputMin) * (outputMax - outputMin) / (inputMax - inputMin);
    }


    TessellationFactor PCF(InputPatch<TessellationInput, 3> patch)
    {
        TessellationFactor f;
        float centerFactor = 0;
        float3 camPosWS = GetCameraPositionWS();

        float3 posWS0 = TransformObjectToWorld(patch[0].vertex);
        float distanceToCam0 = distance(camPosWS, posWS0);
        float tessellation0 = max(Remap(distanceToCam0, 0, _MaxTessellationDistance, _MaxTessellation, 1), 1);
        //这里控制不同边和内部的细分次数
        f.edge[0] = tessellation0;

        float3 posWS1 = TransformObjectToWorld(patch[1].vertex);
        float distanceToCam1 = distance(camPosWS, posWS1);
        float tessellation1 = max(Remap(distanceToCam1, 0, _MaxTessellationDistance, _MaxTessellation, 1), 1);
        //这里控制不同边和内部的细分次数
        f.edge[1] = tessellation1;

        float3 posWS2 = TransformObjectToWorld(patch[2].vertex);
        float distanceToCam2 = distance(camPosWS, posWS2);
        float tessellation2 = max(Remap(distanceToCam2, 0, _MaxTessellationDistance, _MaxTessellation, 1), 1);
        //这里控制不同边和内部的细分次数
        f.edge[2] = tessellation2;

        f.inside = (tessellation0 + tessellation1 + tessellation2) / 3;

        return f;
    }


    [domain("tri")]                   //确定图元：quad,triangle，isoline（等值线）
    [partitioning("fractional_even")] //曲面细分的模式：integer,pow2,fractional_even,fractional_odd
    [outputtopology("triangle_cw")]   //创建三角形绕序：triangle_cw,triangle_ccw,line（对线段细分）
    [outputcontrolpoints(3)]          //输出控制点的数量
    [patchconstantfunc("PCF")]        //PCF函数，用来计算细分因子
    [maxtessfactor(32.0)]             //最大细分因子
    HullOutput hull(InputPatch<TessellationInput, 3> input, uint controlPointId : SV_OutputControlPointID, uint patchId : SV_PrimitiveID)
    {
        HullOutput output;

        output.positionOS = input[controlPointId].vertex.xyz;
        output.uv = input[controlPointId].uv;
        output.normal = input[controlPointId].normal;
        output.tangent = input[controlPointId].tangent;
        return output;
    }


    [domain("tri")] //声明域是一个三角形
    //传入factor，顶点数据，新顶点的重心坐标
    Varyings domain(TessellationFactor patchTess, float3 bary: SV_DomainLocation, const OutputPatch<HullOutput, 3> patch)
    {
        Varyings output;

        //定义处理插值的Macro，用来计算插值
        #define DOMAIN_INTERPOLATE(fieldName) \
		patch[0].fieldName * bary.x + \
		patch[1].fieldName * bary.y + \
		patch[2].fieldName * bary.z;

        //其他属性（如多个uv,normal,tangent）也是一样的操作
        float3 posOS = DOMAIN_INTERPOLATE(positionOS);
        float2 uv = DOMAIN_INTERPOLATE(uv);
        float3 normalOS = DOMAIN_INTERPOLATE(normal);
        float4 tangentOS = DOMAIN_INTERPOLATE(tangent);


        //big wave
        float4 positionCS = mul(UNITY_MATRIX_MVP, posOS);
        //从NDC空间下到上画一个渐变(1->_FarEnhance)
        float farEnhance = lerp(_FarEnhance, 1, positionCS.y / positionCS.w * 0.5 + 0.5);
        float waveMoveIntensity = sin(dot(posOS.xy, normalize(_WaveMovePara.xy)) * 1000 * _WaveMovePara.z + _WaveMovePara.w * _Time.y);
        //裁切空间的y让远处更明显
        posOS.z += waveMoveIntensity * _WaveHeight * 0.01 * farEnhance;


        //转化到裁切空间，输出
        VertexPositionInputs positionInputs = GetVertexPositionInputs(posOS);
        output.positionCS = positionInputs.positionCS;
        output.positionNDC = positionInputs.positionNDC;
        output.positionVS = positionInputs.positionVS;
        output.positionWS = positionInputs.positionWS;
        output.uv = uv;


        VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS);
        output.normalWS = normalInputs.normalWS;
        output.tangentWS = normalInputs.tangentWS;
        output.bitangentWS = normalInputs.bitangentWS;

        return output;
    }

    half4 frag(Varyings IN) : SV_Target
    {
        //MatWS2TS
        float3x3 MatWS2TS = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
        float2 waveMoveDir = normalize(_WaveSpeed.xy);
        float2 uvNormal1 = IN.uv + waveMoveDir * _WaveSpeed.z * _Time.y;
        float2 uvNormal2 = IN.uv + waveMoveDir * _WaveSpeed.w * _Time.y * -1;

        //采样法线贴图
        half3 waveNormal1TS = UnpackNormal(sampleWithST(_WaveNormalMap, sampler_WaveNormalMap, uvNormal1, _WaveNormalMap_ST));
        half3 waveNormal2TS = UnpackNormal(sampleWithST(_WaveNormalMap, sampler_WaveNormalMap, uvNormal2, _WaveNormalMap_ST));
        //切线空间的法线主要用来wrap uv，因为xy的随机性比较大
        half3 waveNormalTS = normalize(lerp(waveNormal1TS, waveNormal2TS, 0.5));
        //转化到世界坐标
        half3 waveNormalWS = mul(waveNormalTS, MatWS2TS);

        //水面的深度
        float depthWater = -IN.positionVS.z;
        //采样静止的深度图        
        float2 screenUV = IN.positionNDC.xy / IN.positionNDC.w;
        half4 blitDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, screenUV);
        float screenDepthValue = LinearEyeDepth(blitDepth, _ZBufferParams);
        //这里还没有处理物体比水面高的情况
        float depthDiff = (screenDepthValue - depthWater) / _ProjectionParams.z;

        //处理静止深度图，让低于深度的部分值为0，作为扰动的蒙版
        float depthWrap = depthDiff < 0 ? 1 : depthDiff;
        depthWrap = smoothstep(0, 0.1, depthWrap);
        depthWrap = depthWrap > 0.5 ? 0 : depthWrap;
        float2 screenUVWrap = screenUV + normalize(waveNormalTS.xy) * _WrapDepthNoiseIntense * 0.1 * depthWrap;
        half4 blitDepthWrap = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, screenUVWrap);
        float screenDepthValueWrap = LinearEyeDepth(blitDepthWrap, _ZBufferParams);
        //因为负数的地方没有扰动，所以被完全挡住，不用处理
        float depthDiffWrap = (screenDepthValueWrap - depthWater) / _ProjectionParams.z;

        //shore:
        float shoreGradient = smoothstep(0, _ShoreDistance * 0.1, depthDiffWrap);
        half4 waterBaseColor = lerp(_ShoreColor, _WaterColor, shoreGradient);
        float transparency = waterBaseColor.a;

        //折射
        half4 refractionSampleBG = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_PointClamp, screenUVWrap);
        float4 refractionColor = lerp(refractionSampleBG, waterBaseColor, transparency);

        //Foam        
        float depthFoam = saturate(depthDiff);
        float foamGradient = smoothstep(0, _FoamGradient * 0.1, depthFoam);
        //找到摄像机的前方的物体空间坐标
        //todo:这里和物体坐标以及uv转换有关，不同模型需要手动校准
        float3 objSpaceFar = TransformViewToWorldDir(TransformWorldToObjectDir(float3(0, 0, 1)));
        //删除y轴
        float2 uvFarDir = normalize(objSpaceFar.xz);
        //uv+法线扰动+摄像机前方的物体空间坐标扰动（泡沫往远处飘）
        float2 uvForm = IN.uv + waveNormalTS * _SinTime * _FoamMoveSpeed * 0.1 - uvFarDir * _FoamMoveSpeed * 0.1 * _Time;
        float foamNoise = sampleWithST(_FoamNoise, sampler_FoamNoise, uvForm, _FoamNoise_ST).r;
        float foamMask = step(foamNoise, foamGradient);
        //用深度裁切远端的泡沫，让边缘整齐
        foamMask = lerp(1, foamMask, step(depthDiff, _FoamClamp * 0.05));
        float4 foamColor = lerp(_FoamColor, refractionColor, foamMask);

        //pbr
        Light light = GetMainLight();
        float3 lightDirWS = light.direction;
        float3 lightColor = light.color * _LightEnhance;
        float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

        half3 GGXHighLight = CalculateBRDF(normalize(waveNormalWS * float3(1, _ShapeOfGGX, 1)), lightDirWS, viewDirWS, 0.05, waterBaseColor, 0);
        GGXHighLight = smoothstep(0.8, 2, GGXHighLight);
        half3 btdf = lerp((BTDFDisney(waveNormalWS, viewDirWS, lightDirWS, 6)), 1, _DiffusionLightness);

        half3 colorWithBTDF = foamColor * btdf * 2;
        half3 colorWithGGX = lerp(colorWithBTDF, lightColor, GGXHighLight);

        float4 color = 0;

        color.rgb = colorWithGGX;

        color.a = transparency;
        return color;
    }
    ENDHLSL


    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
        }

        Pass
        {
            Name "Example"
            Cull Off
            ZWrite Off

            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma domain  domain
            #pragma hull  hull

            #pragma fragment frag
            ENDHLSL
        }
    }
}