struct VS_IN
{
	float2 pos;
	float2 radiusBorder;
	float4 centerColor;
	float4 borderColor;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 localPos : TEXTURE;
	float2 radiusBorder : TEXTURE1;
	float4 centerColor : TEXTURE2;
	float4 borderColor : TEXTURE3;
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
	float radius = vertData[index].radiusBorder.x;

	//four verts are center + u + t, center + u - t, center - u + t, center - u - t
	//offset is being set here to [-1.1, 1.1]
	//for x, vertex 0 & 1 = 1 -- vertex 2 & 3 = -1
	//for y, vertex 0 & 2 = 1 -- vertex 1 & 3 = -1
	//scaling above [-1, 1] to add a little wiggleroom for anti-aliasing 
	float2 offset = 1.1f - 1.1f * float2(((localID << 1) & 2), ((localID) & 2));

	output.pos = mul(mul(projMatrix, viewMatrix), float4(center + (offset * radius), 0, 1));

	output.localPos = offset;
	output.radiusBorder = vertData[index].radiusBorder;
	output.centerColor = vertData[index].centerColor;
	output.borderColor = vertData[index].borderColor;

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

	//length(input.localPos) is [0, sqrt(2)]
	//we want >0 to be inside the circle, <0 to be outside
	//so we transform that to [1 - sqrt(2), 1]
	float dis = 1 - length(input.localPos);

	//now we calculate the border to color -- we use a ratio of radius to border size to translate to dis
	//<0 should be centerColor, >0 should be borderColor
	float edge = dis - input.radiusBorder.y / input.radiusBorder.x;
	//float edge = 0.5f - abs(dis * input.radiusBorder.x / input.radiusBorder.y - 0.5f);

	float radThresh = length(float2(ddx(dis), ddy(dis)));
	float edgeThresh = length(float2(ddx(edge), ddy(edge)));

	output.color = lerp(input.borderColor, input.centerColor, smoothstep(0, 1, edge / edgeThresh + 0.5f));
	output.color.a = output.color.a * smoothstep(0, 1, dis / radThresh + 0.5f);

	return output;
}