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

Texture2D normal : register(t2);
SamplerState mySampler : register(s0);

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(mul(projMatrix, viewMatrix), mul(World, input.pos));
	output.tex = input.tex;
	
	return output;
}

float3 decode (float2 enc)
{
    float2 fenc = enc*4-2;
    float f = dot(fenc,fenc);
    float3 n;
    n.xy = fenc*sqrt(1-f/4);
    n.z = 1-f/2;
    return n;
}

PS_OUT PS( PS_IN input )
{
	PS_OUT output = (PS_OUT)0;

	output.color.rgb = ((decode(normal.Sample(mySampler, input.tex).rg)) + 1.0) / 2.0;
	output.color.a = 1;

	return output;
}
