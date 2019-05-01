struct VS_IN
{
	float4 pos : POSITION;
	float3 normal : NORMAL;
	float2 tex : TEXTURE;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3 normal : NORMAL;
	float2 tex : TEXTURE;
};

struct PS_OUT
{
	float4 color : SV_TARGET0;
	half2 normal : SV_TARGET1;
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

Texture2D myTexture : register(t0);

SamplerState mySampler : register(s0);


PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(mul(projMatrix, viewMatrix), mul(World, input.pos));
	output.normal = mul((float3x3)mul(viewMatrix, World), input.normal);
	output.tex = input.tex;
	
	return output;
}

half2 encode (float3 n)
{ 
	return n.xy * rsqrt(8*n.z+8) + 0.5;
}

PS_OUT PS( PS_IN input ) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

	//output.color.rb = input.tex;
	output.color = myTexture.Sample(mySampler, input.tex);
	output.normal = encode(normalize(input.normal));

	return output;
}