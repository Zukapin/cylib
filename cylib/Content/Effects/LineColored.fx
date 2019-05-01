struct VS_IN
{
    float3 startPos;
    float padding;//to make the struct float4-aligned
    float3 endPos;
    float padding2;
	float4 startColor;
	float4 endColor;
};

struct PS_IN
{
    float4 pos : SV_POSITION;
	float2 tex : TEXTURE;
	float2 tex2 : TEXTURE1;
	float4 startColor : TEXTURE2;
	float4 endColor : TEXTURE3;
	float colorLerp : TEXTURE4;
};

struct PS_OUT
{
    float4 color : SV_TARGET0;
};

cbuffer cCam : register(b0)
{
	float4x4 viewMatrix : packoffset(c0);
	float4x4 projMatrix : packoffset(c4);
};

cbuffer cWorld : register(b1)
{
    float3 camForward : packoffset(c0);
    float lineWidth : packoffset(c0.w);
};

StructuredBuffer<VS_IN> vertData : register(t0);


PS_IN VS(uint vID : SV_VertexID)
{
    PS_IN output = (PS_IN)0;

    uint index = vID >> 2u;
    uint localID = vID & 3u;

	VS_IN dat = vertData[index];
    float3 startPos = dat.startPos;
    float3 endPos = dat.endPos;
	float4 startColor = dat.startColor;
	float4 endColor = dat.endColor;

    float3 d = endPos - startPos;
	float dLen = length(d);

	d = d / dLen;

	//adjust the startPos by lineWidth / 2, and make the actual line-length d + lineWidth, so a '0-length' line is still a circle
	//this makes consecutive lines that start where the last ended have smooth corners
	startPos = startPos - d * lineWidth / 2;
	dLen = dLen + lineWidth;

    //if the line is pointing straight at the camera, this is gonna be nonsensical
    float3 t = normalize(cross(d, camForward)) * lineWidth * 0.6f; //1.2 / 2.0 

    //four verts are startPos + t - lineWidth * dNorm, startPos - t - lineWidth * dNorm, endPos + t + lineWidth * dNorm, endPos - t + lineWidth * dNorm
    float2 offset = float2(
        1.1 - ((localID >> 1) & 1) * 1.2, //this should be -0.1 on ID 2 and 3, 1.1 on ID 0 and 1
        1.0 - ((localID << 1) & 2)); //this should be -1 on 0 and 2, 1 on 1 and 3

    //we set our pos to either start or end
    //then offset that by t in either the positive or negative direction
    //the coords here are 1.2* what they should be, because we want a little extra wiggle room for antialiasing
    float3 pos = (startPos + (d * dLen * offset.x)) + (-t * offset.y);
    output.pos = mul(mul(projMatrix, viewMatrix), float4(pos, 1));

    //this is scale.xy from bounded box
    output.tex2 = float2(dLen, lineWidth);
    output.tex = float2((offset.x - 0.5f) * output.tex2.x, (offset.y * 0.6f) * lineWidth);
	output.startColor = startColor;
	output.endColor = endColor;
	output.colorLerp = offset.x;

    return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

	//this is taken from RoundedRectangle2D.fx, see there for notes
	float rad = input.tex2.y / 2.0f;
	float dis = -length(max(abs(input.tex) - input.tex2 / 2 + rad, 0.0)) / rad + 1;

	float colorThresh = 1.0f * length(float2(ddx(dis), ddy(dis)));

	output.color = lerp(input.startColor, input.endColor, saturate(input.colorLerp));
	output.color.a = output.color.a * smoothstep(-colorThresh, colorThresh, dis);
	return output;
}