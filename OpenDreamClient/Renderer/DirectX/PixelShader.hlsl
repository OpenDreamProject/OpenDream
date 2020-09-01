Texture2D Texture;
SamplerState Sampler;

struct PixelShaderInput {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

float4 PixelMain(PixelShaderInput input) : SV_TARGET {
    return Texture.Sample(Sampler, input.uv);
}