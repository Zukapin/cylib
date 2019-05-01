struct VS_IN
{
	float4 pos : POSITION;
	float2 tex : TEXTURE;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 tex : TEXTURE;
};

struct PS_OUT
{
	float4 color : SV_TARGET;
};

cbuffer cCam : register(b0)
{
	float4x4 viewMatrix : packoffset(c0);
	float4x4 projMatrix : packoffset(c4);
};

cbuffer cWorld : register(b1)
{
	float4x4 World : packoffset(c0);
};

Texture2D color : register(t0);
SamplerState mySampler : register(s0);

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(mul(projMatrix, viewMatrix), mul(World, input.pos));
	output.tex = input.tex;
	
	return output;
}

PS_OUT PS( PS_IN input )
{
	PS_OUT output = (PS_OUT)1;
	output.color = color.Sample(mySampler, input.tex);
	output.color.a = 1;

	return output;
}
