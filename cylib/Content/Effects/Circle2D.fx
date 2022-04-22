struct VS_IN
{
	float3 pos; //z component is kinda worthless, but it makes the struct better memory-aligned
	float radius;
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

	float2 center = vertData[index].pos.xy;
	float radius = vertData[index].radius;

	//four verts are center + u + t, center + u - t, center - u + t, center - u - t
	//offset is being set here to [-1.1, 1.1]
	//for x, vertex 0 & 1 = 1 -- vertex 2 & 3 = -1
	//for y, vertex 0 & 2 = 1 -- vertex 1 & 3 = -1
	//scaling above [-1, 1] to add a little wiggleroom for anti-aliasing 
	float2 offset = 1.1f - 1.1f * float2(((localID << 1) & 2), ((localID) & 2));

	output.pos = mul(mul(projMatrix, viewMatrix), float4(center + (offset * radius), vertData[index].pos.z, 1));

	output.localPos = offset;
	output.color = vertData[index].color;

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

	//length(input.localPos) is [0, sqrt(2)]
	//we want >0 to be inside the circle, <0 to be outside
	//so we transform that to [1 - sqrt(2), 1]
	float dis = 1 - length(input.localPos);

	float radThresh = length(float2(ddx(dis), ddy(dis)));

	output.color = input.color;
	output.color.a = output.color.a * smoothstep(-radThresh, radThresh, dis);

	return output;
}