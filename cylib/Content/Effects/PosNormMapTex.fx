struct VS_IN
{
	float4 pos : POSITION;
	float3 normal : NORMAL;
	float3 binormal : BINORMAL;
	float3 tangent : TANGENT;
	float2 tex : TEXTURE;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float3x3 TnW : NORMAL;
	float2 tex : TEXTURE;
};

struct PS_OUT
{
	float4 color : SV_TARGET0;
	half2 normal : SV_TARGET1;
};

float4x4 WorldViewProj : register(b0);
Texture2D color : register(t0);
Texture2D norm : register(t1);
SamplerState colorSamp : register(s0);


PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = mul(WorldViewProj, input.pos);
	output.TnW = float3x3(input.tangent, input.binormal, input.normal);
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

	output.color = color.Sample(colorSamp, input.tex);
	//output.normal = encode(float3(0, 1, 0));
	//output.normal = encode(input.TnW._m20_m21_m22);
	//output.normal = encode(normalize(norm.Sample(colorSamp, input.tex).rgb));
	output.normal = encode(normalize(mul(norm.Sample(colorSamp, input.tex).rgb * 2 - 1, input.TnW)));

	return output;
}