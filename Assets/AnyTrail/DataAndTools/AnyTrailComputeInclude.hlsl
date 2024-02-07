////记录compute shader的一些工具
//双线性插值取到精确像素值，不然id传入小数，但只用整数采样，整体会不规则移动
float4 sampleBilinearInterpolation(float2 samplePos, RWTexture2D<float4> rt)
{
    //双线性插值取到精确像素值，不然id传入小数，但只用整数采样，整体会不规则移动
    int2 posLeftDown = int2(floor(samplePos));
    int2 posRightUp = int2(ceil(samplePos));
    int2 posLeftUp = int2(floor(samplePos.x), ceil(samplePos.y));
    int2 posRightDown = int2(ceil(samplePos.x), floor(samplePos.y));
    float4 colorLeftDown = rt[posLeftDown.xy];
    float4 colorRightUp = rt[posRightUp.xy];
    float4 colorLeftUp = rt[posLeftUp.xy];
    float4 colorRightDown = rt[posRightDown.xy];

    //插值
    float4 colorTop = lerp(colorLeftUp, colorRightUp, frac(samplePos.x));
    float4 colorDown = lerp(colorLeftDown, colorRightDown, frac(samplePos.x));
    float4 color = lerp(colorDown, colorTop, frac(samplePos.y));
    return color;
}

float4 sampleBilinearInterpolation(float2 samplePos, Texture2D<float4> rt)
{
    //双线性插值取到精确像素值，不然id传入小数，但只用整数采样，整体会不规则移动
    int2 posLeftDown = int2(floor(samplePos));
    int2 posRightUp = int2(ceil(samplePos));
    int2 posLeftUp = int2(floor(samplePos.x), ceil(samplePos.y));
    int2 posRightDown = int2(ceil(samplePos.x), floor(samplePos.y));
    float4 colorLeftDown = rt[posLeftDown.xy];
    float4 colorRightUp = rt[posRightUp.xy];
    float4 colorLeftUp = rt[posLeftUp.xy];
    float4 colorRightDown = rt[posRightDown.xy];

    //插值
    float4 colorTop = lerp(colorLeftUp, colorRightUp, frac(samplePos.x));
    float4 colorDown = lerp(colorLeftDown, colorRightDown, frac(samplePos.x));
    float4 color = lerp(colorDown, colorTop, frac(samplePos.y));
    return color;
}
float sampleBilinearInterpolation(float2 samplePos, Texture2D<float> rt)
{
    //双线性插值取到精确像素值，不然id传入小数，但只用整数采样，整体会不规则移动
    int2 posLeftDown = int2(floor(samplePos));
    int2 posRightUp = int2(ceil(samplePos));
    int2 posLeftUp = int2(floor(samplePos.x), ceil(samplePos.y));
    int2 posRightDown = int2(ceil(samplePos.x), floor(samplePos.y));
    float colorLeftDown = rt[posLeftDown.xy];
    float colorRightUp = rt[posRightUp.xy];
    float colorLeftUp = rt[posLeftUp.xy];
    float colorRightDown = rt[posRightDown.xy];

    //插值
    float colorTop = lerp(colorLeftUp, colorRightUp, frac(samplePos.x));
    float colorDown = lerp(colorLeftDown, colorRightDown, frac(samplePos.x));
    float color = lerp(colorDown, colorTop, frac(samplePos.y));
    return color;
}

//返回一个随机角度
float2 RandomAngle(int2 id)
{
    float seed = dot(id, float2(12.9898, 78.233));
    float rand1 = (sin(seed) * 43758.5453);
    float rand2 = (cos(seed) * 652.4523);
    return float2(rand1, rand2);
}