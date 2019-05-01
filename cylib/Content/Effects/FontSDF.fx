struct VS_IN
{
	float3 pos;
	float2 tex;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 tex : TEXTURE;
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

cbuffer cColor : register(b1)
{
	float4 fontColor : packoffset(c0);
};

StructuredBuffer<VS_IN> vertData : register(t0);

Texture2D myTexture : register(t0);

SamplerState mySampler : register(s0);


PS_IN VS(uint vID : SV_VertexID)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(mul(projMatrix, viewMatrix), float4(vertData[vID].pos, 1));
	output.tex = vertData[vID].tex;

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

	float w = 36.0f * (ddx(input.tex) + ddy(input.tex));
	float d = myTexture.Sample(mySampler, input.tex).a;
	output.color.a = smoothstep(0.5f - w, 0.5f + w, d) * fontColor.a;
	output.color.rgb = fontColor.rgb;

	return output;
}