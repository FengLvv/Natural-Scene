Shader "URP/Caustic"
{
    Properties //着色器的输入 
    {
        _BaseMap ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

    CBUFFER_START(UnityPerMaterial)
        //声明变量
        float4 _BaseMap_ST;
    CBUFFER_END

    TEXTURE2D(_BaseMap); //贴图采样  
    SAMPLER(sampler_BaseMap);

    TEXTURE2D(_CameraDepthTexture); //深度贴图采样
    SAMPLER(sampler_CameraDepthTexture);

    struct a2v //顶点着色器
    {
        float4 positionOS: POSITION;
        float3 normalOS: TANGENT;
    };

    struct v2f //片元着色器
    {
        float4 positionCS: SV_POSITION;
        float4 positionNDC: TEXCOORD1;
    };

    v2f vert(a2v v)
    {
        v2f o;
        VertexPositionInputs vert = GetVertexPositionInputs(v.positionOS);
        o.positionNDC = vert.positionNDC;
        o.positionCS = vert.positionCS;
        return o;
    }

    half4 frag(v2f i) : SV_Target /* 注意在HLSL中，fixed4类型变成了half4类型*/
    {
        //rebuild world space position
        float2 screenUV = i.positionNDC.xy / i.positionNDC.w;
        float depthValue = Linear01Depth(SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV), _ZBufferParams);
        float2 posNDC = screenUV * 2 - 1;
        float3 posCSFar = float3(posNDC, 1) * _ProjectionParams.z;
        //反投影
        float3 farPosVS = mul(unity_CameraInvProjection, posCSFar.xyzz).xyz;
        //获得裁切空间坐标
        float3 posVS = farPosVS * depthValue;
        //转化为世界坐标   
        float3 posWS = TransformViewToWorld(posVS);

        float2 causticUV = posWS.xz * _BaseMap_ST.xy + _BaseMap_ST.zw;
        half caustic = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, causticUV).z;
        return caustic;
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode"="Caustic"
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}