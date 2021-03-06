float4x4 World;
float4x4 View;
float4x4 Projection;

float3 DiffuseColor = float3(1,1,1);
float3 AmbientColor = float3(0.1, 0.1, 0.1);
float3 LightDirection = float3(1,1,1);
float3 LightColor = float3(0.9, 0.9, 0.9);

texture BasicTexture;

sampler BasicTextureSampler = sampler_state {
	texture = <BasicTexture>;
};

bool TextureEnabled = false;


struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
	float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
	float3 Normal : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);

	output.UV = input.UV;
	output.Normal = mul(input.Normal, World); 

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float3 color = DiffuseColor;

	if (TextureEnabled)
		color *= tex2D(BasicTextureSampler, input.UV);

	float3 lighting = AmbientColor;
	float3 lightDir = normalize(LightDirection);
	float3 normal = normalize(input.Normal);

	lighting += saturate(dot(lightDir, normal)) * LightColor;

	float3 output = saturate(lighting) * color;
	
	return float4(output, 1);
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}
