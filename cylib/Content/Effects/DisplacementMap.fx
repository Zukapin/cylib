struct VS_IN
{
	float3 pos : POSITION;
	float3 normal : NORMAL;
	float3 binormal : BINORMAL;
	float3 tangent : TANGENT;
	float2 tex : TEXTURE;
};

struct HS_IN
{
	float3 pos : POSITION;
	float3 normal : NORMAL;
	float3 binormal : BINORMAL;
	float3 tangent : TANGENT;
	float2 tex : TEXTURE;
};

struct PATCH_OUT
{
	float edges[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};

struct HS_OUT
{
	float3 pos : POSITION;
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

cbuffer Cam : register(b0)
{
	float4x4 WorldViewProj : packoffset(c0);
};

Texture2D color : register(t0);
Texture2D norm : register(t1);
Texture2D height : register(t2);
SamplerState samp : register(s0);


HS_IN VS( VS_IN input )
{
	HS_IN output = (HS_IN)0;
	
	output.pos = input.pos;//mul(WorldViewProj, input.pos);
	output.normal = input.normal;
	output.binormal = input.binormal;
	output.tangent = input.tangent;
	//output.TnW = float3x3(input.tangent, input.binormal, input.normal);
	output.tex = input.tex;
	
	return output;
}

PATCH_OUT PatchConstant(InputPatch<HS_IN, 3> inputPatch, uint patchId : SV_PrimitiveID)
{
	PATCH_OUT output;

	output.edges[0] = 64;
	output.edges[1] = 64;
	output.edges[2] = 64;

	output.inside = 64;

	return output;
}

[domain("tri")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[outputcontrolpoints(3)]
[patchconstantfunc("PatchConstant")]
HS_OUT HS(InputPatch<HS_IN, 3> patch, uint pointId : SV_OutputControlPointID, uint patchId : SV_PrimitiveID)
{
	HS_OUT output;

	output.pos = patch[pointId].pos;
	output.normal = patch[pointId].normal;
	output.binormal = patch[pointId].binormal;
	output.tangent = patch[pointId].tangent;
	//output.TnW = patch[pointId].TnW;
	output.tex = patch[pointId].tex;

	return output;
}

[domain("tri")]
PS_IN DS(PATCH_OUT input, float3 uvwCoord : SV_DomainLocation, const OutputPatch<HS_OUT, 3> patch)
{
	PS_IN output;

	float2 tex = uvwCoord.x * patch[0].tex + uvwCoord.y * patch[1].tex + uvwCoord.z * patch[2].tex;
	float3 normal = uvwCoord.x * patch[0].normal + uvwCoord.y * patch[1].normal + uvwCoord.z * patch[2].normal;
	float3 binormal = uvwCoord.x * patch[0].binormal + uvwCoord.y * patch[1].binormal + uvwCoord.z * patch[2].binormal;
	float3 tangent = uvwCoord.x * patch[0].tangent + uvwCoord.y * patch[1].tangent + uvwCoord.z * patch[2].tangent;
	float3 vertexPos = uvwCoord.x * patch[0].pos + uvwCoord.y * patch[1].pos + uvwCoord.z * patch[2].pos;

	vertexPos = vertexPos + normal * height.GatherRed(samp, tex, 0) * 0.1;

	output.pos = mul(WorldViewProj, float4(vertexPos, 1));
	//output.TnW = patch[0].TnW;
	
	output.tex = tex;
	output.TnW = float3x3(tangent, binormal, normal);

	return output;
}

half2 encode (float3 n)
{ 
	return n.xy * rsqrt(8*n.z+8) + 0.5;
}

PS_OUT PS( PS_IN input )
{
	PS_OUT output = (PS_OUT)0;

	output.color = color.Sample(samp, input.tex);
	//output.normal = encode(float3(0, 1, 0));
	//output.normal = encode(input.TnW._m20_m21_m22);
	//output.normal = encode(normalize(norm.Sample(samp, input.tex).rgb * 2 - 1));
	output.normal = encode(normalize(mul(norm.Sample(samp, input.tex).rgb * 2 - 1, input.TnW)));

	return output;
}