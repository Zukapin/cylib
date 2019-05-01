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
	float4 color : SV_TARGET0;
};

cbuffer cCam : register(b0)
{
	float4x4 viewMatrix : packoffset(c0);
	float4x4 projMatrix : packoffset(c4);
};

cbuffer cWorld : register(b1)
{
	float4x4 World : packoffset(c0);
	float2 scale : packoffset(c4.x);
	float2 radius : packoffset(c4.z);
	float4 fgColor : packoffset(c5);
	float4 borderColor : packoffset(c6);
};


PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(mul(projMatrix, viewMatrix), mul(World, input.pos));
	
	//the quad given is 20% larger in each dimension that requested, for smooth antialiased borders
	//we want the tex coords to be at (0, 0) for the center
	//we start with (0, 0) in the upper left, and (1, 1) in the bottom right
	//want want (0, 0) -> (-0.6, -0.6) and (1, 1) -> (0.6, 0.6)
	output.tex = (input.tex * 1.2f - 0.6f) * (scale.xy);

	return output;
}

PS_OUT PS(PS_IN input) : SV_Target
{
	PS_OUT output = (PS_OUT)0;

	//okay, so this works by:
	//input.tex is (0, 0) in the center of the rectangle
	//points inside the box are negative (input.tex) - scale.xy will be negative inside the box, positive outside
	//negatives are then set to 0, and then length is taken
	//the length returns the distance from the edge of the box. so by subtracting radius, everything within the radius distance is negative
	//so along any edge, it goes from 0 to -radius, then the entire inside of the box is -radius
	//that makes it hard to do anything nice with antialiasing/borders, because the only area we have to work with is inside the radius zone
	float dis = -length(max(abs(input.tex) - scale.xy / 2 + radius.x, 0.0)) / radius.x + 1;

	//dis is negative outside of the box, positive inside the box, scaling up to 1 inside the radius
	//set the border color, anything from 0 to borderWidth / radius
	float bord = radius.y / radius.x;

	//this is for antialiasing, some kind of dark magic, pretty sure
	float colorThresh = 1.0f * length(float2(ddx(dis), ddy(dis)));

	output.color = lerp(borderColor, fgColor, smoothstep(bord - colorThresh, bord + colorThresh, dis));
	output.color.a = output.color.a * smoothstep(-colorThresh, colorThresh, dis);
	return output;
}