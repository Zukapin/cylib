struct VS_IN
{
	float2 pos;
	float2 widthHeight;
	float4 color;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 localPos : TEXTURE;
	float4 color : TEXTURE1;
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

StructuredBuffer<VS_IN> vertData : register(t0);


PS_IN VS(uint vID : SV_VertexID)
{
	PS_IN output = (PS_IN)0;

	uint index = vID >> 2u; //which circle is being drawn
	uint localID = vID & 3u; //last 2 bits -- [0, 3], corresponding to the vertex being drawn

	float2 pos = vertData[index].pos.xy;
	float2 widthHeight = vertData[index].widthHeight;

	//four verts are center + u + t, center + u - t, center - u + t, center - u - t
	//offset is being set here to [-1.1, 1.1]
	//for x, vertex 0 & 1 = 1 -- vertex 2 & 3 = -1
	//for y, vertex 0 & 2 = 1 -- vertex 1 & 3 = -1
	//scaling above [-1, 1] to add a little wiggleroom for anti-aliasing 
	float2 offset = 0.6f * float2(((localID << 1) & 2), ((localID) & 2)) - 0.1f;

	output.pos = mul(mul(projMatrix, viewMatrix), float4(pos + (offset * widthHeight), 0, 1));

	output.localPos = offset;
	output.color = vertData[index].color;

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

	//we want >0 inside, <0 to be outside
	//localpos is [-0.1, 1.1]
	//want [0, 1] to be inside
	float2 dis = 0.5f - abs(input.localPos - 0.5f);
	float2 thresh = abs(float2(ddx(dis.x), ddy(dis.y)));
	dis = dis / thresh;

	output.color = input.color;
	//output.color.a = output.color.a * saturate(min(dis.x, dis.y) + 0.5f);
	output.color.a = output.color.a * smoothstep(0, 1, min(dis.x, dis.y) + 0.5f);

	return output;
}