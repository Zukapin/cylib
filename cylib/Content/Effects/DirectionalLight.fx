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

cbuffer cInvProj : register(b2)
{
	float4x4 invViewProj : packoffset(c0);
};

cbuffer PointLight : register(b3)
{
	float4 LightPosRadius : packoffset(c0);
	float4 LightColorIntensity : packoffset(c1);
};

Texture2D color : register(t0);
Texture2D depth : register(t1);
Texture2D normal : register(t2);

SamplerState mySampler : register(s0);

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(mul(projMatrix, viewMatrix), mul(World, input.pos));
	output.tex = input.tex;

	return output;
}

float3 decode(half2 enc)
{
	float2 fenc = enc * 4 - 2;
	float f = dot(fenc, fenc);
	float3 n;
	n.xy = fenc*sqrt(1 - f / 4);
	n.z = 1 - f / 2;
	return n;
}

half2 encode(float3 n)
{
	return n.xy * rsqrt(8 * n.z + 8) + 0.5;
}

PS_OUT PS(PS_IN input)
{
	PS_OUT output = (PS_OUT)1;

	float3 pointNormal = normalize(decode(normal.Sample(mySampler, input.tex).rg));

	float3 dirToLight = LightPosRadius.xyz;
	float intensity = saturate(dot(pointNormal, normalize(dirToLight)));
	output.color.rgb = LightColorIntensity.xyz * intensity * LightColorIntensity.a;
	output.color.a = 1;

	//output.color.rgb = (decode(encode(float3(0, 1, 0))) + 1.0) / 2.0;
	//output.color.rgb = (decode(encode(float3(0, 1, 0))) + 0.0);
	//output.color.rgb = intensity;
	//output.color.rgb = worldPos;
	//output.color.rgb = pointNormal;
	//output.color.rgb = dot(dirToLight, float3(0, 1, 0));
	//output.color.rgb = invViewProj._m00_m01_m02 + 0.5;
	//output.color.rgb = WorldViewProj._m00_m01_m02 + 0.5;
	//output.color.rgb = WorldViewProj2._m00_m01_m02 + 0.5;
	//output.color.rgb = LightPosRadius.rgb;
	//output.color.rgb = LightColorIntensity.rgb;

	return output;
}
