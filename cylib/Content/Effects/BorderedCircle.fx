struct VS_IN
{
	float4 posRadius;
	float4 centerColor;
	float4 borderColor;
	float borderWidth;
	float3 padding;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 localPos : TEXTURE; //xy position inside for the pixel
	float4 centerColor : TEXTURE1;
	float4 borderColor : TEXTURE2;
	float2 borderInfo : TEXTURE3; //radius, borderWidth
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

cbuffer cWorld : register(b1)
{
	float3 camForward : packoffset(c0); //should be normalized
};

StructuredBuffer<VS_IN> vertData : register(t0);


PS_IN VS(uint vID : SV_VertexID)
{
	PS_IN output = (PS_IN)0;

	uint index = vID >> 2u;
	uint localID = vID & 3u;

	float3 center = vertData[index].posRadius.xyz;
	float radius = vertData[index].posRadius.w;

	//need *any* direction orthogonal to camForward
	float3 t = cross(float3(1, 0, 0), camForward);
	float tLen = length(t);
	if (tLen < 0.001)
	{
		t = cross(float3(0, 1, 0), camForward);
		tLen = length(t);
	}

	t = t / tLen;

	float3 u = cross(t, camForward) * radius;
	t = t * radius;

	//four verts are center + u + t, center + u - t, center - u + t, center - u - t
	float2 offset = 1 - float2(
		((localID) & 2),		//this should be 0 on [0, 1] and 2 on [2, 3]
		((localID << 1) & 2)	//this should be 0 on [0, 2] and 2 on [1, 3]
		);

	offset = offset * 1.1; //offset is [-1.1, 1.1]

	output.pos = mul(mul(projMatrix, viewMatrix), float4(center + u * offset.x + t * offset.y, 1));

	output.localPos = offset;
	output.centerColor = vertData[index].centerColor;
	output.borderColor = vertData[index].borderColor;
	output.borderInfo = float2(radius, vertData[index].borderWidth);

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

//length(input.localPos) is [0, sqrt(2)]
//we want >0 to be inside the circle, <0 to be outside
//so we transform that to [1 - sqrt(2), 1]
float dis = 1 - length(input.localPos);

//we want an outline around the edge
//the outer edge of the circle is 0, the inner outline edge is 1
//then we go to outer edge is -0.5f, innter is 0.5
//abs makes the center 0, both edges 0.5
//then we do 0.5 - val to get positive in the center, negative in the outer
float edge = 0.5f - abs(dis * input.borderInfo.x / input.borderInfo.y - 0.5f);

float radThresh = length(float2(ddx(dis), ddy(dis)));
float edgeThresh = length(float2(ddx(edge), ddy(edge)));

output.color = lerp(input.centerColor, input.borderColor, smoothstep(-edgeThresh, edgeThresh, edge));

output.color.a = output.color.a * smoothstep(-radThresh, radThresh, dis);
return output;
}