#ifndef _MYPBR
#define _MYPBR
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#define MYPI 3.14159265359f


inline half Pow5(half x)
{
    return x * x * x * x * x;
}

//disneyDiffusion
real BTDFDisney(float3 normal, float3 viewDirWS, float3 lightDirWS, half perceptualRoughness)
{
    float3 N = normal;
    float3 V = viewDirWS;
    float3 L = lightDirWS;
    float3 H = normalize(V + L);
    float NdotV = max(saturate(dot(N, V)), 0.000001);
    float NdotL = max(saturate(dot(N, L)), 0.000001);
    float LdotV = max(saturate(dot(H, V)), 0.000001);

    real fd90 = 0.5 + (perceptualRoughness + perceptualRoughness * LdotV);
    // Two schlick fresnel term
    half lightScatter = (1 + (fd90 - 1) * Pow5(1 - NdotL));
    half viewScatter = (1 + (fd90 - 1) * Pow5(1 - NdotV));
    return lightScatter * viewScatter*INV_PI;
}

//Distribution of normal : D,GGX
real BRDF_D(float NdotH, float roughness)
{
    float lerpSquareRoughness = pow(lerp(0.002, 1, roughness), 2);
    float D = lerpSquareRoughness / (pow((pow(NdotH, 2) * (lerpSquareRoughness - 1) + 1), 2) * MYPI);
    return D;
}

//Self-shadowing term : G
real BRDF_G(float NdotL, float NdotV, float roughness)
{
    float kInDirectLight = pow(roughness + 1, 2) / 8;
    float kInIBL = pow(roughness, 2) / 8;
    float GLeft = NdotL / lerp(NdotL, 1, kInDirectLight);
    float GRight = NdotV / lerp(NdotV, 1, kInDirectLight);
    float G = GLeft * GRight;
    return G;
}

//Fresnel : F
real3 BRDF_F(float3 albedo, float metallic, float vh)
{
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
    //菲涅尔项计算
    float3 F = F0 + (1 - F0) * exp2((-5.55473 * vh - 6.98316) * vh);
    return F;
}

real3 CalculateBRDF(float3 normalWS, float3 ligDirWS, float3 viewDirWS, float roughness, float3 albedo, float metallic)
{
    float3 N = normalWS;
    float3 V = viewDirWS;
    float3 L = ligDirWS;
    float3 H = normalize(V + L);
    float NdotV = max(saturate(dot(N, V)), 0.000001);
    float NdotL = max(saturate(dot(N, L)), 0.000001);
    float HdotV = max(saturate(dot(H, V)), 0.000001);
    float NdotH = max(saturate(dot(N, H)), 0.000001);
    float VdotH = ((dot(V, H)));
    float D = BRDF_D(NdotH, roughness);
    float G = BRDF_G(NdotL, NdotV, roughness);
    float3 F = BRDF_F(albedo, metallic, VdotH);
    float3 SpecularResult = (D * G * F * 0.25) / (HdotV * NdotL);
    return SpecularResult;
}
real3 CalculatePBR_Direct(float3 normalWS, float3 lightDirWS, float3 viewDirWS, float roughness, float3 albedo, float metallic, float3 lightCol)
{
    float3 N = normalWS;
    float3 V = viewDirWS;
    float3 L = lightDirWS;
    float3 H = normalize(V + L);
    float NdotL = max(saturate(dot(N, L)), 0.000001);
    float VdotH = max(saturate(dot(V, H)), 0.000001);
    float3 brdf = CalculateBRDF(normalWS, lightDirWS, viewDirWS, roughness, albedo, metallic);
    float3 specColor = saturate(MYPI * lightCol * brdf * NdotL);

    float3 F = BRDF_F(albedo, metallic, VdotH);
    float kd = (1 - length(F)) * (1 - metallic);
    float3 diffColor = kd * lightCol * NdotL * albedo;
    //直接光部分之和
    float3 DirectLightResult = diffColor + specColor;
    return DirectLightResult;
}

//立方体贴图的Mip等级计算
float CubeMapMip(float _Roughness)
{
    //基于粗糙度计算CubeMap的Mip等级
    float mip_roughness = _Roughness * (1.7 - 0.7 * _Roughness);
    half mip = mip_roughness * UNITY_SPECCUBE_LOD_STEPS;
    return mip;
}

float3 CalculatePBR_Indirect(float3 normalWS, float3 ligDirWS, float3 viewDirWS, float roughness, float3 albedo, float metallic)
{
    float3 N = normalWS;
    float3 V = viewDirWS;
    float3 L = ligDirWS;
    float3 H = normalize(V + L);
    float VdotH = max(saturate(dot(H, V)), 0.000001);
    float NdotV = max(saturate(dot(N, V)), 0.000001);

    ///间接光    
    half mip = CubeMapMip(roughness);                  //计算Mip等级，用于采样CubeMap
    float3 reflectVec = reflect(-viewDirWS, normalWS); //计算反射向量，用于采样CubeMap
    half4 iblSpecular = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVec, mip);

    half surfaceReduction = 1.0 / (roughness * roughness + 1.0); //压暗非金属的反射
    float oneMinusReflectivity = (1.0 - 0.04) * (1 - metallic);
    half grazingTerm = saturate((1 - roughness) + (1 - oneMinusReflectivity));
    half t = Pow5(1 - NdotV);
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
    float3 FresnelLerp = lerp(F0, grazingTerm, t); //控制反射的菲涅尔和金属色
    float3 iblSpecularResult = surfaceReduction * iblSpecular.xyz * FresnelLerp;
    float3 iblDiffuse = SampleSHVertex(float4(normalWS, 1).xyz).xyz; //获取球谐光照
    float3 Flast = BRDF_F(albedo, metallic, VdotH);
    float kdLast = (1 - length(Flast)) * (1 - metallic); //压暗边缘，边缘处应当有更多的镜面反射
    float3 iblDiffuseResult = iblDiffuse * kdLast * albedo;
    float3 IBLResult = iblSpecularResult + iblDiffuseResult;
    return IBLResult;
}

//计算光照
float3 CalculatePBR_all(float3 normalWS, float3 ligDirWS, float3 viewDirWS, float roughness, float3 albedo, float metallic, float3 lightCol)
{
    float3 dirCol = CalculatePBR_Direct(normalWS, ligDirWS, viewDirWS, roughness, albedo, metallic, lightCol);
    float3 indirCol = CalculatePBR_Indirect(normalWS, ligDirWS, viewDirWS, roughness, albedo, metallic);
    float3 result = dirCol + indirCol;
    return result;
}
#endif
