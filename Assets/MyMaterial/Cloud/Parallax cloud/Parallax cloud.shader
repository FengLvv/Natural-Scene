Shader "Custom/ParallaxCloud"
{
    Properties
    {
        _BaseMap ("Example Texture", 2D) = "white" {}
        _XY_CloudMoveDir ("Cloud Move Direction", Vector) = (0, 1,0,0)
        _Layer ("Layer number", Range(10,30)) = 10
        _Alpha ("Alpha", Range(0,1)) = 0.5
        _Color ("Color", Color) = (1,1,1,1)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    TEXTURE2D(_BaseMap);
    SAMPLER(sampler_BaseMap);
    CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseColor;
        float4 _XY_CloudMoveDir;
        int _Layer;
        float _Alpha;
        float4 _Color;
    CBUFFER_END

    struct Attributes {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float2 uv : TEXCOORD0;
    };

    struct Varyings {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD1;
        float3 tangentWS : TEXCOORD2;
        float3 bitangentWS : TEXCOORD3;
        float3 normalWS : TEXCOORD4;
        float2 uv : TEXCOORD0;
    };

    Varyings vert(Attributes IN)
    {
        Varyings OUT;
        VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
        OUT.positionCS = positionInputs.positionCS;
        OUT.positionWS = positionInputs.positionWS;
        VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
        OUT.normalWS = normalInputs.normalWS;
        OUT.tangentWS = normalInputs.tangentWS;
        OUT.bitangentWS = normalInputs.bitangentWS;
        OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
        return OUT;
    }

    half4 frag(Varyings IN) : SV_Target
    {
        int sampleTimes = min(_Layer, 30);
        Light light=GetMainLight();
        half3 lightColor=light.color;

        //TBN space view dir
        float3x3 MatWS2TS = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
        float3 viewDirWS = GetWorldSpaceViewDir(IN.positionWS);
        float3 viewDirTS = mul(MatWS2TS, -viewDirWS);                    //让光线朝下，z为负
        float3 viewPerMarchTS = viewDirTS / (viewDirTS.z * sampleTimes); //除以z因为最多向下步进深度为1（贴图的深度）

        float2 uvStill = IN.uv;
        //用来采样步进的uv，z储存步进的深度
        float3 uvMove = 0;
        uvMove.xy = IN.uv - _Time.x * _XY_CloudMoveDir.xy;
        uvMove.z = 1; //初始高度在1

        //每一帧采样一个静止的噪声，一个移动的噪声，乘出新的噪声
        float stillCloud = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvStill).r;
        // stillCloud=1;

        float moveCloud = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMove).r * stillCloud;


        float previousCloud = moveCloud;


        for (int i = 1; i < sampleTimes; i++)
        {
            uvMove += viewPerMarchTS; //高度下降，xy步进
            moveCloud += SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMove.xy).r * stillCloud;
            if (moveCloud > uvMove.z)
            {
                break;
            }
            previousCloud = moveCloud;
        }
        //插值出最终的位置
        float3 p1z = uvMove.z - viewPerMarchTS.z; //上一次步进的深度
        float3 p2z = uvMove.z;                    //这一次步进的深度
        float s1 = previousCloud;                 //上一次采样的深度
        float s2 = moveCloud;                     //这一次采样的深度         
        float3 finalPos = lerp(uvMove - viewPerMarchTS, uvMove, (p1z - s1) / (p1z - s1 + s2 - p2z));

        //采样最终的贴图
        half baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, finalPos.xy).r * stillCloud;; 

        half rangeClt = baseMap.r + _Alpha * 0.75;
        half Alpha = abs(smoothstep(rangeClt, _Alpha, 1.0));
        Alpha = Alpha * Alpha * Alpha * Alpha * Alpha;

        return half4(baseMap * _Color.rgb * lightColor, Alpha);
 
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType" = "Transparent"
        }
        Pass
        {
            Name "Parallax Cloud"
            //            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off
            Tags
            {
                "Queue"="Transparent"
                "RenderType" = "Transparent"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}