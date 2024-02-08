Shader "Unlit/PP Volume Sample"
{
    Properties {}
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    struct Attributes1 {
        float4 posOS : POSITION;
        uint vertexID : SV_VertexID;
    };

    struct Varyings1 {
        float4 posCS : SV_POSITION;
        float3 posOS : TEXCOORD0;
        float2 uv : TEXCOORD1;
    };

    CBUFFER_START(UnityPerMaterial)
    CBUFFER_END

    float3 _BoxMin;
    float3 _BoxMax;
    float _RayStepLength;
    int _RaySteps;
    float _SkymaskNoiseScale;
    float _DensityNoiseScale;
    float2 _SkymaskNoiseBias;
    float2 _DensityNoiseBias;
    float _LightAbsorptionTowardSun;
    float3 _CloudMidColor;
    float3 _CloudDarkColor;
    float4 _DensityNoiseWeight;
    float _ColorOffset1;
    float _ColorOffset2;
    float _DarknessThreshold;
    float _CloudDensityScale;
    float _DetailNoiseScale;
    float _DetailNoiseWeight;
    float _HG;
    float _CloudDensityAdd;
    float4 _MoveSpeedSky;

    float _MoveSpeedShape;
    float _MoveSpeedDetail;

    float _LightEnergyScale;

    TEXTURE2D(_CameraDepthTexture);


    TEXTURE2D(_CamColorTexture);
    TEXTURE2D(_NoiseTexture2D1);
    TEXTURE2D(_NoiseTexture2D2);
    TEXTURE3D(_NoiseTexture3D1);
    TEXTURE3D(_NoiseTexture3D2);

    SAMPLER(sampler_CameraDepthTexture);
    SAMPLER(sampler_CamColorTexture);

    SAMPLER(sampler_NoiseTexture2D1);
    SAMPLER(sampler_NoiseTexture2D2);
    SAMPLER(sampler_NoiseTexture3D1);
    SAMPLER(sampler_NoiseTexture3D2);



    Varyings1 vert(Attributes1 v)
    {
        Varyings1 o;
        #if SHADER_API_GLES
    float4 pos = input.positionOS;
    float2 uv  = input.uv;
        #else
        float4 pos = GetFullScreenTriangleVertexPosition(v.vertexID);
        float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
        #endif
        o.uv = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
        VertexPositionInputs vIn = GetVertexPositionInputs(v.posOS);
        o.posCS = pos;
        o.posOS = v.posOS;
        return o;
    }

    float remap(float original_value, float original_min, float original_max, float new_min, float new_max)
    {
        return new_min + (((original_value - original_min) / (original_max - original_min)) * (new_max - new_min));
    }

    float3 GetPixelWorldPosition(float2 uv)
    {
        //获得深度数据
        half4 blitDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv); //采样blitter纹理
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

    float2 RayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 Raydir)
    {
        float3 invRaydir = 1.0f / Raydir;
        float3 t0 = (boundsMin - rayOrigin) * invRaydir;
        float3 t1 = (boundsMax - rayOrigin) * invRaydir;
        float3 tmin = min(t0, t1);
        float3 tmax = max(t0, t1);

        float dstA = max(max(tmin.x, tmin.y), tmin.z); //进入点
        float dstB = min(tmax.x, min(tmax.y, tmax.z)); //出去点

        float dstToBox = max(0, dstA);
        float dstInsideBox = max(0, dstB - dstA);
        return float2(dstToBox, dstInsideBox);
    }

    //调整3d噪波
    float3 ScaledDensitySamplePos(float3 samplePos)
    {
        //用来缩放噪音的采样位置，采样贴图用这个坐标
        float3 biasSamplePos;
        biasSamplePos.xz = (samplePos.xz + _DensityNoiseBias.xy) * _DensityNoiseScale;
        biasSamplePos.y = samplePos.y;
        return biasSamplePos;
    }

    //调整3d噪波
    float3 ScaledDensityDetailSamplePos(float3 samplePos)
    {
        //用来缩放噪音的采样位置，采样贴图用这个坐标
        float3 biasSamplePos;
        biasSamplePos.xz = samplePos.xz * _DetailNoiseScale;
        biasSamplePos.y = samplePos.y;
        return biasSamplePos;
    }

    //调整天空噪波
    float2 ScaledSkymaskSampleUV(float2 uv)
    {
        //用来缩放噪音的采样位置，采样贴图用这个坐标
        float2 biasSamplePos;
        biasSamplePos = (uv + _SkymaskNoiseBias) * _SkymaskNoiseScale;
        return biasSamplePos;
    }

    half SampleNoise3D(float3 rayPos)
    {
        float3 size = _BoxMax - _BoxMin;
        float2 uv = (rayPos.xz - _BoxMin.xz) / (_BoxMax.xz - _BoxMin.xz);
        float speedShape = _Time.x * _MoveSpeedShape;
        float speedDetail = _Time.x * _MoveSpeedDetail;
        //采样偏移贴图（2d噪波2），偏移uv
        float maskNoise = SAMPLE_TEXTURE2D(_NoiseTexture2D2, sampler_NoiseTexture2D2, (uv+_Time.x*normalize(_MoveSpeedSky.xy)*_MoveSpeedSky.z)*0.1*_MoveSpeedSky.w).r * 2 - 1;

        //采样Environment贴图，获得高度遮罩
        float4 weatherMap = SAMPLE_TEXTURE2D(_NoiseTexture2D1, sampler_NoiseTexture2D1, ScaledSkymaskSampleUV(uv+ _MoveSpeedSky.xy * _Time.x));
        weatherMap = smoothstep(0, 0.3, weatherMap);
        float heightPercent = (rayPos.y - _BoxMin.y) / size.y; //计算采样点高度百分比
        //输出云层大轮廓mask,从下网上 0->1->0
        //把高于噪波的部分变成0，低于噪波的部分从下到上映射为1-0     
        float heightGradient1 = saturate(remap(heightPercent, 0, weatherMap.r, 1, 0));
        //从下网上映射为0-1的mask
        float heightGradient2 = saturate(remap(heightPercent, 0.0, weatherMap.r * 0.3 + 0.1, 0, 1));
        float heightGradient = heightGradient1 * heightGradient2;


        //采样形状贴图（3d噪波2），获得密度
        float3 uvwShape = ScaledDensitySamplePos(rayPos) + maskNoise + float3(speedShape, speedShape * 0.2, 0); //向x偏移+噪声偏移
        float4 shapeNoise = SAMPLE_TEXTURE3D(_NoiseTexture3D2, sampler_NoiseTexture3D2, uvwShape);
        //用四个权重把噪波四部分加起来
        float4 normalizedShapeWeights = _DensityNoiseWeight / dot(_DensityNoiseWeight, 1);
        float shapeFBM = dot(shapeNoise, normalizedShapeWeights);
        float baseDensity = shapeFBM + _CloudDensityAdd * 0.01;

        //加上sky mask，做出天空基本形状
        float shapedDensity = baseDensity * heightGradient * 3;

        if (shapedDensity > 0)
        {
            ////在基本密度形状的低密度区域加入细节噪波
            //叠加细节贴图（3d噪波1），获得细节            
            float3 uvwDetail = ScaledDensityDetailSamplePos(rayPos) + maskNoise + float3(speedDetail, speedDetail, 0);
            float4 detailNoise = SAMPLE_TEXTURE3D(_NoiseTexture3D1, sampler_NoiseTexture3D1, uvwDetail);
            //加强细节的对比度
            float detailFBM = pow(detailNoise.r, 5);
            //基本形状的密度反向，找到密度低的位置（接近1）
            float oneMinusShape = 1 - shapedDensity;
            //三次方，把原本密度高的值（接近0）压下去，突出密度低的值
            float detailErodeWeight = oneMinusShape * oneMinusShape * oneMinusShape;
            //细节的密度=基本形状密度-细节密度*原本低密度区域遮罩*细节噪波权重
            float cloudDensity = shapedDensity - detailFBM * detailErodeWeight * _DetailNoiseWeight;
            return saturate(cloudDensity * _CloudDensityScale);
        }

        return shapedDensity;
    }

    //density总密度=\sum局部密度，步长
    float BeerPowder(float density)
    {
        float powder = 1.0 - exp(-density * 2.0);
        float beers = exp(-density);
        float light_energy = 2.0 * beers * powder;
        return beers;
    }

    float HG(float a, float g)
    {
        float g2 = g * g;
        return (1 - g2) / (FOUR_PI * pow(abs(1 + g2 - 2 * g * (a)), 1.5));
    }

    //    float phase(float a) 
    //{
    //  float blend = 0.5;
    //  float hgBlend = hg(a, _phaseParams.x) * (1 - blend) + hg(a, -_phaseParams.y) * blend;
    //  return _phaseParams.z + hgBlend * _phaseParams.w;
    //}
    //    



    half4 frag(Varyings1 i) : SV_Target
    {
        //prepare params
        float3 scenePosWS = GetPixelWorldPosition(i.uv);
        float3 cameraPosWS = GetCameraPositionWS();
        float3 cameraRayDirWS = normalize(scenePosWS - cameraPosWS);
        Light light = GetMainLight();
        float3 dirToLightWS = light.direction;
        float3 lightColor = light.color;

        //Screen color
        float2 shootBox = RayBoxDst(_BoxMin, _BoxMax, cameraPosWS, cameraRayDirWS);
        float disToBox = shootBox.x;
        //入射点
        float3 startPos = cameraPosWS + cameraRayDirWS * disToBox;
        float ditInsideBox = shootBox.y;


        //get march distance
        float disSceneToCamera = distance(cameraPosWS, scenePosWS);
        //if no occulusion, march the whole distance in cloud
        float disCloudMarch = min(disSceneToCamera - disToBox, ditInsideBox);


        //march steps
        float camAttenuation = 1;
        float3 lightEnergy = 0;
        float dstTravelled = 0;
        _RaySteps = min(_RaySteps, 100);
        //当前光线在云中的位置，碰撞和步进用这个坐标
        float3 samplePos = startPos;

        int sampleSteps = min(_RaySteps, 100);
        for (int j = 0; j < sampleSteps; j++)
        {
            if (dstTravelled < disCloudMarch && camAttenuation > 0.01) //被遮住时步进跳过
            {
                samplePos += cameraRayDirWS * _RayStepLength;
                float sampleCamDensity = SampleNoise3D(samplePos).r;
                if (sampleCamDensity > 0)
                {
                    ////sample light
                    float3 lightSamplePos = samplePos;
                    //灯光方向与边界框求交，超出部分不计算
                    float dstInsideBox = RayBoxDst(_BoxMin, _BoxMax, lightSamplePos, dirToLightWS).y;
                    float lightStepLength = dstInsideBox / 5; //十次采样到边界         
                    float lightDensity = 0;
                    for (int step = 0; step < 5; step++) //灯光步进次数
                    {
                        lightSamplePos += dirToLightWS * lightStepLength;                        //向灯光步进
                        lightDensity += max(0, SampleNoise3D(lightSamplePos) * lightStepLength); //步进的时候采样噪音累计受灯光影响密度
                    }
                    //灯光透过率，
                    float transmittance = BeerPowder(lightDensity * _LightAbsorptionTowardSun);
                    float darkness = lerp(transmittance, 1, _DarknessThreshold);

                    //phase
                    float3 cosAngle = dot(dirToLightWS, cameraRayDirWS);
                    float phase = HG(cosAngle, _HG);

                    //自定义颜色
                    //将重亮到暗映射为 3段颜色 ,亮->灯光颜色 中->ColorA 暗->ColorB
                    float3 cloudColor = lerp(_CloudMidColor, lightColor * _LightEnergyScale, saturate(darkness * _ColorOffset1));
                    cloudColor = lerp(_CloudDarkColor, cloudColor, saturate(pow(darkness * _ColorOffset2, 3)));
                    cloudColor *= phase;

                    // cloudColor = darkness * phase * lightColor * _LightEnergyScale;

                    //返回的灯光能量=当前小块的密度*步长*灯光颜色*灯光到相机的衰减透过率
                    lightEnergy += sampleCamDensity * _RayStepLength * cloudColor * camAttenuation;
                    //灯光强度*灯光衰减*步长*相机衰减
                    // lightEnergy += lightColor * darkness *sampleCamDensity*  _RayStepLength * camAttenuation ;

                    //bears law: I=I0*e^(-k*ds),multiply for e equal to add for k
                    //当前点到摄像机的衰减
                }
                camAttenuation *= exp(-sampleCamDensity * _RayStepLength);
                dstTravelled += _RayStepLength; //每次步进长度
            }
        }

        // //获得深度数据
        // half4 blitDepth = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.uv); //采样blitter纹理
        // //重建世界坐标深度
        // float depthValue = Linear01Depth(blitDepth, _ZBufferParams);

        half4 blitDepth = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.uv); //采样blitter纹理
        //重建世界坐标深度
        float depthValue = Linear01Depth(blitDepth, _ZBufferParams);

        float4 color;
        color.xyz = lightEnergy;
        // color.xyz = camAttenuation;

        color.w = 1 - camAttenuation;

        return color;
    }

    half4 fragBlit(Varyings1 i):SV_Target
    {
        half4 blitTex = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.uv); //采样blitter纹理
        half4 color = blitTex;
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
            Name "cloud"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
        //        Pass
        //        {
        //            Blend SrcAlpha OneMinusSrcAlpha
        //            ZWrite Off
        //            Cull Off
        //            HLSLPROGRAM
        //            #pragma vertex vert
        //            #pragma fragment fragBlit
        //            ENDHLSL
        //        }
    }
}