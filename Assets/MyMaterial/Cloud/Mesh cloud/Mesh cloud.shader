Shader "Custom/MeshCloud"
{
    Properties
    {
        _3Dnoise("3Dnoise_XYZmove_Wscale", 3D) = "white" {}
        _3DnoiseDetail("3DnoiseDetail_XYZmove_Wscale", 3D) = "white" {}
        _BaseMap ("BaseMap", 2D) = "white" {}
        _MooveSpeed("MooveSpeed",Range(0, 64))=1
        _ColorDark("ColorDark",Color)=(0,0,0,1)
        [HDR] _ColorLight("ColorLight",Color)=(1,1,1,1)
        _MaxDistance("MaxDistance",Range(0, 500))=500

        _TessellationFactor("_TessellationFactor",Range(0, 64))=1
        _BumpShape("_BumpShape",Float)=5
        _BumpDetail("_BumpDetail",Float)=5

    }


    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    TEXTURE3D(_3Dnoise);
    SAMPLER(sampler_3Dnoise);
    TEXTURE3D(_3DnoiseDetail);
    SAMPLER(sampler_3DnoiseDetail);
    TEXTURE2D(_ScreenDepth);
    SAMPLER(sampler_ScreenDepth);

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float _TessellationFactor;
        float _BumpShape;
        float _BumpDetail;
        float _MooveSpeed;
        float4 _3Dnoise_ST;
        float4 _3DnoiseDetail_ST;
        float4 _ColorDark;
        float4 _ColorLight;
        float _MaxDistance;
    CBUFFER_END

    struct Attributes {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
    };

    struct TessellationInput {
        float4 positionOS : INTERNALTESSPOS;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
    };

    struct HullOutput {
        float3 positionOS : TEXCOORD0;
        float2 uv : TEXCOORD1;
        float3 normal : NORMAL;
        float3 positionOS0 : TEXCOORD2;
        float3 positionOS1 : TEXCOORD3;
        float3 normalOS0 : TEXCOORD4;
    };

    struct TessellationFactor {
        float edge[3] : SV_TessFactor;
        float inside : SV_InsideTessFactor;
    };

    struct TessellationOutput {
        float3 positionOS : TEXCOORD0;
        float2 uv : TEXCOORD1;
        float3 normal : NORMAL;
    };


    struct Varyings {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD1;
        float4 positionNDC : TEXCOORD2;
        float2 uv : TEXCOORD0;
        float3 normal : NORMAL;
        float4 color : COLOR;
    };


    //顶点着色器
    TessellationInput vert(Attributes v)
    {
        TessellationInput p;
        p.positionOS = v.positionOS;
        p.uv = v.uv;
        p.normal = v.normal;
        return p;
    }

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    //根据距离计算细分因子
    float CalculateTessellationFactorByDistance(float3 posOS)
    {
        float3 camPosWS = GetCameraPositionWS();
        float maxDistance = _MaxDistance;
        float cameraDistance = clamp(distance(camPosWS, TransformObjectToWorld(posOS)), 0, maxDistance);
        float tessellationFactor = Remap(cameraDistance * cameraDistance, 0, maxDistance * maxDistance, _TessellationFactor, 1);
        return tessellationFactor;
    }

    TessellationFactor PCF(InputPatch<TessellationInput, 3> patch)
    {
        TessellationFactor f;

        //这里控制不同边和内部的细分次数
        float facotr0 = CalculateTessellationFactorByDistance(patch[0].positionOS);
        f.edge[0] = facotr0;
        float facotr1 = CalculateTessellationFactorByDistance(patch[1].positionOS);
        f.edge[1] = facotr1;
        float facotr2 = CalculateTessellationFactorByDistance(patch[2].positionOS);
        f.edge[2] = facotr2;
        float factorIn = (facotr0 + facotr1 + facotr2) / 3;
        f.inside = factorIn;
        return f;
    }


    [domain("tri")]                 //确定图元：quad,triangle，isoline（等值线）
    [partitioning("integer")]       //曲面细分的模式：integer,pow2,fractional_even,fractional_odd
    [outputtopology("triangle_cw")] //创建三角形绕序：triangle_cw,triangle_ccw,line（对线段细分）
    [outputcontrolpoints(3)]        //输出控制点的数量
    [patchconstantfunc("PCF")]      //PCF函数，用来计算细分因子
    [maxtessfactor(64.0)]           //最大细分因子
    HullOutput hull(InputPatch<TessellationInput, 3> input, uint controlPointId : SV_OutputControlPointID, uint patchId : SV_PrimitiveID)
    {
        HullOutput output;

        output.positionOS = input[controlPointId].positionOS.xyz;
        output.uv = input[controlPointId].uv;
        output.normal = input[controlPointId].normal;

        return output;
    }

    [domain("tri")] //声明域是一个三角形
    //传入factor，顶点数据，新顶点的重心坐标
    TessellationOutput domain(TessellationFactor patchTess, float3 bary: SV_DomainLocation, const OutputPatch<HullOutput, 3> patch)
    {
        TessellationOutput output;

        //其他属性（如多个uv,normal,tangent）也是一样的操作
        float3 posOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
        float2 uv = patch[0].uv * bary.x + patch[1].uv * bary.y + patch[2].uv * bary.z;
        float3 normal = patch[0].normal * bary.x + patch[1].normal * bary.y + patch[2].normal * bary.z;

        float3 samplePosShape = posOS * _3Dnoise_ST.w + _3Dnoise_ST.xyz * _Time.x * _MooveSpeed * 0.05;
        float3 samplePosDetail = posOS * _3DnoiseDetail_ST.w * 10 + _3DnoiseDetail_ST.xyz * _Time.x * _MooveSpeed * 0.1;
        float noise3DShape = SAMPLE_TEXTURE3D_LOD(_3Dnoise, sampler_3Dnoise, samplePosShape, 0).r;
        float noise3DDetail = SAMPLE_TEXTURE3D_LOD(_3DnoiseDetail, sampler_3DnoiseDetail, samplePosDetail, 0).r;

        output.positionOS = posOS + normal *  (noise3DShape * 0.5 *_BumpShape + noise3DDetail * 0.1 * _BumpDetail);
        output.uv = uv;
        output.normal = normal;
        return output;
    }

    [maxvertexcount(3)]
    void geo(triangle TessellationOutput IN[3], inout TriangleStream<Varyings> triStream)
    {
        Varyings o;
        float3 dir1 = IN[1].positionOS - IN[0].positionOS;
        float3 dir2 = IN[2].positionOS - IN[0].positionOS;
        float3 normal = normalize(cross(dir1, dir2));

        VertexPositionInputs v0 = GetVertexPositionInputs(IN[0].positionOS);
        o.positionCS = v0.positionCS;
        o.positionWS = v0.positionWS;
        o.positionNDC = v0.positionNDC;
        o.uv = IN[0].uv;
        o.normal = TransformObjectToWorldNormal(IN[0].normal);
        o.color = float4(1, 0, 0, 0);
        triStream.Append(o);

        VertexPositionInputs v1 = GetVertexPositionInputs(IN[1].positionOS);
        o.positionCS = v1.positionCS;
        o.positionWS = v1.positionWS;
        o.positionNDC = v1.positionNDC;
        o.uv = IN[1].uv;
        o.normal = TransformObjectToWorldNormal(IN[1].normal);
        o.color = float4(0, 1, 0, 0);
        triStream.Append(o);


        VertexPositionInputs v2 = GetVertexPositionInputs(IN[2].positionOS);
        o.positionCS = v2.positionCS;
        o.positionWS = v2.positionWS;
        o.positionNDC = v2.positionNDC;


        o.uv = IN[2].uv;
        o.normal = TransformObjectToWorldNormal(IN[2].normal);
        o.color = float4(0, 0, 1, 0);
        triStream.Append(o);
    }

    float3 GetPixelWorldPosition(float2 uv)
    {
        //获得深度数据
        half4 blitDepth = SAMPLE_TEXTURE2D_X(_ScreenDepth, sampler_ScreenDepth, uv); //采样blitter纹理
        //重建世界坐标深度
        float depthValue = Linear01Depth(blitDepth, _ZBufferParams);
        //重建世界坐标
        //NDC反透视除法
        float3 farPosCS = float3(uv.x * 2 - 1, uv.y * 2 - 1, 1) * _ProjectionParams.z;
        //反投影
        float3 farPosVS = mul(unity_CameraInvProjection, farPosCS.xyzz).xyz;
        //获得裁切空间坐标
        float3 posVS = farPosVS * depthValue;
        //转化为世界坐标   
        float3 posWS = TransformViewToWorld(posVS);
        return posWS;
    }

    half4 frag(Varyings IN) : SV_Target
    {
        //线框
        float color1 = IN.color.x;
        color1 = step(color1 / fwidth(color1), 0.5);
        float color2 = IN.color.y;
        color2 = step(color2 / fwidth(color2), 0.5);
        float color3 = IN.color.z;
        color3 = step(color3 / fwidth(color3), 0.5);
        float outline = max(max(color1, color2), color3);

        float3 normalWS = IN.normal;
        Light mainLight = GetMainLight();
        float3 lightDirWS = mainLight.direction;

        //lambert
        float3 lambert = max(dot(normalWS, lightDirWS), 0);
        lambert = lambert * lambert;

        //light scatter：光向边缘亮
        float3 backLightDirWS = IN.normal * 0.7 + lightDirWS;
        float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
        float3 scatterCol = saturate(dot(viewDirWS, -backLightDirWS));
        scatterCol = saturate(scatterCol * scatterCol * scatterCol * 3);

        //视角方向中心亮
        half NdotV = max(0, dot(IN.normal, viewDirWS));
        half smoothNdotV = NdotV * NdotV * NdotV * NdotV;


        half finalLit = saturate(smoothNdotV * 0.2 + saturate(lambert + scatterCol) * (1 - NdotV * 0.12));

        // finalLit=smoothNdotV*0.5+scatterCol+lambert;

        float3 cloudCol = lerp(_ColorDark, _ColorLight, finalLit);

        float2 screenUV = IN.positionNDC.xy / IN.positionNDC.w;
        float3 screenPosWS = GetPixelWorldPosition(screenUV);
        float distanceToScreenPos=distance(screenPosWS,IN.positionWS);

        float transParentToObject=saturate(Remap(distanceToScreenPos,1,5,0,1));
        float4 color = 1;
        color.a=transParentToObject;
        color.xyz = cloudCol;
        color.xyz = lerp(color, color*0.5 , outline);

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
            Blend SrcAlpha OneMinusSrcAlpha 

            Name "TessellationCloud"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment frag
            ENDHLSL
        }
    }
}