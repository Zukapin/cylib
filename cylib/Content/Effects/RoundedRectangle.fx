struct VS_IN
{
	float4 posRadius;
	float3 scaleThickness;
	float padding;
	float4 mainColor;
	float4 borderColor;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 localPosScale : TEXTURE; //xy position inside for the pixel
	float4 mainColor : TEXTURE1;
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

StructuredBuffer<VS_IN> vertData : register(t0);


PS_IN VS(uint vID : SV_VertexID)
{
	PS_IN output = (PS_IN)0;

	uint index = vID >> 2u;
	uint localID = vID & 3u;

	VS_IN input = vertData[index];

	float3 startPos = input.posRadius.xyz;
	float2 scale = input.scaleThickness.xy;

	float3 t = float3(scale.x, 0, 0);
	float3 u = float3(0, scale.y, 0);

	//four verts are pos, pos + t, pos + u, pos + u + t
	float2 offset = float2(
		((localID >> 1) & 1),
		((localID) & 1)
		);

	offset = offset * 1.2 - 0.1; //offset is [-0.1, 1.1]

	output.pos = mul(mul(projMatrix, viewMatrix), float4(startPos + u * offset.x + t * offset.y, 1));

	output.localPosScale = float4((offset - 0.5) * scale, scale * 0.5);
	output.mainColor = input.mainColor;
	output.borderColor = input.borderColor;
	output.borderInfo = float2(input.posRadius.w, input.scaleThickness.z / input.posRadius.w);

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

//okay, so this works by:
//localPos is (0, 0) in the center of the rectangle, +/- width/2, +/- height/2 at corners
//points inside the box are negative localPos - scale.xy / 2 will be negative inside the box, positive outside
//negatives are then set to 0, and then length is taken
//the length returns the distance from the edge of the box. so by subtracting radius, everything within the radius distance is negative
//so along any edge, it goes from 0 to -radius, then the entire inside of the box is -radius
//that makes it hard to do anything nice with antialiasing/borders, because the only area we have to work with is inside the radius zone
float dis = -length(max(abs(input.localPosScale.xy) - input.localPosScale.zw + input.borderInfo.x, 0.0)) / input.borderInfo.x + 1;

//dis is negative outside of the box, positive inside the box, scaling up to 1 inside the radius
//set the border color, anything from 0 to borderWidth / radius
float bord = input.borderInfo.y;

//this is for antialiasing, some kind of dark magic, pretty sure
float colorThresh = 1.0f * length(float2(ddx(dis), ddy(dis)));

output.color = lerp(input.borderColor, input.mainColor, smoothstep(bord - colorThresh, bord + colorThresh, dis));
output.color.a = output.color.a * smoothstep(-colorThresh, colorThresh, dis);
return output;
}