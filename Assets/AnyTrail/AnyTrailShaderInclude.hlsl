#ifndef _ANYTRAIL
#define _ANYTRAIL

TEXTURE2D(_AnyTrailFluid);
SAMPLER(sampler_AnyTrailFluid);
TEXTURE2D(_AnyTrailWave);
SAMPLER(sampler_AnyTrailWave);
TEXTURE2D(_AnyTrailTrail);
SAMPLER(sampler_AnyTrailTrail);
TEXTURE2D(_AnyTrailPattern);
SAMPLER(sampler_AnyTrailPattern);



float3 _AnyTrailPosCenter;
float _AnyTrailCanvasSize;

float remap(float value, float from1, float to1, float from2, float to2)
{
    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
}

real4 SampleAnyTrail(float3 posWS,Texture2D _AnyTrailTex, SamplerState sampler_AnyTrailTex)
{
    float2 LB = float2(_AnyTrailPosCenter.x - _AnyTrailCanvasSize, _AnyTrailPosCenter.y - _AnyTrailCanvasSize);
    float2 RT = float2(_AnyTrailPosCenter.x + _AnyTrailCanvasSize, _AnyTrailPosCenter.y + _AnyTrailCanvasSize);

    if (posWS.x > LB.x & posWS.x < RT.x & posWS.z < RT.y & posWS.z > LB.y)
    {
        float2 uvTrail = float2((posWS.x - LB.x) / (_AnyTrailCanvasSize * 2), (posWS.z - LB.y) / (_AnyTrailCanvasSize * 2));
        return SAMPLE_TEXTURE2D_LOD(_AnyTrailTex, sampler_AnyTrailTex, uvTrail,0);
    }
    
    return real4(0, 0, 0, 0);
}

real4 SampleAnyTrailFluid(float3 posWS)
{
    return SampleAnyTrail(posWS, _AnyTrailFluid, sampler_AnyTrailFluid);
}

real sampleAnyTrailWave(float3 posWS)
{
    return SampleAnyTrail(posWS, _AnyTrailWave, sampler_AnyTrailWave).x;
}

real sampleAnyTrailTrail(float3 posWS)
{
    return SampleAnyTrail(posWS, _AnyTrailTrail, sampler_AnyTrailTrail).x;
}


real sampleAnyTrailPattern(float3 posWS)
{
    return SampleAnyTrail(posWS, _AnyTrailPattern, sampler_AnyTrailPattern).x;
}
#endif