struct VS_IN
{
	float4 posRadius;
	float4 startColor;
	float4 endColor;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 localPos : TEXTURE; //xy position inside for the pixel
	float radius : TEXTURE1; //radius of the circle
	float4 startColor : TEXTURE2; //color in the center of the circle
	float4 endColor : TEXTURE3; //color at the end of the circle
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

	output.radius = radius;
	output.localPos = offset;
	output.startColor = vertData[index].startColor;
	output.endColor = vertData[index].endColor;

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

//length(input.localPos) is [0, sqrt(2)]
//we want >0 to be inside the circle, <0 to be outside
//so we transform that to [1, 1 - sqrt(2)]
float dis = 1 - length(input.localPos); 

float colorThresh = 1.0f * length(float2(ddx(dis), ddy(dis)));

output.color = lerp(input.endColor, input.startColor, saturate(dis));
output.color.a = output.color.a * smoothstep(-colorThresh, colorThresh, dis);
return output;
}