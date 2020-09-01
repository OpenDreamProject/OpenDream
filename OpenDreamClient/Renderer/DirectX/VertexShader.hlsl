cbuffer VertexShaderConstants0 : register(b0) {
    float2 viewportSize;
}

cbuffer VertexShaderConstants1 : register(b1) {
    float2 vertexOffset;
}

struct VertexShaderInput {
    float2 pos : POSITION;
    float2 uv : TEXCOORD0;
};

struct PixelShaderInput {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

PixelShaderInput VertexMain(VertexShaderInput input) {
    PixelShaderInput vertexShaderOutput;

    vertexShaderOutput.pos = float4((input.pos + vertexOffset) / viewportSize * 2 , 0.0f, 1.0f);
    vertexShaderOutput.uv = input.uv;
    return vertexShaderOutput;
}